using System.Reflection;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.GrobStandard
{
    public class GrobClamping_Container : ContainerBaseClass, ILogicOwner
    {

        public GrobClamping_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {LogicsStandard.Grob_Clamping.Slots.ReleaseClamping, typeof(GrobClamping_Container).GetProperty("Signal_ReleaseClamping") },
                {LogicsStandard.Grob_Clamping.Slots.ClampingReleased, typeof(GrobClamping_Container).GetProperty("Signal_ClampingReleased") },
            };
        }


        public FeeLogic Logic_Clamping { get; set; }

        public FeeInterfaceSignal Signal_ReleaseClamping { get; set; }
        public FeeInterfaceSignal Signal_ClampingReleased { get; set; }

        public float Parameter_ClampingDelay { get; set; }






        async Task<FeeLogic> ILogicOwner.CreateLogicAsync(FeeAbstractObject parentObject)
        {
            // Reference right Logic Version and Guid to create LogicObject
            Logic_Clamping = new FeeLogic()
            {
                Name = this.ComponentName,
                LogicDefinitionName = LogicsStandard.Grob_Clamping.Name,
                LogicDefinitionPath = LogicsStandard.Grob_Clamping.Path,
                Parent = parentObject,
            };

            (Logic_Clamping.LogicDefinitionGuid, Logic_Clamping.LogicDefinitionVersion) = await GetOrImportLogicDefinition(Logic_Clamping.LogicDefinitionName, Logic_Clamping.LogicDefinitionPath);
            await Logic_Clamping.CreateSendAssignAndWaitAsync();

            return Logic_Clamping;
        }

        async Task ILogicOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to LogicObject if existing
            var mappings = new (FeeInterfaceSignal Signal, string SlotName)[]
            {
                (Signal_ReleaseClamping, LogicsStandard.Grob_Clamping.Slots.ReleaseClamping),
                (Signal_ClampingReleased, LogicsStandard.Grob_Clamping.Slots.ClampingReleased),
            };

            foreach (var (signal, slotname) in mappings)
            {
                if (signal != null)
                {
                    await signal.CreateSignalAsync(targetInterface);
                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Logic_Clamping.Guid, slotname, signal.Guid, true);
                }
            }

            // Map parameters
            if (Parameter_ClampingDelay != -1)
            {
                Services.ApiInstance.Object.SetSlotValue(Logic_Clamping.Guid, LogicsStandard.Grob_Clamping.Slots.ClampingDelay, Parameter_ClampingDelay);
            }

        }




    }
}
