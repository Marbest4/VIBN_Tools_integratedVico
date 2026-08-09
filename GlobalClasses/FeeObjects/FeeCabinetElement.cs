using FS.SDK.Mathematics;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeCabinetElement;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeCabinetElement : FeeAbstractObject, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================


        public string ElementType { get; set; }


        private string _Label;
        public string Label
        {
            get => _Label;
            set => SetPropertyChange(ref _Label, value);
        }

        private string _toolTip;
        public string Tooltip
        {
            get => _toolTip;
            set => SetPropertyChange(ref _toolTip, value);
        }

        private float _positionX;
        public float PositionX
        {
            get => _positionX;
            set => SetPropertyChange(ref _positionX, value);
        }

        private float _positionY;
        public float PositionY
        {
            get => _positionY;
            set => SetPropertyChange(ref _positionY, value);
        }



        public CabinetElementAssignmentInfo AssignmentInfo { get; set; } = new CabinetElementAssignmentInfo();



        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeCabinetElement()
        {
            Guid = Guid.NewGuid();
            FeeType = "CabinetElement";
            Visible = false;
        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public async Task<bool> CreateAndSendAsync()
        {

            Services.ApiInstance.Object.CreateObject(FeeType, Guid);

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, "Definition", ElementType);
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, "Name", Name);
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, "Label", Label);
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, "Tooltip", Tooltip);
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, "IsComponentActive", Visible, "Model");

            Services.ApiInstance.Object.Send(Guid);
            await Services.ApiInstance.Object.WaitForSceneObjectAsync(Guid.ToString());

            await Services.ApiInstance.Object.AddChildToParentAsync(Parent.Guid, Guid);

            // Set local Position after reparent
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, "LocalPosition", ConvertFromPixelPosition(PositionX, PositionY), "Transform");
            Services.ApiInstance.Object.Send(Guid);

            return true;

        }



        /// <summary>
        /// Function reads the CabinetElement-Type and connects its slots to the corresponding InterfaceSignal, if there is AssignmentInfo information
        /// Returns true when finished
        /// </summary>
        /// <returns></returns>
        public async Task<bool> AssignSignalsToElementAsync()
        {

            if (CabinetElementSlotMapping.SlotMapping.TryGetValue(ElementType, out var assignments))
            {
                foreach (var kvp in assignments)
                {
                    var property = typeof(CabinetElementAssignmentInfo).GetProperty(kvp.Key);
                    var signal = property?.GetValue(AssignmentInfo) as FeeInterfaceSignal;

                    if (signal != null)
                    {
                        await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Guid, kvp.Value, signal.Guid, false);
                    }
                }
            }

            return true;
        }


        public Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            throw new NotImplementedException();
        }




        /// <summary>
        /// Function converts the Pixel Position of a CebinetElement (displayed in the CabinetManager) into a Vector3 value
        /// which can be used for the LocalPosition property
        /// </summary>
        /// <param name="PositionX"></param>
        /// <param name="PositionY"></param>
        /// <returns></returns>
        private Vector3 ConvertFromPixelPosition(float PositionX, float PositionY)
        {
            Vector3 LocalPosition = new();

            LocalPosition.X = 0.06f - (0.0005f * PositionX);
            LocalPosition.Y = 0.0f;
            LocalPosition.Z = 1.571429f - (0.00142858f * PositionY);


            return LocalPosition;
        }





        //===================================================================================================================
        // A D D I T I O N A L S :   C O N S T A N T S ,   D E F I N E S ,   E T C .
        //===================================================================================================================
        public readonly struct CabinetElementType
        {
            public const string Button = "Grob_Button";
            public const string EStop = "Grob_NotAus";
            public const string Fuse = "Fuse";
            public const string PositionSwitch2 = "Grob_2PositionSwitch";
            public const string PositionSwitch3 = "Grob_3PositionSwitch";
            public const string MainSwitch = "MainSwitch";
            public const string UnsignedInterInput = "Unsigned Integer Numeric Input";
            public const string RealInput = "Real Numeric Input";
            public const string TextInput = "TextInput";
            public const string TextDisplay = "TextDisplay";

            public const string LampBlue = "Lamp Blue";
            public const string LampGreen = "Lamp Green";
            public const string LampYellow = "Lamp Yellow";
            public const string LampRed = "Lamp Red";

            public CabinetElementType()
            {
            }
        }



        public struct CabinetElementPaths
        {
            public const string TwoPositionSwitch = "\\CabinetDefinitions\\Grob_2PositionSwitch.xml";
            public const string ThreePositionSwitch = "\\CabinetDefinitions\\Grob_3PositionSwitch.xml";
            public const string NotAus = "\\CabinetDefinitions\\Grob_NotAus.xml";
            public const string Button = "\\CabinetDefinitions\\Grob_Button.xml";
        }
    }



    public class CabinetElementAssignmentInfo
    {

        // Properties to store Assignment Information for specific slots

        // Button
        public FeeInterfaceSignal Button_NO1 { get; set; } = null;
        public FeeInterfaceSignal Button_NO2 { get; set; } = null;
        public FeeInterfaceSignal Button_NC1 { get; set; } = null;
        public FeeInterfaceSignal Button_NC2 { get; set; } = null;

        public FeeInterfaceSignal Button_LT_R { get; set; } = null;
        public FeeInterfaceSignal Button_LT_G { get; set; } = null;
        public FeeInterfaceSignal Button_LT_B { get; set; } = null;

        // Fuse
        public FeeInterfaceSignal Fuse_NC { get; set; } = null;
        public FeeInterfaceSignal Fuse_NO { get; set; } = null;

        // EStop
        public FeeInterfaceSignal EStop_NC1 { get; set; } = null;
        public FeeInterfaceSignal EStop_NC2 { get; set; } = null;
        public FeeInterfaceSignal EStop_NO1 { get; set; } = null;
        public FeeInterfaceSignal EStop_NO2 { get; set; } = null;

        // 2 Position Switch
        public FeeInterfaceSignal TwoPosSwitch_NC1 { get; set; } = null;
        public FeeInterfaceSignal TwoPosSwitch_NC2 { get; set; } = null;
        public FeeInterfaceSignal TwoPosSwitch_NO1 { get; set; } = null;
        public FeeInterfaceSignal TwoPosSwitch_NO2 { get; set; } = null;

        // 3 Position Switch
        public FeeInterfaceSignal ThreePosSwitch_NO11 { get; set; } = null;
        public FeeInterfaceSignal ThreePosSwitch_NO12 { get; set; } = null;
        public FeeInterfaceSignal ThreePosSwitch_NC11 { get; set; } = null;
        public FeeInterfaceSignal ThreePosSwitch_NC12 { get; set; } = null;
        public FeeInterfaceSignal ThreePosSwitch_NO21 { get; set; } = null;
        public FeeInterfaceSignal ThreePosSwitch_NO22 { get; set; } = null;

        // Lamp / LED
        public FeeInterfaceSignal LED_ON { get; set; } = null;

        // Main Switch
        public FeeInterfaceSignal MainSwitch_NC { get; set; } = null;
        public FeeInterfaceSignal MainSwitch_NO { get; set; } = null;

        // Unsigned Integer Input
        public FeeInterfaceSignal UnsignedInterInput_InputBox { get; set; } = null;

        // Real Inout
        public FeeInterfaceSignal RealInput_InputBox { get; set; } = null;

        // Text Input
        public FeeInterfaceSignal TextInput_InputBox { get; set; } = null;

        // Text Display
        public FeeInterfaceSignal TextDisplay_InputBox { get; set; } = null;


    }


    public static class CabinetElementSlotMapping
    {
        public static readonly Dictionary<string, Dictionary<string, string>> SlotMapping = new Dictionary<string, Dictionary<string, string>>
        {
            [CabinetElementType.Button] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.Button_NC1), "NC1" },
                { nameof(CabinetElementAssignmentInfo.Button_NC2), "NC2" },
                { nameof(CabinetElementAssignmentInfo.Button_NO1), "NO1" },
                { nameof(CabinetElementAssignmentInfo.Button_NO2), "NO2" },
                { nameof(CabinetElementAssignmentInfo.Button_LT_R), "LT_R" },
                { nameof(CabinetElementAssignmentInfo.Button_LT_G), "LT_G" },
                { nameof(CabinetElementAssignmentInfo.Button_LT_B), "LT_B" },
            },

            [CabinetElementType.EStop] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.EStop_NC1), "NC1" },
                { nameof(CabinetElementAssignmentInfo.EStop_NC2), "NC2" },
                { nameof(CabinetElementAssignmentInfo.EStop_NO1), "NO1" },
                { nameof(CabinetElementAssignmentInfo.EStop_NO2), "NO2" },
            },

            [CabinetElementType.PositionSwitch2] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.TwoPosSwitch_NC1), "NC1" },
                { nameof(CabinetElementAssignmentInfo.TwoPosSwitch_NC2), "NC2" },
                { nameof(CabinetElementAssignmentInfo.TwoPosSwitch_NO1), "NO1" },
                { nameof(CabinetElementAssignmentInfo.TwoPosSwitch_NO2), "NO2" },
            },

            [CabinetElementType.PositionSwitch3] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.ThreePosSwitch_NC11), "NC11" },
                { nameof(CabinetElementAssignmentInfo.ThreePosSwitch_NC12), "NC12" },
                { nameof(CabinetElementAssignmentInfo.ThreePosSwitch_NO11), "NO11" },
                { nameof(CabinetElementAssignmentInfo.ThreePosSwitch_NO12), "NO12" },
                { nameof(CabinetElementAssignmentInfo.ThreePosSwitch_NO21), "NO21" },
                { nameof(CabinetElementAssignmentInfo.ThreePosSwitch_NO22), "NO22" },
            },

            [CabinetElementType.Fuse] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.Fuse_NC), "NC" },
                { nameof(CabinetElementAssignmentInfo.Fuse_NO), "NO" },
            },

            [CabinetElementType.MainSwitch] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.MainSwitch_NC), "NC" },
                { nameof(CabinetElementAssignmentInfo.MainSwitch_NO), "NO" },
            },

            [CabinetElementType.LampBlue] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.LED_ON), "ON" },
            },

            [CabinetElementType.LampGreen] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.LED_ON), "ON" },
            },

            [CabinetElementType.LampYellow] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.LED_ON), "ON" },
            },

            [CabinetElementType.LampRed] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.LED_ON), "ON" },
            },

            [CabinetElementType.UnsignedInterInput] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.UnsignedInterInput_InputBox), "InputBox" },
            },

            [CabinetElementType.RealInput] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.RealInput_InputBox), "InputBox" },
            },

            [CabinetElementType.TextInput] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.TextInput_InputBox), "InputBox" },
            },

            [CabinetElementType.TextDisplay] = new Dictionary<string, string>
            {
                { nameof(CabinetElementAssignmentInfo.TextDisplay_InputBox), "InputBox" },
            },
        };
    }
}
