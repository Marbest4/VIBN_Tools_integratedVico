using FS.SDK.Io;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.SpecialDevices;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace MaNiAC_Tool.SpecialDevices.FieldDevices.IPG
{
    public class IpgLaserPicker : SpecialDevice
    {

        //===========================================================================================================================
        // D E V I C E   S P E C I F I C   P R O P E R T I E S
        //===========================================================================================================================





        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public IpgLaserPicker(string prefix, SpecialDeviceAddresses addresses)
            : base(prefix, addresses, DeviceManufacturer.Ipg, IpgDeviceTypes.LaserPicker)
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
                LogicDefinitionName = LogicsSpecialDevice.IPG_LaserPicker.Name,
                LogicDefinitionPath = LogicsSpecialDevice.IPG_LaserPicker.Path,
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
                // new SignalDefinition("PLC_OUT_Reserve", 0.0, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Manual_Mode", 0.1, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Reset", 0.2, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Power_Supply_On", 0.3, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Reset_Errors", 0.4, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_No_Laser", 0.5, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_No_Force", 0.6, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_No_Compensation", 0.7, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("PLC_OUT_No_Wobble", 1.0, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Seam_Drive_Referencing", 1.1, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Seam_Drive_To_PGCP", 1.2, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Seam_Drive_To_Start_Pos", 1.3, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_PS_Open", 1.4, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_WS_Open", 1.5, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_WS_closed", 1.6, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_setProgramNumber", 1.7, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("PLC_OUT_Fumator_Start", 2.0, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Fumator_Stop", 2.1, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Fumator_Turbo_Power", 2.2, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Fumator_Sucction_Way", 2.3, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Reduce_Laser_Power", 2.4, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Overjet_Control", 2.5, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Reserved1", 2.6, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Compensation_Control", 2.7, IOMode.Read, IOType.Bool, ""),

                // new SignalDefinition("PLC_OUT_Parker_Teach_Up", 3.0, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Parker_Teach_Down", 3.1, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Reserved2", 3.2, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Initial_Pos_Clean_Upp", 3.3, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Initial_Pos_clean_Lpp", 3.4, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Welt_Tool_Positioned", 3.5, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Sequence_Start_1", 3.6, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Sequence_Start_2", 3.7, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("PLC_OUT_ProgramNumber", 4.0, IOMode.Read, IOType.Word, ""),

                new SignalDefinition("PLC_OUT_Seam_Drive_To_Mid_Pos", 6.0, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("PLC_OUT_Seam_Drive_To_End_Pos", 6.1, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Switch_Wobble_On", 6.2, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Switch_Gas_Valve_On1", 6.3, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Switch_Gas_Valve_On2", 6.4, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Guide_Laser_On", 6.5, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Linear_Suttlemode", 6.6, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("PLC_OUT_Weld_Marking", 6.7, IOMode.Read, IOType.Bool, ""),


                // Logic Write
                // new SignalDefinition("PLC_IN_FB_Servicel_Mode", 0.0, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_FB_Manual_Mode", 0.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_FB_Automatic_Mode", 0.2, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Power_Supply_On", 0.3, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Errors", 0.4, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_With_Laser", 0.5, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_With_Force", 0.6, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_With_Compensation", 0.7, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("PLC_IN_With_Wobble", 1.0, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Seam_Drive_Referenced", 1.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Seam_Drive_In_PGCP", 1.2, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Seam_Drive_In_Start_Pos", 1.3, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_PS_Is_Open", 1.4, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_WS_Is_Open", 1.5, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_WS_Is_Close", 1.6, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_ProgramNrSelected", 1.7, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("PLC_IN_No_Laser_Errors", 2.0, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Laser_Warnings", 2.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_LSS_Errors", 2.2, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_LSS_Warnings", 2.3, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Protection_Glass_Error", 2.4, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Protection_Glass_Warning", 2.5, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Make_Welt_Tip_Geometry_Check", 2.6, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_CompensationInWorkPos", 2.7, IOMode.Write, IOType.Bool, ""),

                // new SignalDefinition("PLC_IN_Reduce_Power", 3.0, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Welding_Cycle_Active", 3.1, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Sensor_Geo_Station", 3.2, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Clean_Done", 3.3, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Clean_Error", 3.4, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Clean_Station_Ready", 3.5, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Program_Finish", 3.6, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Start_Lost", 3.7, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("PLC_IN_ProgramNumber", 4.0, IOMode.Write, IOType.Word, ""),

                new SignalDefinition("PLC_IN_Seam_Drive_in_Mid_Pos", 6.0, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Seam_Drive_in_End_Pos", 6.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_GasSensorUppOK", 6.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_GasSensorIppOK", 6.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_PPSensor1OK", 6.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_PPSensor2OK", 6.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Fumator_Working", 6.6, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Chiller_Errors", 6.7, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("PLC_IN_No_Chiller_Warnings", 7.0, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Geometry_Check_Errors", 7.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Geometry_Check_Warnings", 7.2, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Process_Monitor_Errors", 7.3, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Process_Monitor_Warnings", 7.4, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Fumator_Errors", 7.5, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Fumator_Warnings", 7.6, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_Quality_Monitor_Data_ready", 7.7, IOMode.Write, IOType.Bool, ""),

                // new SignalDefinition("PLC_IN_Counter_GeoStation_1", 8.0, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Counter_GeoStation_2", 8.1, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Counter_GeoStation_4", 8.2, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Counter_GeoStation_8", 8.3, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Counter_GeoStation_16", 8.4, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Counter_GeoStation_32", 8.5, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Counter_GeoStation_64", 8.6, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Counter_GeoStation_128", 8.7, IOMode.Write, IOType.Bool, ""),

                // new SignalDefinition("PLC_IN_Counter_GeoStation_256", 9.0, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Counter_GeoStation_512", 9.1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Quality_Monitor_Error", 9.2, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("PLC_IN_No_Quality_Monitor_Warnings", 9.3, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Z_Axis_Is_Up", 9.4, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Z_Axis_Is_Down", 9.5, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Y_Axis_Is_Left", 9.6, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Y_Axis_Is_Right", 9.7, IOMode.Write, IOType.Bool, ""),

                // new SignalDefinition("PLC_IN_Position_Parker_Drive", 10.0, IOMode.Write, IOType.Byte, ""),

                // new SignalDefinition("PLC_IN_Fumator_Sucction_Way_1", 11.0, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Fumator_Sucction_Way_2", 11.1, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Reserved_10_2", 11.2, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Reserved_10_3", 11.3, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Reserved_10_4", 11.4, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Reserved_10_5", 11.5, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Reserved_10_6", 11.6, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("PLC_IN_Reserved_10_7", 11.7, IOMode.Write, IOType.Bool, ""),

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

