using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FS.SDK.Scene.Objects;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeCabinetElement;

namespace VIBN_Tools.ContainerToFee.General
{
    public class SensorFaultSim_Container : ContainerBaseClass, ICabinetElementOwner
    {

        private readonly Sensor_Container _sensor;

        public SensorFaultSim_Container(Sensor_Container sensor)
        {
            _sensor = sensor;
        }


        public FeeCabinetElement CabinetElement_Switch { get; set; }



        public string CabinetName => "Sensor Fault Sim";

        public Vector2 ElementPosition { get; set; }
        public bool IsCreationRequested { get; set; }




        async Task ISimObjectOwner.CreateSimObjectsAsync(FeeAbstractObject parentObject)
        {
            // Create CabinetElement
            CabinetElement_Switch = new FeeCabinetElement()
            {
                Parent = parentObject,
                ElementType = CabinetElementType.PositionSwitch2,
                PositionX = ElementPosition.X,
                PositionY = ElementPosition.Y,
                Name = $"Set fault: {_sensor.ComponentName}",
                Label = $"Set fault: {_sensor.ComponentName}",
                Tooltip = $"Set fault for signal '{_sensor.ComponentName}'",
            };

            await CabinetElement_Switch.CreateAndSendAsync();
        }

        async Task ISimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            // Map CabinetElement to Sensor AND and OR blocks
            await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(CabinetElement_Switch.Guid, "NC1", _sensor.FaultSimSimpleAnd.Guid, "Input 02");
            await Services.ApiInstance.Interface.SendSlotSlotAssignmentAsync(CabinetElement_Switch.Guid, "NO1", _sensor.FaultSimSimpleOr.Guid, "Input 02");

        }

        
    }
}
