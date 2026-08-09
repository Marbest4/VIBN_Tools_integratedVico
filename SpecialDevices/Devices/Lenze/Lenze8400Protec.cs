using FS.SDK.Io;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.SpecialDevices;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Services;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace MaNiAC_Tool.SpecialDevices.FieldDevices.Lenze
{
    public class Lenze8400Protec : SpecialDevice
    {

        //===========================================================================================================================
        // D E V I C E   S P E C I F I C   P R O P E R T I E S
        //===========================================================================================================================





        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public Lenze8400Protec(string prefix, SpecialDeviceAddresses addresses)
            : base(prefix, addresses, DeviceManufacturer.Lenze, LenzeDeviceTypes.Protec8400)
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
                LogicDefinitionName = LogicsSpecialDevice.Lenze_8400Protec.Name,
                LogicDefinitionPath = LogicsSpecialDevice.Lenze_8400Protec.Path,
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
                new SignalDefinition("I_PLC_PO_1_MainSetValue", 2.0, IOMode.Read, IOType.Int, "PA1: Main Set Value"),
                new SignalDefinition("I_PLC_PO_F_ControlDword", 20.0, IOMode.Read, IOType.DWord, "Sicheres Steuer DWort"),

                // Logic Write
                new SignalDefinition("O_PLC_PI_0_Statusword", 0.0, IOMode.Write, IOType.Word, "PE0: Statuswort 0"),
                new SignalDefinition("O_PLC_PI_1_Speed", 2.0, IOMode.Write, IOType.Int, "PE1: Motorendrehzahl in %"),
                new SignalDefinition("O_PLC_PI_F_StatusDword", 20.0, IOMode.Write, IOType.DWord, "Sicheres Status DWort"),

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
                new(DeviceLogicObject.Guid, "Para_JogSpeed_3", 333333),                         // Geschwindigkeit 3
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
