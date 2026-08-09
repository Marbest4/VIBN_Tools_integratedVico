using FS.SDK.Scene.Objects;
using System.Xml.Linq;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;
using static VIBN_Tools.GlobalClasses.Services;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeDetectionFlag : FeeAbstractObject, IPlausibilityCheck
    {
        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================


        // DetectionFlag is Workpiece Templatea
        private int _numberOfColliders;
        public int NumberOfColliders
        {
            get => _numberOfColliders;
            set => SetPropertyChange(ref _numberOfColliders, value);
        }



        public bool IsWorkpiece => Parent is FeeBasicFrame frame && frame.Name == "Workpieces";



        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeDetectionFlag()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(DetectFlag);
            Visible = true;
        }





        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            return true;
        }


        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);            
        }

        public override void ApplyBatchData(FeePropertyBatchData data)
        {
            base.ApplyBatchData(data);
        }



        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;

            if (string.IsNullOrEmpty(MarksString))
                PlausibilityIssues.Add(new PlausibilityIssue($"Kein Mark vergeben", Severity.Warning));

            if (maxOneZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Warning));
            else if (twoOrMoreZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Error));


            if (IsWorkpiece)
            {
                var pickPlacers = newObjects.OfType<FeePickAndPlace>().ToList();

                var success = pickPlacers.Any(x => x.PickMarks.Intersect(Marks).Any());

                if (!success)
                    PlausibilityIssues.Add(new PlausibilityIssue($"Kein PickAndPlacer mit überschneidendem Pick-Mark vorhanden", Severity.Warning));
            }

        }


        protected override void OnParentChanged()
        {
            OnPropertyChanged(nameof(IsWorkpiece));
        }

    }
}
