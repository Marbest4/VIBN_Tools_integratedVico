using FS.SDK.Scene.Objects;
using FS.SDK.Utilities;
using System.Xml.Linq;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeButton : FeeAbstractObject, IAssignableSimObject, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================


        private ButtonType _buttonType;
        public ButtonType ButtonType
        {
            get => _buttonType;
            set => SetPropertyChange(ref _buttonType, value);
        }


        private int _latchingTime;
        public int LatchingTime
        {
            get => _latchingTime;
            set => SetPropertyChange(ref _latchingTime, value);
        }


        private bool _isPressed;
        public bool IsPressed
        {
            get => _isPressed;
            set => SetPropertyChange(ref _isPressed, value);
        }





        // Need for Container Generation
        public ISimObjectFindOrSelect AssignedContainer { get; set; }


        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeButton()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(Button);
            Visible = true;

        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(Button.ButtonType), ButtonType);
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(Button.LatchingTime), LatchingTime);

            return true;
        }


        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

            ButtonType = Enum.TryParse<ButtonType>((string)xElement.Element($"ButtonType") ?? string.Empty, out var type) ? type : ButtonType.Latching;
            LatchingTime = (int?)xElement.Element("LatchingTime") ?? 0;
        }

        public override void ApplyBatchData(FeePropertyBatchData data)
        {
            base.ApplyBatchData(data);
        }



        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            // Check connected slots
            Guid guid;
            var pressedConnected = Slots.TryGetValue("Pressed", out guid) && guid != Guid.Empty;
            var pressedInvConnected = Slots.TryGetValue("PressedInverted", out guid) && guid != Guid.Empty;

            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;    

            if (!pressedConnected && !pressedInvConnected)
                PlausibilityIssues.Add(new PlausibilityIssue($"Kein Slot verbunden", Severity.Error));

            if(maxOneZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Warning));
            else if(twoOrMoreZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Error));


        }



        //===================================================================================================================
        // M A N U A L   C O N T R O L
        //===================================================================================================================

        public async Task SetPressReleaseAsync()
        {

            var guids = Enumerable.Repeat(this.Guid, 2).ToArray();
            var names = new[] { "Pressed", "PressedInverted" };
            var values = new object[] { IsPressed, !IsPressed };

            await Services.ApiInstance.Object.SetSlotValuesAsync(guids, names, values);
        }



    }



    //===================================================================================================================
    // A D D I T I O N A L S :   C O N S T A N T S ,   D E F I N E S ,   E T C .
    //===================================================================================================================



    public static class ButtonTypeEnumValues
    {
        public static Array All => Enum.GetValues(typeof(ButtonType));
    }


}
