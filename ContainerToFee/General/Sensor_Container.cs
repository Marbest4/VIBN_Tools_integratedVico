using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using DocumentFormat.OpenXml.Spreadsheet;
using FS.SDK.Mathematics;
using FS.SDK.Scene.Objects;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;

namespace VIBN_Tools.ContainerToFee.General
{
    public class Sensor_Container : ContainerBaseClass, ISimObjectFindOrSelect, ISimObjectOwner
    {

        public Sensor_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {"PLC_IN_PartPresent", typeof(Sensor_Container).GetProperty("Signal_PartPresent") },
                {"PLC_IN_PartPresent1", typeof(Sensor_Container).GetProperty("Signal_PartPresent1") },
                {"PLC_IN_PartPresent2", typeof(Sensor_Container).GetProperty("Signal_PartPresent2") },
                {"PLC_IN_NoPartPresent", typeof(Sensor_Container).GetProperty("Signal_NoPartPresent") },
                {"PLC_IN_NoPartPresent1", typeof(Sensor_Container).GetProperty("Signal_NoPartPresent1") },
                {"PLC_IN_NoPartPresent2", typeof(Sensor_Container).GetProperty("Signal_NoPartPresent2") },
            };
        }

        public FeeLogic Logic_Sensor { get; set; }

        public FeeInterfaceSignal Signal_PartPresent { get; set; }
        public FeeInterfaceSignal Signal_PartPresent1 { get; set; }
        public FeeInterfaceSignal Signal_PartPresent2 { get; set; }
        public FeeInterfaceSignal Signal_NoPartPresent { get; set; }
        public FeeInterfaceSignal Signal_NoPartPresent1 { get; set; }
        public FeeInterfaceSignal Signal_NoPartPresent2 { get; set; }

        public List<FeeSensor> Sensors { get; set; } = new List<FeeSensor>();
        public bool IsCreationRequested { get; set; }


        public FeeSimpleAnd FaultSimSimpleAnd { get; set; }
        public FeeSimpleOr FaultSimSimpleOr { get; set; }




        void ISimObjectFindOrSelect.FindSimObjects(ObservableCollection<FeeAbstractObject> mappableSimObjects)
        {
            Sensors = FindSimObjectsByNameAndType<FeeSensor>(mappableSimObjects);
        }

        void ISimObjectFindOrSelect.AssignSelectedSimObjectsToContainer(IEnumerable<FeeAbstractObject> selectedSimObjects)
        {
            Sensors = selectedSimObjects.OfType<FeeSensor>().ToList();
        }

        IEnumerable<IList> ISimObjectFindOrSelect.GetSimObjectLists()
        {
            yield return Sensors;
        }





        async Task ISimObjectOwner.CreateSimObjectsAsync(FeeAbstractObject parentObject)
        {
            // Create Sensor
            if (!Sensors.Any() && IsCreationRequested)
            {
                var sensor = new FeeSensor()
                {
                    Name = this.ComponentName,
                    Parent = parentObject,
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(0.03f, 0.1f, 0.03f),
                };

                await sensor.CreateAsync();
                await sensor.SendAndWaitAsync();
                Sensors.Add(sensor);
            }
        }

        async Task ISimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            var mappings = new List<SensorSignalMapping>
            {
                new(Signal_PartPresent,  "Channel1"),
                new(Signal_PartPresent1, "Channel1"),
                new(Signal_PartPresent2, null, true, Signal_PartPresent1),

                new(Signal_NoPartPresent,  "Channel2"),
                new(Signal_NoPartPresent1, "Channel2"),
                new(Signal_NoPartPresent2, null, true, Signal_NoPartPresent1),
            };


            foreach (var map in mappings)
            {
                if (map.Signal == null) continue;

                await map.Signal.CreateSignalAsync(targetInterface);

                // Assign move signal
                if (map.UseMoveBool && map.MoveSignal != null)
                {
                    var moveBool = new FeeSimpleMove();
                    await moveBool.CreateAsync();
                    await moveBool.SendAndWaitAsync();

                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(moveBool.Guid, "Input 01", map.MoveSignal.Guid, true);
                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(moveBool.Guid, "Output 01", map.Signal.Guid, true);
                }
                

                // Assign AND and OR block
                if(!map.UseMoveBool && map.Signal != null)
                {
                    FaultSimSimpleAnd = new FeeSimpleAnd();
                    await FaultSimSimpleAnd.CreateAsync();
                    await FaultSimSimpleAnd.SendAndWaitAsync();

                    FaultSimSimpleOr = new FeeSimpleOr();
                    await FaultSimSimpleOr.CreateAsync();
                    await FaultSimSimpleOr.SendAndWaitAsync();

                    if (map.SlotName == "Channel1")
                    {
                        await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(FaultSimSimpleAnd.Guid, "Output 01", map.Signal.Guid, true);
                    }
                    if (map.SlotName == "Channel2")
                    {
                        await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(FaultSimSimpleOr.Guid, "Output 01", map.Signal.Guid, true);
                    }

                }

            }


            if (Sensors != null)
            {
                foreach (var sensor in Sensors)
                {
                    await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(sensor.Guid, "Channel1", FaultSimSimpleAnd.Guid, "Input 01");
                    await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(sensor.Guid, "Channel2", FaultSimSimpleOr.Guid, "Input 01");
                }
            }

        }



        public SensorFaultSim_Container CreateFaultSimCabinetElementContainer()
        {
            return new SensorFaultSim_Container(this);
        }






        public record SensorSignalMapping(
            FeeInterfaceSignal Signal,
            string SlotName,
            bool UseMoveBool = false,
            FeeInterfaceSignal MoveSignal = null
        );


}
}
