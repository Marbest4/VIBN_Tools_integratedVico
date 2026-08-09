using FS.SDK.Io;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.SpecialDevices;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Services;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace MaNiAC_Tool.SpecialDevices.FieldDevices.Lenze
{
    public class Lenze8400Motec : SpecialDevice
    {

        //===========================================================================================================================
        // D E V I C E   S P E C I F I C   P R O P E R T I E S
        //===========================================================================================================================





        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public Lenze8400Motec(string prefix, SpecialDeviceAddresses addresses)
            : base(prefix, addresses, DeviceManufacturer.Lenze, LenzeDeviceTypes.Motec8400)
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
                LogicDefinitionName = LogicsSpecialDevice.Lenze_8400Motec.Name,
                LogicDefinitionPath = LogicsSpecialDevice.Lenze_8400Motec.Path,
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
                new SignalDefinition("I_PLC_PO_0_Controlword", 0.0, IOMode.Read, IOType.Word, "PA0: Steuerwort 0"),
                new SignalDefinition("I_PLC_PO_1_MainSetValue", 2.0, IOMode.Read, IOType.Int, "PA1: Drehzahl entspricht 16384"),

                // Logic Write
                new SignalDefinition("O_PLC_PI_0_Statusword", 0.0, IOMode.Write, IOType.Word, "PE0: Statuswort 0"),
                new SignalDefinition("O_PLC_PI_1_Speed", 2.0, IOMode.Write, IOType.Int, "PE1: Motorendrehzahl in %"),
                new SignalDefinition("O_PLC_PI_2_FailNoHigh", 4.0, IOMode.Write, IOType.Word, "PE2: Fehlernummer High-Word"),
                new SignalDefinition("O_PLC_PI_3_FailNoLow", 6.0, IOMode.Write, IOType.Word, "PE3: Fehlernummer Low-Word"),
                new SignalDefinition("O_PLC_PI_4_IO_Stat_1", 8.6, IOMode.Write, IOType.Bool, "PE4: I/O Daten gültig"),
                new SignalDefinition("O_PLC_PI_4_AN_Stat_1", 8.7, IOMode.Write, IOType.Bool, "PE4: Antriebsstatus (1=online;0=Offline)"),
                new SignalDefinition("O_PLC_PI_5_IO_Stat2", 10.6, IOMode.Write, IOType.Bool, "PE5:  I/O Daten gültig"),
                new SignalDefinition("O_PLC_PI_5_AN_Stat_2", 10.7, IOMode.Write, IOType.Bool, "PE5:  Antriebsstatus (1=online;0=Offline)"),
                new SignalDefinition("O_PLC_PI_5_RFR", 11.0, IOMode.Write, IOType.Bool, "PE5: Antrieb ist freigegeben"),

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
                new(DeviceLogicObject.Guid, "Para_Transmissionfactor_Speed", 10000000),         // Integra: 16384 entspricht 100%
                new(DeviceLogicObject.Guid, "Para_JogSpeed_1", 333333),                         // Geschwindigkeit 1
                new(DeviceLogicObject.Guid, "Para_JogSpeed_2", 333333),                         // Geschwindigkeit 2
                new(DeviceLogicObject.Guid, "SIM_MaxSpeed", 0.6m)                               // Maximale Geschwindigkeit Simulation [m/s]
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
