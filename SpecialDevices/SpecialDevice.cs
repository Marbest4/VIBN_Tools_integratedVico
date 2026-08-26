using FS.SDK.Io;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.Services;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace VIBN_Tools.SpecialDevices
{
    public abstract class SpecialDevice
    {

        // Special Device Properties


        public DeviceManufacturer DeviceManufacturer { get; set; }
        public Enum DeviceType { get; set; }
        public string DevicePrefix { get; set; }
        public SpecialDeviceAddresses DeviceAddresses { get; set; }
        public IReadOnlyList<FeeInterfaceSignal> DeviceSignals { get; set; }
        public IReadOnlyList<FeeInterfaceSignal> DeviceParameters { get; set; }

        public RobotType? RobotType { get; set; }


        public FeeLogic DeviceLogicObject { get; set; }
        public FeeBasicFrame DeviceBasicFrame { get; set; }

        public FeeInterface DeviceInterface { get; set; }

        //public Guid InterfaceGuid { get; set; }





        public SpecialDevice(string prefix, SpecialDeviceAddresses addresses, DeviceManufacturer manufacturer, Enum deviceType)
        {
            DevicePrefix = prefix;
            DeviceAddresses = addresses;
            DeviceManufacturer = manufacturer;
            DeviceType = deviceType;

        }


        public SpecialDevice(string prefix, SpecialDeviceAddresses addresses, DeviceManufacturer manufacturer, Enum deviceType, RobotType robotType)
            : this(prefix, addresses, manufacturer, deviceType)
        {
            RobotType = robotType;
        }






        // Calculate Signals from SignalDefinitions
        protected virtual IEnumerable<SignalDefinition> DefineSignals() => Enumerable.Empty<SignalDefinition>();

        protected void InitializeSignals()
        {
            DeviceSignals = DefineSignals()
                .Select(def => new FeeInterfaceSignal(
                    tag: GenerateTag(DevicePrefix, def.Name),
                    address: CalculateAddress(DeviceAddresses, def.Offset, def.Mode, def.Type),
                    usage: def.Mode.ToString(),
                    type: def.Type.ToString(),
                    comment: def.Comment
                ))
                .ToList();
        }


        protected virtual string CalculateAddress(SpecialDeviceAddresses baseAddresses, double offset, IOMode ioMode, IOType ioType)
        {
            return PlcAddressCalculator.Calculate(baseAddresses, offset, ioMode, ioType);
        }

        protected static string GenerateTag(string Prefix, string Tag)
        {
            return Prefix + "_" + Tag;
        }




        // Create Special Device
        public async Task<bool> CreateAsync()
        {
            if (!await CreateDeviceBaseAsync())
                return false;

            if (!await WriteDeviceParameters())
                return false;

            return await CreateDeviceSpecificAsync();


        }

        protected abstract Task<bool> CreateDeviceSpecificAsync();

        protected abstract Task<bool> WriteDeviceParameters();




        private async Task<bool> CreateDeviceBaseAsync()
        {
            if (!await InitializeFeeObjectsAsync())
                return false;

            if (!await CreateInterfaceAndSignalsAsync())
                return false;

            if (!await AssignSignalsToDeviceLogic())
                return false;

            return true;
        }


        private async Task<bool> InitializeFeeObjectsAsync()
        {
            (DeviceLogicObject.LogicDefinitionGuid, DeviceLogicObject.LogicDefinitionVersion) = await FeeLogic.GetOrImportLogicDefinition(DeviceLogicObject.LogicDefinitionName, DeviceLogicObject.LogicDefinitionPath);
            if (DeviceLogicObject.LogicDefinitionGuid == Guid.Empty || DeviceLogicObject.LogicDefinitionVersion == String.Empty)
                return false;

            // Create Basic Frame to hold all elements
            await DeviceBasicFrame.CreateAsync();
            await DeviceBasicFrame.SendAndWaitAsync();

            // Create LogicObject
            if (!await DeviceLogicObject.CreateSendAssignAndWaitAsync())
                return false;


            return true;
        }


        private async Task<bool> CreateInterfaceAndSignalsAsync()
        {
            // Create Interface and Device Signals
            DeviceInterface = new FeeInterface()
            {
                Name = DevicePrefix + " (" + DeviceType + ")",
            };

            if (await DeviceInterface.CreateInterfaceAsync())
            {
                foreach (var signal in DeviceSignals)
                {
                    if (!await signal.CreateSignalAsync(DeviceInterface))
                        return false;
                }

                //foreach (var signal in DeviceSignals)
                //{
                //    await signal.CreateSignalAsync(DeviceInterface);
                //}
                return true;
            }

            return false;
        }

        private async Task<bool> AssignSignalsToDeviceLogic()
        {
            foreach (var signal in DeviceSignals)
            {
                var slotName = signal.Tag.Substring(this.DevicePrefix.Length + 1);

                if(DeviceManufacturer == DeviceManufacturer.Grob && DeviceType.Equals(GrobDeviceTypes.SimModeSiemens))
                {
                    slotName = signal.Comment;
                }

                await ApiInstance.Interface.SendSlotVarAssignmentAsync(DeviceLogicObject.Guid, slotName, signal.Guid, true);
            }


            //foreach (var signal in DeviceSignals)
            //{
            //    var slotName = signal.Tag.Substring(this.DevicePrefix.Length + 1);

            //    await ApiInstance.Interface.SendSlotVarAssignmentAsync(DeviceLogicObject.Guid, slotName, signal.Guid, true);
            //}

            return true;
        }








    }


    public record DeviceParameter(Guid ObjectGuid, string SlotName, object Value);



    public record SpecialDeviceAddresses
    {
        public int Input { get; init; }
        public int Output { get; init; }


        public SpecialDeviceAddresses(int address)
        {
            Input = address;
            Output = address;
        }

        public SpecialDeviceAddresses(int input, int output)
        {
            Input = input;
            Output = output;
        }


        public int GetBaseAddress(IOMode ioMode)
        {
            return ioMode switch
            {
                IOMode.Read => Output,
                IOMode.Write => Input,
                _ => throw new ArgumentOutOfRangeException(nameof(ioMode))
            };
        }
    }
}
