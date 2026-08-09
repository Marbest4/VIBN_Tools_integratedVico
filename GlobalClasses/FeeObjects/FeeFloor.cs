using FS.SDK.Scene.Objects;
using System.Xml.Linq;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeFloor : FeeAbstractObject, IAssignableSimObject, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================


        private bool _useCollisionSlot;
        public bool UseCollisionSlot
        {
            get => _useCollisionSlot;
            set => SetPropertyChange(ref _useCollisionSlot, value);
        }


        private bool _isCollisionActive;
        public bool IsCollisionActive
        {
            get => _isCollisionActive;
            set => SetPropertyChange(ref _isCollisionActive, value);
        }





        // Need for Container Generation
        public ISimObjectFindOrSelect AssignedContainer { get; set; }


        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeFloor()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(Floor);
            Visible = true;

        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(Floor.CollisionSlot), UseCollisionSlot);

            return true;
        }


        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

            UseCollisionSlot = (bool?)xElement.Element("CollisionSlot") ?? false;
        }

        public override void ApplyBatchData(FeePropertyBatchData data)
        {
            base.ApplyBatchData(data);
        }



        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {

            // Check connected slots
            Guid guid;
            var connectedCollisionSlot = Slots.TryGetValue("Collision", out guid) && guid != Guid.Empty;

            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;

            if (!UseCollisionSlot)
                PlausibilityIssues.Add(new PlausibilityIssue($"Collision Slot ist nicht aktiviert", Severity.Warning));

            if (UseCollisionSlot && !connectedCollisionSlot)
                PlausibilityIssues.Add(new PlausibilityIssue($"Collision Slot aktiv, aber nicht verbunden", Severity.Error));

            if (maxOneZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Warning));
            else if (twoOrMoreZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Error));

        }

        //===================================================================================================================
        // M A N U A L   C O N T R O L
        //===================================================================================================================

        public async Task SetCollision()
        {
            var guids = new[] { Guid };
            var names = new[] { "Collision" };
            var values = new object[] { IsCollisionActive};

            await Services.ApiInstance.Object.SetSlotValuesAsync(guids, names, values);

        }


    }
}
