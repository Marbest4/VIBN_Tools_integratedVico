using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing;
using FS.SDK.Io;
using NPOI.SS.Formula.Functions;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace VIBN_Tools.SpecialDevices.Devices.Grob
{
    public class SafePnPN : SpecialDevice
    {
        //===========================================================================================================================
        // D E V I C E   S P E C I F I C   P R O P E R T I E S
        //===========================================================================================================================





        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public SafePnPN(string prefix, SpecialDeviceAddresses addresses)
            : base(prefix, addresses, DeviceManufacturer.Grob, GrobDeviceTypes.SafePnPn)
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
                LogicDefinitionName = LogicsStandard.Grob_SafePnPn.Name,
                LogicDefinitionPath = LogicsStandard.Grob_SafePnPn.Path,
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
                new SignalDefinition("PLC_OUT_SEND_BYTE00", 0, IOMode.Read, IOType.Byte, "SD_BD_0-7"),
                new SignalDefinition("PLC_OUT_SEND_BYTE01", 1, IOMode.Read, IOType.Byte, "SD_BD_8-15"),
                new SignalDefinition("PLC_OUT_SEND_BYTE02", 2, IOMode.Read, IOType.Byte, "SD_I_00"),
                new SignalDefinition("PLC_OUT_SEND_BYTE03", 3, IOMode.Read, IOType.Byte, "SD_I_01"),
                new SignalDefinition("PLC_OUT_SEND_BYTE04", 4, IOMode.Read, IOType.Byte, "not used"),
                new SignalDefinition("PLC_OUT_SEND_BYTE05", 5, IOMode.Read, IOType.Byte, "not used"),

                // Logic Write
                new SignalDefinition("PLC_IN_RECV_BYTE00", 0, IOMode.Write, IOType.Byte, "RD_BD_0-7"),
                new SignalDefinition("PLC_IN_RECV_BYTE01", 1, IOMode.Write, IOType.Byte, "RD_BD_8-15"),
                new SignalDefinition("PLC_IN_RECV_BYTE02", 2, IOMode.Write, IOType.Byte, "RD_I_00"),
                new SignalDefinition("PLC_IN_RECV_BYTE03", 3, IOMode.Write, IOType.Byte, "RD_I_01"),
                new SignalDefinition("PLC_IN_RECV_BYTE04", 4, IOMode.Write, IOType.Byte, "not used"),
                new SignalDefinition("PLC_IN_RECV_BYTE05", 5, IOMode.Write, IOType.Byte, "not used"),
                new SignalDefinition("PLC_IN_RECV_BYTE06", 6, IOMode.Write, IOType.Byte, "Activation simulation receive"),
                new SignalDefinition("PLC_IN_RECV_BYTE07", 7, IOMode.Write, IOType.Byte, "not used"),
                new SignalDefinition("PLC_IN_RECV_BYTE08", 8, IOMode.Write, IOType.Byte, "not used"),
                new SignalDefinition("PLC_IN_RECV_BYTE09", 9, IOMode.Write, IOType.Byte, "not used"),
                new SignalDefinition("PLC_IN_RECV_BYTE10", 10, IOMode.Write, IOType.Byte, "not used"),
                new SignalDefinition("PLC_IN_RECV_BYTE11", 11, IOMode.Write, IOType.Byte, "not used"),

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

            FeeInterfaceSignal activationSimulationSend = new FeeInterfaceSignal()
            {
                Tag = DevicePrefix + $"PLC_IN_SEND_BYTE00",
                Address = $"EB{DeviceAddresses.Output}",
                IOType = IOType.Byte,
                Usage = IOMode.Write,
                Comment = $"activation simulation send",
            };

            await activationSimulationSend.CreateSignalAsync(DeviceInterface);

            await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(DeviceLogicObject.Guid, "PLC_IN_SEND_BYTE00", activationSimulationSend.Guid, true);

            return true;

        }


    }
}
