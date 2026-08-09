using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;

namespace VIBN_Tools.Settings
{
    public class RemoteConnection
    {

        public static async Task WriteRemoteConnectionCredentials()
        {
            foreach (var item in ServerUserNames)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmdkey.exe"),
                    Arguments = $"/generic:TERMSRV/{item.Server} /user:{item.User} /pass:{RemotePassword}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit();
                }
            }
        }



        public static async Task<bool> CheckServerReachableAsync(string serverName)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(serverName, 2000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }



        private static readonly string RemotePassword = "TerPet.Vun4S#.";

        public static readonly ObservableCollection<(string Server, string User)> ServerUserNames = new ObservableCollection<(string, string)>()
        {
            ("localhost", ""),

            ("GM19900", ""),

            ("GM15993", "zkds-simulation-p01"),

            ("GM16431", "zkds-simulation-p06"),

            ("GM17117", "zkds-simulation-p03"),
            ("GM17128", "zkds-simulation-p02"),
            ("GM17143", "zkds-simulation-p03"),
            ("GM17573", "zkds-simulation-p05"),
            ("GM17574", "zkds-simulation-p02"),
            ("GM17575", "zkds-simulation-p02")   ,
            ("GM17576", "zkds-simulation-p05"),
            ("GM17577", "zkds-simulation-p02"),
            ("GM17578", "zkds-simulation-p02"),
            ("GM17579", "zkds-simulation-p03"),
            ("GM17580", "zkds-simulation-p05"),
            ("GM17581", "zkds-simulation-p04"),
            ("GM17582", "zkds-simulation-p04"),
            ("GM17583", "zkds-simulation-p04"),
            ("GM17584", "zkds-simulation-p06"),
            ("GM17585", "zkds-simulation-p04"),
            ("GM17586", "zkds-simulation-p04"),
            ("GM17587", "zkds-simulation-p02"),
            ("GM17691", "zkds-simulation-p01"),
            ("GM17692", "zkds-simulation-p03"),
            ("GM17695", "zkds-simulation-p05"),

            ("GM18076", "zkds-simulation-p05"),
            ("GM18077", "zkds-simulation-p02"),
            ("GM18078", "zkds-simulation-p05"),
            ("GM18079", "zkds-simulation-p02"),
            ("GM18080", "zkds-simulation-p02"),
            ("GM18081", "zkds-simulation-p05"),
            ("GM18082", "zkds-simulation-p02"),
            ("GM18083", "zkds-simulation-p02"),
            ("GM18084", "zkds-simulation-p02"),
            ("GM18086", "zkds-simulation-p03"),
            ("GM18302", "zkds-simulation-p02"),
            ("GM18304", "zkds-simulation-p01"),
            ("GM18308", "zkds-simulation-p01"),
            ("GM18309", "zkds-simulation-p03"),
            ("GM18310", "zkds-simulation-p03"),
            ("GM18311", "zkds-simulation-p01"),
            ("GM18319", "zkds-simulation-p02"),

            ("GM19339", "zkds-simulation-p02"),
            ("GM19340", "zkds-simulation-p01"),
            ("GM19341", "zkds-simulation-p03"),
            ("GM19342", "zkds-simulation-p02"),
            ("GM19344", "zkds-simulation-p01"),
            ("GM19365", "zkds-simulation-p03"),
            ("GM19373", "zkds-simulation-p03"),
            ("GM19374", "zkds-simulation-p01"),
            ("GM19375", "zkds-simulation-p03"),
            ("GM19383", "zkds-simulation-p03"),
            ("GM19387", "zkds-simulation-p03"),
            ("GM19388", "zkds-simulation-p03"),
            ("GM19391", "zkds-simulation-p03"),
            ("GM19392", "zkds-simulation-p05"),
            ("GM19395", "zkds-simulation-p03"),
            ("GM19398", "zkds-simulation-p06"),
            ("GM19400", "zkds-simulation-p01"),
            ("GM19983", "zkds-simulation-p05"),

            ("GM20001", "zkds-simulation-p03"),
            ("GM20037", "zkds-simulation-p04"),
            ("GM20043", "zkds-simulation-p03"),
            ("GM20045", "zkds-simulation-p03"),
            ("GM20047", "zkds-simulation-p03"),
            ("GM20898", "zkds-simulation-p04"),

        };




    }
}
