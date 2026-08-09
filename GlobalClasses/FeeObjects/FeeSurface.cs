using FS.SDK.Components;
using FS.SDK.Mathematics;
using FS.SDK.Scene.Objects;
using System.Xml.Linq;
using VIBN_Tools.CAD_Wizard;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeSurface : FeeAbstractObject, IAssignableSimObject, ICadWizardCreatable<FeeSurface>, IPlausibilityCheck
    {
        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================


        private bool _manualModeActive;
        public bool ManualModeActive
        {
            get => _manualModeActive;
            set => SetPropertyChange(ref _manualModeActive, value);
        }

        private float _staticFriction;
        public float StaticFriction
        {
            get => _staticFriction;
            set => SetPropertyChange(ref _staticFriction, value);
        }


        private float _kineticFriction;
        public float KineticFriction
        {
            get => _kineticFriction;
            set => SetPropertyChange(ref _kineticFriction, value);
        }

        private float _isActualVelocityX;
        public float IsActualVelocityX
        {
            get => _isActualVelocityX;
            set => SetPropertyChange(ref _isActualVelocityX, value);
        }

        private float _isActualVelocityY;
        public float IsActualVelocityY
        {
            get => _isActualVelocityY;
            set => SetPropertyChange(ref _isActualVelocityY, value);
        }

        private float _isActualVelocityZ;
        public float IsActualVelocityZ
        {
            get => _isActualVelocityZ;
            set => SetPropertyChange(ref _isActualVelocityZ, value);
        }






        // Needed for CAD Wizard
        public string Coding { get; set; }
        public Guid CadDecoGuid { get; set; }


        // Need for Container Generation
        public ISimObjectFindOrSelect AssignedContainer { get; set; }






        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeSurface()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(Surface);
            Visible = true;
            KineticFriction = 0.4f;
            StaticFriction = 0.4f;
        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            Services.ApiInstance.Object.SetProperty(Guid, nameof(ColliderComponent.StaticFriction), StaticFriction, nameof(ColliderComponent));
            Services.ApiInstance.Object.SetProperty(Guid, nameof(ColliderComponent.KineticFriction), KineticFriction, nameof(ColliderComponent));

            return true;
        }

        public bool SetFriction(int staticFriction, int kineticFriction)
        {
            // Create object
            Services.ApiInstance.Object.CreateObject(FeeType, Guid);
            // Set Properties
            Services.ApiInstance.Object.SetProperty(Guid, nameof(ColliderComponent.StaticFriction), staticFriction, nameof(ColliderComponent));
            Services.ApiInstance.Object.SetProperty(Guid, nameof(ColliderComponent.KineticFriction), kineticFriction, nameof(ColliderComponent));

            Services.ApiInstance.Object.Send(Guid);

            return true;
        }




        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

            StaticFriction = (float?)xElement.Element("Collider").Element("StaticFriction") ?? 0f;
            KineticFriction = (float?)xElement.Element("Collider").Element("KineticFriction") ?? 0f;

        }

        public override void ApplyBatchData(FeePropertyBatchData data)
        {
            base.ApplyBatchData(data);

            if (data.SurfaceManualModeActive is bool manual)
                ManualModeActive = manual;

            if (data.IsActualVelocityX is float velX)
                IsActualVelocityX = velX;

            if (data.IsActualVelocityY is float velY)
                IsActualVelocityY = velY;

            if (data.IsActualVelocityZ is float velZ)
                IsActualVelocityZ = velZ;


        }



        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            // Check connected slots
            Guid guid;
            var connectedVelocityX = Slots.TryGetValue("InVelocityX", out guid) && guid != Guid.Empty;

            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;

            if (!connectedVelocityX)
                PlausibilityIssues.Add(new PlausibilityIssue($"Slot 'InVelocityX' ist nicht verbunden", Severity.Error));

            if (maxOneZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Warning));
            else if (twoOrMoreZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Error));


            if (ManualModeActive)
                PlausibilityIssues.Add(new PlausibilityIssue($"Manual Mode ist aktiviert", Severity.Error));

        }


        //===================================================================================================================
        // M A N U A L   C O N T R O L
        //===================================================================================================================

        public async Task SetVelocity()
        {
            var guids = new[] { Guid };
            var names = new[] { "InVelocityX" };
            var values = new object[] { IsActualVelocityX };

            await Services.ApiInstance.Object.SetSlotValuesAsync(guids, names, values);

        }



        //===================================================================================================================
        // M E T H O D S   ( C A D - W I Z A R D )
        //===================================================================================================================

        public static FeeSurface? CadWizardFactory(string name, Vector3 position, Vector3 rotation, Guid cadDecoGuid)
        {
            foreach (var coding in ConveyorCodings.AllCodings)
            {
                if (name.StartsWith(coding))
                {
                    var surface = new FeeSurface()
                    {
                        Name = name.Substring(coding.Length + 1),
                        Coding = coding,
                        Position = position,
                        Rotation = rotation,
                        Scale = new Vector3(0.5f, 0.5f, 0.5f),
                        CadDecoGuid = cadDecoGuid,
                    };

                    return surface;
                }
            }
            return null;
        }





        //===================================================================================================================
        // A D D I T I O N A L S :   C O N S T A N T S ,   D E F I N E S ,   E T C .
        //===================================================================================================================


    }
}
