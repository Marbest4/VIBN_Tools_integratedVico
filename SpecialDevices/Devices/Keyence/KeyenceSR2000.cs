using FS.SDK.Io;
using FS.SDK.Mathematics;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.SpecialDevices;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Services;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace MaNiAC_Tool.SpecialDevices.FieldDevices.Keyence
{
    public class KeyenceSR2000 : SpecialDevice
    {
        //===========================================================================================================================
        // D E V I C E   S P E C I F I C   P R O P E R T I E S
        //===========================================================================================================================

        public FeeReadingUnit ReadingUnit { get; set; }




        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public KeyenceSR2000(string prefix, SpecialDeviceAddresses addresses)
            : base(prefix, addresses, DeviceManufacturer.Keyence, KeyenceDeviceTypes.SR2000)
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
                LogicDefinitionName = LogicsSpecialDevice.Keyence_SR2000.Name,
                LogicDefinitionPath = LogicsSpecialDevice.Keyence_SR2000.Path,
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
                new SignalDefinition("IO_OUT_Operation_Read_Request", 1.0, IOMode.Read, IOType.Bool, "Scanner Anforderung Lesen"),
                new SignalDefinition("IO_OUT_Completion_Read_Complete_Clear", 2.0, IOMode.Read, IOType.Bool, "Scanner Bestätigung fertig gelesen"),

                new SignalDefinition("IO_IN_Busy_BUSY", 1.0, IOMode.Write, IOType.Bool, "Scanner Busy"),
                new SignalDefinition("IO_IN_Completion_Read_Complete", 2.0, IOMode.Write, IOType.Bool, "Scanner Fertig Lesen"),
                new SignalDefinition("IO_IN_Error_Read_Failure", 3.0, IOMode.Write, IOType.Bool, "Scanner Fehler beim Lesen"),

                new SignalDefinition("IO_IN_Read_Data_Result_Data_Size", 40.0, IOMode.Write, IOType.Word, "Scanner Datengröße"),


                // Logic Write
                new SignalDefinition("IO_IN_ReadData_1", 42.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 1"),
                new SignalDefinition("IO_IN_ReadData_2", 43.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 2"),
                new SignalDefinition("IO_IN_ReadData_3", 44.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 3"),
                new SignalDefinition("IO_IN_ReadData_4", 45.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 4"),
                new SignalDefinition("IO_IN_ReadData_5", 46.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 5"),
                new SignalDefinition("IO_IN_ReadData_6", 47.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 6"),
                new SignalDefinition("IO_IN_ReadData_7", 48.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 7"),
                new SignalDefinition("IO_IN_ReadData_8", 49.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 8"),
                new SignalDefinition("IO_IN_ReadData_9", 50.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 9"),
                new SignalDefinition("IO_IN_ReadData_10", 51.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 10"),

                new SignalDefinition("IO_IN_ReadData_11", 52.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 11"),
                new SignalDefinition("IO_IN_ReadData_12", 53.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 12"),
                new SignalDefinition("IO_IN_ReadData_13", 54.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 13"),
                new SignalDefinition("IO_IN_ReadData_14", 55.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 14"),
                new SignalDefinition("IO_IN_ReadData_15", 56.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 15"),
                new SignalDefinition("IO_IN_ReadData_16", 57.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 16"),
                new SignalDefinition("IO_IN_ReadData_17", 58.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 17"),
                new SignalDefinition("IO_IN_ReadData_18", 59.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 18"),
                new SignalDefinition("IO_IN_ReadData_19", 60.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 19"),
                new SignalDefinition("IO_IN_ReadData_20", 61.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 20"),

                new SignalDefinition("IO_IN_ReadData_21", 62.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 21"),
                new SignalDefinition("IO_IN_ReadData_22", 63.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 22"),
                new SignalDefinition("IO_IN_ReadData_23", 64.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 23"),
                new SignalDefinition("IO_IN_ReadData_24", 65.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 24"),
                new SignalDefinition("IO_IN_ReadData_25", 66.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 25"),
                new SignalDefinition("IO_IN_ReadData_26", 67.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 26"),
                new SignalDefinition("IO_IN_ReadData_27", 68.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 27"),
                new SignalDefinition("IO_IN_ReadData_28", 69.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 28"),
                new SignalDefinition("IO_IN_ReadData_29", 70.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 29"),
                new SignalDefinition("IO_IN_ReadData_30", 71.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 30"),

                new SignalDefinition("IO_IN_ReadData_31", 82.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 31"),
                new SignalDefinition("IO_IN_ReadData_32", 83.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 32"),
                new SignalDefinition("IO_IN_ReadData_33", 84.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 33"),
                new SignalDefinition("IO_IN_ReadData_34", 85.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 34"),
                new SignalDefinition("IO_IN_ReadData_35", 86.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 35"),
                new SignalDefinition("IO_IN_ReadData_36", 87.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 36"),
                new SignalDefinition("IO_IN_ReadData_37", 88.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 37"),
                new SignalDefinition("IO_IN_ReadData_38", 89.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 38"),
                new SignalDefinition("IO_IN_ReadData_39", 90.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 39"),
                new SignalDefinition("IO_IN_ReadData_40", 81.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 40"),

                new SignalDefinition("IO_IN_ReadData_41", 82.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 41"),
                new SignalDefinition("IO_IN_ReadData_42", 83.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 42"),
                new SignalDefinition("IO_IN_ReadData_43", 84.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 43"),
                new SignalDefinition("IO_IN_ReadData_44", 85.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 44"),
                new SignalDefinition("IO_IN_ReadData_45", 86.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 45"),
                new SignalDefinition("IO_IN_ReadData_46", 87.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 46"),
                new SignalDefinition("IO_IN_ReadData_47", 88.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 47"),
                new SignalDefinition("IO_IN_ReadData_48", 89.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 48"),
                new SignalDefinition("IO_IN_ReadData_49", 90.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 49"),
                new SignalDefinition("IO_IN_ReadData_50", 91.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 50"),

                new SignalDefinition("IO_IN_ReadData_51", 92.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 51"),
                new SignalDefinition("IO_IN_ReadData_52", 93.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 52"),
                new SignalDefinition("IO_IN_ReadData_53", 94.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 53"),
                new SignalDefinition("IO_IN_ReadData_54", 95.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 54"),
                new SignalDefinition("IO_IN_ReadData_55", 96.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 55"),
                new SignalDefinition("IO_IN_ReadData_56", 97.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 56"),
                new SignalDefinition("IO_IN_ReadData_57", 98.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 57"),
                new SignalDefinition("IO_IN_ReadData_58", 99.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 58"),
                new SignalDefinition("IO_IN_ReadData_59", 100.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 59"),
                new SignalDefinition("IO_IN_ReadData_60", 101.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 60"),

                new SignalDefinition("IO_IN_ReadData_61", 102.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 61"),
                new SignalDefinition("IO_IN_ReadData_62", 103.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 62"),
                new SignalDefinition("IO_IN_ReadData_63", 104.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 63"),
                new SignalDefinition("IO_IN_ReadData_64", 105.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 64"),
                new SignalDefinition("IO_IN_ReadData_65", 106.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 65"),
                new SignalDefinition("IO_IN_ReadData_66", 107.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 66"),
                new SignalDefinition("IO_IN_ReadData_67", 108.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 67"),
                new SignalDefinition("IO_IN_ReadData_68", 109.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 68"),
                new SignalDefinition("IO_IN_ReadData_69", 110.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 69"),
                new SignalDefinition("IO_IN_ReadData_70", 111.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 70"),

                new SignalDefinition("IO_IN_ReadData_71", 112.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 71"),
                new SignalDefinition("IO_IN_ReadData_72", 113.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 72"),
                new SignalDefinition("IO_IN_ReadData_73", 114.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 73"),
                new SignalDefinition("IO_IN_ReadData_74", 115.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 74"),
                new SignalDefinition("IO_IN_ReadData_75", 116.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 75"),
                new SignalDefinition("IO_IN_ReadData_76", 117.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 76"),
                new SignalDefinition("IO_IN_ReadData_77", 118.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 77"),
                new SignalDefinition("IO_IN_ReadData_78", 119.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 78"),
                new SignalDefinition("IO_IN_ReadData_79", 120.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 79"),
                new SignalDefinition("IO_IN_ReadData_80", 121.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 80"),

                new SignalDefinition("IO_IN_ReadData_81", 122.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 81"),
                new SignalDefinition("IO_IN_ReadData_82", 123.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 82"),
                new SignalDefinition("IO_IN_ReadData_83", 124.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 83"),
                new SignalDefinition("IO_IN_ReadData_84", 125.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 84"),
                new SignalDefinition("IO_IN_ReadData_85", 126.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 85"),
                new SignalDefinition("IO_IN_ReadData_86", 127.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 86"),
                new SignalDefinition("IO_IN_ReadData_87", 128.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 87"),
                new SignalDefinition("IO_IN_ReadData_88", 129.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 88"),
                new SignalDefinition("IO_IN_ReadData_89", 130.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 89"),
                new SignalDefinition("IO_IN_ReadData_90", 131.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 90"),

                new SignalDefinition("IO_IN_ReadData_91", 132.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 91"),
                new SignalDefinition("IO_IN_ReadData_92", 133.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 92"),
                new SignalDefinition("IO_IN_ReadData_93", 134.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 93"),
                new SignalDefinition("IO_IN_ReadData_94", 135.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 94"),
                new SignalDefinition("IO_IN_ReadData_95", 136.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 95"),
                new SignalDefinition("IO_IN_ReadData_96", 137.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 96"),
                new SignalDefinition("IO_IN_ReadData_97", 138.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 97"),
                new SignalDefinition("IO_IN_ReadData_98", 139.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 98"),
                new SignalDefinition("IO_IN_ReadData_99", 140.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 99"),
                new SignalDefinition("IO_IN_ReadData_100", 141.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 100"),

                new SignalDefinition("IO_IN_ReadData_101", 142.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 101"),
                new SignalDefinition("IO_IN_ReadData_102", 143.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 102"),
                new SignalDefinition("IO_IN_ReadData_103", 144.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 103"),
                new SignalDefinition("IO_IN_ReadData_104", 145.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 104"),
                new SignalDefinition("IO_IN_ReadData_105", 146.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 105"),
                new SignalDefinition("IO_IN_ReadData_106", 147.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 106"),
                new SignalDefinition("IO_IN_ReadData_107", 148.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 107"),
                new SignalDefinition("IO_IN_ReadData_108", 149.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 108"),
                new SignalDefinition("IO_IN_ReadData_109", 150.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 109"),
                new SignalDefinition("IO_IN_ReadData_110", 151.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 110"),

                new SignalDefinition("IO_IN_ReadData_111", 152.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 111"),
                new SignalDefinition("IO_IN_ReadData_112", 153.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 112"),
                new SignalDefinition("IO_IN_ReadData_113", 154.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 113"),
                new SignalDefinition("IO_IN_ReadData_114", 155.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 114"),
                new SignalDefinition("IO_IN_ReadData_115", 156.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 115"),
                new SignalDefinition("IO_IN_ReadData_116", 157.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 116"),
                new SignalDefinition("IO_IN_ReadData_117", 158.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 117"),
                new SignalDefinition("IO_IN_ReadData_118", 159.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 118"),
                new SignalDefinition("IO_IN_ReadData_119", 160.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 119"),
                new SignalDefinition("IO_IN_ReadData_120", 161.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 120"),

                new SignalDefinition("IO_IN_ReadData_121", 162.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 121"),
                new SignalDefinition("IO_IN_ReadData_122", 163.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 122"),
                new SignalDefinition("IO_IN_ReadData_123", 164.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 123"),
                new SignalDefinition("IO_IN_ReadData_124", 165.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 124"),
                new SignalDefinition("IO_IN_ReadData_125", 166.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 125"),
                new SignalDefinition("IO_IN_ReadData_126", 167.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 126"),
                new SignalDefinition("IO_IN_ReadData_127", 168.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 127"),
                new SignalDefinition("IO_IN_ReadData_128", 169.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 128"),
                new SignalDefinition("IO_IN_ReadData_129", 170.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 129"),
                new SignalDefinition("IO_IN_ReadData_130", 171.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 130"),

                new SignalDefinition("IO_IN_ReadData_131", 172.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 131"),
                new SignalDefinition("IO_IN_ReadData_132", 173.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 132"),
                new SignalDefinition("IO_IN_ReadData_133", 174.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 133"),
                new SignalDefinition("IO_IN_ReadData_134", 175.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 134"),
                new SignalDefinition("IO_IN_ReadData_135", 176.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 135"),
                new SignalDefinition("IO_IN_ReadData_136", 177.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 136"),
                new SignalDefinition("IO_IN_ReadData_137", 178.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 137"),
                new SignalDefinition("IO_IN_ReadData_138", 179.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 138"),
                new SignalDefinition("IO_IN_ReadData_139", 180.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 139"),
                new SignalDefinition("IO_IN_ReadData_140", 181.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 140"),

                new SignalDefinition("IO_IN_ReadData_141", 182.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 141"),
                new SignalDefinition("IO_IN_ReadData_142", 183.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 142"),
                new SignalDefinition("IO_IN_ReadData_143", 184.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 143"),
                new SignalDefinition("IO_IN_ReadData_144", 185.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 144"),
                new SignalDefinition("IO_IN_ReadData_145", 186.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 145"),
                new SignalDefinition("IO_IN_ReadData_146", 187.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 146"),
                new SignalDefinition("IO_IN_ReadData_147", 188.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 147"),
                new SignalDefinition("IO_IN_ReadData_148", 189.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 148"),
                new SignalDefinition("IO_IN_ReadData_149", 190.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 149"),
                new SignalDefinition("IO_IN_ReadData_150", 191.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 150"),

                new SignalDefinition("IO_IN_ReadData_151", 192.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 151"),
                new SignalDefinition("IO_IN_ReadData_152", 193.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 152"),
                new SignalDefinition("IO_IN_ReadData_153", 194.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 153"),
                new SignalDefinition("IO_IN_ReadData_154", 195.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 154"),
                new SignalDefinition("IO_IN_ReadData_155", 196.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 155"),
                new SignalDefinition("IO_IN_ReadData_156", 197.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 156"),
                new SignalDefinition("IO_IN_ReadData_157", 198.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 157"),
                new SignalDefinition("IO_IN_ReadData_158", 199.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 158"),
                new SignalDefinition("IO_IN_ReadData_159", 200.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 159"),
                new SignalDefinition("IO_IN_ReadData_160", 201.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 160"),

                new SignalDefinition("IO_IN_ReadData_161", 202.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 161"),
                new SignalDefinition("IO_IN_ReadData_162", 203.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 162"),
                new SignalDefinition("IO_IN_ReadData_163", 204.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 163"),
                new SignalDefinition("IO_IN_ReadData_164", 205.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 164"),
                new SignalDefinition("IO_IN_ReadData_165", 206.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 165"),
                new SignalDefinition("IO_IN_ReadData_166", 207.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 166"),
                new SignalDefinition("IO_IN_ReadData_167", 208.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 167"),
                new SignalDefinition("IO_IN_ReadData_168", 209.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 168"),
                new SignalDefinition("IO_IN_ReadData_169", 210.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 169"),
                new SignalDefinition("IO_IN_ReadData_170", 211.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 170"),

                new SignalDefinition("IO_IN_ReadData_171", 212.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 171"),
                new SignalDefinition("IO_IN_ReadData_172", 213.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 172"),
                new SignalDefinition("IO_IN_ReadData_173", 214.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 173"),
                new SignalDefinition("IO_IN_ReadData_174", 215.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 174"),
                new SignalDefinition("IO_IN_ReadData_175", 216.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 175"),
                new SignalDefinition("IO_IN_ReadData_176", 217.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 176"),
                new SignalDefinition("IO_IN_ReadData_177", 218.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 177"),
                new SignalDefinition("IO_IN_ReadData_178", 219.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 178"),
                new SignalDefinition("IO_IN_ReadData_179", 220.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 179"),
                new SignalDefinition("IO_IN_ReadData_180", 221.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 180"),

                new SignalDefinition("IO_IN_ReadData_181", 222.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 181"),
                new SignalDefinition("IO_IN_ReadData_182", 223.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 182"),
                new SignalDefinition("IO_IN_ReadData_183", 224.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 183"),
                new SignalDefinition("IO_IN_ReadData_184", 225.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 184"),
                new SignalDefinition("IO_IN_ReadData_185", 226.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 185"),
                new SignalDefinition("IO_IN_ReadData_186", 227.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 186"),
                new SignalDefinition("IO_IN_ReadData_187", 228.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 187"),
                new SignalDefinition("IO_IN_ReadData_188", 229.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 188"),
                new SignalDefinition("IO_IN_ReadData_189", 230.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 189"),
                new SignalDefinition("IO_IN_ReadData_190", 231.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 190"),

                new SignalDefinition("IO_IN_ReadData_191", 232.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 191"),
                new SignalDefinition("IO_IN_ReadData_192", 233.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 192"),
                new SignalDefinition("IO_IN_ReadData_193", 234.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 193"),
                new SignalDefinition("IO_IN_ReadData_194", 235.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 194"),
                new SignalDefinition("IO_IN_ReadData_195", 236.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 195"),
                new SignalDefinition("IO_IN_ReadData_196", 237.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 196"),
                new SignalDefinition("IO_IN_ReadData_197", 238.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 197"),
                new SignalDefinition("IO_IN_ReadData_198", 239.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 198"),
                new SignalDefinition("IO_IN_ReadData_199", 240.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 199"),
                new SignalDefinition("IO_IN_ReadData_200", 241.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 200"),

                new SignalDefinition("IO_IN_ReadData_201", 242.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 201"),
                new SignalDefinition("IO_IN_ReadData_202", 243.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 202"),
                new SignalDefinition("IO_IN_ReadData_203", 244.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 203"),
                new SignalDefinition("IO_IN_ReadData_204", 245.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 204"),
                new SignalDefinition("IO_IN_ReadData_205", 246.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 205"),
                new SignalDefinition("IO_IN_ReadData_206", 247.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 206"),
                new SignalDefinition("IO_IN_ReadData_207", 248.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 207"),
                new SignalDefinition("IO_IN_ReadData_208", 249.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 208"),
                new SignalDefinition("IO_IN_ReadData_209", 250.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 209"),
                new SignalDefinition("IO_IN_ReadData_210", 251.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 210"),

                new SignalDefinition("IO_IN_ReadData_211", 252.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 211"),
                new SignalDefinition("IO_IN_ReadData_212", 253.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 212"),
                new SignalDefinition("IO_IN_ReadData_213", 254.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 213"),
                new SignalDefinition("IO_IN_ReadData_214", 255.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 214"),
                new SignalDefinition("IO_IN_ReadData_215", 256.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 215"),
                new SignalDefinition("IO_IN_ReadData_216", 257.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 216"),
                new SignalDefinition("IO_IN_ReadData_217", 258.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 217"),
                new SignalDefinition("IO_IN_ReadData_218", 259.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 218"),
                new SignalDefinition("IO_IN_ReadData_219", 260.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 219"),
                new SignalDefinition("IO_IN_ReadData_220", 261.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 220"),

                new SignalDefinition("IO_IN_ReadData_221", 262.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 221"),
                new SignalDefinition("IO_IN_ReadData_222", 263.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 222"),
                new SignalDefinition("IO_IN_ReadData_223", 264.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 223"),
                new SignalDefinition("IO_IN_ReadData_224", 265.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 224"),
                new SignalDefinition("IO_IN_ReadData_225", 266.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 225"),
                new SignalDefinition("IO_IN_ReadData_226", 267.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 226"),
                new SignalDefinition("IO_IN_ReadData_227", 268.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 227"),
                new SignalDefinition("IO_IN_ReadData_228", 269.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 228"),
                new SignalDefinition("IO_IN_ReadData_229", 270.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 229"),
                new SignalDefinition("IO_IN_ReadData_230", 271.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 230"),

                new SignalDefinition("IO_IN_ReadData_231", 272.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 231"),
                new SignalDefinition("IO_IN_ReadData_232", 273.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 232"),
                new SignalDefinition("IO_IN_ReadData_233", 274.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 233"),
                new SignalDefinition("IO_IN_ReadData_234", 275.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 234"),
                new SignalDefinition("IO_IN_ReadData_235", 276.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 235"),
                new SignalDefinition("IO_IN_ReadData_236", 277.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 236"),
                new SignalDefinition("IO_IN_ReadData_237", 278.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 237"),
                new SignalDefinition("IO_IN_ReadData_238", 279.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 238"),
                new SignalDefinition("IO_IN_ReadData_239", 280.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 239"),
                new SignalDefinition("IO_IN_ReadData_240", 281.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 240"),

                new SignalDefinition("IO_IN_ReadData_241", 282.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 241"),
                new SignalDefinition("IO_IN_ReadData_242", 283.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 242"),
                new SignalDefinition("IO_IN_ReadData_243", 284.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 243"),
                new SignalDefinition("IO_IN_ReadData_244", 285.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 244"),
                new SignalDefinition("IO_IN_ReadData_245", 286.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 245"),
                new SignalDefinition("IO_IN_ReadData_246", 287.0, IOMode.Write, IOType.Byte, "Scanner Datenbyte 246"),


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

            return true;
        }



    }
}
