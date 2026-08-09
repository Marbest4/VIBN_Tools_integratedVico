using System.Reflection;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.GrobStandard
{
    public class GrobSafetyDoor_Container : ContainerBaseClass, ILogicOwner
    {

        public GrobSafetyDoor_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {LogicsStandard.Grob_SafetyDoor.Slots.Unlock, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_Unlock)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.LedStart, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_LedStart)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.LedQuitReset, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_LedQuitReset)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.LedRequestEntry, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_LedRequestEntry)) },

                {LogicsStandard.Grob_SafetyDoor.Slots.Unlocked, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_Unlocked)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.Opened, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_Opened)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.Closed_Ch1, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_Closed_Ch1)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.Closed_Ch2, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_Closed_Ch2)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.ClosedAndLocked, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_ClosedAndLocked)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.ClosedAndLocked_Ch1, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_ClosedAndLocked_Ch1)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.ClosedAndLocked_Ch2, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_ClosedAndLocked_Ch2)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.BoltTongueInserted, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_BoltTongueInserted)) },

                {LogicsStandard.Grob_SafetyDoor.Slots.Start, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_Start)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.QuitReset, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_QuitReset)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.RequestEntry, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_RequestEntry)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.Fault, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_Fault)) },

                {LogicsStandard.Grob_SafetyDoor.Slots.EStopPressed_Ch1, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_EStopPressed_Ch1)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.EStopPressed_Ch2, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_EStopPressed_Ch2)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.EStopNotPressed_Ch1, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_EStopNotPressed_Ch1)) },
                {LogicsStandard.Grob_SafetyDoor.Slots.EStopNotPressed_Ch2, typeof(GrobSafetyDoor_Container).GetProperty(nameof(Signal_EStopNotPressed_Ch2)) },
            };
        }



        public FeeLogic Logic_SafetyDoor { get; set; }

        public FeeInterfaceSignal Signal_Unlock { get; set; }
        public FeeInterfaceSignal Signal_LedStart { get; set; }
        public FeeInterfaceSignal Signal_LedQuitReset { get; set; }
        public FeeInterfaceSignal Signal_LedRequestEntry { get; set; }

        public FeeInterfaceSignal Signal_Unlocked { get; set; }
        public FeeInterfaceSignal Signal_Opened { get; set; }
        public FeeInterfaceSignal Signal_Closed_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_Closed_Ch2 { get; set; }
        public FeeInterfaceSignal Signal_ClosedAndLocked { get; set; }
        public FeeInterfaceSignal Signal_ClosedAndLocked_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_ClosedAndLocked_Ch2 { get; set; }
        public FeeInterfaceSignal Signal_BoltTongueInserted { get; set; }

        public FeeInterfaceSignal Signal_Start { get; set; }
        public FeeInterfaceSignal Signal_QuitReset { get; set; }
        public FeeInterfaceSignal Signal_RequestEntry { get; set; }
        public FeeInterfaceSignal Signal_Fault { get; set; }

        public FeeInterfaceSignal Signal_EStopPressed_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_EStopPressed_Ch2 { get; set; }
        public FeeInterfaceSignal Signal_EStopNotPressed_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_EStopNotPressed_Ch2 { get; set; }





        async Task<FeeLogic> ILogicOwner.CreateLogicAsync(FeeAbstractObject parentObject)
        {
            // Reference right Logic Version and Guid to create LogicObject
            Logic_SafetyDoor = new FeeLogic()
            {
                Name = this.ComponentName,
                LogicDefinitionName = LogicsStandard.Grob_SafetyDoor.Name,
                LogicDefinitionPath = LogicsStandard.Grob_SafetyDoor.Path,
                Parent = parentObject,
            };

            (Logic_SafetyDoor.LogicDefinitionGuid, Logic_SafetyDoor.LogicDefinitionVersion) = await GetOrImportLogicDefinition(Logic_SafetyDoor.LogicDefinitionName, Logic_SafetyDoor.LogicDefinitionPath);
            await Logic_SafetyDoor.CreateSendAssignAndWaitAsync();

            return Logic_SafetyDoor;
        }

        async Task ILogicOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to LogicObject if existing
            var mappings = new (FeeInterfaceSignal Signal, string SlotName)[]
            {
                (Signal_Unlock, LogicsStandard.Grob_SafetyDoor.Slots.Unlock),
                (Signal_LedStart, LogicsStandard.Grob_SafetyDoor.Slots.LedStart),
                (Signal_LedQuitReset, LogicsStandard.Grob_SafetyDoor.Slots.LedQuitReset),
                (Signal_LedRequestEntry, LogicsStandard.Grob_SafetyDoor.Slots.LedRequestEntry),
                (Signal_Unlocked, LogicsStandard.Grob_SafetyDoor.Slots.Unlocked),
                (Signal_Opened, LogicsStandard.Grob_SafetyDoor.Slots.Opened),
                (Signal_Closed_Ch1, LogicsStandard.Grob_SafetyDoor.Slots.Closed_Ch1),
                (Signal_Closed_Ch2, LogicsStandard.Grob_SafetyDoor.Slots.Closed_Ch2),
                (Signal_ClosedAndLocked, LogicsStandard.Grob_SafetyDoor.Slots.ClosedAndLocked),
                (Signal_ClosedAndLocked_Ch1, LogicsStandard.Grob_SafetyDoor.Slots.ClosedAndLocked_Ch1),
                (Signal_ClosedAndLocked_Ch2, LogicsStandard.Grob_SafetyDoor.Slots.ClosedAndLocked_Ch2),
                (Signal_Start, LogicsStandard.Grob_SafetyDoor.Slots.Start),
                (Signal_QuitReset, LogicsStandard.Grob_SafetyDoor.Slots.QuitReset),
                (Signal_RequestEntry, LogicsStandard.Grob_SafetyDoor.Slots.RequestEntry),
                (Signal_Fault, LogicsStandard.Grob_SafetyDoor.Slots.Fault),
                (Signal_EStopPressed_Ch1, LogicsStandard.Grob_SafetyDoor.Slots.EStopPressed_Ch1),
                (Signal_EStopPressed_Ch2, LogicsStandard.Grob_SafetyDoor.Slots.EStopPressed_Ch2),
                (Signal_EStopNotPressed_Ch1, LogicsStandard.Grob_SafetyDoor.Slots.EStopNotPressed_Ch1),
                (Signal_EStopNotPressed_Ch2, LogicsStandard.Grob_SafetyDoor.Slots.EStopNotPressed_Ch2),
            };

            foreach (var (signal, slotname) in mappings)
            {
                if (signal != null)
                {
                    await signal.CreateSignalAsync(targetInterface);
                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Logic_SafetyDoor.Guid, slotname, signal.Guid, true);
                }
            }
        }


    }
}
