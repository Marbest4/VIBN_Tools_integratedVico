using FS.SDK.Network.API;
using System.Windows.Threading;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Settings
{
    /// <summary>Polls the FEE SDK state and exposes confirmed connection transitions to the UI.</summary>
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

        /// <summary>
        /// Waits for the FEE SDK to report a real connected state. Connect can
        /// return before the remote server has accepted the session, so callers
        /// must not use its return alone as a success indication.
        /// </summary>
        public async Task<bool> WaitForConnectedAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            do
            {
                CheckConnection();
                if (IsConnected)
                    return true;
                await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);
            }
            while (DateTimeOffset.UtcNow < deadline);

            CheckConnection();
            return IsConnected;
        }

        /// <summary>Waits until the SDK no longer reports a live remote session.</summary>
        public async Task<bool> WaitForDisconnectedAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            do
            {
                CheckConnection();
                if (!IsConnected && !IsConnecting)
                    return true;
                await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);
            }
            while (DateTimeOffset.UtcNow < deadline);

            CheckConnection();
            return !IsConnected && !IsConnecting;
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
