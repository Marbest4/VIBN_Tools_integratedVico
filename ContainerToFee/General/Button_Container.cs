using FS.SDK.Mathematics;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.General
{
    public class Button_Container : ContainerBaseClass, ISimObjectFindOrSelect, ISimObjectOwner
    {
        public Button_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {"PLC_IN_NO", typeof(Button_Container).GetProperty("Signal_NormallyOpened") },
                {"PLC_IN_NC", typeof(Button_Container).GetProperty("Signal_NormallyClosed") },
            };
        }



        public FeeButton Button { get; set; }
        public FeeInterfaceSignal Signal_NormallyOpened { get; set; }
        public FeeInterfaceSignal Signal_NormallyClosed { get; set; }

        public bool IsCreationRequested { get; set; }





        void ISimObjectFindOrSelect.FindSimObjects(ObservableCollection<FeeAbstractObject> mappableSimObjects)
        {
            Button = FindSimObjectsByNameAndType<FeeButton>(mappableSimObjects).FirstOrDefault();
        }

        IEnumerable<SimObjectTarget> ISimObjectFindOrSelect.GetSimObjectTargets()
        {
            yield return new SimObjectTarget()
            {
                DisplayName = "Button",

                AllowedType = typeof(FeeButton),

                AllowMultiSelect = false,

                GetObjects = () => Button != null ? new[] { Button } : Enumerable.Empty<FeeAbstractObject>(),

                AssignObjects = objects =>
                {
                    Button = objects.OfType<FeeButton>().FirstOrDefault();
                }
            };
        }



        async Task ISimObjectOwner.CreateSimObjectsAsync(FeeAbstractObject parentObject)
        {
            if (Button == null && IsCreationRequested)
            {
                // Create Button
                var button = new FeeButton()
                {
                    Parent = parentObject,
                    Name = this.ComponentName,
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(0.5f, 0.5f, 0.5f),
                };

                await button.CreateAsync();
                await button.SendAndWaitAsync();
                Button = button;
            }
        }


        async Task ISimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {

            if (Signal_NormallyOpened != null)
            {
                await Signal_NormallyOpened.CreateSignalAsync(targetInterface);
            }
            if (Signal_NormallyClosed != null)
            {
                await Signal_NormallyClosed.CreateSignalAsync(targetInterface);
            }

            if (Button != null)
            {
                // Map signals to Button
                if (Signal_NormallyOpened != null)
                {
                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Button.Guid, "Pressed", Signal_NormallyOpened.Guid, true);
                }
                if (Signal_NormallyClosed != null)
                {
                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Button.Guid, "PressedInverted", Signal_NormallyClosed.Guid, true);
                }
            }
        }



    }
}
