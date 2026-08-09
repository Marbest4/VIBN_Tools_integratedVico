using FS.SDK.Scene.Objects;
using System.Xml.Linq;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeRemover : FeeAbstractObject, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   P R O P E R T I E S
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

        private bool _isActivationSlotEnabled;

        public bool IsActivationSlotEnabled
        {
            get => _isActivationSlotEnabled;
            set => SetPropertyChange(ref _isActivationSlotEnabled, value);
        }





        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeRemover()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(Remover);
            Visible = true;
        }






        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================
        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(Remover.DetectPayload), true);
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(Remover.DetectDetectionFlag), true);
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(Remover.IsRemovingActive), true);

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

            IsActivationSlotEnabled = (bool?)xElement.Element("ActivationSlot") ?? false;
        }



        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            // Check connected slots
            Guid guid;
            var removeActiveConnected = Slots.TryGetValue("IsRemovingActive", out guid) && guid != Guid.Empty;

            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;

            if (IsActivationSlotEnabled && !removeActiveConnected)
                PlausibilityIssues.Add(new PlausibilityIssue($"Slot 'IsRemovingActive' nicht verbunden", Severity.Error));

            if (!DetectPayload && !DetectDetectionFlag)
                PlausibilityIssues.Add(new PlausibilityIssue($"Weder 'DetectPayload' noch 'DetectDetectionFlag' ist aktiv", Severity.Error));

            if (DetectMark && String.IsNullOrEmpty(MarkToDetect))
                PlausibilityIssues.Add(new PlausibilityIssue($"'UseDetectMark' ist aktiv, aber 'DetectMark' fehlt", Severity.Error));

            if (maxOneZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Warning));
            else if (twoOrMoreZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Error));


        }


        //===================================================================================================================
        // A D D I T I O N A L S :   C O N S T A N T S ,   D E F I N E S ,   E T C .
        //===================================================================================================================







    }
}
