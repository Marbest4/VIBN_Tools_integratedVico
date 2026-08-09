using FS.SDK.Io;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.SpecialDevices;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace MaNiAC_Tool.SpecialDevices.FieldDevices.Promess
{
    public class PromessSpindleUP : SpecialDevice
    {

        //===========================================================================================================================
        // D E V I C E   S P E C I F I C   P R O P E R T I E S
        //===========================================================================================================================





        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public PromessSpindleUP(string prefix, SpecialDeviceAddresses addresses)
            : base(prefix, addresses, DeviceManufacturer.Promess, PromessDeviceTypes.SpindleUp)
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
                LogicDefinitionName = LogicsSpecialDevice.PromessSpindleUP.Name,
                LogicDefinitionPath = LogicsSpecialDevice.PromessSpindleUP.Path,
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
                new SignalDefinition("PLC_OUT_CycleStart", 0.0, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_MoveSelectZeroActive", 0.1, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_DryCycle", 0.2, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_Home", 0.3, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ResetPartStatus", 0.4, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ResetDriveFault", 0.5, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ProgramSelectBit0", 0.6, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ProgramSelectBit1", 0.7, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ProgramSelectBit2", 1.0, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ProgramSelectBit3", 1.1, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ProgramSelectBit4", 1.2, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ProgramSelectBit5", 1.3, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ProgramNumberStrobe", 1.4, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_SerialNumberStrobe", 1.5, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ExtendAxis1", 1.6, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_RetractAxis1", 1.7, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ExtendAxis2", 2.0, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_RetractAxis2", 2.1, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ExtendAxis3", 2.2, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_RetractAxis3", 2.3, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_ExtendAxis4", 2.4, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_RetractAxis4", 2.5, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_User_2_6", 2.6, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_User_2_7", 2.7, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_User_3_0", 3.0, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_User_3_1", 3.1, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_User_3_2", 3.2, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_User_3_3", 3.3, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_User_3_4", 3.4, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_User_3_5", 3.5, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_User_3_6", 3.6, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_User_3_7", 3.7, IOMode.Read, IOType.Bool),
                new SignalDefinition("PLC_OUT_MoveSelect", 4.0, IOMode.Read, IOType.DInt),
                new SignalDefinition("PLC_OUT_Setpoint", 8.0, IOMode.Read, IOType.Real),

                new SignalDefinition("PLC_OUT_PartID_01", 14.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_02", 15.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_03", 16.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_04", 17.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_05", 18.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_06", 19.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_07", 20.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_08", 21.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_09", 22.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_10", 23.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_11", 24.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_12", 25.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_13", 26.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_14", 27.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_15", 28.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_16", 29.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_17", 30.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_18", 31.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_19", 32.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_20", 33.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_21", 34.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_22", 35.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_23", 36.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_24", 37.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_25", 38.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_26", 39.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_27", 40.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_28", 41.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_29", 42.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_30", 43.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_31", 44.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_32", 45.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_33", 46.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_34", 47.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_35", 48.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_36", 49.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_37", 50.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_38", 51.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_39", 52.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_40", 53.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_41", 54.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_42", 55.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_43", 56.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_44", 57.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_45", 58.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_46", 59.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_47", 60.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_48", 61.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_49", 62.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_50", 63.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_51", 64.0, IOMode.Read, IOType.Char),
                new SignalDefinition("PLC_OUT_PartID_52", 65.0, IOMode.Read, IOType.Char),


                // Logic Write
                new SignalDefinition("PLC_IN_Ready", 0.0, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_CycleEnd", 0.1, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_Pass", 0.2, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_Fail", 0.3, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_CycleStop", 0.4, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_ErrorHomeReqd", 0.5, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_CycleStartRelease", 0.6, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_MoveComplete", 0.7, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_Press_Started", 1.0, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_DxD_BufferingDataWarning", 1.1, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_DxD_NoCommWarning", 1.2, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_DriveFault_0", 1.3, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_OverloadFault_1", 1.4, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_Fault_2", 1.5, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_Fault_3", 1.6, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_Fault_4", 1.7, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_Fault_5", 2.0, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_Fault_6", 2.1, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_Fault_7", 2.2, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_ProgramSelectEchoBit0", 2.3, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_ProgramSelectEchoBit1", 2.4, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_ProgramSelectEchoBit2", 2.5, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_ProgramSelectEchoBit3", 2.6, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_ProgramSelectEchoBit4", 2.7, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_ProgramSelectEchoBit5", 3.0, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_ProgramSelectEchoBit6", 3.1, IOMode.Write, IOType.Bool),

                // new SignalDefinition("PLC_IN_Spare_1", 3.2, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_2", 3.3, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_3", 3.4, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_4", 3.5, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_5", 3.6, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_6", 3.7, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_7", 4.0, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_8", 4.1, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_9", 4.2, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_10", 4.3, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_11", 4.4, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_12", 4.5, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_13", 4.6, IOMode.Write, IOType.Bool),
                // new SignalDefinition("PLC_IN_Spare_14", 4.7, IOMode.Write, IOType.Bool),
                new SignalDefinition("PLC_IN_MoveActive", 6.0, IOMode.Write, IOType.DInt),
                // new SignalDefinition("PLC_IN_LiveData_Struct", 10.0, IOMode.Write, IOType.String),
                // new SignalDefinition("PLC_IN_ResultData_Struct", 42.0, IOMode.Write, IOType.String),

                // new SignalDefinition("PLC_IN_SpareReal_1", 78.0, IOMode.Write, IOType.Real),
                // new SignalDefinition("PLC_IN_SpareReal_2", 82.0, IOMode.Write, IOType.Real),
                // new SignalDefinition("PLC_IN_SpareReal_3", 86.0, IOMode.Write, IOType.Real),
                // new SignalDefinition("PLC_IN_SpareReal_4", 90.0, IOMode.Write, IOType.Real),
                // new SignalDefinition("PLC_IN_SpareReal_5", 94.0, IOMode.Write, IOType.Real),
                // new SignalDefinition("PLC_IN_SpareReal_6", 98.0, IOMode.Write, IOType.Real),
                // new SignalDefinition("PLC_IN_SpareReal_7", 102.0, IOMode.Write, IOType.Real),
                // new SignalDefinition("PLC_IN_SpareReal_8", 106.0, IOMode.Write, IOType.Real),
                // new SignalDefinition("PLC_IN_SpareReal_9", 110.0, IOMode.Write, IOType.Real),

                new SignalDefinition("PLC_IN_PartID_01", 114.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_02", 115.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_03", 116.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_04", 117.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_05", 118.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_06", 119.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_07", 120.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_08", 121.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_09", 122.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_10", 123.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_11", 124.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_12", 125.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_13", 126.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_14", 127.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_15", 128.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_16", 129.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_17", 130.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_18", 131.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_19", 132.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_20", 133.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_21", 134.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_22", 135.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_23", 136.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_24", 137.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_25", 138.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_26", 139.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_27", 140.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_28", 141.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_29", 142.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_30", 143.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_31", 144.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_32", 145.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_33", 146.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_34", 147.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_35", 148.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_36", 149.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_37", 150.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_38", 151.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_39", 152.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_40", 153.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_41", 154.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_42", 155.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_43", 156.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_44", 157.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_45", 158.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_46", 159.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_47", 160.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_48", 161.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_49", 162.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_50", 163.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_51", 164.0, IOMode.Write, IOType.Char),
                new SignalDefinition("PLC_IN_PartID_52", 165.0, IOMode.Write, IOType.Char),

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


        protected override async Task<bool> CreateDeviceSpecificAsync() => true;



    }
}
