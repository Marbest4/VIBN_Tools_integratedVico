using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics.Tensors;
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
    public class GrobGripperAddon_MultiPartTypes_Container : ContainerBaseClass, IAddonContainer<GrobGripperBasic_Container>, ISimObjectFindOrSelect, ILogicSimObjectOwner
    {

        public GrobGripperAddon_MultiPartTypes_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {

                {LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.ClampedWithPart1, typeof(GrobGripperAddon_MultiPartTypes_Container).GetProperty(nameof(Signal_ClampedWithType1)) },
                {LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.ClampedWithPart2, typeof(GrobGripperAddon_MultiPartTypes_Container).GetProperty(nameof(Signal_ClampedWithType2)) },
                {LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.ClampedWithPart3, typeof(GrobGripperAddon_MultiPartTypes_Container).GetProperty(nameof(Signal_ClampedWithType3)) },
            };
        }


        public FeeLogic Logic_Addon { get; set; }
        public GrobGripperBasic_Container ParentContainer { get; set; }

        public FeeInterfaceSignal Signal_ClampedWithType1 { get; set; }
        public FeeInterfaceSignal Signal_ClampedWithType2 { get; set; }
        public FeeInterfaceSignal Signal_ClampedWithType3 { get; set; }


        public FeeSensor Sensor_Type1 { get; set; }
        public FeeSensor Sensor_Type2 { get; set; }
        public FeeSensor Sensor_Type3 { get; set; }


        public float Parameter_ClampedPos_Type1 { get; set; }
        public float Parameter_ClampedPos_Type2 { get; set; }
        public float Parameter_ClampedPos_Type3 { get; set; }
        public float Parameter_ClampedPos_NoPart { get; set; }


        public bool IsCreationRequested { get; set; }




        void ISimObjectFindOrSelect.FindSimObjects(ObservableCollection<FeeAbstractObject> mappableSimObjects)
        {
            Sensor_Type1 = FindSimObjectsByNameAndType<FeeSensor>(mappableSimObjects).FirstOrDefault();
            Sensor_Type2 = FindSimObjectsByNameAndType<FeeSensor>(mappableSimObjects).FirstOrDefault();
            Sensor_Type3 = FindSimObjectsByNameAndType<FeeSensor>(mappableSimObjects).FirstOrDefault();
        }

        IEnumerable<SimObjectTarget> ISimObjectFindOrSelect.GetSimObjectTargets()
        {
            yield return new SimObjectTarget()
            {
                AllowedType = typeof(FeeSensor),
                AllowMultiSelect = false,
                DisplayName = $"Sensor Type1",

                GetObjects = () => Sensor_Type1 != null ? new[] {Sensor_Type1} : Enumerable.Empty<FeeAbstractObject>(),

                AssignObjects = objects =>
                {
                    Sensor_Type1 = objects.OfType<FeeSensor>().FirstOrDefault();
                }
            };

            yield return new SimObjectTarget()
            {
                AllowedType = typeof(FeeSensor),
                AllowMultiSelect = false,
                DisplayName = $"Sensor Type2",

                GetObjects = () => Sensor_Type2 != null ? new[] { Sensor_Type2 } : Enumerable.Empty<FeeAbstractObject>(),

                AssignObjects = objects =>
                {
                    Sensor_Type2 = objects.OfType<FeeSensor>().FirstOrDefault();
                }
            };

            yield return new SimObjectTarget()
            {
                AllowedType = typeof(FeeSensor),
                AllowMultiSelect = false,
                DisplayName = $"Sensor Type3",

                GetObjects = () => Sensor_Type3 != null ? new[] { Sensor_Type3 } : Enumerable.Empty<FeeAbstractObject>(),

                AssignObjects = objects =>
                {
                    Sensor_Type3 = objects.OfType<FeeSensor>().FirstOrDefault();
                }
            };            
        }



        async Task<FeeLogic> ILogicSimObjectOwner.CreateLogicAsync(FeeAbstractObject parentObject)
        {
            Logic_Addon = new FeeLogic()
            {
                Name = this.ComponentName,
                LogicDefinitionName = LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Name,
                LogicDefinitionPath = LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Path,
                Parent = parentObject,
            };

            (Logic_Addon.LogicDefinitionGuid, Logic_Addon.LogicDefinitionVersion) = await GetOrImportLogicDefinition(Logic_Addon.LogicDefinitionName, Logic_Addon.LogicDefinitionPath);
            await Logic_Addon.CreateSendAssignAndWaitAsync();

            return Logic_Addon;
        }


        async Task ILogicSimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to LogicObject if existing
            var singleMappings = new (FeeInterfaceSignal Signal, string SlotName)[]
            {
                (Signal_ClampedWithType1, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.ClampedWithPart1),
                (Signal_ClampedWithType2, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.ClampedWithPart2),
                (Signal_ClampedWithType3, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.ClampedWithPart3),
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
                (Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.ClampedPosition1, Parameter_ClampedPos_Type1),
                (Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.ClampedPosition2, Parameter_ClampedPos_Type2),
                (Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.ClampedPosition3, Parameter_ClampedPos_Type3),
                (Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.ClampedPositionNoPart, Parameter_ClampedPos_NoPart),
            };

            var guids = parametermapping.Select(x => x.ObjectGuid).ToArray();
            var slotNames = parametermapping.Select(x => x.SlotName).ToArray();
            var values = parametermapping.Select(x => x.Value).ToArray();

            await Services.ApiInstance.Object.SetSlotValuesAsync(guids, slotNames, values);
        }


        async Task ILogicSimObjectOwner.CreateSimObjectsAsync()
        {
            if (Sensor_Type1 == null && IsCreationRequested)
            {
                var sensor = new FeeSensor()
                {
                    Name = $"{this.ComponentName} Detect Type1",
                    Parent = Logic_Addon,
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(0.1f, 0.1f, 0.05f),
                };

                await sensor.CreateAsync();
                await sensor.SendAndWaitAsync();
                Sensor_Type1 = sensor;          
            }

            if (Sensor_Type2 == null && IsCreationRequested)
            {
                var sensor = new FeeSensor()
                {
                    Name = $"{this.ComponentName} Detect Type2",
                    Parent = Logic_Addon,
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(0.1f, 0.1f, 0.05f),
                };

                await sensor.CreateAsync();
                await sensor.SendAndWaitAsync();
                Sensor_Type2 = sensor;
            }

            if (Sensor_Type3 == null && IsCreationRequested)
            {
                var sensor = new FeeSensor()
                {
                    Name = $"{this.ComponentName} Detect Type3",
                    Parent = Logic_Addon,
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(0.1f, 0.1f, 0.05f),
                };

                await sensor.CreateAsync();
                await sensor.SendAndWaitAsync();
                Sensor_Type3 = sensor;
            }
        }


        async Task ILogicSimObjectOwner.AssignSimObjectsAsync()
        {
            if (Sensor_Type1 != null)
            {
                await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(Sensor_Type1.Guid, "Channel1", Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.PartPresent1);
            }
            if (Sensor_Type2 != null)
            {
                await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(Sensor_Type2.Guid, "Channel1", Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.PartPresent2);
            }
            if (Sensor_Type3 != null)
            {
                await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(Sensor_Type3.Guid, "Channel1", Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.PartPresent3);
            }
        }





        async Task IAddonContainer.ConnectToParentAsync()
        {
            if (ParentContainer == null)
                return;

            if (ParentContainer.Logic_Gripper == null)
                return;

            await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(ParentContainer.Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.AddOnStatus, Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.AddOnStatus);
            await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(ParentContainer.Logic_Gripper.Guid, LogicsStandard.Grob_GripperBasic.Slots.ClampedPos, Logic_Addon.Guid, LogicsAddons.Grob_GripperAddOn_MultiplePartTypes.Slots.TargetPosition);
        }


    }
}
