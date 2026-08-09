using System.Reflection;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.General
{
    public class SimpleMove_Container : ContainerBaseClass, ISimObjectOwner
    {
        public SimpleMove_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {"PLC_OUT_Signal", typeof(SimpleMove_Container).GetProperty("Signal_PlcOutSignal") },
                {"PLC_IN_Signal", typeof(SimpleMove_Container).GetProperty("Signal_PlcInSignal") },

            };
        }


        public FeeInterfaceSignal Signal_PlcOutSignal { get; set; }
        public FeeInterfaceSignal Signal_PlcInSignal { get; set; }

        public FeeSimpleMove SimpleLogic_MoveBit { get; set; }
        public bool IsCreationRequested { get; set; }





        async Task ISimObjectOwner.CreateSimObjectsAsync(FeeAbstractObject parentObject)
        {
            if (Signal_PlcOutSignal != null || Signal_PlcInSignal != null)
            {
                SimpleLogic_MoveBit = new FeeSimpleMove();
                await SimpleLogic_MoveBit.CreateAsync();
                await SimpleLogic_MoveBit.SendAndWaitAsync();
            }
        }

        async Task ISimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to LogicObject if existing
            if (Signal_PlcOutSignal != null)
            {
                await Signal_PlcOutSignal.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(SimpleLogic_MoveBit.Guid, "Input 01", Signal_PlcOutSignal.Guid, true);
            }
            if (Signal_PlcInSignal != null)
            {
                await Signal_PlcInSignal.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(SimpleLogic_MoveBit.Guid, "Output 01", Signal_PlcInSignal.Guid, true);
            }
        }

    }





    public class SimpleNot_Container : ContainerBaseClass, ISimObjectOwner
    {
        public SimpleNot_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {"PLC_OUT_Signal", typeof(SimpleNot_Container).GetProperty("Signal_PlcOutSignal") },
                {"PLC_IN_Signal", typeof(SimpleNot_Container).GetProperty("Signal_PlcInSignal") },
            };
        }


        public FeeInterfaceSignal Signal_PlcOutSignal { get; set; }
        public FeeInterfaceSignal Signal_PlcInSignal { get; set; }

        public FeeSimpleNot SimpleLogic_BoolNot { get; set; }

        public bool IsCreationRequested { get; set; }





        async Task ISimObjectOwner.CreateSimObjectsAsync(FeeAbstractObject parentObject)
        {
            if (Signal_PlcOutSignal != null || Signal_PlcInSignal != null)
            {
                SimpleLogic_BoolNot = new FeeSimpleNot();
                await SimpleLogic_BoolNot.CreateAsync();
                await SimpleLogic_BoolNot.SendAndWaitAsync();
            }
        }

        async Task ISimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to LogicObject if existing
            if (Signal_PlcOutSignal != null)
            {
                await Signal_PlcOutSignal.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(SimpleLogic_BoolNot.Guid, "Input 01", Signal_PlcOutSignal.Guid, true);
            }
            if (Signal_PlcInSignal != null)
            {
                await Signal_PlcInSignal.CreateSignalAsync(targetInterface);
                await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(SimpleLogic_BoolNot.Guid, "Output 01", Signal_PlcInSignal.Guid, true);
            }
        }


    }
}
