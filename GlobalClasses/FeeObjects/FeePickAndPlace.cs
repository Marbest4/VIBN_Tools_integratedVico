using FS.SDK.Components;
using FS.SDK.Mathematics;
using FS.SDK.Scene.Objects;
using System.Xml.Linq;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeePickAndPlace : FeeAbstractObject, IAssignableSimObject, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================



        public List<string> PickMarks { get; set; }
        public List<string> DropMarks { get; set; }


        private string _pickMarksString;
        public string PickMarksString
        {
            get => string.Join(";", PickMarks);
            set
            {
                PickMarks = value.Split(";").ToList();
                SetPropertyChange(ref _pickMarksString, value);
            }
        }

        private string _dropMarksString;
        public string DropMarksString
        {
            get => string.Join(";", DropMarks);
            set
            {
                DropMarks = value.Split(";").ToList();
                SetPropertyChange(ref _dropMarksString, value);
            }
        }

        private float _pickRange;
        public float PickRange
        {
            get => _pickRange;
            set => SetPropertyChange(ref _pickRange, value);
        }

        private float _dropRange;
        public float DropRange
        {
            get => _dropRange;
            set => SetPropertyChange(ref _dropRange, value);
        }


        private bool _isPick;
        public bool IsPick
        {
            get => _isPick;
            set
            {
                if (SetPropertyChange(ref _isPick, value))
                {
                    if (value)
                        IsDrop = false;
                }
            }
        }

        private bool _isDrop;
        public bool IsDrop
        {
            get => _isDrop;
            set
            {
                if (SetPropertyChange(ref _isDrop, value))
                {
                    if (value)
                        IsPick = false;
                }
            }
        }



        // Pick / Drop Offset
        public bool UsePickOffset_PosX { get; set; }
        public bool UsePickOffset_PosY { get; set; }
        public bool UsePickOffset_PosZ { get; set; }
        public bool UsePickOffset_RotX { get; set; }
        public bool UsePickOffset_RotY { get; set; }
        public bool UsePickOffset_RotZ { get; set; }

        public bool UseDropOffset_PosX { get; set; }
        public bool UseDropOffset_PosY { get; set; }
        public bool UseDropOffset_PosZ { get; set; }
        public bool UseDropOffset_RotX { get; set; }
        public bool UseDropOffset_RotY { get; set; }
        public bool UseDropOffset_RotZ { get; set; }

        public Vector3 PickPositionOffset { get; set; }
        public Vector3 PickRotationOffset { get; set; }
        public Vector3 DropPositionOffset { get; set; }
        public Vector3 DropRotationOffset { get; set; }




        // Needed for Container Generation
        public ISimObjectFindOrSelect AssignedContainer { get; set; }



        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeePickAndPlace()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(PickAndPlace);
            Visible = true;

            PickMarks = new List<string>();
            DropMarks = new List<string>();
        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(PickAndPlaceComponent.MarkToPick), string.Join(";", PickMarks), nameof(PickAndPlaceComponent));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(PickAndPlaceComponent.MarkOfDropPlaces), string.Join(";", DropMarks), nameof(PickAndPlaceComponent));

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(PickAndPlaceComponent.MaxDistanceToPick), PickRange, nameof(PickAndPlaceComponent));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(PickAndPlaceComponent.MaxDistanceToPlace), DropRange, nameof(PickAndPlaceComponent));

            return true;
        }


        public bool SetPickMark(string pickMark)
        {
            Services.ApiInstance.Object.CreateObject(nameof(PickAndPlace), Guid);
            Services.ApiInstance.Object.SetProperty(Guid, nameof(PickAndPlaceComponent.MarkToPick), pickMark, nameof(PickAndPlaceComponent));
            Services.ApiInstance.Object.Send(Guid);

            return true;
        }

        public bool SetDropMark(string dropMark)
        {
            Services.ApiInstance.Object.CreateObject(nameof(PickAndPlace), Guid);
            Services.ApiInstance.Object.SetProperty(Guid, nameof(PickAndPlaceComponent.MarkOfDropPlaces), dropMark, nameof(PickAndPlaceComponent));
            Services.ApiInstance.Object.Send(Guid);
            return true;
        }


        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

            var pickPlaceComponent = xElement.Element("PickAndPlaceComponent");

            PickMarks = ((string?)pickPlaceComponent.Element("MarkToPick") ?? String.Empty).Split(";").ToList();
            DropMarks = ((string?)pickPlaceComponent.Element("MarkOfDropPlaces") ?? String.Empty).Split(";").ToList();

            PickRange = (float?)pickPlaceComponent.Element("MaxDistanceToPick") ?? 0;
            DropRange = (float?)pickPlaceComponent.Element("MaxDistanceToPlace") ?? 0;

            UsePickOffset_PosX = (bool?)pickPlaceComponent.Element("PickPosOffsetX") ?? false;
            UsePickOffset_PosY = (bool?)pickPlaceComponent.Element("PickPosOffsetY") ?? false;
            UsePickOffset_PosZ = (bool?)pickPlaceComponent.Element("PickPosOffsetZ") ?? false;
            UsePickOffset_RotX = (bool?)pickPlaceComponent.Element("PickRotOffsetX") ?? false;
            UsePickOffset_RotY = (bool?)pickPlaceComponent.Element("PickRotOffsetY") ?? false;
            UsePickOffset_RotZ = (bool?)pickPlaceComponent.Element("PickRotOffsetZ") ?? false;

            UseDropOffset_PosX = (bool?)pickPlaceComponent.Element("DropPosOffsetX") ?? false;
            UseDropOffset_PosY = (bool?)pickPlaceComponent.Element("DropPosOffsetY") ?? false;
            UseDropOffset_PosZ = (bool?)pickPlaceComponent.Element("DropPosOffsetZ") ?? false;
            UseDropOffset_RotX = (bool?)pickPlaceComponent.Element("DropRotOffsetX") ?? false;
            UseDropOffset_RotY = (bool?)pickPlaceComponent.Element("DropRotOffsetY") ?? false;
            UseDropOffset_RotZ = (bool?)pickPlaceComponent.Element("DropRotOffsetZ") ?? false;

            PickPositionOffset = ParseVector3(pickPlaceComponent.Element("PickPositionOffset"));
            PickRotationOffset = ParseVector3(pickPlaceComponent.Element("PickRotationOffset"));
            DropPositionOffset = ParseVector3(pickPlaceComponent.Element("DropPositionOffset"));
            DropRotationOffset = ParseVector3(pickPlaceComponent.Element("DropRotationOffset"));

        }

        public override void ApplyBatchData(FeePropertyBatchData data)
        {
            base.ApplyBatchData(data);

            if (data.IsPick is bool pick)
                IsPick = pick;

            if (data.IsDrop is bool drop)
                IsDrop = drop;

        }


        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            // Check connected slots
            Guid guid;
            var connectedPick = Slots.TryGetValue("Pick", out guid) && guid != Guid.Empty;
            var connectedDrop = Slots.TryGetValue("Drop", out guid) && guid != Guid.Empty;

            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;

            if (!connectedPick)
                PlausibilityIssues.Add(new PlausibilityIssue($"Pick Slot nicht verunden", Severity.Error));

            if (!connectedDrop)
                PlausibilityIssues.Add(new PlausibilityIssue($"Drop Slot nicht verbunden", Severity.Error));

            if (PickMarks.Count == 0)
                PlausibilityIssues.Add(new PlausibilityIssue($"Keine Pick Marks vergeben", Severity.Error));

            if (DropMarks.Count == 0)
                PlausibilityIssues.Add(new PlausibilityIssue($"Keine Drop Marks vergeben", Severity.Error));

            if (Math.Abs(PickRange - 1f) == 0f)
                PlausibilityIssues.Add(new PlausibilityIssue($"Pick Range ist Default-Wert: {PickRange}", Severity.Warning));

            if (Math.Abs(DropRange - 1f) == 0f)
                PlausibilityIssues.Add(new PlausibilityIssue($"Drop Range ist Default-Wert: {DropRange}", Severity.Warning));

            if (maxOneZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Warning));
            else if (twoOrMoreZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Error));


        }

        //===================================================================================================================
        // M E T H O D S   ( M A N U A L - M O D E )
        //===================================================================================================================

        public async Task SetPickDropAsync()
        {

            var guids = new[] { Guid, Guid };
            var names = new[] { "Pick", "Drop" };
            var values = new object[] { IsPick, IsDrop };

            await Services.ApiInstance.Object.SetSlotValuesAsync(guids, names, values);
        }




    }
}
