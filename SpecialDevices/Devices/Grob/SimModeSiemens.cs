using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS.SDK.Io;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;
using static VIBN_Tools.GlobalClasses.Services;

namespace VIBN_Tools.SpecialDevices.Devices.Grob
{
    public class SimModeSiemens : SpecialDevice
    {

        public SimModeSiemens(string prefix, SpecialDeviceAddresses addresses)
            : base(prefix, addresses, DeviceManufacturer.Grob, GrobDeviceTypes.SimModeSiemens)
        {

            // Initialise BasicFrame
            DeviceBasicFrame = new FeeBasicFrame()
            {
                Name = "SimMode",
            };


            // Initialise LogicObject
            DeviceLogicObject = new FeeLogic()
            {
                Name = DevicePrefix + " (" + DeviceType + ")",
                LogicDefinitionName = LogicsStandard.Grob_SimModeSiemens.Name,
                LogicDefinitionPath = LogicsStandard.Grob_SimModeSiemens.Path,

                Parent = DeviceBasicFrame,
            };

            DeviceSignals = new List<FeeInterfaceSignal>
            {
                new FeeInterfaceSignal("viCo_Mode_FB_IDB.toSim.LifeTime", String.Empty, "Read", "Bool" ,"PLC_OUT_Lifetime"),
                new FeeInterfaceSignal("viCo_Mode_FB_IDB.toSim.simulationDeactivated", String.Empty, "Read", "Bool" ,"PLC_OUT_SimDeactivated"),
                new FeeInterfaceSignal("viCo_Mode_FB_IDB.toSim.simulationActivated", String.Empty, "Read", "Bool" ,"PLC_OUT_SimActivated"),
                new FeeInterfaceSignal("viCo_Mode_FB_IDB.toSim.SafetyBypassed", String.Empty, "Read", "Bool" ,"PLC_OUT_SafetyBypassed"),

                new FeeInterfaceSignal("viCo_Mode_FB_IDB.fromSim.LifeTime", String.Empty, "Write", "Bool" ,"PLC_IN_Lifetime"),
                new FeeInterfaceSignal("viCo_Mode_FB_IDB.fromSim.deactivateAllSimulationModes", String.Empty, "Write", "Bool" ,"PLC_IN_DeactivateSim"),
                new FeeInterfaceSignal("viCo_Mode_FB_IDB.fromSim.activateSimulation", String.Empty, "Write", "Bool" ,"PLC_IN_ActivateSim"),
                new FeeInterfaceSignal("viCo_Mode_FB_IDB.fromSim.activateBypassSafety", String.Empty, "Write", "Bool" ,"PLC_IN_BypassSafety"),

            };



        }





        protected override async Task<bool> WriteDeviceParameters()
        {
            var parameters = new List<DeviceParameter>
            {
                new(DeviceLogicObject.Guid, "PAR_Lifetime_sec", 1),         // Standard: impulse time 1 second
            };

            var guids = parameters.Select(p => p.ObjectGuid).ToArray();
            var slots = parameters.Select(p => p.SlotName).ToArray();
            var values = parameters.Select(p => p.Value).ToArray();


            if (!await ApiInstance.Object.SetSlotValuesAsync(guids, slots, values))
                return false;

            return true;

        }



        protected override async Task<bool> CreateDeviceSpecificAsync() => true;

        
    }
}
