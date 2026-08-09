using FS.SDK.Io;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.SpecialDevices;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace MaNiAC_Tool.SpecialDevices.RobotApplications.AtlasCopco
{
    public class AtlasCopcoSys6000_Glueing_VASS : SpecialDevice
    {

        //===========================================================================================================================
        // D E V I C E   S P E C I F I C   P R O P E R T I E S
        //===========================================================================================================================





        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public AtlasCopcoSys6000_Glueing_VASS(string prefix, SpecialDeviceAddresses addresses, RobotType robotType)
            : base(prefix, addresses, DeviceManufacturer.AtlasCopco, AtlasCopcoDeviceTypes.Sys6000_Glueing_VASS, robotType)
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
                LogicDefinitionName = LogicsSpecialDevice.AtlasCopco_Sys6000_Glueing_VASS.Name,
                LogicDefinitionPath = LogicsSpecialDevice.AtlasCopco_Sys6000_Glueing_VASS.Path,
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
                new SignalDefinition("ROB_OUT_Programmanwahl_W1", 0, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_W2", 1, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_W4", 2, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_W8", 3, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_W16", 4, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_W32", 5, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_W64", 6, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Programmanwahl_W128", 7, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_Parameteranwahl_W1", 8, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_W2", 9, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_W4", 10, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_W8", 11, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_W16", 12, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_W32", 13, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_W64", 14, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Parameteranwahl_W128", 15, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_Pistolenanwahl_W1", 16, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Pistolenanwahl_W2", 17, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Pistolenanwahl_W4", 18, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Steppnaht_Aktiv", 19, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 20, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 21, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Betriebsfreigabe", 22, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Roboter_In_Automatik", 23, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_Start_Prozess", 24, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Vorwahl_Simulationsbetrieb", 25, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Dosierer_Fuellen", 26, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Dosierer_Wechsel", 27, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Vordrucktrigger", 28, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Freigabe_Spuelen", 29, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Spuelen", 30, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 31, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_Start_Prozesskontrolle", 32, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Duese_Freiblasen", 33, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Bewertung_BT_iO", 34, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Bewertung_BT_Wiederholung", 35, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Bewertung_BT_Ausschleusen", 36, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Bewertung_Auto_Rep", 37, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Messen_Ende", 38, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Pistole_Auf", 39, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_Systemkomponenten_Ein", 40, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Energiesparmodus_Ein", 41, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Pumpenzeit_Reset", 42, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Vorwahl_BT_Zeigen", 43, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Heizung_Aus", 44, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Heizung_Reset", 45, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 46, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_Abstandsmessung_Ein", 47, IOMode.Read, IOType.Bool, ""),

                new SignalDefinition("ROB_OUT_1K_Freispuelen", 48, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 49, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 50, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_2K_Messen_Kontrolle", 51, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_2K_Mischerwechsel", 52, IOMode.Read, IOType.Bool, ""),
                new SignalDefinition("ROB_OUT_2K_Mischer_Ein", 53, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 54, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 55, IOMode.Read, IOType.Bool, ""),

                //new SignalDefinition("ROB_OUT_Reserviert", 56, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 57, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 58, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 59, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 60, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 61, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 62, IOMode.Read, IOType.Bool, ""),
                //new SignalDefinition("ROB_OUT_Reserviert", 63, IOMode.Read, IOType.Bool, ""),



                // Logic Write
                new SignalDefinition("ROB_IN_Quittierung_Programm_W1", 0, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_W2", 1, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_W4", 2, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_W8", 3, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_W16", 4, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_W32", 5, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_W64", 6, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Programm_W128", 7, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Quittierung_Parameter_W1", 8, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_W2", 9, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_W4", 10, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_W8", 11, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_W16", 12, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_W32", 13, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_W64", 14, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Parameter_W128", 15, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Quittierung_Pistole_W1", 16, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Pistole_W2", 17, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Pistole_W4", 18, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Keine_Sammelstoerung", 19, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Kein_Fehler_Dosiereinheit", 20, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Kein_Fehler_Klebeauftrag", 21, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Betriebsbereit", 22, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_In_Automatik", 23, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Vordruck_Kleben_Bereit", 24, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Rueckmeldung_Simulationsbetrieb", 25, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Anforderung_Fuellen", 26, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Dosierer1_Gefuellt", 27, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Dosierer2_Gefuellt", 28, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Anforderung_Spuelen", 29, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Spuelen_Laeuft", 30, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Spuelen_Fertig", 31, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Prozesskontrolle_niO", 32, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Prozesskontrolle_iO", 33, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_BT_iO", 34, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_BT_Wiederholung", 35, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_BT_Ausschleusen", 36, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Quittierung_Auto_Rep", 37, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Messen_Beendet", 38, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_TCP_Offset", 39, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Systemkomponenten_Ein", 40, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Energiesparmodus_Ein", 41, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_BT_Auto_Rep_Moeglich", 42, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Kein_Fehler_Pumpensystem", 43, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Vorwarnung_Fassfuellstand", 44, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Heizung_An", 45, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Heizung_Temperatur_iO", 46, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Vorwarnungen", 47, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_1K_Freispuelen_Laeuft", 48, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_1K_Im_Mischer", 49, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_2K_Im_Mischer", 50, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Mischung_Undefiniert", 51, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Anforderung_Mischerwechsel", 52, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Drehzahl_Mischer_iO", 53, IOMode.Write, IOType.Bool, ""),
                //new SignalDefinition("ROB_IN_Reserviert", 54, IOMode.Write, IOType.Bool, ""),
                //new SignalDefinition("ROB_IN_Reserviert", 55, IOMode.Write, IOType.Bool, ""),

                new SignalDefinition("ROB_IN_Anforderung_Wiegen", 56, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Waage_Leeren", 57, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Kontrolle_Menge_Wiegen_Fertig", 58, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_Kontrollmessung_Fertig", 59, IOMode.Write, IOType.Bool, ""),
                //new SignalDefinition("ROB_IN_Reserviert", 60, IOMode.Write, IOType.Bool, ""),
                //new SignalDefinition("ROB_IN_Reserviert", 61, IOMode.Write, IOType.Bool, ""),
                //new SignalDefinition("ROB_IN_Reserviert", 62, IOMode.Write, IOType.Bool, ""),
                new SignalDefinition("ROB_IN_2K_System", 63, IOMode.Write, IOType.Bool, ""),

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


