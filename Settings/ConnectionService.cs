using FS.SDK.Network.API;
using System.Windows.Threading;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Settings
{
    public class FeeConnectionService : NotifyBase
    {
        private readonly DispatcherTimer _timer;

        public event Action Connected;

        public bool LoadFeeDataOnConnect { get; set; }


        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                bool changed = SetPropertyChange(ref _isConnected, value);

                if (changed && value)
                {
                    Connected?.Invoke();
                }
            }
        }

        private bool _isConnecting;
        public bool IsConnecting
        {
            get => _isConnecting;
            private set => SetPropertyChange(ref _isConnecting, value);
        }

        public FeeConnectionService()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += (sender, eventargs) => CheckConnection();
            _timer.Start();
        }




        private void CheckConnection()
        {
            // API Call for Connection State
            var state = Services.ApiInstance.ApiState;

            IsConnected = state == NetworkState.Connected;
            IsConnecting = state == NetworkState.Connecting;

        }
    }
}
