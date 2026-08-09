using FS.SDK.Mathematics;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.GrobStandard
{
    public class GrobStop_Container : ContainerBaseClass, ISimObjectFindOrSelect, ILogicSimObjectOwner
    {

        public GrobStop_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {LogicsStandard.Grob_Stop.Slots.Open, typeof(GrobStop_Container).GetProperty("Signal_Open") },
                {LogicsStandard.Grob_Stop.Slots.Close, typeof(GrobStop_Container).GetProperty("Signal_Close") },
                {LogicsStandard.Grob_Stop.Slots.Opened, typeof(GrobStop_Container).GetProperty("Signal_Opened") },
                {LogicsStandard.Grob_Stop.Slots.Closed, typeof(GrobStop_Container).GetProperty("Signal_Closed") },
            };
        }


        public FeeLogic Logic_Stop { get; set; }

        public FeeInterfaceSignal Signal_Open { get; set; }
        public FeeInterfaceSignal Signal_Close { get; set; }
        public FeeInterfaceSignal Signal_Opened { get; set; }
        public FeeInterfaceSignal Signal_Closed { get; set; }

        public List<FeeFloor> Floors_Stop { get; set; } = new List<FeeFloor>();

        public float Parameter_CollisionDelay { get; set; } = -1f;
        public bool IsCreationRequested { get; set; }




        void ISimObjectFindOrSelect.FindSimObjects(ObservableCollection<FeeAbstractObject> mappableSimObjects)
        {
            Floors_Stop = FindSimObjectsByNameAndType<FeeFloor>(mappableSimObjects);
        }

        IEnumerable<SimObjectTarget> ISimObjectFindOrSelect.GetSimObjectTargets()
        {
            yield return new SimObjectTarget()
            {
                DisplayName = "Floors",

                AllowedType = typeof(FeeFloor),

                AllowMultiSelect = true,

                GetObjects = () => Floors_Stop,

                AssignObjects = objects =>
                {
                    Floors_Stop = objects.OfType<FeeFloor>().ToList();
                }
            };
        }




        async Task<FeeLogic> ILogicSimObjectOwner.CreateLogicAsync(FeeAbstractObject parentObject)
        {
            // Reference right Logic Version and Guid to create LogicObject
            Logic_Stop = new FeeLogic()
            {
                Name = this.ComponentName,
                LogicDefinitionName = LogicsStandard.Grob_Stop.Name,
                LogicDefinitionPath = LogicsStandard.Grob_Stop.Path,
                Parent = parentObject,
            };

            (Logic_Stop.LogicDefinitionGuid, Logic_Stop.LogicDefinitionVersion) = await GetOrImportLogicDefinition(Logic_Stop.LogicDefinitionName, Logic_Stop.LogicDefinitionPath);
            await Logic_Stop.CreateSendAssignAndWaitAsync();

            return Logic_Stop;
        }

        async Task ILogicSimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to LogicObject if existing
            var mappings = new (FeeInterfaceSignal Signal, string SlotName)[]
            {
                (Signal_Open, LogicsStandard.Grob_Stop.Slots.Open),
                (Signal_Close, LogicsStandard.Grob_Stop.Slots.Close),
                (Signal_Opened, LogicsStandard.Grob_Stop.Slots.Opened),
                (Signal_Closed, LogicsStandard.Grob_Stop.Slots.Closed),
            };

            foreach (var (signal, slotname) in mappings)
            {
                if (signal != null)
                {
                    await signal.CreateSignalAsync(targetInterface);
                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Logic_Stop.Guid, slotname, signal.Guid, true);
                }
            }


            // Map parameters
            if (Parameter_CollisionDelay != -1)
            {
                Services.ApiInstance.Object.SetSlotValue(Logic_Stop.Guid, LogicsStandard.Grob_Stop.Slots.CollisionDelay, Parameter_CollisionDelay);
            }
        }

        async Task ILogicSimObjectOwner.CreateSimObjectsAsync()
        {
            if (!Floors_Stop.Any() && IsCreationRequested)
            {
                var floor = new FeeFloor()
                {
                    Name = this.ComponentName,
                    Parent = Logic_Stop,
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(0.01f, 0.2f, 0.05f),
                };

                await floor.CreateAsync();
                await floor.SendAndWaitAsync();
                Floors_Stop.Add(floor);
            }
        }

        async Task ILogicSimObjectOwner.AssignSimObjectsAsync()
        {
            if (Floors_Stop.Any())
            {
                // Lists with slot assignments for later assignment
                var slotsToAssignFloor = new List<(Guid, string)>() { (Logic_Stop.Guid, LogicsStandard.Grob_Stop.Slots.Collision) };

                foreach (var floor in Floors_Stop)
                {
                    slotsToAssignFloor.Add((floor.Guid, "Collision"));
                }

                // Assign all slots parallel
                await Services.ApiInstance.Interface.SendMultipleSlotSlotAssignmentsAsync(slotsToAssignFloor.Select(x => x.Item1).ToArray(), slotsToAssignFloor.Select(x => x.Item2).ToArray());

            }
        }


    }
}
