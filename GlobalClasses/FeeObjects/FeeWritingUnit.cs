using FS.SDK.Io;
using ReadingUnitPlugin.Component;
using ReadingUnitPlugin.SO;
using System.Xml.Linq;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeWritingUnit : FeeAbstractObject, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   P R O P E R T I E S
        //===================================================================================================================


        private string _markToDetect;
        public string MarkToDetect
        {
            get => _markToDetect;
            set => SetPropertyChange(ref _markToDetect, value);
        }

        private bool _detectMark;
        public bool DetectMark
        {
            get => _detectMark;
            set => SetPropertyChange(ref _detectMark, value);
        }


        public Dictionary<string, IOType> UdtDefinition { get; set; }
        public string UdtDefinitionString => string.Join("\n", UdtDefinition.Select(x => $"{x.Key}, {x.Value}"));


        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeWritingUnit()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(WritingUnitUdt);
            Visible = true;

            UdtDefinition = new Dictionary<string, IOType>();
        }





        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(ReadingUnitUdt.DetectByMark), DetectMark);
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(ReadingUnitUdt.MarkToDetect), MarkToDetect);

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(UDTComponent.ReadWriteTypes), UdtDefinition, nameof(UDTComponent));

            return true;
        }


        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

            DetectMark = (bool?)xElement.Element("UseDetectMark") ?? false;
            MarkToDetect = (string?)xElement.Element("DetectMark") ?? String.Empty;

            UdtDefinition = xElement.Element("UDT").Descendants("Entry")
                .ToDictionary(
                    e => (string)e.Element("String"),
                    e => FeeInterfaceSignal.ParseIOType((string)e.Element("IOType"))
                    );

        }

        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            // Check connected slots
            Guid guid;
            var doWriteConnected = Slots.TryGetValue("DoWrite", out guid) && guid != Guid.Empty;

            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;

            if (!doWriteConnected)
                PlausibilityIssues.Add(new PlausibilityIssue($"Slot 'DoWrite' nicht verknüpft", Severity.Error));

            if (DetectMark && String.IsNullOrEmpty(MarkToDetect))
                PlausibilityIssues.Add(new PlausibilityIssue($"'DetectByMark' ist aktiv, aber 'MarkToDetect' fehlt", Severity.Error));

            if (UdtDefinition.Count == 0)
                PlausibilityIssues.Add(new PlausibilityIssue($"Keine UDT Definition hinterlegt", Severity.Error));

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
