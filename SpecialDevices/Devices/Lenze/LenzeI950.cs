using FS.SDK.Io;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.SpecialDevices;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Services;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace MaNiAC_Tool.SpecialDevices.FieldDevices.Lenze
{
    public class LenzeI950 : SpecialDevice
    {

        //===========================================================================================================================
        // D E V I C E   S P E C I F I C   P R O P E R T I E S
        //===========================================================================================================================





        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public LenzeI950(string prefix, SpecialDeviceAddresses addresses)
            : base(prefix, addresses, DeviceManufacturer.Lenze, LenzeDeviceTypes.I950)
        {
            // Initialise BasicFrame
            DeviceBasicFrame = new FeeBasicFrame()
            {
                Name = DevicePrefix + " (" + DeviceType + ")"
            };

            // Initialise LogicObject
            DeviceLogicObject = new FeeLogic()
            {
                Name = DevicePrefix + " (" + DeviceType + ")",
                LogicDefinitionName = LogicsSpecialDevice.Lenze_i950.Name,
                LogicDefinitionPath = LogicsSpecialDevice.Lenze_i950.Path,
                Parent = DeviceBasicFrame,
            };

            InitializeSignals();


        }







        //===========================================================================================================================
        // D E F I N E   S I G N A L S
        //===========================================================================================================================

        protected override IEnumerable<SignalDefinition> DefineSignals()
        {
            return new[]
            {

                // Logic Read
                new SignalDefinition("I_PLC_Controlword_PO0_PO1", 0.0, IOMode.Read, IOType.DWord, "Steuer DWort 0-1"),
                new SignalDefinition("I_PLC_extVelocityOverride", 4.0, IOMode.Read, IOType.DInt, "Externer Geschwindigkeitsoverride (0.00%)"),
                new SignalDefinition("I_PLC_extSetVelocityOverride", 8.0, IOMode.Read, IOType.DInt, "Externe Geschwindigkeit (0.0000Units/s)"),
                new SignalDefinition("I_PLC_extSetPosition", 12.0, IOMode.Read, IOType.DInt, "Externe Position (0.0000Units)"),
                new SignalDefinition("I_PLC_extSetAcceleration", 16.0, IOMode.Read, IOType.DInt, "Externe Beschleunigung (0.0000Units/s2)"),
                new SignalDefinition("I_PLC_extSetDeceleration", 20.0, IOMode.Read, IOType.DInt, "Externe Verzögerung (0.0000Units/s2)"),
                new SignalDefinition("I_PLC_extSetTorque", 24.0, IOMode.Read, IOType.DInt, "Externes Moment (0.00%)"),
                new SignalDefinition("I_PLC_Safety_Output_Dword", 30.0, IOMode.Read, IOType.DWord, "Sicheres Ausgangs DWort"),

                // Logic Write
                new SignalDefinition("O_PLC_Status_DWord_PI0_PI1", 0.0, IOMode.Write, IOType.DWord, "Status DWort 0-1"),
                new SignalDefinition("O_PLC_Status_DWord_PI2_PI3", 4.0, IOMode.Write, IOType.DWord, "Status DWort 2-3"),
                new SignalDefinition("O_PLC_Actual_Velocity", 8.0, IOMode.Write, IOType.DInt, "Aktuelle Geschwindigkeit"),
                new SignalDefinition("O_PLC_Actual_Position", 1.0, IOMode.Write, IOType.DInt, "Aktuelle Position"),
                new SignalDefinition("O_PLC_errorModule", 1.0, IOMode.Write, IOType.Word, "Fehler Modul"),
                new SignalDefinition("O_PLC_errorNumber", 1.0, IOMode.Write, IOType.Word, "Fehlernummer"),
                new SignalDefinition("O_Safety_Status_DWord", 3.0, IOMode.Write, IOType.DWord, "Sicheres Eingangs DWort"),
            };
        }


        protected override string CalculateAddress(SpecialDeviceAddresses baseAddresses, double offset, IOMode ioMode, IOType ioType)
        {
            return PlcAddressCalculator.Calculate(baseAddresses, offset, ioMode, ioType);
        }






        //===========================================================================================================================
        // C R E A T E   D E V I C E
        //===========================================================================================================================

        protected override async Task<bool> WriteDeviceParameters()
        {
            var parameters = new List<DeviceParameter>
            {
                new(DeviceLogicObject.Guid, "Para_Transmissionfactor_Speed", 10000000),         // Umrechnung Units zu Meter (Integra: 16384 entspricht 100%)
                new(DeviceLogicObject.Guid, "Para_Transmissionfactor_Acceleration", 333333),    // Umrechnung Units zu Meter
                new(DeviceLogicObject.Guid, "Para_Transmissionfactor_Deceleration", 333333),    // Umrechnung Units zu Meter
                new(DeviceLogicObject.Guid, "Para_Setpoint_Speed_Fast", 0.6),                   // [m/s]
                new(DeviceLogicObject.Guid, "Para_Percent_Speed_Slow", 0.2),                    // [m/s]
                new(DeviceLogicObject.Guid, "Para_PLC_Max_Speed", 3),                           // Maximale Geschwindigkeit
                new(DeviceLogicObject.Guid, "Para_Default_RampTimeUp", 0.5),                    // [s]
                new(DeviceLogicObject.Guid, "Para_Default_RampTimeDown", 0.5),                  // [s]
                new(DeviceLogicObject.Guid, "Para_PLC_Min_Position", 0),                        // [Units]
                new(DeviceLogicObject.Guid, "Para_PLC_Max_Position", 100000000),                // [Units]
                //new(DeviceLogicObject.Guid, "Para_SIM_Min_Position", ""),                       // [m]
                //new(DeviceLogicObject.Guid, "Para_SIM_Max_Position", ""),                       // [m]
                //new(DeviceLogicObject.Guid, "Para_SIM_Min_SwCam", ""),                          // Software Endlage Minus
                //new(DeviceLogicObject.Guid, "Para_SIM_Max_SwCam", ""),                          // Software Endlage Plus
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
