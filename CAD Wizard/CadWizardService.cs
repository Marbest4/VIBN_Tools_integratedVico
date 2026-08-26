using FS.SDK;
using FS.SDK.Mathematics;
using FS.SDK.Scene.Objects;
using System.Collections.Concurrent;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Interfaces;
using static VIBN_Tools.Settings.ProjectSettings;

namespace VIBN_Tools.CAD_Wizard
{
    public class CadWizardService
    {


        public static async Task<List<T>> SearchCodingsAsync<T>(Func<string, Vector3, Vector3, Guid, T?> factory) where T : FeeAbstractObject
        {
            var results = new ConcurrentBag<T>();

            var guids = await Services.ApiInstance.Object.GetSceneObjectGuidsOfTypeAsync(nameof(Decoration));

            await Parallel.ForEachAsync(guids, async (el, cancellationToken) =>
            {
                var phraseXml = await Services.ApiInstance.Object.GetPropertyAsync(el, nameof(SceneObject.Name));
                string vibnPhrase = Services.ApiInstance.XmlHelper.ConvertToString(phraseXml);

                var valuePosition = await Services.ApiInstance.Object.GetPropertyAsync(el, nameof(SceneObject.Transform.Position), nameof(SceneObject.Transform));
                var position = Services.ApiInstance.XmlHelper.ConvertToVector3(valuePosition);

                var valueRotation = await Services.ApiInstance.Object.GetPropertyAsync(el, nameof(SceneObject.Transform.Rotation), nameof(SceneObject.Transform));
                var rotation = Services.ApiInstance.XmlHelper.ConvertToVector3(valueRotation);


                // Factory entscheidet, ob ein Objekt erzeugt wird oder nicht
                var obj = factory(vibnPhrase, position, rotation, Guid.Parse(el));
                if (obj != null)
                {
                    results.Add(obj);
                }
            });

            return results.ToList();
        }


        public static async Task ReparentObjectsToBasicFrame(IEnumerable<FeeAbstractObject> objectList, string basicFrameName)
        {
            Guid guidBasicFrame = Guid.Empty;

            // Get all existing BasicFrames and check for name
            var guidsBasicFrames = await Services.ApiInstance.Object.GetSceneObjectGuidsOfTypeAsync(nameof(BasicFrame));
            foreach (var guidFrame in guidsBasicFrames)
            {
                string strFrameXML = await Services.ApiInstance.Object.GetPropertyAsync(guidFrame, nameof(SceneObject.Name));
                string strFrame = Services.ApiInstance.XmlHelper.ConvertToString(strFrameXML);
                if (strFrame == basicFrameName)
                {
                    guidBasicFrame = Guid.Parse(guidFrame);
                    break;
                }
            }

            guidBasicFrame = guidBasicFrame == Guid.Empty ? Guid.NewGuid() : guidBasicFrame;

            // Create Basic-Frame
            Services.ApiInstance.Object.CreateObject(nameof(BasicFrame), guidBasicFrame);

            await Services.ApiInstance.Object.SetPropertyAsync(guidBasicFrame, nameof(SceneObject.Name), basicFrameName);
            await Services.ApiInstance.Object.SetPropertyAsync(guidBasicFrame, "IsComponentActive", false, "Model");
            await Services.ApiInstance.Object.SetPropertyAsync(guidBasicFrame, nameof(SceneObject.Transform.Position), new Vector3(0, 0, 0), nameof(SceneObject.Transform));

            Services.ApiInstance.Object.Send(guidBasicFrame);
            await Services.ApiInstance.Object.WaitForSceneObjectAsync(guidBasicFrame.ToString());

            await Parallel.ForEachAsync(objectList, async (el, token) =>
            {
                await Services.ApiInstance.Object.AddChildToParentAsync(guidBasicFrame, el.Guid);
            });

        }





        public static IAxisLogicStrategy GetAxisLogicStrategy(TemplateType templateType)
        {
            return templateType switch
            {
                TemplateType.Siemens => new SiemensAxisLogicStrategy(),
                TemplateType.Beckhoff_Old => new BeckhoffAxisLogicStrategy(),
                TemplateType.Beckhoff_New => new BeckhoffAxisLogicStrategy(),
                _ => throw new NotSupportedException($"Template {templateType} not supported"),
            };
        }


        public static async Task<bool> CreateAndAssignAxisLogicAsync(FeeJoint joint, TemplateType templateType)
        {
            var strategy = GetAxisLogicStrategy(templateType);
            return await strategy.CreateAndAssignAxisLogicAsync(joint);
        }








    }


    public class SiemensAxisLogicStrategy : IAxisLogicStrategy
    {
        public async Task<bool> CreateAndAssignAxisLogicAsync(FeeJoint joint)
        {
            if (!joint.Coding.StartsWith("P"))
            {
                return false;
            }

            var axisLogic = new FeeLogic()
            {
                Name = joint.Name,
                LogicDefinitionName = LogicsStandard.Grob_AxisSiemens.Name,
                Parent = joint,
            };

            // Get or Import Axis Logic Definition
            (axisLogic.LogicDefinitionGuid, axisLogic.LogicDefinitionVersion) = await GetOrImportLogicDefinition(axisLogic.LogicDefinitionName, LogicsStandard.Grob_AxisSiemens.Path);

            if (axisLogic.LogicDefinitionGuid == Guid.Empty || axisLogic.LogicDefinitionVersion == String.Empty) return false;
            if (!await axisLogic.CreateSendAssignAndWaitAsync()) return false;

            // Create new Interface for axis signals
            FeeInterface axisInterface = new FeeInterface(Defines.GrobGenerationInterfaceProviderGuid, Defines.CadWizardInterfaceGuid, "Axis Values (Temp)");
            if (!await axisInterface.CreateInterfaceAsync()) return false;

            // Create Signals
            var signalAxisControlWord = new FeeInterfaceSignal()
            {
                Tag = joint.Name + "_Tel902_ControlDWord",
                Comment = "ControlDWord for axis (Telegram 902): " + joint.Name,
                IOType = FS.SDK.Io.IOType.DWord,
                Usage = FS.SDK.Io.IOMode.Read,
                Guid = Guid.NewGuid(),
            };
            var signalAxisStatusWord = new FeeInterfaceSignal()
            {
                Tag = joint.Name + "_Tel902_StatusDWord",
                Comment = "StatusDWord for axis (Telegram 902): " + joint.Name,
                IOType = FS.SDK.Io.IOType.DWord,
                Usage = FS.SDK.Io.IOMode.Write,
                Guid = Guid.NewGuid(),
            };
            var signalAxisValue = new FeeInterfaceSignal()
            {
                Tag = joint.Name,
                Comment = "PLC Axis Value",
                IOType = FS.SDK.Io.IOType.Real,
                Usage = FS.SDK.Io.IOMode.Read,
                Guid = Guid.NewGuid(),
            };
            var signalAxisSafePosition = new FeeInterfaceSignal()
            {
                Tag = joint.Name + "_SafePosition",
                Comment = "Axis Safe Position",
                IOType = FS.SDK.Io.IOType.DInt,
                Usage = FS.SDK.Io.IOMode.Write,
                Guid = Guid.NewGuid(),
            };

            await signalAxisControlWord.CreateSignalAsync(axisInterface);
            await signalAxisStatusWord.CreateSignalAsync(axisInterface);
            await signalAxisValue.CreateSignalAsync(axisInterface);
            await signalAxisSafePosition.CreateSignalAsync(axisInterface);

            // Assign Signals
            await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(axisLogic.Guid, "PLC_OUT_Axis_ControlWord", signalAxisControlWord.Guid, false);
            await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(axisLogic.Guid, "PLC_IN_Axis_StatusWord", signalAxisStatusWord.Guid, false);
            await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(axisLogic.Guid, "PLC_OUT_AxisValue", signalAxisValue.Guid, false);
            await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(axisLogic.Guid, "PLC_IN_Axis_SafePosition", signalAxisSafePosition.Guid, false);

            await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(axisLogic.Guid, "SIM_InValue_Joint", joint.Guid, nameof(MotionJoint.InValue));

            if (joint.Coding.Contains("ROT"))
            {
                Services.ApiInstance.Object.SetSlotValue(axisLogic.Guid, "PARA_AxisIsRotary", true);
            }

            await Services.ApiInstance.Object.AddChildToParentAsync(joint.Guid, axisLogic.Guid);


            return true;



        }
    }



    public class BeckhoffAxisLogicStrategy : IAxisLogicStrategy
    {
        public async Task<bool> CreateAndAssignAxisLogicAsync(FeeJoint joint)
        {
            if (!joint.Coding.StartsWith("P"))
            {
                return false;
            }

            var axisLogic = new FeeLogic()
            {
                Name = joint.Name,
                LogicDefinitionName = LogicsStandard.Grob_AxisBeckhoff.Name,
                Parent = joint,
            };

            // Get or Import Axis Logic Definition
            (axisLogic.LogicDefinitionGuid, axisLogic.LogicDefinitionVersion) = await GetOrImportLogicDefinition(axisLogic.LogicDefinitionName, LogicsStandard.Grob_AxisBeckhoff.Path);

            if (axisLogic.LogicDefinitionGuid == Guid.Empty || axisLogic.LogicDefinitionVersion == String.Empty) return false;
            if (!await axisLogic.CreateSendAssignAndWaitAsync()) return false;

            // Create new Interface for axis signals
            FeeInterface axisInterface = new FeeInterface(Defines.GrobGenerationInterfaceProviderGuid, Defines.CadWizardInterfaceGuid, "Axis Values (Temp)");
            if (!await axisInterface.CreateInterfaceAsync()) return false;

            // Create Signals
            var signalAxisValue = new FeeInterfaceSignal()
            {
                Tag = joint.Name,
                Comment = "PLC Axis Value",
                IOType = FS.SDK.Io.IOType.Double,
                Usage = FS.SDK.Io.IOMode.Read,
                Guid = Guid.NewGuid(),
                Path = "GVL_Axis.gstAxisRef[].NcToPlc.ActPos",
            };

            await signalAxisValue.CreateSignalAsync(axisInterface);

            // Assign Signals
            await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(axisLogic.Guid, "PLC_OUT_AxisValue", signalAxisValue.Guid, false);

            await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(axisLogic.Guid, "SIM_InValue_Joint", joint.Guid, nameof(MotionJoint.InValue));

            if (joint.Coding.Contains("ROT"))
            {
                Services.ApiInstance.Object.SetSlotValue(axisLogic.Guid, "PARA_AxisIsRotary", true);
            }

            await Services.ApiInstance.Object.AddChildToParentAsync(joint.Guid, axisLogic.Guid);


            return true;
        }
    }



}
