using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.GrobStandard
{
    public class GrobPneumaticSupply_Container : ContainerBaseClass, ILogicOwner
    {


        public GrobPneumaticSupply_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {LogicsStandard.Grob_PneumaticSupply.Slots.SwitchOnCh1, typeof(GrobPneumaticSupply_Container).GetProperty(nameof(Signal_SwitchOn_Ch1)) },
                {LogicsStandard.Grob_PneumaticSupply.Slots.SwitchOnCh2, typeof(GrobPneumaticSupply_Container).GetProperty(nameof(Signal_SwitchOn_Ch2)) },

                {LogicsStandard.Grob_PneumaticSupply.Slots.PneumaticOkCh1, typeof(GrobPneumaticSupply_Container).GetProperty(nameof(Signal_PneumaticOk_Ch1)) },
                {LogicsStandard.Grob_PneumaticSupply.Slots.PneumaticOkCh2, typeof(GrobPneumaticSupply_Container).GetProperty(nameof(Signal_PneumaticOk_Ch2)) },
                {LogicsStandard.Grob_PneumaticSupply.Slots.NotSwitchedOnCh1, typeof(GrobPneumaticSupply_Container).GetProperty(nameof(Signal_NotSwitchedOn_Ch1)) },
                {LogicsStandard.Grob_PneumaticSupply.Slots.NotSwitchedOnCh2, typeof(GrobPneumaticSupply_Container).GetProperty(nameof(Signal_NotSwitchedOn_Ch2)) },

                 {LogicsStandard.Grob_PneumaticSupply.Slots.SwitchedOnImpulse, typeof(GrobPneumaticSupply_Container).GetProperty(nameof(Signal_SwitchedOnImpulse)) },
            };
        }


        public FeeLogic Logic_PneumaticSupply { get; set; }

        public FeeInterfaceSignal Signal_SwitchOn_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_SwitchOn_Ch2 { get; set; }
        public FeeInterfaceSignal Signal_PneumaticOk_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_PneumaticOk_Ch2 { get; set; }
        public FeeInterfaceSignal Signal_NotSwitchedOn_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_NotSwitchedOn_Ch2 { get; set; }
        public FeeInterfaceSignal Signal_SwitchedOnImpulse { get; set; }



        async Task<FeeLogic> ILogicOwner.CreateLogicAsync(FeeAbstractObject parentObject)
        {
            // Reference right Logic Version and Guid to create LogicObject
            Logic_PneumaticSupply = new FeeLogic()
            {
                Name = ComponentName,
                LogicDefinitionName = LogicsStandard.Grob_PneumaticSupply.Name,
                LogicDefinitionPath = LogicsStandard.Grob_PneumaticSupply.Path,
                Parent = parentObject,
            };

            (Logic_PneumaticSupply.LogicDefinitionGuid, Logic_PneumaticSupply.LogicDefinitionVersion) = await GetOrImportLogicDefinition(Logic_PneumaticSupply.LogicDefinitionName, Logic_PneumaticSupply.LogicDefinitionPath);
            await Logic_PneumaticSupply.CreateSendAssignAndWaitAsync();

            return Logic_PneumaticSupply;
        }


        async Task ILogicOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to LogicObject if existing
            var mappings = new (FeeInterfaceSignal Signal, string SlotName)[]
            {
                (Signal_SwitchOn_Ch1, LogicsStandard.Grob_PneumaticSupply.Slots.SwitchOnCh1),
                (Signal_SwitchOn_Ch2, LogicsStandard.Grob_PneumaticSupply.Slots.SwitchOnCh2),
                (Signal_PneumaticOk_Ch1, LogicsStandard.Grob_PneumaticSupply.Slots.PneumaticOkCh1),
                (Signal_PneumaticOk_Ch2, LogicsStandard.Grob_PneumaticSupply.Slots.PneumaticOkCh2),
                (Signal_NotSwitchedOn_Ch1, LogicsStandard.Grob_PneumaticSupply.Slots.NotSwitchedOnCh1),
                (Signal_NotSwitchedOn_Ch2, LogicsStandard.Grob_PneumaticSupply.Slots.NotSwitchedOnCh2),
                (Signal_SwitchedOnImpulse, LogicsStandard.Grob_PneumaticSupply.Slots.SwitchedOnImpulse),
            };


            foreach (var (signal, slotname) in mappings)
            {
                if (signal != null)
                {
                    await signal.CreateSignalAsync(targetInterface);
                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Logic_PneumaticSupply.Guid, slotname, signal.Guid, true);
                }
            }
        }



    }
}
