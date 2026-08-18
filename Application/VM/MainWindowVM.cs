using VIBN_Tools.GlobalClasses;
using VIBN_Tools.Settings;

namespace VIBN_Tools.Application.VM
{
    public class MainWindowVM : MvvmBase
    {
        public MainWindowVM()
        {

        }



        public async Task InitializeAsync()
        {
            await RemoteConnection.WriteRemoteConnectionCredentials();
        }
    }
}
