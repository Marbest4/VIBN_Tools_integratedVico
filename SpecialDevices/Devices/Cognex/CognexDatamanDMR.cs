using FS.SDK.Io;
using FS.SDK.Mathematics;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.SpecialDevices;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Services;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;
using DateTime = System.DateTime;

namespace MaNiAC_Tool.SpecialDevices.FieldDevices.Cognex
{
    public class CognexDatamanDMR : SpecialDevice
    {

        //===========================================================================================================================
        // D E V I C E   S P E C I F I C   P R O P E R T I E S
        //===========================================================================================================================

        public FeeReadingUnit ReadingUnit { get; set; }



        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public CognexDatamanDMR(string prefix, SpecialDeviceAddresses addresses)
            : base(prefix, addresses, DeviceManufacturer.Cognex, CognexDeviceTypes.DatamanDMR)
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
                LogicDefinitionName = LogicsSpecialDevice.Cognex_Dataman_DMR.Name,
                LogicDefinitionPath = LogicsSpecialDevice.Cognex_Dataman_DMR.Path,
                Parent = DeviceBasicFrame,
            };

            ReadingUnit = new FeeReadingUnit()
            {
                Name = DevicePrefix + " (ReadingUnit)",
                Scale = new Vector3(0.1f, 0.1f, 0.1f),
                Parent = DeviceBasicFrame,
                UdtDefinition = new Dictionary<string, IOType>()
                {
                    {"ReadData", IOType.String }
                }
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
                // Aufnahmesteuerung
                new SignalDefinition("PLC_OUT_TriggerEnable", 0.0, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Trigger", 0.1, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_02", 0.2, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_03", 0.3, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_04", 0.4, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_05", 0.5, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_06", 0.6, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_07", 0.7, IOMode.Read, IOType.Bool, ""),

                // Ergebnissteuerung
                new SignalDefinition("PLC_OUT_ResultsBufferEnable", 1.0, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_ResultsAck", 1.1, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_12", 1.2, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_13", 1.3, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_14", 1.4, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_15", 1.5, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_16", 1.6, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_RFU_17", 1.7, IOMode.Read, IOType.Bool, ""),

                // SoftEvent
                new SignalDefinition("PLC_OUT_SoftEvent_TrainCode", 4.0, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_SoftEvent_TrainMatchString", 4.1, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_SoftEvent_TrainFocus", 4.2, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_SoftEvent_TrainBrightness", 4.3, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_SoftEvent_Untrain", 4.4, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_SoftEvent_RFU_45", 4.5, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_SoftEvent_ExecuteDMCC", 4.6, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_SoftEvent_SetMatchString", 4.7, IOMode.Read, IOType.Bool, ""),

                // UserData Header
                new SignalDefinition("PLC_OUT_UserData_Option", 5.0, IOMode.Read, IOType.Int, ""),
                new SignalDefinition("PLC_OUT_UserData_DataLength", 7.0, IOMode.Read, IOType.Int, ""),

                // UserData Bytes 1–64
                new SignalDefinition("PLC_OUT_UserData_Data1", 9.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data2", 10.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data3", 11.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data4", 12.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data5", 13.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data6", 14.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data7", 15.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data8", 16.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data9", 17.0, IOMode.Read, IOType.Byte, ""),

                new SignalDefinition("PLC_OUT_UserData_Data10", 18.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data11", 19.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data12", 20.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data13", 21.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data14", 22.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data15", 23.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data16", 24.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data17", 25.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data18", 26.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data19", 27.0, IOMode.Read, IOType.Byte, ""),

                new SignalDefinition("PLC_OUT_UserData_Data20", 28.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data21", 29.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data22", 30.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data23", 31.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data24", 32.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data25", 33.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data26", 34.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data27", 35.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data28", 36.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data29", 37.0, IOMode.Read, IOType.Byte, ""),

                new SignalDefinition("PLC_OUT_UserData_Data30", 38.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data31", 39.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data32", 40.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data33", 41.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data34", 42.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data35", 43.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data36", 44.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data37", 45.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data38", 46.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data39", 47.0, IOMode.Read, IOType.Byte, ""),

                new SignalDefinition("PLC_OUT_UserData_Data40", 48.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data41", 49.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data42", 50.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data43", 51.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data44", 52.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data45", 53.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data46", 54.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data47", 55.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data48", 56.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data49", 57.0, IOMode.Read, IOType.Byte, ""),

                new SignalDefinition("PLC_OUT_UserData_Data50", 58.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data51", 59.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data52", 60.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data53", 61.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data54", 62.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data55", 63.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data56", 64.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data57", 65.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data58", 66.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data59", 67.0, IOMode.Read, IOType.Byte, ""),

                new SignalDefinition("PLC_OUT_UserData_Data60", 68.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data61", 69.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data62", 70.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data63", 71.0, IOMode.Read, IOType.Byte, ""),
                new SignalDefinition("PLC_OUT_UserData_Data64", 72.0, IOMode.Read, IOType.Byte, ""),



                // Logic Write
                // Aufnahmestatus
                new SignalDefinition("PLC_IN_TriggerReady", 0.0, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_TriggerAck", 0.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Acquiring", 0.2, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_MissedAcquiring", 0.3, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_RFU_04", 0.4, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_RFU_05", 0.5, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_RFU_06", 0.6, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_RFU_07", 0.7, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("PLC_IN_TriggerID1", 1.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_TriggerID2", 2.0, IOMode.Write, IOType.Byte, ""),

                // Ergebnisstatus
                new SignalDefinition("PLC_IN_Decoding", 3.0, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_DecodeComplete", 3.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_ResultBufferOverrun", 3.2, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_ResultsAvailable", 3.3, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_RFU_34", 3.4, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_RFU_35", 3.5, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_RFU_36", 3.6, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_RFU_37", 3.7, IOMode.Write, IOType.Bool, ""),

                // SoftEvent
                new SignalDefinition("PLC_IN_SoftEvent_TrainCodeAck", 4.0, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_SoftEvent_TrainMatchStringAck", 4.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_SoftEvent_TrainFocusAck", 4.2, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_SoftEvent_TrainBrightnessAck", 4.3, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_SoftEvent_UntrainAck", 4.4, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_SoftEvent_RFU_45", 4.5, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_SoftEvent_ExecuteDMCCAck", 4.6, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_SoftEvent_SetMatchStringAck", 4.7, IOMode.Write, IOType.Bool, ""),

                // ResultData Header
                new SignalDefinition("PLC_IN_ResultData_ResultID", 5.0, IOMode.Write, IOType.Int, ""),
                new SignalDefinition("PLC_IN_ResultData_ResultCode", 7.0, IOMode.Write, IOType.Word, ""),
                new SignalDefinition("PLC_IN_ResultData_DataLength", 11.0, IOMode.Write, IOType.Int, ""),

                // ResultData Bytes 1–128
                new SignalDefinition("PLC_IN_ResultData1", 13.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData2", 14.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData3", 15.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData4", 16.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData5", 17.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData6", 18.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData7", 19.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData8", 20.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData9", 21.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData10", 22.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData11", 23.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData12", 24.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData13", 25.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData14", 26.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData15", 27.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData16", 28.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData17", 29.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData18", 30.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData19", 31.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData20", 32.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData21", 33.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData22", 34.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData23", 35.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData24", 36.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData25", 37.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData26", 38.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData27", 39.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData28", 40.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData29", 41.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData30", 42.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData31", 43.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData32", 44.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData33", 45.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData34", 46.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData35", 47.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData36", 48.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData37", 49.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData38", 50.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData39", 51.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData40", 52.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData41", 53.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData42", 54.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData43", 55.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData44", 56.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData45", 57.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData46", 58.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData47", 59.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData48", 60.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData49", 61.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData50", 62.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData51", 63.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData52", 64.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData53", 65.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData54", 66.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData55", 67.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData56", 68.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData57", 69.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData58", 70.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData59", 71.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData60", 72.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData61", 73.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData62", 74.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData63", 75.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData64", 76.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData65", 77.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData66", 78.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData67", 79.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData68", 80.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData69", 81.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData70", 82.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData71", 83.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData72", 84.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData73", 85.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData74", 86.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData75", 87.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData76", 88.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData77", 89.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData78", 90.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData79", 91.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData80", 92.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData81", 93.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData82", 94.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData83", 95.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData84", 96.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData85", 97.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData86", 98.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData87", 99.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData88", 100.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData89", 101.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData90", 102.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData91", 103.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData92", 104.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData93", 105.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData94", 106.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData95", 107.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData96", 108.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData97", 109.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData98", 110.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData99", 111.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData100", 112.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData101", 113.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData102", 114.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData103", 115.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData104", 116.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData105", 117.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData106", 118.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData107", 119.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData108", 120.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData109", 121.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData110", 122.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData111", 123.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData112", 124.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData113", 125.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData114", 126.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData115", 127.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData116", 128.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData117", 129.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData118", 130.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData119", 131.0, IOMode.Write, IOType.Byte, ""),

                new SignalDefinition("PLC_IN_ResultData120", 132.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData121", 133.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData122", 134.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData123", 135.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData124", 136.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData125", 137.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData126", 138.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData127", 139.0, IOMode.Write, IOType.Byte, ""),
                new SignalDefinition("PLC_IN_ResultData128", 140.0, IOMode.Write, IOType.Byte, ""),

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
            await ReadingUnit.CreateAsync();
            await ReadingUnit.SendAndWaitAsync();


            // Check for successfully writing UDT Definition
            var expectedSlot = ReadingUnit.UdtDefinition.Keys.FirstOrDefault();
            var timeout = TimeSpan.FromSeconds(10);
            var startTime = DateTime.Now;

            string[] slots = Array.Empty<string>();

            while (true)
            {
                slots = await ApiInstance.Object.GetSlotNamesAsync(ReadingUnit.Guid);

                if (slots.Contains(expectedSlot))
                    break;

                if (DateTime.Now - startTime > timeout)
                    throw new TimeoutException($"UDT Definition was not written in time.");

                await Task.Delay(20);
            }

            foreach (var slot in slots)
            {
                await ApiInstance.Interface.SendSlotSlotAssignmentAsync(ReadingUnit.Guid, slot, DeviceLogicObject.Guid, "SIM_" + slot);
            }

            // Assign IsDetecting -> DoRead
            await ApiInstance.Interface.SendSlotSlotAssignmentAsync(ReadingUnit.Guid, "IsDetecting", ReadingUnit.Guid, "DoRead");

            return true;
        }



    }

}

