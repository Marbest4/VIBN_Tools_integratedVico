using FS.SDK;
using FS.SDK.API;
using FS.SDK.Scene.Objects;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeLogic : FeeAbstractObject, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================

        public Guid LogicDefinitionGuid { get; set; }
        public string LogicDefinitionName { get; set; }
        public string LogicDefinitionVersion { get; set; }
        public string LogicDefinitionPath { get; set; }

        public bool IsEnabled { get; set; }



        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeLogic()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(LogicObject);
            Visible = false;
        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        /// <summary>
        /// Function creates and sends a LogicObject and waits for it to appear in the simulation.
        /// After the LogicObject is in the simulation, the LogicDefinition is assigned to it
        /// Returns true, if Create and Assign is successful, otherwise false
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CreateSendAssignAndWaitAsync()
        {

            if (LogicDefinitionGuid != Guid.Empty && LogicDefinitionName != null && LogicDefinitionVersion != null)
            {
                Services.ApiInstance.Object.CreateObject(nameof(LogicObject), Guid);
                await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SceneObject.Name), Name);
                await Services.ApiInstance.Object.SetPropertyAsync(Guid, "IsComponentActive", false, "Model");

                Services.ApiInstance.Object.Send(Guid);
                if (await Services.ApiInstance.Object.WaitForSceneObjectAsync(Guid.ToString()))
                {
                    if (await Services.ApiInstance.Logic.AssignLogicToElementAsync(LogicDefinitionGuid, LogicDefinitionVersion, Guid))
                    {
                        var slots = await Services.ApiInstance.Object.GetSlotNamesAsync(Guid);
                        var waitUntil = DateTimeOffset.UtcNow.AddSeconds(10);
                        while (slots.Length < 1 && DateTimeOffset.UtcNow < waitUntil)
                        {
                            await Task.Delay(20);
                            slots = await Services.ApiInstance.Object.GetSlotNamesAsync(Guid);
                        }
                        if (slots.Length < 1)
                            return false;

                        if (Parent != null)
                        {
                            await Services.ApiInstance.Object.AddChildToParentAsync(Parent.Guid, Guid);
                        }
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }




        /// <summary>
        /// Function tries to find the Guid and Version of a given LogicDefinition by its name.
        /// If the LogicDefinition is not yet imported, it will import it from the local Logics path
        /// Returns the LogicDefinition Guid and Version as a tuple
        /// </summary>
        /// <param name="logicName"></param>
        /// <param name="localLogicPath"></param>
        /// <returns></returns>
        public async static Task<(Guid Guid, string Version)> GetOrImportLogicDefinition(string logicName, string localLogicPath)
        {
            if (string.IsNullOrWhiteSpace(logicName))
                return (Guid.Empty, string.Empty);
            bool logicImported = false;

            Guid guid = Guid.Empty;
            string version = String.Empty;

            // Check existing logic definitions
            ApiLogicDefinition[] allLogicDefinitions = await Services.ApiInstance.Logic.GetAllAvailableLogicDefinitionsAsync();
            foreach (var logicDef in allLogicDefinitions)
            {
                if (logicDef.Name == logicName)
                {
                    logicImported = true;
                    guid = Guid.Parse(logicDef.Guid);
                    version = logicDef.Versions.LastOrDefault();
                    break;
                }
            }

            // Import logic definition if not existing
            if (!logicImported)
            {
                if (string.IsNullOrWhiteSpace(localLogicPath))
                    return (Guid.Empty, string.Empty);

                // Import Logic Definition
                string basePathContent = Path.Combine(AppContext.BaseDirectory, @"Content");
                var definitionPath = basePathContent + localLogicPath;
                if (!File.Exists(definitionPath))
                    throw new FileNotFoundException($"Logic definition '{logicName}' was not found.", definitionPath);

                await Services.ApiInstance.Logic.SendLogicDefinitionAsync(definitionPath);

                var refreshedDefinitions = await Services.ApiInstance.Logic.GetAllAvailableLogicDefinitionsAsync();
                var imported = refreshedDefinitions.FirstOrDefault(definition =>
                    string.Equals(definition.Name, logicName, StringComparison.Ordinal));
                if (imported is not null)
                {
                    guid = Guid.Parse(imported.Guid);
                    version = imported.Versions.LastOrDefault() ?? string.Empty;
                }
            }

            return (guid, version);
        }


        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

            var logic = xElement.Element("Logic");

            LogicDefinitionGuid = Guid.Parse((string)logic.Element("PersistedLogicGuid") ?? Guid.Empty.ToString());
            LogicDefinitionVersion = (string)logic.Element("PersistedLogicVersion") ?? String.Empty;
            IsEnabled = (bool?)logic.Element("IsEnable") ?? false;

        }

        public override void ApplyBatchData(FeePropertyBatchData data)
        {
            base.ApplyBatchData(data);

            var logicDefinition = data.AllLogicDefinitions
                .FirstOrDefault(x => x.Guid == LogicDefinitionGuid.ToString());

            LogicDefinitionName = logicDefinition?.Name ?? "UNDEFINED";
        }




        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            if (LogicValidation.Map.TryGetValue(this.LogicDefinitionGuid, out var validator))
            {
                PlausibilityIssues = (await validator.ValidateAsync(this)).ToList();
            }
            else
            {
                // Fallback for user generated logics
                var fallback = new GenericLogicValidator();
                PlausibilityIssues = (await fallback.ValidateAsync(this)).ToList();
            }
        }




        private static (string Name, string Path) FindPropertiesByPersistedGuid(Guid guid)
        {
            var nestedTypes = typeof(LogicsStandard).GetNestedTypes(BindingFlags.Public);

            var match = nestedTypes
                .Select(t => new
                {
                    Name = t.GetField("Name", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string,
                    Path = t.GetField("Path", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string,
                    Guid = t.GetField("PersistedGuid", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string
                })
                .FirstOrDefault(x => x.Guid == guid.ToString());

            return match == null ? (String.Empty, String.Empty) : (match.Name, match.Path);
        }







        //===================================================================================================================
        // A D D I T I O N A L S :   C O N S T A N T S ,   D E F I N E S ,   E T C .
        //===================================================================================================================

        public static class LogicsStandard
        {
            /***************************************************************************************************************/
            /* G R O B   S T A N D A R D
            /***************************************************************************************************************/


            public static class Grob_AxisBeckhoff
            {
                public const string Name = "Grob_AxisBeckhoff";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_AxisBeckhoff.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("abc65c60-b8e5-4ed5-8967-e4f4ed1aa528");
                public static class Slots
                {
                    public const string AxisValue = "PLC_OUT_AxisValue";
                    public const string SimValue = "SIM_InValueJoint";
                }
            }

            public static class Grob_AxisSiemens
            {
                public const string Name = "Grob_AxisSiemens";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_AxisSiemens.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("abc7867a-b3b9-4b1a-afec-f3e0fcee4153");

                public static class Slots
                {
                    public const string AxisValue = "PLC_OUT_AxisValue";
                    public const string SimValue = "SIM_InValue_Joint";

                    public const string ControlWord = "PLC_OUT_Axis_ControlWord";
                    public const string StatusWord = "PLC_IN_Axis_StatusWord";

                }
            }
            public static class Grob_BeltControl
            {
                public const string Name = "Grob_BeltControl";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_BeltControl.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("abc50194-ea47-45d3-b5ef-cd2ba3d2c38f");
                public static class Slots
                {
                    public const string AxisValue = "PLC_OUT_AxisValue";
                    public const string BeltControlState = "PLC_IN_BeltControlState";

                    public const string AxisIsRotary = "PAR_AxisIsRotary";
                    public const string ChangeStep = "PAR_ChangeStep_mm";
                }
            }

            public static class Grob_Clamping
            {
                public const string Name = "Grob_Clamping";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_Clamping.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("abccd7d5-b56a-47d8-ac84-efc6aba6da4a");

                public static class Slots
                {
                    public const string ReleaseClamping = "PLC_OUT_ReleaseClamping";
                    public const string ClampingReleased = "PLC_IN_ClampingReleased";

                    public const string ClampingDelay = "PAR_ClampingDelay_sec";
                }
            }

            public static class Grob_Conveyor
            {
                public const string Name = "Grob_Conveyor";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_Conveyor.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("abc8152d-40e0-4e70-a679-56e24f881b0b");

                public static class Slots
                {
                    public const string ControlWord = "PLC_OUT_ControlWord";
                    public const string StatusWord = "PLC_IN_StatusWord";
                    public const string Clockwise = "PLC_OUT_Clockwise";
                    public const string CounterClockwise = "PLC_OUT_CounterClockwise";

                    public const string Speed = "PLC_OUT_Speed";
                    public const string SlowSpeed = "PLC_OUT_SlowSpeed";
                    public const string PowerSupplyTurnedOn = "PLC_OUT_PowerSupplyTurnedOn";
                    public const string TurnOff = "PLC_OUT_TurnOff";

                    public const string ReadyForOperation = "PLC_IN_ReadyForOperation";
                    public const string ConveyorActive = "PLC_IN_ConveyorActive";
                    public const string ConveyorOk = "PLC_IN_ConveyorOk";

                    public const string AckFault = "PLC_OUT_AcknowledgeFault";
                    public const string Warning = "PLC_IN_Warning";
                    public const string Error = "PLC_IN_Error";

                    public const string VelocityOut = "SIM_Velocity";
                    public const string VelocityIn = "PAR_Velocity";
                }
            }

            public static class Grob_Cylinder
            {
                public const string Name = "Grob_Cylinder";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_Cylinder.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("abcbfc04-3a01-44af-a827-1fd3213a5849");

                public static class Slots
                {
                    public const string ToHomePos = "PLC_OUT_ToHomePos";
                    public const string ToWorkPos = "PLC_OUT_ToWorkPos";
                    public const string InHomePos = "PLC_IN_InHomePos";
                    public const string InWorkPos = "PLC_IN_InWorkPos";
                    public const string ReleaseClamping = "PLC_OUT_ReleaseClamping";
                    public const string ClampingReleased = "PLC_IN_ClampingReleased";

                    public const string HomePos = "SIM_HomePosition";
                    public const string WorkPos = "SIM_WorkPosition";
                    public const string OperationTime = "PAR_OperationTime_Sec";

                    public const string ActualPosition = "SIM_ActualPosition";
                    public const string TargetPosition = "SIM_TargetPosition";
                    public const string Velocity = "SIM_Velocity";
                }
            }

            public static class Grob_GripperBasic
            {
                public const string Name = "Grob_GripperBasic";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_GripperBasic.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("483a511e-09d2-4982-abc7-24a50750ea22");

                public static class Slots
                {

                    public const string Unclamp = "PLC_OUT_Unclamp";
                    public const string Clamp = "PLC_OUT_Clamp";

                    public const string Unclamped = "PLC_IN_Unclamped";
                    public const string Clamped = "PLC_IN_Clamped";
                    public const string ClampedWithPart = "PLC_IN_ClampedWithPart";
                    public const string ClampedNoPart = "PLC_IN_ClampedWithoutPart";

                    public const string ReleaseClamping = "PLC_OUT_ReleaseClamping";
                    public const string ClampingReleased = "PLC_IN_ClampingReleased";

                    public const string UnclampedPos = "PAR_UnclampedPosition";
                    public const string ClampedPos = "PAR_ClampedPosition";
                    public const string OperationTime = "PAR_OperationTime_sec";
                    public const string ClampedDebounceTime = "PAR_ClampedDebounce_sec_";

                    public const string ActualPosition = "SIM_ActualPosition";
                    public const string PartPicked = "SIM_PartPicked";
                    public const string Velocity = "SIM_Velocity";
                    public const string TargetPosition = "SIM_TargetPosition";
                    public const string Drop = "SIM_Drop";
                    public const string Pick = "SIM_Pick";
                    public const string AddOnStatus = "SIM_AddOnStatus";
                }
            }

            public static class Grob_GripperVacuum
            {
                public const string Name = "Grob_GripperVacuum";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_GripperVacuum.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("25afe623-5c72-45d6-8e40-2dce14bb5610");

                public static class Slots
                {
                    public const string VacuumOn = "PLC_OUT_VacuumOn";
                    public const string VacuumOff = "PLC_OUT_VacuumOff";
                    public const string BlowAirOn = "PLC_OUT_BlowAirOn";

                    public const string VacuumPressureOk = "PLC_IN_Vacuum_PressureOk";

                    public const string Pick = "SIM_Pick";
                    public const string Drop = "SIM_Drop";

                }
            }

            public static class Grob_LiftUnit
            {
                public const string Name = "Grob_LiftUnit";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_LiftUnit.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("abc9bc0a-c339-4057-9b7c-f2b8066d9d83");

                public static class Slots
                {
                    public const string ToHomePos = "PLC_OUT_ToHomePos";
                    public const string ToWorkPos = "PLC_OUT_ToWorkPos";
                    public const string InHomePos = "PLC_IN_InHomePos";
                    public const string InMiddlePos = "PLC_IN_InMiddlePos";
                    public const string InWorkPos = "PLC_IN_InWorkPos";
                    public const string ReleaseClamping = "PLC_OUT_ReleaseClamping";
                    public const string ClampingReleased = "PLC_IN_ClampingReleased";

                    public const string HomePos = "SIM_HomePositionition";
                    public const string WorkPos = "SIM_WorkPositionition";
                    public const string OperationTime = "SIM_OperationTime_sec";

                    public const string ActualPosition = "SIM_ActualPosition";
                    public const string TargetPosition = "SIM_TargetPosition";
                    public const string Velocity = "SIM_Velocity";
                    public const string EnableMiddlePos = "PAR_EnableMiddlePos";
                }
            }

            public static class Grob_PneumaticSupply
            {
                public const string Name = "Grob_PneumaticSupply";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_PneumaticSupply.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("dd3be183-7fb7-43b6-aa4a-3cba0557d5aa");

                public static class Slots
                {
                    public const string SwitchOnCh1 = "PLC_OUT_SwitchOn_Ch1";
                    public const string SwitchOnCh2 = "PLC_OUT_SwitchOn_Ch2";

                    public const string PneumaticOkCh1 = "PLC_IN_PneumaticOk_Ch1";
                    public const string PneumaticOkCh2 = "PLC_IN_PneumaticOk_Ch2";
                    public const string NotSwitchedOnCh1 = "PLC_IN_NotSwitchedOn_Ch1";
                    public const string NotSwitchedOnCh2 = "PLC_IN_NotSwitchedOn_Ch2";

                    public const string SwitchedOnImpulse = "PLC_IN_SwitchedOn_Impulse";
                }
            }

            public static class Grob_SafetyDoor
            {
                public const string Name = "Grob_SafetyDoor";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_SafetyDoor.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("abc3b5da-e130-4c28-b4ea-bc11a23d5795");

                public static class Slots
                {
                    public const string Unlock = "PLC_OUT_Unlock";
                    public const string LedStart = "PLC_OUT_Start";
                    public const string LedQuitReset = "PLC_OUT_QuitReset";
                    public const string LedRequestEntry = "PLC_OUT_RequestEntry";

                    public const string Unlocked = "PLC_IN_Unlocked";
                    public const string Opened = "PLC_IN_Opened";
                    public const string Closed_Ch1 = "PLC_IN_Closed_Ch1";
                    public const string Closed_Ch2 = "PLC_IN_Closed_Ch2";
                    public const string ClosedAndLocked = "PLC_IN_ClosedAndLocked";
                    public const string ClosedAndLocked_Ch1 = "PLC_IN_ClosedAndLocked_Ch1";
                    public const string ClosedAndLocked_Ch2 = "PLC_IN_ClosedAndLocked_Ch2";
                    public const string BoltTongueInserted = "PLC_IN_BoltTongueInserted";

                    public const string Start = "PLC_IN_Start";
                    public const string QuitReset = "PLC_IN_QuitReset";
                    public const string RequestEntry = "PLC_IN_RequestEntry";
                    public const string Fault = "PLC_IN_Fault";

                    public const string EStopPressed_Ch1 = "PLC_IN_EStopPressed_Ch1";
                    public const string EStopPressed_Ch2 = "PLC_IN_EStopPressed_Ch2";
                    public const string EStopNotPressed_Ch1 = "PLC_IN_EStopNotPressed_Ch1";
                    public const string EStopNotPressed_Ch2 = "PLC_IN_EStopNotPressed_Ch2";

                    public const string ActualPosition = "SIM_ActualPosition";                    
                    public const string Velocity = "SIM_Velocity";
                    public const string TargetPosition = "SIM_TargetPosition";
                    public const string TargetPositionTongue = "SIM_TargetPositionTongue";

                    public const string SimOpenDoor = "SIM_OpenDoor";
                    public const string SimQuitReset = "SIM_QuitReset";

                    public const string OpenPosition = "PAR_OpenPosition";
                    public const string OpenPositionTongue = "PAR_OpenPositionTongue";


                }
            }

            public static class Grob_Sensor
            {
                public const string Name = "Grob_Sensor";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_Sensor.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("37c90470-4822-40b4-b408-ae244bb04ae5");

                public static class Slots
                {
                    public const string PartPresent_Ch1 = "PLC_IN_PartPresent_Ch1";
                    public const string PartPresent_Ch2 = "PLC_IN_PartPresent_Ch2";
                    public const string NoPartPresent_Ch1 = "PLC_IN_NoPartPresent_Ch1";
                    public const string NoPartPresent_Ch2 = "PLC_IN_NoPartPresent_Ch2";

                    public const string SensorValue = "SIM_Sensor";
                }
            }

            public static class Grob_Stop
            {
                public const string Name = "Grob_Stop";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_Stop.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("abced01b-0d20-4d2c-b5df-ff86f6532b58");

                public static class Slots
                {
                    public const string Open = "PLC_OUT_Open";
                    public const string Close = "PLC_OUT_Close";
                    public const string Opened = "PLC_IN_Opened";
                    public const string Closed = "PLC_IN_Closed";

                    public const string Collision = "SIM_Collision";
                    public const string CollisionStatus = "SIM_CollisionStatus";

                    public const string CollisionDelay = "PAR_CollisionDelay_sec";
                }
            }

            public static class Grob_Stacklight
            {
                public static class Slots
                {
                    public const string Red = "Red";
                    public const string Yellow = "Yellow";
                    public const string Green = "Green";
                    public const string Blue = "Blue";
                    public const string White = "White";

                }
            }

            public static class Grob_SimModeSiemens
            {
                public const string Name = "Grob_SimModeSiemens";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_SimModeSiemens.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("8adbd1a5-3201-49fe-b187-60b0d8ddaa5f");

                public static class Slots
                {
                    public const string LifetimeOut = "PLC_OUT_Lifetime";
                    public const string SimActivated = "PLC_OUT_SimActivated";
                    public const string SimDeactivated = "PLC_OUT_SimDeactivated";
                    public const string SafetyBypassed = "PLC_OUT_SafetyBypassed";

                    public const string LifetimeIn = "PLC_IN_Lifetime";
                    public const string ActivateSim = "PLC_IN_ActivateSim";
                    public const string DeactivateSim = "PLC_IN_DeactivateSim";
                    public const string BypassSafety = "PLC_IN_BypassSafety";

                    public const string Lifetime_sec = "PAR_Lifetime_sec";
                }
            }

            public static class Grob_SafePnPn
            {
                public const string Name = "Grob_SafePnPn";
                public const string Path = "\\LogicDefinitions\\GrobStandard\\Grob_SafePnPn.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("1d356237-b736-4d75-875c-fba170ea191c");

                public static class Slots
                {
                }
            }
        }



        public static class LogicsAddons
        {
            /***************************************************************************************************************/
            /* G R O B   A D D O N S
            /***************************************************************************************************************/


            public static class Grob_GripperAddOn_AnalogValues
            {
                public const string Name = "Grob_GripperAddOn_AnalogValues";
                public const string Path = "\\LogicDefinitions\\GrobAddOns\\Grob_GripperAddOn_AnalogValues.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("89687567-45a3-4ded-9f65-e5fc49905861");

                public static class Slots
                {
                    public const string AddOnStatus = "SIM_AddOnStatus";

                    public const string UnclampedValue = "PAR_UnclampedValue";
                    public const string ClampedValue = "PAR_ClampedValue";
                    public const string ClampedWithPartValue = "PAR_ClampedWithPartValue";
                    public const string ClampedNoPartValue = "PAR_ClampedWithoutPartValue";

                    public const string UnclampedAnalog = "PLC_IN_Unclamped_Analog";
                    public const string ClampedAnalog = "PLC_IN_Clamped_Analog";
                    public const string ClampedWithPartAnalog = "PLC_IN_ClampedWithPart_Analog";
                    public const string ClampedNoPartAnalog = "PLC_IN_ClampedWithoutPart_Analog";

                }
            }

            public static class Grob_GripperAddOn_MultiplePartTypes
            {
                public const string Name = "Grob_GripperAddOn_MultiplePartTypes";
                public const string Path = "\\LogicDefinitions\\GrobAddOns\\Grob_GripperAddOn_MultiplePartTypes.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("d4057e69-e813-4274-8ab9-37c66a17c19f");

                public static class Slots
                {
                    public const string AddOnStatus = "SIM_AddOnStatus";                    

                    public const string ClampedPosition1 = "PAR_ClampedPosition_Type1";
                    public const string ClampedPosition2 = "PAR_ClampedPosition_Type2";
                    public const string ClampedPosition3 = "PAR_ClampedPosition_Type3";
                    public const string ClampedPositionNoPart = "PAR_ClampedPosition_NoPart";

                    public const string ClampedWithPart1 = "PLC_IN_ClampedWithPart_Type1";
                    public const string ClampedWithPart2 = "PLC_IN_ClampedWithPart_Type2";
                    public const string ClampedWithPart3 = "PLC_IN_ClampedWithPart_Type3";

                    public const string TargetPosition = "SIM_TargetPosition";

                    public const string PartPresent1 = "SIM_PartPresent_Type1";
                    public const string PartPresent2 = "SIM_PartPresent_Type2";
                    public const string PartPresent3 = "SIM_PartPresent_Type3";

                }
            }


        }


        public static class LogicsAdditional
        {
            /***************************************************************************************************************/
            /* G R O B   A D D I T I O N A L
            /***************************************************************************************************************/


            public static class Absaugung
            {
                public const string Name = "Absaugung";
                public const string Path = "\\LogicDefinitions\\GrobAdditional\\Absaugung.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("65ffd260-7c5e-4784-8071-db25ed84c314");
            }
            public static class AddCountToString
            {
                public const string Name = "AddCountToString";
                public const string Path = "\\LogicDefinitions\\GrobAdditional\\AddCountToString.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("1374ab54-392e-40ed-86d4-a23e7e50e756");
            }
            public static class Analogsensor_Skalierung
            {
                public const string Name = "Analogsensor_Skalierung";
                public const string Path = "\\LogicDefinitions\\GrobAdditional\\Analogsensor_Skalierung.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("48d3b210-b6c2-4525-9eb8-d634c648d3b5");
            }
            public static class AnalogValueSwitch
            {
                public const string Name = "AnalogValueSwitch";
                public const string Path = "\\LogicDefinitions\\GrobAdditional\\AnalogValueSwitch.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("7b8a4917-aab3-4337-8b3e-f00458102fca");
            }
            public static class AsciiToUnicode
            {
                public const string Name = "AsciiToUnicode";
                public const string Path = "\\LogicDefinitions\\GrobAdditional\\AsciiToUnicode.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("2b721619-cb54-48ec-9c3c-5b6eed0576e6");
            }
            public static class Auflagekontrolle
            {
                public const string Name = "Auflagekontrolle";
                public const string Path = "\\LogicDefinitions\\GrobAdditional\\Auflagekontrolle.xml";
                public static readonly Guid PersistedGuid = Guid.Parse("eb3c4aa9-8517-4713-88bc-0574905a5095");
            }
            //public static class Absaugung
            //{
            //    public const string Name = "Absaugung";
            //    public const string Path = "\\Content\\LogicDefinitions\\GrobAdditional\\Absaugung.xml";
            //    public static readonly Guid PersistedGuid = Guid.Parse("65ffd260-7c5e-4784-8071-db25ed84c314");
            //}

        }


        



        public static class LogicsSpecialDevice
        {
            /***************************************************************************************************************/
            /* S P E C I A L   D E V I C E S
            /***************************************************************************************************************/

            public static class AtlasCopco_Sys6000_Glueing_BMW
            {
                public const string Name = "AtlasCopco_SYS_6000_(BMW)";
                public const string Path = "\\LogicDefinitions\\SpecialDevices\\AtlasCopco_SYS_6000_(BMW).xml";
            }
            public static class AtlasCopco_Sys6000_Glueing_VASS
            {
                public const string Name = "AtlasCopco_SYS_6000_(VASS)";
                public const string Path = "\\LogicDefinitions\\SpecialDevices\\AtlasCopco_SYS_6000_(VASS).xml";
            }

            public static class Cognex_Dataman_DMR
            {
                public const string Name = "Cognex_DataMan_DMR";
                public const string Path = "\\LogicDefinitions\\SpecialDevices\\Cognex_DataMan_DMR.xml";
            }

            public static class IPG_LaserPicker
            {
                public const string Name = "IPG_LaserPicker";
                public const string Path = "\\LogicDefinitions\\SpecialDevices\\IPG_LaserPicker.xml";
            }

            public static class Keyence_SR2000
            {
                public const string Name = "KeyenceBarcodeSR2000";
                public const string Path = "\\LogicDefinitions\\SpecialDevices\\KeyenceBarcodeSR2000.xml";
            }

            public static class KukaSafety
            {
                public const string Name = "KukaSafety";
                public const string Path = "\\LogicDefinitions\\SpecialDevices\\KukaSafety.xml";
            }

            public static class Lenze_8400Motec
            {
                public const string Name = "LENZE_8400_Motec";
                public const string Path = "\\LogicDefinitions\\SpecialDevices\\LENZE_8400_Motec.xml";
            }

            public static class Lenze_8400Protec
            {
                public const string Name = "LENZE_8400_Protec";
                public const string Path = "\\LogicDefinitions\\SpecialDevices\\LENZE_8400_Protec.xml";
            }

            public static class Lenze_i950
            {
                public const string Name = "Lenze_i950";
                public const string Path = "\\LogicDefinitions\\SpecialDevices\\Lenze_i950.xml";
            }

            public static class PromessSpindleUP
            {
                public const string Name = "Promess_SpindleUP";
                public const string Path = "\\LogicDefinitions\\SpecialDevices\\Promess_SpindleUP.xml";
            }

            public static class Sensopart_VisorV20
            {
                public const string Name = "Sensopart Visor V20 Camera";
                public const string Path = "\\LogicDefinitions\\SpecialDevices\\Sensopart Visor V20 Camera.xml";
            }












        }
    }
}
