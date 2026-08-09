using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FS.SDK.Components;
using FS.SDK.Mathematics;
using FS.SDK.Scene.Objects;
using FS.SDK.Utilities;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.GrobStandard
{
    public class GrobGripperVacuum_Container : ContainerBaseClass, ISimObjectFindOrSelect, ILogicSimObjectOwner
    {

        public GrobGripperVacuum_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {LogicsStandard.Grob_GripperVacuum.Slots.VacuumOn, typeof(GrobGripperVacuum_Container).GetProperty(nameof(Signal_VacuumOn)) },
                {LogicsStandard.Grob_GripperVacuum.Slots.VacuumOff, typeof(GrobGripperVacuum_Container).GetProperty(nameof(Signal_VacuumOff)) },
                {LogicsStandard.Grob_GripperVacuum.Slots.BlowAirOn, typeof(GrobGripperVacuum_Container).GetProperty(nameof(Signal_BlowAirOn)) },

                {LogicsStandard.Grob_GripperVacuum.Slots.VacuumPressureOk, typeof(GrobGripperVacuum_Container).GetProperty(nameof(Signals_PressureOk)) },

            };
        }



        public FeeLogic Logic_Gripper { get; set; }

        public FeeInterfaceSignal Signal_VacuumOn { get; set; }
        public FeeInterfaceSignal Signal_VacuumOff { get; set; }
        public FeeInterfaceSignal Signal_BlowAirOn { get; set; }
        public List<FeeInterfaceSignal> Signals_PressureOk { get; set; }

        public List<FeePickAndPlace> PickPlacers_Gripper { get; set; } = new List<FeePickAndPlace>();
        public bool SimObjectAssigned => PickPlacers_Gripper != null;

        public bool IsCreationRequested { get; set; }




        void ISimObjectFindOrSelect.FindSimObjects(ObservableCollection<FeeAbstractObject> mappableSimObjects)
        {
            PickPlacers_Gripper = FindSimObjectsByNameAndType<FeePickAndPlace>(mappableSimObjects);
        }

        IEnumerable<SimObjectTarget> ISimObjectFindOrSelect.GetSimObjectTargets()
        {
            yield return new SimObjectTarget()
            {
                DisplayName = "Pick&Placers",

                AllowedType = typeof(FeePickAndPlace),

                AllowMultiSelect = true,

                GetObjects = () => PickPlacers_Gripper,

                AssignObjects = objects =>
                {
                    PickPlacers_Gripper = objects.OfType<FeePickAndPlace>().ToList();
                }
            };
        }



        async Task<FeeLogic> ILogicSimObjectOwner.CreateLogicAsync(FeeAbstractObject parentObject)
        {
            Logic_Gripper = new FeeLogic()
            {
                Name = this.ComponentName,
                LogicDefinitionName = LogicsStandard.Grob_GripperVacuum.Name,
                LogicDefinitionPath = LogicsStandard.Grob_GripperVacuum.Path,
                Parent = parentObject,
            };

            (Logic_Gripper.LogicDefinitionGuid, Logic_Gripper.LogicDefinitionVersion) = await GetOrImportLogicDefinition(Logic_Gripper.LogicDefinitionName, Logic_Gripper.LogicDefinitionPath);
            await Logic_Gripper.CreateSendAssignAndWaitAsync();

            return Logic_Gripper;
        }

        async Task ILogicSimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to LogicObject if existing
            var singleMappings = new (FeeInterfaceSignal Signal, string SlotName)[]
            {
                (Signal_VacuumOn, LogicsStandard.Grob_GripperVacuum.Slots.VacuumOn),
                (Signal_VacuumOff, LogicsStandard.Grob_GripperVacuum.Slots.VacuumOff),
                (Signal_BlowAirOn, LogicsStandard.Grob_GripperVacuum.Slots.BlowAirOn),
            };

            var listMappings = new (List<FeeInterfaceSignal> Signals, string SlotName)[]
            {
                (Signals_PressureOk, LogicsStandard.Grob_GripperVacuum.Slots.VacuumPressureOk),
            };

            foreach (var (signal, slotname) in singleMappings)
            {
                if (signal != null)
                {
                    await signal.CreateSignalAsync(targetInterface);
                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Logic_Gripper.Guid, slotname, signal.Guid, true);
                }
            }

            foreach (var (signals, slotName) in listMappings)
            {
                if (signals == null) continue;

                // Save Slot Assignments for parallel creation, initialize with Logic slot
                var slotsToAssign = new List<(Guid, string)>() { (Logic_Gripper.Guid, slotName) };

                foreach (var signal in signals)
                {
                    FeeSimpleMove moveBit = new FeeSimpleMove();
                    await moveBit.CreateAsync();
                    await moveBit.SendAndWaitAsync();

                    await signal.CreateSignalAsync(targetInterface);

                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(moveBit.Guid, "Output 01", signal.Guid, true);

                    // Add current assignment information
                    slotsToAssign.Add((moveBit.Guid, "Input 01"));
                }

                // Assign slots parallel
                await Services.ApiInstance.Interface.SendMultipleSlotSlotAssignmentsAsync(slotsToAssign.Select(x => x.Item1).ToArray(), slotsToAssign.Select(x => x.Item2).ToArray());

            }
        }

        async Task ILogicSimObjectOwner.CreateSimObjectsAsync()
        {
            
            if (!PickPlacers_Gripper.Any() && IsCreationRequested)
            {
                var pickplace = new FeePickAndPlace()
                {
                    Name = this.ComponentName,
                    Parent = Logic_Gripper,
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(0.1f, 0.1f, 0.1f),
                    PickRange = 0.25f,
                    DropRange = 0.5f,
                };

                await pickplace.CreateAsync();
                await pickplace.SendAndWaitAsync();
                PickPlacers_Gripper.Add(pickplace);
            }
        }

        async Task ILogicSimObjectOwner.AssignSimObjectsAsync()
        {
            if (PickPlacers_Gripper.Any())
            {
                //bool isPartPickedAssigned = false;

                // Lists with slot assignments for later assignment
                var slotsToAssignPick = new List<(Guid, string)>() { (Logic_Gripper.Guid, LogicsStandard.Grob_GripperVacuum.Slots.Pick) };
                var slotsToAssignDrop = new List<(Guid, string)>() { (Logic_Gripper.Guid, LogicsStandard.Grob_GripperVacuum.Slots.Drop) };

                foreach (var pickplace in PickPlacers_Gripper)
                {
                    //if (!isPartPickedAssigned)
                    //{
                    //    isPartPickedAssigned = await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(Logic_Gripper.Guid, LogicsStandard.Grob_GripperVacuum.Slots.PartPicked, pickplace.Guid, "Feedback");
                    //}
                    slotsToAssignPick.Add((pickplace.Guid, "Pick"));
                    slotsToAssignDrop.Add((pickplace.Guid, "Drop"));

                }

                // Assign all slots parallel
                await Services.ApiInstance.Interface.SendMultipleSlotSlotAssignmentsAsync(slotsToAssignPick.Select(x => x.Item1).ToArray(), slotsToAssignPick.Select(x => x.Item2).ToArray());
                await Services.ApiInstance.Interface.SendMultipleSlotSlotAssignmentsAsync(slotsToAssignDrop.Select(x => x.Item1).ToArray(), slotsToAssignDrop.Select(x => x.Item2).ToArray());

            }
        }




    }
}
