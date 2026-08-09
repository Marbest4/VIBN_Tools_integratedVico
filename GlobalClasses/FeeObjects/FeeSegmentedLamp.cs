using FS.SDK;
using FS.SDK.Components;
using FS.SDK.Scene.Objects;
using FS.SDK.Utilities;
using System.Numerics;
using System.Windows.Media;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeSegmentedLamp : FeeAbstractObject, IAssignableSimObject
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================

        //public int LampCount { get; set; }

        public bool EnableLampRed { get; set; }
        public bool EnableLampYellow { get; set; }
        public bool EnableLampGreen { get; set; }
        public bool EnableLampBlue { get; set; }
        public bool EnableLampWhite { get; set; }

        public List<LampSegmentInformation> SegmentsInformation { get; set; }


        // Need for Container Generation
        public ISimObjectFindOrSelect AssignedContainer { get; set; }



        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeSegmentedLamp()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(SegmentedLamp);
            Visible = true;
            SegmentsInformation = new List<LampSegmentInformation>();
        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            // Define segments and add to segments list with mapping
            var lampMappings = new (bool Enabled, string SlotName, Color Color)[]
            {
                (EnableLampRed,    LogicsStandard.Grob_Stacklight.Slots.Red,    Colors.Red),
                (EnableLampYellow, LogicsStandard.Grob_Stacklight.Slots.Yellow, Colors.Yellow),
                (EnableLampGreen,  LogicsStandard.Grob_Stacklight.Slots.Green,  Colors.Green),
                (EnableLampBlue,   LogicsStandard.Grob_Stacklight.Slots.Blue,   Colors.Blue),
                (EnableLampWhite,  LogicsStandard.Grob_Stacklight.Slots.White,  Colors.White),
            };

            foreach (var (enabled, slotName, color) in lampMappings)
            {
                if (!enabled) continue;

                var segment = new LampSegmentInformation
                {
                    Name = slotName
                };
                segment.SetColorFromArgb(color.A, color.R, color.G, color.B);
                SegmentsInformation.Add(segment);
            }


            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SegmentedLampComponent.SegmentsInformation), SegmentsInformation, nameof(SegmentedLamp.SegmentedLampComponent));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SceneObject.Transform.LocalScale), new Vector3(0.5f, 0.5f, 0.5f), nameof(SceneObject.Transform));

            return true;
        }

    }
}
