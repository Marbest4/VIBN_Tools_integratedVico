using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic.LogicsStandard;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.General
{
    public class Stacklight_Container : ContainerBaseClass, ISimObjectFindOrSelect, ISimObjectOwner
    {
        public Stacklight_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {"PLC_NO_Red", typeof(Stacklight_Container).GetProperty("Signal_LampRed") },
                {"PLC_NO_Yellow", typeof(Stacklight_Container).GetProperty("Signal_LampYellow") },
                {"PLC_NO_Green", typeof(Stacklight_Container).GetProperty("Signal_LampGreen") },
                {"PLC_NO_Blue", typeof(Stacklight_Container).GetProperty("Signal_LampBlue") },
                {"PLC_NO_White", typeof(Stacklight_Container).GetProperty("Signal_LampWhite") },
            };
        }


        // dynamic live mapping
        private IEnumerable<(FeeInterfaceSignal Signal, string SlotName)> LampMappings
        {
            get
            {
                yield return (Signal_LampRed, Grob_Stacklight.Slots.Red);
                yield return (Signal_LampYellow, Grob_Stacklight.Slots.Yellow);
                yield return (Signal_LampGreen, Grob_Stacklight.Slots.Green);
                yield return (Signal_LampBlue, Grob_Stacklight.Slots.Blue);
                yield return (Signal_LampWhite, Grob_Stacklight.Slots.White);
            }
        }



        public FeeInterfaceSignal Signal_LampRed { get; set; }
        public FeeInterfaceSignal Signal_LampYellow { get; set; }
        public FeeInterfaceSignal Signal_LampGreen { get; set; }
        public FeeInterfaceSignal Signal_LampBlue { get; set; }
        public FeeInterfaceSignal Signal_LampWhite { get; set; }

        public List<FeeSegmentedLamp> Lamps_Stacklight { get; set; } = new List<FeeSegmentedLamp>();

        public bool IsCreationRequested { get; set; }




        void ISimObjectFindOrSelect.FindSimObjects(ObservableCollection<FeeAbstractObject> mappableSimObjects)
        {
            Lamps_Stacklight = FindSimObjectsByNameAndType<FeeSegmentedLamp>(mappableSimObjects);
        }

        IEnumerable<SimObjectTarget> ISimObjectFindOrSelect.GetSimObjectTargets()
        {
            yield return new SimObjectTarget()
            {
                DisplayName = "SegmentedLamps",

                AllowedType = typeof(FeeSegmentedLamp),

                AllowMultiSelect = true,

                GetObjects = () => Lamps_Stacklight,

                AssignObjects = objects =>
                {
                    Lamps_Stacklight = objects.OfType<FeeSegmentedLamp>().ToList();
                }
            };
        }





        async Task ISimObjectOwner.CreateSimObjectsAsync(FeeAbstractObject parentObject)
        {
            // Initialise SegmentedLamp
            if (!Lamps_Stacklight.Any() && IsCreationRequested)
            {
                // Create Stacklight, if any signal is other than null
                if (LampMappings.Any(x => x.Signal != null))
                {
                    var stackLight = new FeeSegmentedLamp()
                    {
                        Name = this.ComponentName,
                        Parent = parentObject,
                        EnableLampRed = Signal_LampRed != null,
                        EnableLampYellow = Signal_LampYellow != null,
                        EnableLampGreen = Signal_LampGreen != null,
                        EnableLampBlue = Signal_LampBlue != null,
                        EnableLampWhite = Signal_LampWhite != null,
                    };

                    await stackLight.CreateAsync();
                    await stackLight.SendAndWaitAsync();
                    Lamps_Stacklight.Add(stackLight);
                }

            }


        }

        async Task ISimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Create Stacklight, if any signal is other than null
            if (LampMappings.Any(x => x.Signal != null))
            {
                // Map signals to SceneObject if existing   
                foreach (var (signal, slotName) in LampMappings)
                {
                    if (signal == null) continue;

                    await signal.CreateSignalAsync(targetInterface);

                    if (Lamps_Stacklight.Any())
                    {
                        await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Lamps_Stacklight.First().Guid, slotName, signal.Guid, true);
                    }
                }
            }
        }


    }
}
