using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
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
    public class GrobGripperBasic_Container : ContainerBaseClass, ISimObjectFindOrSelect, ILogicSimObjectOwner
    {

        public GrobGripperBasic_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {LogicsStandard.Grob_GripperBasic.Slots.Unclamp, typeof(GrobGripperBasic_Container).GetProperty(nameof(Signal_Unclamp)) },
                {LogicsStandard.Grob_GripperBasic.Slots.Clamp, typeof(GrobGripperBasic_Container).GetProperty(nameof(Signal_Clamp)) },

                {LogicsStandard.Grob_GripperBasic.Slots.Unclamped, typeof(GrobGripperBasic_Container).GetProperty(nameof(Signals_Unclamped)) },
                {LogicsStandard.Grob_GripperBasic.Slots.Clamped, typeof(GrobGripperBasic_Container).GetProperty(nameof(Signals_Clamped)) },
                {LogicsStandard.Grob_GripperBasic.Slots.ClampedWithPart, typeof(GrobGripperBasic_Container).GetProperty(nameof(Signals_ClampedWithPart)) },
                {LogicsStandard.Grob_GripperBasic.Slots.ClampedNoPart, typeof(GrobGripperBasic_Container).GetProperty(nameof(Signals_ClampedNoPart)) },

                {LogicsStandard.Grob_GripperBasic.Slots.ReleaseClamping, typeof(GrobGripperBasic_Container).GetProperty(nameof(Signal_ReleaseClamping)) },
                {LogicsStandard.Grob_GripperBasic.Slots.ClampingReleased, typeof(GrobGripperBasic_Container).GetProperty(nameof(Signal_ClampingReleased)) },
            };
        }



        public FeeLogic Logic_Gripper { get; set; }

        public FeeInterfaceSignal Signal_Unclamp { get; set; }
        public FeeInterfaceSignal Signal_Clamp { get; set; }

        public List<FeeInterfaceSignal> Signals_Unclamped { get; set; }
        public List<FeeInterfaceSignal> Signals_Clamped { get; set; }
        public List<FeeInterfaceSignal> Signals_ClampedWithPart { get; set; }
        public List<FeeInterfaceSignal> Signals_ClampedNoPart { get; set; }

        public List<FeeInterfaceSignal> Signals_Unclamped_Analog { get; set; }
        public List<FeeInterfaceSignal> Signals_Clamped_Analog { get; set; }
        public List<FeeInterfaceSignal> Signals_ClampedWithPart_Analog { get; set; }
        public List<FeeInterfaceSignal> Signals_ClampedNoPart_Analog { get; set; }

        public FeeInterfaceSignal Signal_ReleaseClamping { get; set; }
        public FeeInterfaceSignal Signal_ClampingReleased { get; set; }

        public List<FeeJoint> Joints_Gripper { get; set; } = new List<FeeJoint>();
        public List<FeePickAndPlace> PickPlacers_Gripper { get; set; } = new List<FeePickAndPlace>();

        public List<IAddonContainer> Addons { get; set; } = new();

        public bool SimObjectAssigned => Joints_Gripper != null;

        public float Parameter_UnclampedPosition { get; set; } = -1f;
        public float Parameter_ClampedPosition { get; set; } = -1f;
        public float Parameter_OperationTime { get; set; } = -1f;

        public bool IsCreationRequested { get; set; }




        void ISimObjectFindOrSelect.FindSimObjects(ObservableCollection<FeeAbstractObject> mappableSimObjects)
        {
            Joints_Gripper = FindSimObjectsByNameAndType<FeeJoint>(mappableSimObjects);
            PickPlacers_Gripper = FindSimObjectsByNameAndType<FeePickAndPlace>(mappableSimObjects);
        }


        IEnumerable<SimObjectTarget> ISimObjectFindOrSelect.GetSimObjectTargets()
        {
            yield return new SimObjectTarget()
            {
                DisplayName = "MotionJoints",

                AllowedType = typeof(FeeJoint),

                AllowMultiSelect = true,

                GetObjects = () => Joints_Gripper,

                AssignObjects = objects =>
                {
                    Joints_Gripper = objects.OfType<FeeJoint>().ToList();
                }
            };

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
                LogicDefinitionName = LogicsStandard.Grob_GripperBasic.Name,
                LogicDefinitionPath = LogicsStandard.Grob_GripperBasic.Path,
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
                (Signal_Unclamp, LogicsStandard.Grob_GripperBasic.Slots.Unclamp),
                (Signal_Clamp, LogicsStandard.Grob_GripperBasic.Slots.Clamp),
                (Signal_ReleaseClamping, LogicsStandard.Grob_GripperBasic.Slots.ReleaseClamping),
                (Signal_ClampingReleased, LogicsStandard.Grob_GripperBasic.Slots.ClampingReleased),                
            };

            var listMappings = new (List<FeeInterfaceSignal> Signals, string SlotName)[]
            {
                (Signals_Unclamped, LogicsStandard.Grob_GripperBasic.Slots.Unclamped),
                (Signals_Clamped, LogicsStandard.Grob_GripperBasic.Slots.Clamped),
                (Signals_ClampedWithPart, LogicsStandard.Grob_GripperBasic.Slots.ClampedWithPart),
                (Signals_ClampedNoPart, LogicsStandard.Grob_GripperBasic.Slots.ClampedNoPart),
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

            // Map parameters
            var parametermapping = new (Guid ObjectGuid, string SlotName, object Value)[]
            {
                (Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.UnclampedPos, Parameter_UnclampedPosition),
                (Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.ClampedPos,   Parameter_ClampedPosition),
                (Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.OperationTime, Parameter_OperationTime),
            };

            var guids = parametermapping.Select(x => x.ObjectGuid).ToArray();
            var slotNames = parametermapping.Select(x => x.SlotName).ToArray();
            var values = parametermapping.Select(x => x.Value).ToArray();

            await Services.ApiInstance.Object.SetSlotValuesAsync(guids, slotNames, values);
        }

        async Task ILogicSimObjectOwner.CreateSimObjectsAsync()
        {
            if (!Joints_Gripper.Any() && IsCreationRequested)
            {
                var jointA = new FeeJoint()
                {
                    Name = $"{this.ComponentName}-a",
                    Parent = Logic_Gripper,
                    JointType = MotionType.Translate,
                    ControlType = MotionSource.Position,
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(0.5f, 0.5f, 0.5f),
                };

                await jointA.CreateAsync();
                await jointA.SendAndWaitAsync();
                Joints_Gripper.Add(jointA);

                var jointB = new FeeJoint()
                {
                    Name = $"{this.ComponentName}-b",
                    Parent = Logic_Gripper,
                    JointType = MotionType.Translate,
                    ControlType = MotionSource.Position,
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(0.5f, 0.5f, 0.5f),
                };

                await jointB.CreateAsync();
                await jointB.SendAndWaitAsync();
                Joints_Gripper.Add(jointB);


            }

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
            if (Joints_Gripper.Any())
            {
                bool isActualPosAssigned = false;

                // Lists with slot assignments for later assignment
                var slotsToAssignTarget = new List<(Guid, string)>() { (Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.TargetPosition) };
                var slotsToAssignVelocity = new List<(Guid, string)>() { (Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.Velocity) };

                foreach (var joint in Joints_Gripper)
                {
                    // Set ControlType to Position
                    Services.ApiInstance.Object.CreateObject(nameof(MotionJoint), joint.Guid);
                    await Services.ApiInstance.Object.SetPropertyAsync(joint.Guid, nameof(JointControllerComponent.MotionSource), MotionSource.Position, "Controller");
                    await Services.ApiInstance.Object.SendAndWait(joint.Guid);

                    if (!isActualPosAssigned)
                    {
                        isActualPosAssigned = await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.ActualPosition, joint.Guid, "OutValue");
                    }

                    slotsToAssignTarget.Add((joint.Guid, "InTarget"));
                    slotsToAssignVelocity.Add((joint.Guid, "InVelocity"));
                }

                // Assign all slots parallel
                await Services.ApiInstance.Interface.SendMultipleSlotSlotAssignmentsAsync(slotsToAssignTarget.Select(x => x.Item1).ToArray(), slotsToAssignTarget.Select(x => x.Item2).ToArray());
                await Services.ApiInstance.Interface.SendMultipleSlotSlotAssignmentsAsync(slotsToAssignVelocity.Select(x => x.Item1).ToArray(), slotsToAssignVelocity.Select(x => x.Item2).ToArray());


            }

            if (PickPlacers_Gripper.Any())
            {
                bool isPartPickedAssigned = false;

                // Lists with slot assignments for later assignment
                var slotsToAssignPick = new List<(Guid, string)>() { (Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.Pick) };
                var slotsToAssignDrop = new List<(Guid, string)>() { (Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.Drop) };

                foreach (var pickplace in PickPlacers_Gripper)
                {
                    if (!isPartPickedAssigned)
                    {
                        isPartPickedAssigned = await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.PartPicked, pickplace.Guid, "Feedback");
                    }
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
