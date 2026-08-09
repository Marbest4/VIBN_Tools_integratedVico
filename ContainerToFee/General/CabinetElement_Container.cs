using System.Numerics;
using System.Reflection;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeCabinetElement;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.General
{
    #region Switch
    //============================================================================================================================
    // C A B I N E T   E L E M E N T   S W I T C H
    //============================================================================================================================
    public class CabinetSwitch_Container : ContainerBaseClass, ICabinetElementOwner
    {

        public CabinetSwitch_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {"PLC_IN_NO1", typeof(CabinetSwitch_Container).GetProperty(nameof(Signal_NormallyOpened_Ch1)) },
                {"PLC_IN_NO2", typeof(CabinetSwitch_Container).GetProperty(nameof(Signal_NormallyOpened_Ch2)) },
                {"PLC_IN_NC1", typeof(CabinetSwitch_Container).GetProperty(nameof(Signal_NormallyOpened_Ch1)) },
                {"PLC_IN_NC2", typeof(CabinetSwitch_Container).GetProperty(nameof(Signal_NormallyClosed_Ch2)) },
            };
        }



        public FeeCabinetElement CabinetElement_Switch { get; set; }

        public FeeInterfaceSignal Signal_NormallyOpened_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_NormallyOpened_Ch2 { get; set; }
        public FeeInterfaceSignal Signal_NormallyClosed_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_NormallyClosed_Ch2 { get; set; }


        public string CabinetName => "Cabinet Switches";
        public Vector2 ElementPosition { get; set; }
        public bool IsCreationRequested { get; set; }




        async Task ISimObjectOwner.CreateSimObjectsAsync(FeeAbstractObject parentObject)
        {
            // Create CabinetElement
            CabinetElement_Switch = new FeeCabinetElement()
            {
                Parent = parentObject,
                ElementType = CabinetElementType.PositionSwitch2,
                PositionX = ElementPosition.X,
                PositionY = ElementPosition.Y,
                Name = $"{this.ComponentName};{this.Signal_NormallyOpened_Ch1?.Comment ?? "-"};{this.Signal_NormallyOpened_Ch1?.Comment ?? "-"}",
                Label = this.ComponentName,
                Tooltip = (Signal_NormallyOpened_Ch1 != null) ? Signal_NormallyOpened_Ch1.Comment : String.Empty,
            };

            await CabinetElement_Switch.CreateAndSendAsync();
        }

        async Task ISimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to CabinetElement
            if (Signal_NormallyOpened_Ch1 != null)
            {
                await Signal_NormallyOpened_Ch1.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(CabinetElement_Switch.Guid, "NO1", Signal_NormallyOpened_Ch1.Guid, true);
            }
            if (Signal_NormallyOpened_Ch2 != null)
            {
                await Signal_NormallyOpened_Ch2.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(CabinetElement_Switch.Guid, "NO2", Signal_NormallyOpened_Ch2.Guid, true);
            }
            if (Signal_NormallyClosed_Ch1 != null)
            {
                await Signal_NormallyClosed_Ch1.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(CabinetElement_Switch.Guid, "NC1", Signal_NormallyClosed_Ch1.Guid, true);
            }            
            if (Signal_NormallyClosed_Ch2 != null)
            {
                await Signal_NormallyClosed_Ch2.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(CabinetElement_Switch.Guid, "NC2", Signal_NormallyClosed_Ch2.Guid, true);
            }
        }



    }
    #endregion



    #region Fuse
    //============================================================================================================================
    // C A B I N E T   E L E M E N T   F U S E
    //============================================================================================================================
    public class CabinetFuse_Container : ContainerBaseClass, ICabinetElementOwner
    {
        public CabinetFuse_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {"PLC_IN_NO", typeof(CabinetFuse_Container).GetProperty("Signal_NormallyOpened") },
                {"PLC_IN_NC", typeof(CabinetFuse_Container).GetProperty("Signal_NormallyClosed") },
            };
        }



        public FeeCabinetElement CabinetElement_Switch { get; set; }

        public FeeInterfaceSignal Signal_NormallyOpened { get; set; }
        public FeeInterfaceSignal Signal_NormallyClosed { get; set; }


        public string CabinetName => "Cabinet Fuses";
        public Vector2 ElementPosition { get; set; }
        public bool IsCreationRequested { get; set; }




        async Task ISimObjectOwner.CreateSimObjectsAsync(FeeAbstractObject parentObject)
        {
            // Create CabinetElement
            CabinetElement_Switch = new FeeCabinetElement()
            {
                Parent = parentObject,
                ElementType = "Fuse",
                PositionX = ElementPosition.X,
                PositionY = ElementPosition.Y,
                Name = $"{this.ComponentName};{this.Signal_NormallyOpened?.Comment ?? "-"};{this.Signal_NormallyClosed?.Comment ?? "-"}",
                Label = this.ComponentName,
            };

            await CabinetElement_Switch.CreateAndSendAsync();
        }

        async Task ISimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to CabinetElement
            if (Signal_NormallyClosed != null)
            {
                await Signal_NormallyClosed.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(CabinetElement_Switch.Guid, "NC", Signal_NormallyClosed.Guid, true);
            }
            if (Signal_NormallyOpened != null)
            {
                await Signal_NormallyOpened.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(CabinetElement_Switch.Guid, "NO", Signal_NormallyOpened.Guid, true);
            }
        }


    }
    #endregion



    #region EStop
    //============================================================================================================================
    // C A B I N E T   E L E M E N T   E S T O P
    //============================================================================================================================
    public class CabinetEStop_Container : ContainerBaseClass, ICabinetElementOwner
    {
        public CabinetEStop_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {"PLC_IN_NO1", typeof(CabinetEStop_Container).GetProperty("Signal_NormallyOpened_Ch1") },
                {"PLC_IN_NO2", typeof(CabinetEStop_Container).GetProperty("Signal_NormallyOpened_Ch2") },
                {"PLC_IN_NC1", typeof(CabinetEStop_Container).GetProperty("Signal_NormallyClosed_Ch1") },
                {"PLC_IN_NC2", typeof(CabinetEStop_Container).GetProperty("Signal_NormallyClosed_Ch2") },
            };
        }



        public FeeCabinetElement CabinetElement_EStop { get; set; }

        public FeeInterfaceSignal Signal_NormallyOpened_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_NormallyOpened_Ch2 { get; set; }
        public FeeInterfaceSignal Signal_NormallyClosed_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_NormallyClosed_Ch2 { get; set; }


        public string CabinetName => "Cabinet EStops";
        public Vector2 ElementPosition { get; set; }
        public bool IsCreationRequested { get; set; }




        async Task ISimObjectOwner.CreateSimObjectsAsync(FeeAbstractObject parentObject)
        {
            // Create CabinetElement
            CabinetElement_EStop = new FeeCabinetElement()
            {
                Parent = parentObject,
                ElementType = "Grob_NotAus",
                PositionX = ElementPosition.X,
                PositionY = ElementPosition.Y,
                Name = $"{this.ComponentName};{this.Signal_NormallyOpened_Ch1?.Comment ?? "-"};{this.Signal_NormallyClosed_Ch1?.Comment ?? "-"}",
                Label = this.ComponentName,
            };

            await CabinetElement_EStop.CreateAndSendAsync();
        }

        async Task ISimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to CabinetElement
            if (Signal_NormallyClosed_Ch1 != null)
            {
                await Signal_NormallyClosed_Ch1.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(CabinetElement_EStop.Guid, "NC1", Signal_NormallyClosed_Ch1.Guid, true);
            }
            if (Signal_NormallyClosed_Ch2 != null)
            {
                await Signal_NormallyClosed_Ch2.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(CabinetElement_EStop.Guid, "NC2", Signal_NormallyClosed_Ch2.Guid, true);
            }
            if (Signal_NormallyOpened_Ch1 != null)
            {
                await Signal_NormallyOpened_Ch1.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(CabinetElement_EStop.Guid, "NO1", Signal_NormallyOpened_Ch1.Guid, true);
            }
            if (Signal_NormallyOpened_Ch2 != null)
            {
                await Signal_NormallyOpened_Ch2.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(CabinetElement_EStop.Guid, "NO2", Signal_NormallyOpened_Ch2.Guid, true);
            }
        }


    }
    #endregion



    #region Lamp
    //============================================================================================================================
    // C A B I N E T   E L E M E N T   L A M P
    //============================================================================================================================
    public class CabinetLamp_Container : ContainerBaseClass, ICabinetElementOwner
    {
        public CabinetLamp_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {"PLC_OUT_ON", typeof(CabinetLamp_Container).GetProperty("Signal_LampOn") },
            };
        }



        public FeeCabinetElement CabinetElement_Lamp { get; set; }

        public FeeInterfaceSignal Signal_LampOn { get; set; }


        public string CabinetName => "Cabinet Lamps";
        public Vector2 ElementPosition { get; set; }
        public bool IsCreationRequested { get; set; }




        async Task ISimObjectOwner.CreateSimObjectsAsync(FeeAbstractObject parentObject)
        {
            // Create CabinetElement
            CabinetElement_Lamp = new FeeCabinetElement()
            {
                Parent = parentObject,
                ElementType = "Lamp Yellow",
                PositionX = ElementPosition.X,
                PositionY = ElementPosition.Y,
                Name = this.ComponentName,
                Label = this.ComponentName,
            };

            await CabinetElement_Lamp.CreateAndSendAsync();
        }

        async Task ISimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to CabinetElement
            if (Signal_LampOn != null)
            {
                await Signal_LampOn.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(CabinetElement_Lamp.Guid, "ON", Signal_LampOn.Guid, true);
            }
        }

    }
    #endregion

}
