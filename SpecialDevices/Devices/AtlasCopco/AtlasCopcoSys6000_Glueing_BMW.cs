using FS.SDK.Io;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.SpecialDevices;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace MaNiAC_Tool.SpecialDevices.RobotApplications.AtlasCopco
{
    public class AtlasCopcoSys6000_Glueing_BMW : SpecialDevice
    {


        //===========================================================================================================================
        // D E V I C E   S P E C I F I C   P R O P E R T I E S
        //===========================================================================================================================





        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public AtlasCopcoSys6000_Glueing_BMW(string prefix, SpecialDeviceAddresses addresses, RobotType robotType)
            : base(prefix, addresses, DeviceManufacturer.AtlasCopco, AtlasCopcoDeviceTypes.Sys6000_Glueing_BMW, robotType)
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
                LogicDefinitionName = LogicsSpecialDevice.AtlasCopco_Sys6000_Glueing_BMW.Name,
                LogicDefinitionPath = LogicsSpecialDevice.AtlasCopco_Sys6000_Glueing_BMW.Path,
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
                new SignalDefinition("ROB_OUT_Start", 0, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Pistole1_Oeffnen", 1, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Pistole2_Oeffnen", 2, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Pistole3_Oeffnen", 3, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Fuellen", 4, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Swirl", 5, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Vordruck_Trigger", 6, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Betriebsfreigabe", 7, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_Spuelen", 8, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Anspuelen", 9, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Freigabe_Spuelen", 10, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Zyklus_Ende", 11, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Fehler_Quittieren", 12, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_System_EIN", 13, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Heizung_Retrigger", 14, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Pumpe_Retrigger", 15, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_Programmanwahl_B0", 16, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_B1", 17, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_B2", 18, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_B3", 19, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_B4", 20, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_B5", 21, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_B6", 22, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_B7", 23, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_Parameteranwahl_B0", 24, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_B1", 25, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_B2", 26, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_B3", 27, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_B4", 28, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_B5", 29, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_B6", 30, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_B7", 31, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_Simulationsmodus", 32, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Energiesparmodus", 33, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Laser_EIN", 34, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Laser_Messen", 35, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Laser_SollwertLernen", 36, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Pistole4_Oeffnen", 37, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Steppnaht_EIN", 38, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Zirkulation", 39, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_1K_Spuelen_Komponente1", 40, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_1K_Spuelen_Komponente2", 41, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_2K_Spuelen", 42, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Komponente2_Ausschalten", 43, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Nahtreparatur", 44, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_DosiererInNachfuellstationBereit", 45, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_DosiererAnRoboterGedockt", 46, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Mischer_Wechsel", 47, IOMode.Read, IOType.Bool, ""),

                // Material B0–B15
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B0", 48, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B1", 49, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B2", 50, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B3", 51, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B4", 52, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B5", 53, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B6", 54, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B7", 55, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B8", 56, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B9", 57, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B10", 58, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B11", 59, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B12", 60, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B13", 61, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B14", 62, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Material_B15", 63, IOMode.Read, IOType.Bool, ""),

                // Swirl B0–B15
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B0", 64, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B1", 65, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B2", 66, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B3", 67, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B4", 68, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B5", 69, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B6", 70, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B7", 71, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B8", 72, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B9", 73, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B10", 74, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B11", 75, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B12", 76, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B13", 77, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B14", 78, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Analogwertvorgabe_Swirl_B15", 79, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_Anfoderung_FahrenZuDockingStation", 80, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Anfoderung_FahrenAusDockingStation", 81, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_82", 82, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Anfoderung_Simulation_Vision", 83, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_84", 84, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_85", 85, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_86", 86, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Vision_Abgewaehlt", 87, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_88", 88, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_89", 89, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_90", 90, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_91", 91, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_92", 92, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_93", 93, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_94", 94, IOMode.Read, IOType.Bool, ""),
                // new SignalDefinition("Reserve_95", 95, IOMode.Read, IOType.Bool, ""),

                // MultiplexAdresse B0–B15
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B0", 96, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B1", 97, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B2", 98, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B3", 99, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B4", 100, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B5", 101, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B6", 102, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B7", 103, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B8", 104, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B9", 105, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B10", 106, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B11", 107, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B12", 108, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B13", 109, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B14", 110, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexAdresse_B15", 111, IOMode.Read, IOType.Bool, ""),

                // MultiplexIndex B0–B7
                new SignalDefinition("ROB_OUT_MultiplexIndex_B0", 112, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexIndex_B1", 113, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexIndex_B2", 114, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexIndex_B3", 115, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexIndex_B4", 116, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexIndex_B5", 117, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexIndex_B6", 118, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_MultiplexIndex_B7", 119, IOMode.Read, IOType.Bool, ""),



                // Logic Write
                new SignalDefinition("ROB_IN_Sammelstoerung", 0, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Oberer_Grenzwert", 1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Unterer_Grenzwert", 2, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Fehler_Material", 3, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Fehler_System", 4, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Simulation", 5, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Materialauftrag_OK", 6, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Betriebsbereit", 7, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Anforderung_Fuellen", 8, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Spuelanforderung", 9, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Anforderung_Anspuelen", 10, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Live_Bit", 11, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Dosierer_Voll", 12, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Spuelen_Beendet", 13, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Zyklus_Beendet", 14, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Bereit_Fuer_Applikation", 15, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Quittierung_Programm_B0", 16, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_B1", 17, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_B2", 18, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_B3", 19, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_B4", 20, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_B5", 21, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_B6", 22, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_B7", 23, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Quittierung_Parameter_B0", 24, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_B1", 25, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_B2", 26, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_B3", 27, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_B4", 28, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_B5", 29, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_B6", 30, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_B7", 31, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Automatik", 32, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Pumpe_EIN", 33, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Schmierung_EIN", 34, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Heizung_EIN", 35, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Heizung_Bereit", 36, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Sicherungsfall", 37, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Fass_Warnung_Unter10Prozent", 38, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Fehler_PumpeHeizung", 39, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_HPS1_Fass1_Warnung_Unter10Prozenz", 40, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_HPS1_Fass1_Leer", 41, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_HPS1_Fass2_Warnung_Unter10Prozenz", 42, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_HPS1_Fass2_Leer", 43, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_HPS2_Fass1_Warnung_Unter10Prozenz", 44, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_HPS2_Fass1_Leer", 45, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_HPS2_Fass2_Warnung_Unter10Prozenz", 46, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_HPS2_Fass2_Leer", 47, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_2K_ImMischer", 48, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_1K_Komponente1_ImMischer", 49, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_1K_Komponente2_ImMischer", 50, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Komponente1_Ueberschuss", 51, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Komponente2_Ueberschuss", 52, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Mischung_Undefiniert", 53, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Spuelanforderung_1K", 54, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Spuelanforderung_2K", 55, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Fehler_Mischung", 56, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Fehler_Einzelnaht_ObereGrenze", 57, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Fehler_Einzelnaht_UntereGrenze", 58, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Spuelen_Aktiv", 59, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Fehler_LaserUeberwachung", 60, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Fehler_Vision", 61, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Mischer_Vorhanden", 62, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Anforderung_Mischerwechsel", 63, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Quittierung_Nachfuellstation", 64, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_RoboterAngedockt", 65, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Vision_Naht_Reparierbar", 66, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("Reserve_67", 67, IOMode.Write, IOType.Bool, ""),
                // new SignalDefinition("Reserve_68", 68, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Dosierer_MitDockingStationVerbunden", 69, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_NOT_HALT_Aktiv", 70, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Fuellventil_Nachfuellstation", 71, IOMode.Write, IOType.Bool, ""),

                // Fehlercode B0–B15
                new SignalDefinition("ROB_IN_FehlerCode_B0", 72, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B1", 73, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B2", 74, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B3", 75, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B4", 76, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B5", 77, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B6", 78, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B7", 79, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_FehlerCode_B8", 80, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B9", 81, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B10", 82, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B11", 83, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B12", 84, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B13", 85, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B14", 86, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_FehlerCode_B15", 87, IOMode.Write, IOType.Bool, ""),

                // Vision Ergebnis
                new SignalDefinition("ROB_IN_VisionErgebnis_OK", 88, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_VisionErgebnis_Warnung", 89, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_VisionErgebnis_NOK", 90, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_VisionErgebnis_Fehler", 91, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_VisionSystem_EIN", 92, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Vision_Automatik", 93, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_VisionErgebnis_Verfuegbar", 94, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_KeinDockingFehler", 95, IOMode.Write, IOType.Bool, ""),

                // Multiplex Adressquittierung B0–B15
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B0", 96, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B1", 97, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B2", 98, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B3", 99, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B4", 100, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B5", 101, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B6", 102, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B7", 103, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B8", 104, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B9", 105, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B10", 106, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B11", 107, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B12", 108, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B13", 109, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Adressquittierung_B14", 110, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Fehlerbit", 111, IOMode.Write, IOType.Bool, ""),

                // Multiplex Indexquittierung B0–B7
                new SignalDefinition("ROB_IN_Multiplex_Indexquittierung_B0", 112, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Indexquittierung_B1", 113, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Indexquittierung_B2", 114, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Indexquittierung_B3", 115, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Indexquittierung_B4", 116, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Indexquittierung_B5", 117, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Indexquittierung_B6", 118, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Multiplex_Indexquittierung_B7", 119, IOMode.Write, IOType.Bool, ""),

                // Multiplex Daten B0–B31
                new SignalDefinition("ROB_IN_MultiplexDaten_B0", 120, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B1", 121, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B2", 122, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B3", 123, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B4", 124, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B5", 125, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B6", 126, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B7", 127, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_MultiplexDaten_B8", 128, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B9", 129, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B10", 130, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B11", 131, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B12", 132, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B13", 133, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B14", 134, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B15", 135, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_MultiplexDaten_B16", 136, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B17", 137, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B18", 138, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B19", 139, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B20", 140, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B21", 141, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B22", 142, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B23", 143, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_MultiplexDaten_B24", 144, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B25", 145, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B26", 146, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B27", 147, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B28", 148, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B29", 149, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B30", 150, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_MultiplexDaten_B31", 151, IOMode.Write, IOType.Bool, ""),


            };
        }



        protected override string CalculateAddress(SpecialDeviceAddresses baseAddresses, double offset, IOMode ioMode, IOType ioType)
        {
            return RobotAddressCalculator.Calculate(baseAddresses, offset, ioMode, ioType, RobotType!.Value);
        }





        //===========================================================================================================================
        // C R E A T E   D E V I C E
        //===========================================================================================================================

        protected override async Task<bool> WriteDeviceParameters() => true;

        protected override async Task<bool> CreateDeviceSpecificAsync() => true;


    }
}
