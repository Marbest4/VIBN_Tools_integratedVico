using FS.SDK.Io;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.SpecialDevices;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Services;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace MaNiAC_Tool.SpecialDevices.RobotApplications.Safety
{
    public class KukaSafety : SpecialDevice
    {

        //===========================================================================================================================
        // D E V I C E   S P E C I F I C   P R O P E R T I E S
        //===========================================================================================================================





        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public KukaSafety(string prefix, SpecialDeviceAddresses addresses)
            : base(prefix, addresses, DeviceManufacturer.Kuka, KukaDeviceTypes.RobotSafety)
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
                LogicDefinitionName = LogicsSpecialDevice.KukaSafety.Name,
                LogicDefinitionPath = LogicsSpecialDevice.KukaSafety.Path,
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
                new SignalDefinition("PLC_SafeOut_Byte0", 0.0, IOMode.Read, IOType.Byte, ""),

                // Logic Write
                new SignalDefinition("PLC_SafeIn_Byte0", 0.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_SafeIn_Byte1", 1.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_SafeIn_Byte2", 2.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_SafeIn_Byte3", 3.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_SafeIn_Byte4", 4.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_SafeIn_Byte5", 5.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_SafeIn_Byte6", 6.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_SafeIn_Byte7", 7.0, IOMode.Write, IOType.Byte, ""),


            };
        }


        protected override string CalculateAddress(SpecialDeviceAddresses baseAddresses, double offset, IOMode ioMode, IOType ioType)
        {
            return PlcAddressCalculator.Calculate(baseAddresses, offset, ioMode, ioType);
        }





        //===========================================================================================================================
        // C R E A T E   D E V I C E
        //===========================================================================================================================
        protected override async Task<bool> WriteDeviceParameters() => true;


        protected override async Task<bool> CreateDeviceSpecificAsync()
        {
            FeeInterfaceSignal robotExtSignal = new FeeInterfaceSignal()
            {
                Tag = $"{DevicePrefix}_RobotEXT",
                Address = "$OUT[25]",
                IOType = IOType.Bool,
                Usage = IOMode.Read,
                Comment = "Robot external control",
                ParentInterface = base.DeviceInterface,
            };

            if (!await robotExtSignal.CreateSignalAsync())
                return false;

            if (!await ApiInstance.Interface.SendSlotVarAssignmentAsync(DeviceLogicObject.Guid, "SIM_RobotEXT", robotExtSignal.Guid, true))
                return false;

            return true;

        }


    }
}
