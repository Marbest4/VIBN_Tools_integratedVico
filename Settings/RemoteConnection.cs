using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;

namespace VIBN_Tools.Settings
{
    public class RemoteConnection
    {

        public static Task WriteRemoteConnectionCredentials()
        {
            // Credentials are intentionally no longer preloaded from the legacy fixed mapping.
            // ViCo synchronizes the current Kanbanize users and stores only the selected
            // workstation credential immediately before mstsc is started.
            return Task.CompletedTask;
        }

        public static void SynchronizeServerUsers(IEnumerable<(string Server, string User)> kanbanizeUsers)
        {
            var usersByServer = kanbanizeUsers
                .Where(item => !string.IsNullOrWhiteSpace(item.Server))
                .GroupBy(item => item.Server.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.User.Trim())
                        .FirstOrDefault(user => !string.IsNullOrWhiteSpace(user)) ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < ServerUserNames.Count; index++)
            {
                var current = ServerUserNames[index];
                ServerUserNames[index] = usersByServer.TryGetValue(current.Server, out var user)
                    ? (current.Server, user)
                    : (current.Server, string.Empty);
            }

            foreach (var item in usersByServer.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!ServerUserNames.Any(existing =>
                    string.Equals(existing.Server, item.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    ServerUserNames.Add((item.Key, item.Value));
                }
            }
        }

        public static void SaveCredential(string server, string user)
        {
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(user))
                throw new InvalidOperationException("Server und Kanbanize-Benutzer müssen gesetzt sein.");

            using var process = Process.Start(CreateCmdKeyStartInfo(
                $"/generic:TERMSRV/{server}",
                $"/user:{user}",
                $"/pass:{RemotePassword}"));
            process?.WaitForExit();
            if (process is not null && process.ExitCode != 0)
                throw new InvalidOperationException($"Windows-Anmeldeinformationen für {server} konnten nicht gesetzt werden.");
        }

        public static async Task DeleteCredentialLaterAsync(string server, TimeSpan delay)
        {
            try
            {
                await Task.Delay(delay);
                using var process = Process.Start(CreateCmdKeyStartInfo($"/delete:TERMSRV/{server}"));
                process?.WaitForExit();
            }
            catch
            {
                // Cleanup is best effort and must never terminate the desktop application.
            }
        }

        private static ProcessStartInfo CreateCmdKeyStartInfo(params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmdkey.exe"),
                CreateNoWindow = true,
                UseShellExecute = false
            };
            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);
            return startInfo;
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
            ("GM15993", ""),
            ("GM16431", ""),
            ("GM17117", ""),
            ("GM17128", ""),
            ("GM17143", ""),
            ("GM17573", ""),
            ("GM17574", ""),
            ("GM17575", ""),
            ("GM17576", ""),
            ("GM17577", ""),
            ("GM17578", ""),
            ("GM17579", ""),
            ("GM17580", ""),
            ("GM17581", ""),
            ("GM17582", ""),
            ("GM17583", ""),
            ("GM17584", ""),
            ("GM17585", ""),
            ("GM17586", ""),
            ("GM17587", ""),
            ("GM17691", ""),
            ("GM17692", ""),
            ("GM17695", ""),
            ("GM18076", ""),
            ("GM18077", ""),
            ("GM18078", ""),
            ("GM18079", ""),
            ("GM18080", ""),
            ("GM18081", ""),
            ("GM18082", ""),
            ("GM18083", ""),
            ("GM18084", ""),
            ("GM18086", ""),
            ("GM18302", ""),
            ("GM18304", ""),
            ("GM18308", ""),
            ("GM18309", ""),
            ("GM18310", ""),
            ("GM18311", ""),
            ("GM18319", ""),
            ("GM19339", ""),
            ("GM19340", ""),
            ("GM19341", ""),
            ("GM19342", ""),
            ("GM19344", ""),
            ("GM19365", ""),
            ("GM19373", ""),
            ("GM19374", ""),
            ("GM19375", ""),
            ("GM19383", ""),
            ("GM19387", ""),
            ("GM19388", ""),
            ("GM19391", ""),
            ("GM19392", ""),
            ("GM19395", ""),
            ("GM19398", ""),
            ("GM19400", ""),
            ("GM19983", ""),
            ("GM20001", ""),
            ("GM20037", ""),
            ("GM20043", ""),
            ("GM20045", ""),
            ("GM20047", ""),
            ("GM20898", "")
        };




    }
}
