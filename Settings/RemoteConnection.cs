using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;

namespace VIBN_Tools.Settings;

public static class RemoteConnection
{
    private static readonly string RemotePassword = "TerPet.Vun4S#.";

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
}
