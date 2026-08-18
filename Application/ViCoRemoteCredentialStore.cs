using VIBN_Tools.Core.ViCo;
using VIBN_Tools.Settings;

namespace VIBN_Tools.Application;

public sealed class ViCoRemoteCredentialStore : IRemoteCredentialStore
{
    public void Save(string hostName, string userName) =>
        RemoteConnection.SaveCredential(hostName, userName);

    public void RemoveLater(string hostName, TimeSpan delay) =>
        _ = RemoteConnection.DeleteCredentialLaterAsync(hostName, delay);
}
