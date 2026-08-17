using VIBN_Tools.GlobalClasses;
using VIBN_Tools.Settings;

namespace VIBN_Tools.Application.VM
{
    public class MainWindowVM : MvvmBase
    {
        public MainWindowVM()
        {
            FeeSdkStatusText = Services.IsFeeSdkAvailable
                ? string.Empty
                : $"fe.screen-sim SDK nicht verfügbar: {Services.FeeSdkInitializationError}";
        }

        public string FeeSdkStatusText { get; }

        public bool IsFeeSdkAvailable => Services.IsFeeSdkAvailable;


        public async Task InitializeAsync()
        {
            await RemoteConnection.WriteRemoteConnectionCredentials();
        }
    }
}
