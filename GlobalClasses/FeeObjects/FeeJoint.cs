using FS.API;
using FS.SDK;
using FS.SDK.Components;
using FS.SDK.Mathematics;
using FS.SDK.Scene.Objects;
using FS.SDK.Utilities;
using System.Xml.Linq;
using VIBN_Tools.CAD_Wizard;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeJoint : FeeAbstractObject, IAssignableSimObject, ICadWizardCreatable<FeeJoint>, IPlausibilityCheck
    {
        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================




        private MotionType _jointType;
        public MotionType JointType
        {
            get => _jointType;
            set => SetPropertyChange(ref _jointType, value);
        }

        private MotionSource _controlType;
        public MotionSource ControlType
        {
            get => _controlType;
            set => SetPropertyChange(ref _controlType, value);
        }


        private bool _useLimits;
        public bool UseLimits
        {
            get => _useLimits;
            set => SetPropertyChange(ref _useLimits, value);
        }

        private bool _useLimitIndication;
        public bool UseLimitIndication
        {
            get => _useLimitIndication;
            set => SetPropertyChange(ref _useLimitIndication, value);
        }

        private bool _manualModeActive;
        public bool ManualModeActive
        {
            get => _manualModeActive;
            set => SetPropertyChange(ref _manualModeActive, value);
        }


        // Values for manual control
        private float _isActualPosition;
        public float IsActualPosition
        {
            get => _isActualPosition;
            set => SetPropertyChange(ref _isActualPosition, value);
        }

        private float _positionValue;
        public float PositionValue
        {
            get => _positionValue;
            set => SetPropertyChange(ref _positionValue, value);
        }

        private float _velocityValue;
        public float VelocityValue
        {
            get => _velocityValue;
            set => SetPropertyChange(ref _velocityValue, value);
        }

        private float _targetPositionValue;
        public float TargetPositionValue
        {
            get => _targetPositionValue;
            set => SetPropertyChange(ref _targetPositionValue, value);
        }






        // Needed for CAD Wizard
        public Quaternion Orientation { get; set; }
        public string Coding { get; set; }
        public Guid CadDecoGuid { get; set; }


        // Needed for Container Generation
        public ISimObjectFindOrSelect AssignedContainer { get; set; }



        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeJoint()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(MotionJoint);
            Visible = false;
        }






        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SceneObject.Transform.Orientation), Orientation, nameof(SceneObject.Transform));

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(MotionJoint.JointType), JointType);
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(JointControllerComponent.MotionSource), ControlType, "Controller");

            return true;
        }


        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

            var controller = xElement.Element("Controller");

            UseLimits = (bool?)controller.Element("UseLimits") ?? false;
            UseLimitIndication = (bool?)controller.Element("UseLimitIndication") ?? false;

            ControlType = ParseMotionSource((string)controller.Element("MotionSource") ?? String.Empty);
            JointType = ParseMotionType((string)xElement.Element("JointType") ?? String.Empty);

        }

        public override void ApplyBatchData(FeePropertyBatchData data)
        {
            base.ApplyBatchData(data);

            if (data.JointManualModeactive is bool manualValue)
                ManualModeActive = manualValue;

            if (data.PositionValue is float posValue)
                PositionValue = posValue;

            if (data.VelocityValue is float velValue)
                VelocityValue = velValue;

            if (data.TargetPositionValue is float targetValue)
                TargetPositionValue = targetValue;

            if (data.IsActualPosition is float actualValue)
                IsActualPosition = actualValue;
        }




        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {

            // Check connected slots
            Guid guid;
            var inValueConnected = Slots.TryGetValue("InValue", out guid) && guid != Guid.Empty;
            var inTargetConnected = Slots.TryGetValue("InTarget", out guid) && guid != Guid.Empty;
            var inVelocityConnected = Slots.TryGetValue("InVelocity", out guid) && guid != Guid.Empty;

            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;

            if (ControlType == MotionSource.None && !inValueConnected)
                PlausibilityIssues.Add(new PlausibilityIssue($"Slot 'InValue' nicht verknüpft", Severity.Error));

            if (ControlType == MotionSource.Velocity && !inVelocityConnected)
                PlausibilityIssues.Add(new PlausibilityIssue($"Slot 'InVelocity' nicht verknüpft", Severity.Error));

            if (ControlType == MotionSource.Position && !(inVelocityConnected && inTargetConnected))
                PlausibilityIssues.Add(new PlausibilityIssue($"Slots 'InVelocity' und/oder 'InTarget' nicht verknüpft", Severity.Error));

            if (ManualModeActive)
                PlausibilityIssues.Add(new PlausibilityIssue($"Manueller Modus aktiv", Severity.Error));

            if (maxOneZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Warning));
            else if (twoOrMoreZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Error));


        }





        //===================================================================================================================
        // M A N U A L   C O N T R O L
        //===================================================================================================================


        public async Task SetManualVelocityPositionAsync()
        {
            Guid[] guids;
            string[] names;
            object[] values;


            switch (ControlType)
            {
                case MotionSource.None:
                    guids = new[] { Guid };
                    names = new[] { "InValue" };
                    values = new object[] { PositionValue };
                    break;

                case MotionSource.Velocity:
                    guids = new[] { Guid };
                    names = new[] { "InVelocity" };
                    values = new object[] { VelocityValue };
                    break;

                case MotionSource.Position:
                    guids = new[] { Guid, Guid };
                    names = new[] { "InVelocity", "InTarget" };
                    values = new object[] { VelocityValue, TargetPositionValue };
                    break;

                default:
                    return;
            }

            await Services.ApiInstance.Object.SetSlotValuesAsync(guids, names, values);

            float lastPosition = IsActualPosition;

            do
            {
                IsActualPosition = Services.ApiInstance.XmlHelper.ConvertToFloat(await Services.ApiInstance.Object.GetSlotValueAsync(Guid, "OutValue"));

                await Task.Delay(100);


            } while (!ShouldStopPolling(ControlType, lastPosition));

        }


        private bool ShouldStopPolling(MotionSource controlType, float lastPosition)
        {
            switch (controlType)
            {
                case MotionSource.None:
                    return IsActualPosition == PositionValue;

                case MotionSource.Velocity:
                    return Math.Abs(IsActualPosition - lastPosition) < 0.001f;

                case MotionSource.Position:
                    return IsActualPosition == TargetPositionValue;

                default:
                    return true;
            }
        }






        //===================================================================================================================
        // M E T H O D S   ( C A D - W I Z A R D )
        //===================================================================================================================

        /// <summary>
        /// Function reparents the CAD-Decoration under the generated Joint and compounds it
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ReparentCadDecoToJointAsync()
        {
            // Rename Deco Object
            Services.ApiInstance.Object.CreateObject(nameof(Decoration), CadDecoGuid);

            await Services.ApiInstance.Object.SetPropertyAsync(CadDecoGuid, nameof(SceneObject.Name), "CAD-" + Name);
            await Services.ApiInstance.Object.SendAndWait(CadDecoGuid);

            // Reparent Deco Object
            await Services.ApiInstance.Object.AddChildToParentAsync(Guid, CadDecoGuid);

            // Compound Deco Object and children
            var childrenTemp = (await Services.ApiInstance.Object.GetAllChildrenFromSceneObjectAsync(CadDecoGuid.ToString())).ToList();

            if (childrenTemp.Count() > 0 && !ApiEnums.IsErrorCode(childrenTemp[0]))
            {
                await Services.ApiInstance.Object.CompoundObjectsAsync(CadDecoGuid.ToString(), true, childrenTemp.ToArray());
            }

            await Services.ApiInstance.Object.SetPropertyAsync(CadDecoGuid, "IsComponentActive", true, "Model");
            Services.ApiInstance.Object.Send(CadDecoGuid);

            return true;
        }


        public static FeeJoint? CadWizardFactory(string name, Vector3 position, Vector3 rotation, Guid cadDecoGuid)
        {
            foreach (var coding in JointCodings.AllCodings)
            {
                if (name.StartsWith(coding))
                {
                    var joint = new FeeJoint()
                    {
                        Name = name.Substring(coding.Length + 1),
                        Coding = coding,
                        Position = position,
                        Rotation = rotation,
                        Scale = new Vector3(0.5f, 0.5f, 0.5f),
                        CadDecoGuid = cadDecoGuid,
                    };

                    joint.SetOrientation();
                    joint.SetMotionType();
                    joint.SetControlType();

                    return joint;
                }
            }
            return null;
        }







        // Helpers
        public void SetOrientation()
        {
            if (Coding.Contains("X+"))
            {
                Orientation = Quaternion.FromRollPitchYawDegrees(Rotation) * Quaternion.FromRollPitchYawDegrees(new Vector3(0, 90, 0));
            }
            else if (Coding.Contains("X-"))
            {
                Orientation = Quaternion.FromRollPitchYawDegrees(Rotation) * Quaternion.FromRollPitchYawDegrees(new Vector3(0, -90, 0));
            }
            else if (Coding.Contains("Y+"))
            {
                Orientation = Quaternion.FromRollPitchYawDegrees(Rotation) * Quaternion.FromRollPitchYawDegrees(new Vector3(-90, 0, 0));
            }
            else if (Coding.Contains("Y-"))
            {
                Orientation = Quaternion.FromRollPitchYawDegrees(Rotation) * Quaternion.FromRollPitchYawDegrees(new Vector3(90, 0, 0));
            }
            else if (Coding.Contains("Z+"))
            {
                Orientation = Quaternion.FromRollPitchYawDegrees(Rotation) * Quaternion.FromRollPitchYawDegrees(new Vector3(0, 0, 0));
            }
            else if (Coding.Contains("Z-"))
            {
                Orientation = Quaternion.FromRollPitchYawDegrees(Rotation) * Quaternion.FromRollPitchYawDegrees(new Vector3(-180, 0, -180));
            }
        }

        public void SetMotionType()
        {
            if (Coding.Contains("LIN"))
            {
                JointType = MotionType.Translate;
            }
            else if (Coding.Contains("ROT"))
            {
                JointType = MotionType.Rotate;
            }
        }

        public void SetMotionType(MotionType motionType)
        {
            JointType = motionType;
        }

        public void SetControlType()
        {
            if (Coding.StartsWith('P') || Coding.StartsWith('V'))
            {
                ControlType = MotionSource.None;
            }
            else if (Coding.StartsWith('Z'))
            {
                ControlType = MotionSource.Position;
            }
        }

        public void SetControlType(MotionSource controlType)
        {
            ControlType = controlType;
        }



        public static MotionSource ParseMotionSource(string input)
        {
            if (Enum.TryParse<MotionSource>(input, ignoreCase: true, out var result))
            {
                return result;
            }
            return MotionSource.None;

        }

        public static MotionType ParseMotionType(string input)
        {
            if (Enum.TryParse<MotionType>(input, ignoreCase: true, out var result))
            {
                return result;
            }
            return MotionType.Translate;

        }




        //===================================================================================================================
        // A D D I T I O N A L S :   C O N S T A N T S ,   D E F I N E S ,   E T C .
        //===================================================================================================================



    }




    public static class JointTypeEnumValues
    {
        public static Array All => Enum.GetValues(typeof(MotionType));
    }

    public static class MotionSourceEnumValues
    {
        public static Array All => Enum.GetValues(typeof(MotionSource));
    }
}
