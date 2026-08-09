using System.Xml.Linq;
using FS.SDK.Scene.Objects;
using NPOI.SS.Formula.Functions;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeInserter : FeeAbstractObject, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   P R O P E R T I E S
        //===================================================================================================================


        private bool _useIndexInsertSlot;
        public bool UseIndexInsertSlot
        {
            get => _useIndexInsertSlot;
            set => SetPropertyChange(ref _useIndexInsertSlot, value);
        }

        private bool _useDropAreaBlockedSlot;
        public bool UseDropAreaBlockedSlot
        {
            get => _useDropAreaBlockedSlot;
            set => SetPropertyChange(ref _useDropAreaBlockedSlot, value);
        }

        private bool _isInsertionEnabled;
        public bool IsInsertionEnabled
        {
            get => _isInsertionEnabled;
            set => SetPropertyChange(ref _isInsertionEnabled, value);
        }

        private string _triggerSource;
        public string TriggerSource
        {
            get => _triggerSource;
            set => SetPropertyChange(ref _triggerSource, value);
        }


        public List<string> TemplateNames { get; set; }
        public bool HasTemplates => TemplateNames.Count > 0;





        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeInserter()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(SequenceInserter);
            Visible = true;
            UseIndexInsertSlot = true;
            TriggerSource = TriggerSources.BySignalOneShot;
            TemplateNames = new List<string>();
        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SequenceInserter.Inserter.UseIndexInsertSlot), UseIndexInsertSlot, nameof(SequenceInserter.Inserter));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SequenceInserter.Inserter.UseDropAreaBlockedSlot), UseDropAreaBlockedSlot, nameof(SequenceInserter.Inserter));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SequenceInserter.Inserter.IsInsertionEnabled), IsInsertionEnabled, nameof(SequenceInserter.Inserter));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SequenceInserter.Inserter.Trigger), TriggerSource, nameof(SequenceInserter.Inserter));

            return true;
        }


        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

            var inserter = xElement.Element("Inserter");

            UseIndexInsertSlot = (bool?)inserter.Element("UseIndexInsertSlot") ?? false;
            UseDropAreaBlockedSlot = (bool?)inserter.Element("UseDropAreaBlockedSlot") ?? false;
            IsInsertionEnabled = (bool?)inserter.Element("IsInsertionEnabled") ?? false;

            TriggerSource = (string?)inserter.Element("Trigger") ?? String.Empty;

            TemplateNames = inserter.Descendants("TemplateSequenceItem").Select(x => (string)x.Element("Name")).ToList();
        }

        public override void ApplyBatchData(FeePropertyBatchData data)
        {
            base.ApplyBatchData(data);
        }



        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {

            // Check connected slots
            Guid guid;
            var insertConnected = Slots.TryGetValue("InsertSignal", out guid) && guid != Guid.Empty;
            var useIndexConnected = Slots.TryGetValue("InsertIndex", out guid) && guid != Guid.Empty;

            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;

            if (!insertConnected)
                PlausibilityIssues.Add(new PlausibilityIssue($"Slot 'InsertSignal' nicht verbunden", Severity.Error));

            if (UseIndexInsertSlot && !useIndexConnected)
                PlausibilityIssues.Add(new PlausibilityIssue($"Index wird verwendet, aber Slot 'InsertIndex' nicht verbunden", Severity.Error));

            if (!HasTemplates)
                PlausibilityIssues.Add(new PlausibilityIssue($"Kein Template hinterlegt", Severity.Error));

            if (!IsInsertionEnabled)
                PlausibilityIssues.Add(new PlausibilityIssue($"'Enable insertion' nicht aktiv", Severity.Error));

            if (TriggerSource == TriggerSources.ManuallyOneShot || TriggerSource == TriggerSources.ManuallyByTime)
                PlausibilityIssues.Add(new PlausibilityIssue($"'Trigger source' ist auf manuell gestellt", Severity.Error));

            if (maxOneZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Warning));
            else if (twoOrMoreZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Error));


        }

        //===================================================================================================================
        // M E T H O D S   ( M A N U A L - M O D E )
        //===================================================================================================================

        public void ManualInsert(bool state)
        {
            Services.ApiInstance.Object.SetSlotValue(Guid, "InsertSignal", state);
            Services.ApiInstance.Object.Send(Guid);
        }


        //===================================================================================================================
        // A D D I T I O N A L S :   C O N S T A N T S ,   D E F I N E S ,   E T C .
        //===================================================================================================================



    }


    public static class TriggerSources
    {
        public const string ManuallyOneShot = "ManuallyOneShot";
        public const string ManuallyByTime = "ManuallyByTime";
        public const string BySignalOneShot = "BySignalOneShot";
        public const string BySignalByTime = "BySignalAndTime";

        public static IEnumerable<string> All =>
            new[]
            {
                ManuallyOneShot,
                ManuallyByTime,
                BySignalOneShot,
                BySignalByTime,
            };

    }




}
