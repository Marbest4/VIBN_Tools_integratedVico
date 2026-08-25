using System.Net.NetworkInformation;

namespace VIBN_Tools.Settings;

/// <summary>
/// Provides small, credential-free network helpers for the settings page.
/// Remote Desktop credentials deliberately remain in the Windows Credential
/// Manager of the interactive user and are never read from or written to the
/// application source, cache, or role store.
/// </summary>
public static class RemoteConnection
{
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
}
