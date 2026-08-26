using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FS.Components.SimulationSceneObjects.SimpleLogicObjects.Implementations.Mover;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.GrobStandard
{
    public class GrobGripperAddon_AnalogValues_Container : ContainerBaseClass, IAddonContainer<GrobGripperBasic_Container>, ILogicOwner
    {

        public GrobGripperAddon_AnalogValues_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {

                {LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.UnclampedAnalog, typeof(GrobGripperAddon_AnalogValues_Container).GetProperty(nameof(Signal_UnclampedAnalog)) },
                {LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.ClampedAnalog, typeof(GrobGripperAddon_AnalogValues_Container).GetProperty(nameof(Signal_ClampedAnalog)) },
                {LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.ClampedWithPartAnalog, typeof(GrobGripperAddon_AnalogValues_Container).GetProperty(nameof(Signal_ClampedWithParAnalogt)) },
                {LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.ClampedNoPartAnalog, typeof(GrobGripperAddon_AnalogValues_Container).GetProperty(nameof(Signal_ClampedNoPartAnalog)) },
            };
        }




        public FeeLogic Logic_Addon { get; set; }
        public GrobGripperBasic_Container ParentContainer { get; set; }

        public FeeInterfaceSignal Signal_UnclampedAnalog { get; set; }
        public FeeInterfaceSignal Signal_ClampedAnalog { get; set; }
        public FeeInterfaceSignal Signal_ClampedWithParAnalogt { get; set; }
        public FeeInterfaceSignal Signal_ClampedNoPartAnalog { get; set; }


        public ushort Parameter_UnclampedValue { get; set; }
        public ushort Parameter_ClampedValue { get; set; }
        public ushort Parameter_ClampedWithPartValue { get; set; } 
        public ushort Parameter_ClampedNoPartValue { get; set; }






        async Task<FeeLogic> ILogicOwner.CreateLogicAsync(FeeAbstractObject parentObject)
        {
            Logic_Addon = new FeeLogic()
            {
                Name = this.ComponentName,
                LogicDefinitionName = LogicsAddons.Grob_GripperAddOn_AnalogValues.Name,
                LogicDefinitionPath = LogicsAddons.Grob_GripperAddOn_AnalogValues.Path,
                Parent = parentObject,
            };

            (Logic_Addon.LogicDefinitionGuid, Logic_Addon.LogicDefinitionVersion) = await GetOrImportLogicDefinition(Logic_Addon.LogicDefinitionName, Logic_Addon.LogicDefinitionPath);
            await Logic_Addon.CreateSendAssignAndWaitAsync();

            return Logic_Addon;
        }



        async Task ILogicOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to LogicObject if existing
            var singleMappings = new (FeeInterfaceSignal Signal, string SlotName)[]
            {
                (Signal_UnclampedAnalog, LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.UnclampedAnalog),
                (Signal_ClampedAnalog, LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.ClampedAnalog),
                (Signal_ClampedWithParAnalogt, LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.ClampedWithPartAnalog),
                (Signal_ClampedNoPartAnalog, LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.ClampedNoPartAnalog),
            };

            

            foreach (var (signal, slotname) in singleMappings)
            {
                if (signal != null)
                {
                    await signal.CreateSignalAsync(targetInterface);
                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Logic_Addon.Guid, slotname, signal.Guid, true);
                }
            }         
            

            // Map parameters
            var parametermapping = new (Guid ObjectGuid, string SlotName, object Value)[]
            {
                (Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.UnclampedValue, Parameter_UnclampedValue),
                (Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.ClampedValue,   Parameter_ClampedValue),
                (Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.ClampedWithPartValue, Parameter_ClampedWithPartValue),
                (Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.ClampedNoPartValue, Parameter_ClampedNoPartValue),
            };

            var guids = parametermapping.Select(x => x.ObjectGuid).ToArray();
            var slotNames = parametermapping.Select(x => x.SlotName).ToArray();
            var values = parametermapping.Select(x => x.Value).ToArray();

            await Services.ApiInstance.Object.SetSlotValuesAsync(guids, slotNames, values);
        }



        async Task IAddonContainer.ConnectToParentAsync()
        {
            if (ParentContainer == null)
                return;

            if (ParentContainer.Logic_Gripper == null)
                return;

            await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(ParentContainer.Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.AddOnStatus, Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_AnalogValues.Slots.AddOnStatus);
        }



    }
}
