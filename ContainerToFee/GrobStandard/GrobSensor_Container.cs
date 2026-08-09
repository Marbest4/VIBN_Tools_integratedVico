using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FS.SDK.Mathematics;
using FS.SDK.Scene.Objects;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.GrobStandard
{
    public class GrobSensor_Container : ContainerBaseClass, ISimObjectFindOrSelect, ILogicSimObjectOwner
    {


        public GrobSensor_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {LogicsStandard.Grob_Sensor.Slots.PartPresent_Ch1, typeof(GrobSensor_Container).GetProperty(nameof(Signal_PartPresent_Ch1)) },
                {LogicsStandard.Grob_Sensor.Slots.PartPresent_Ch2, typeof(GrobSensor_Container).GetProperty(nameof(Signal_PartPresent_Ch2)) },
                {LogicsStandard.Grob_Sensor.Slots.NoPartPresent_Ch1, typeof(GrobSensor_Container).GetProperty(nameof(Signal_NoPartPresent_Ch1)) },
                {LogicsStandard.Grob_Sensor.Slots.NoPartPresent_Ch2, typeof(GrobSensor_Container).GetProperty(nameof(Signal_NoPartPresent_Ch2)) },
            };
        }


        public FeeLogic Logic_Sensor { get; set; }

        public FeeInterfaceSignal Signal_PartPresent_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_PartPresent_Ch2 { get; set; }
        public FeeInterfaceSignal Signal_NoPartPresent_Ch1 { get; set; }
        public FeeInterfaceSignal Signal_NoPartPresent_Ch2 { get; set; }

        public FeeSensor Sensor { get; set; }

        public bool IsCreationRequested { get; set; }




        void ISimObjectFindOrSelect.FindSimObjects(ObservableCollection<FeeAbstractObject> mappableSimObjects)
        {
            Sensor = FindSimObjectsByNameAndType<FeeSensor>(mappableSimObjects).FirstOrDefault();
        }

        IEnumerable<SimObjectTarget> ISimObjectFindOrSelect.GetSimObjectTargets()
        {
            yield return new SimObjectTarget()
            {
                DisplayName = "Sensor",

                AllowedType = typeof(FeeSensor),

                AllowMultiSelect = false,

                GetObjects = () => Sensor != null ? new[] { Sensor } : Enumerable.Empty<FeeAbstractObject>(),

                AssignObjects = objects =>
                {
                    Sensor = objects.OfType<FeeSensor>().FirstOrDefault();
                }
            };
        }




        async Task<FeeLogic> ILogicSimObjectOwner.CreateLogicAsync(FeeAbstractObject parentObject)
        {
            // Reference right Logic Version and Guid to create LogicObject
            Logic_Sensor = new FeeLogic()
            {
                Name = this.ComponentName,
                LogicDefinitionName = LogicsStandard.Grob_Sensor.Name,
                LogicDefinitionPath = LogicsStandard.Grob_Sensor.Path,
                Parent = parentObject,
            };

            (Logic_Sensor.LogicDefinitionGuid, Logic_Sensor.LogicDefinitionVersion) = await GetOrImportLogicDefinition(Logic_Sensor.LogicDefinitionName, Logic_Sensor.LogicDefinitionPath);
            await Logic_Sensor.CreateSendAssignAndWaitAsync();

            return Logic_Sensor;
        }

        async Task ILogicSimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map signals to LogicObject if existing
            var mappings = new (FeeInterfaceSignal Signal, string SlotName)[]
            {
                (Signal_PartPresent_Ch1, LogicsStandard.Grob_Sensor.Slots.PartPresent_Ch1),
                (Signal_PartPresent_Ch2, LogicsStandard.Grob_Sensor.Slots.PartPresent_Ch2),
                (Signal_NoPartPresent_Ch1, LogicsStandard.Grob_Sensor.Slots.NoPartPresent_Ch1),
                (Signal_NoPartPresent_Ch2, LogicsStandard.Grob_Sensor.Slots.NoPartPresent_Ch2),
            };

            foreach (var (signal, slotname) in mappings)
            {
                if (signal != null)
                {
                    await signal.CreateSignalAsync(targetInterface);
                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Logic_Sensor.Guid, slotname, signal.Guid, true);
                }
            }
        }


        async Task ILogicSimObjectOwner.CreateSimObjectsAsync()
        {
            if (Sensor == null && IsCreationRequested)
            {
                var sensor = new FeeSensor()
                {
                    Name = this.ComponentName,
                    Parent = Logic_Sensor,
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(0.01f, 0.03f, 0.01f),
                };

                await sensor.CreateAsync();
                await sensor.SendAndWaitAsync();
                Sensor = sensor;
            }
        }

        async Task ILogicSimObjectOwner.AssignSimObjectsAsync()
        {
            if (Sensor != null)
            {
                await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(Sensor.Guid, "Channel1", Logic_Sensor.Guid, LogicsStandard.Grob_Sensor.Slots.SensorValue);

            }
        }

    }
}
