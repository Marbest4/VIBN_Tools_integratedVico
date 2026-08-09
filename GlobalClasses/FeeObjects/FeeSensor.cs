using FS.SDK;
using FS.SDK.Mathematics;
using FS.SDK.Scene.Objects;
using FS.SDK.Utilities;
using System.Xml.Linq;
using VIBN_Tools.CAD_Wizard;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeSensor : FeeAbstractObject, IAssignableSimObject, ICadWizardCreatable<FeeSensor>, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================


        private bool _detectPayload;
        public bool DetectPayload
        {
            get => _detectPayload;
            set => SetPropertyChange(ref _detectPayload, value);
        }

        private bool _detectDetectionFlag;
        public bool DetectDetectionFlag
        {
            get => _detectDetectionFlag;
            set => SetPropertyChange(ref _detectDetectionFlag, value);
        }

        private bool _detectBumper;
        public bool DetectBumper
        {
            get => _detectBumper;
            set => SetPropertyChange(ref _detectBumper, value);
        }

        private bool _detectTag;
        public bool DetectTag
        {
            get => _detectTag;
            set => SetPropertyChange(ref _detectTag, value);
        }

        private bool _detectMark;
        public bool DetectMark
        {
            get => _detectMark;
            set => SetPropertyChange(ref _detectMark, value);
        }

        private string _markToDetect;
        public string MarkToDetect
        {
            get => _markToDetect;
            set => SetPropertyChange(ref _markToDetect, value);
        }


        private SafetySensorType _sensorType;
        public SafetySensorType SensorType
        {
            get => _sensorType;
            set => SetPropertyChange(ref _sensorType, value);
        }

        //private bool _isDetecting;
        //public bool IsDetecting
        //{
        //    get => _isDetecting;
        //    set => SetPropertyChange(ref _isDetecting, value);
        //}





        // Needed for CAD Wizard
        public string Coding { get; set; }
        public Guid CadDecoGuid { get; set; }


        // Need for Container Generation
        public ISimObjectFindOrSelect AssignedContainer { get; set; }




        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeSensor()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(SafetySensor);
            Visible = true;
        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SceneObject.Transform.Rotation), Rotation, nameof(SceneObject.Transform));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SafetySensor.DetectPayload), true);
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SafetySensor.DetectDetectionFlag), true);
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, "SafetySensorType", nameof(SafetySensorType.NonEquivalent));

            return true;
        }



        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

            DetectPayload = (bool?)xElement.Element("DetectPayload") ?? false;
            DetectDetectionFlag = (bool?)xElement.Element("DetectDetectionFlag") ?? false;
            DetectBumper = (bool?)xElement.Element("DetectBumper") ?? false;
            DetectTag = (bool?)xElement.Element("DetectTag") ?? false;

            DetectMark = (bool?)xElement.Element("UseDetectMark") ?? false;
            MarkToDetect = (string?)xElement.Element("DetectMark") ?? String.Empty;

            SensorType = Enum.TryParse<SafetySensorType>((string?)xElement.Element("SafetySensorType") ?? String.Empty, out var parsedType) ? parsedType : SafetySensorType.NonEquivalent;
        }


        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            // Check connected slots
            Guid guid;
            var slots = await Services.ApiInstance.Object.GetSlotsAsync(Guid);
            var slotConnectionCh1 = Slots.TryGetValue("Channel1", out guid) && guid != Guid.Empty;
            var slotConnectionCh2 = Slots.TryGetValue("Channel2", out guid) && guid != Guid.Empty;

            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;

            if (!slotConnectionCh1 && !slotConnectionCh2)
                PlausibilityIssues.Add(new PlausibilityIssue($"Weder Slot 'Channel1' noch Slot 'Channel2' verbunden", Severity.Error));

            if (!DetectPayload && !DetectDetectionFlag)
                PlausibilityIssues.Add(new PlausibilityIssue($"Weder 'DetectPayload' noch 'DetectDetectionFlag' ist aktiv", Severity.Error));

            if (DetectMark && String.IsNullOrEmpty(MarkToDetect))
                PlausibilityIssues.Add(new PlausibilityIssue($"'UseDetectMark' ist aktiv, aber 'DetectMark' fehlt", Severity.Error));

            if (maxOneZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Warning));
            else if (twoOrMoreZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Error));


            if (SensorType == SafetySensorType.Equivalent)
                PlausibilityIssues.Add(new PlausibilityIssue($"Sensor ist äquivalent", Severity.Warning));

        }



        //===================================================================================================================
        // M A N U A L   C O N T R O L
        //===================================================================================================================

        // Is Detecting is overwritten by simulation value

        //public async Task SetDetection()
        //{
        //    var guids = new[] { Guid, Guid };
        //    var names = new[] { "Channel1", "Channel2"};
        //    var values = new object[] { IsDetecting, !IsDetecting };

        //    await Services.ApiInstance.Object.SetSlotValuesAsync(guids, names, values);

        //}




        //===================================================================================================================
        // M E T H O D S   ( C A D - W I Z A R D )
        //===================================================================================================================

        public static FeeSensor? CadWizardFactory(string name, Vector3 position, Vector3 rotation, Guid cadDecoGuid)
        {
            foreach (var coding in SensorCodings.AllCodings)
            {
                if (name.StartsWith(coding))
                {
                    var sensor = new FeeSensor()
                    {
                        Name = name.Substring(coding.Length + 1),
                        Coding = coding,
                        Position = position,
                        Rotation = rotation,
                        Scale = new Vector3(0.03f, 0.03f, 0.1f),
                        CadDecoGuid = cadDecoGuid,
                    };

                    return sensor;
                }
            }
            return null;
        }


        public async Task<bool> CadWizardCreateAndSendAsync()
        {
            if (Coding == CodingSensor)
            {
                await base.CreateAsync();

                await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SceneObject.Transform.Rotation), Rotation, nameof(SceneObject.Transform));
                await Services.ApiInstance.Object.SetPropertyAsync(Guid, "SafetySensorType", "NonEquivalent");
                await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SafetySensor.DetectPayload), true);
                await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SafetySensor.DetectDetectionFlag), true);

                Services.ApiInstance.Object.Send(Guid);
                await Services.ApiInstance.Object.WaitForSceneObjectAsync(Guid.ToString());

                // Remove Coding from Deco Object
                Services.ApiInstance.Object.CreateObject(nameof(Decoration), CadDecoGuid);

                await Services.ApiInstance.Object.SetPropertyAsync(CadDecoGuid, nameof(SceneObject.Name), Name);
                Services.ApiInstance.Object.Send(CadDecoGuid);
                await Services.ApiInstance.Object.WaitForSceneObjectAsync(CadDecoGuid.ToString());
            }
            else if (Coding == CodingLightBeam)
            {
                // Create Object
                Services.ApiInstance.Object.CreateObject(nameof(Decoration), CadDecoGuid);

                // Replace Object to SafetySensor
                await Services.ApiInstance.Object.ReplaceObjectAsync(CadDecoGuid.ToString(), nameof(SafetySensor));

                // Set Properties
                await Services.ApiInstance.Object.SetPropertyAsync(CadDecoGuid, nameof(SceneObject.Name), Name);
                await Services.ApiInstance.Object.SetPropertyAsync(CadDecoGuid, nameof(SafetySensor.DetectPayload), true);
                await Services.ApiInstance.Object.SetPropertyAsync(CadDecoGuid, nameof(SafetySensor.DetectDetectionFlag), true);
                await Services.ApiInstance.Object.SetPropertyAsync(CadDecoGuid, "SafetySensorType", "NonEquivalent");

                Services.ApiInstance.Object.Send(CadDecoGuid);
                await Services.ApiInstance.Object.WaitForSceneObjectAsync(CadDecoGuid.ToString());

                // Exchange Guid with the old CadDecoGuid, because object was replaced
                this.Guid = CadDecoGuid;

            }

            return true;
        }








        //===================================================================================================================
        // A D D I T I O N A L S :   C O N S T A N T S ,   D E F I N E S ,   E T C .
        //===================================================================================================================

        public const string CodingSensor = "SENS";
        public const string CodingLightBeam = "BEAM";
    }



    public static class SensorTypeEnumValues
    {
        public static Array All => Enum.GetValues(typeof(SafetySensorType));
    }




}
