using FS.API;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.Settings;

namespace VIBN_Tools.GlobalClasses
{
    public static class Services
    {
        public static CoreApi ApiInstance { get; private set; } = null!;
        public static FeeConnectionService Connection { get; private set; } = null!;
        public static FeeObjectService FeeObjects { get; private set; } = null!;
        public static ProjectSettings ProjectSettings { get; } = new ProjectSettings();

        public static bool IsFeeSdkAvailable { get; private set; }

        public static string? FeeSdkInitializationError { get; private set; }

        public static void Initialize()
        {
            // ViCo and TIA must remain usable even when the optional fe.screen-sim
            // runtime is missing or incomplete on the current workstation.
            FeeObjects = new FeeObjectService();

            try
            {
                ApiInstance = new CoreApi();
                IsFeeSdkAvailable = true;
                FeeSdkInitializationError = null;
            }
            catch (Exception exception)
            {
                IsFeeSdkAvailable = false;
                FeeSdkInitializationError = GetInnermostMessage(exception);
            }

            Connection = new FeeConnectionService();

            if (IsFeeSdkAvailable)
            {
                Connection.Connected += async () =>
                {
                    if (Connection.LoadFeeDataOnConnect)
                        await FeeObjects.GetInitialFeeDataAsync();
                };
            }
        }

        private static string GetInnermostMessage(Exception exception)
        {
            while (exception.InnerException is not null)
                exception = exception.InnerException;

            return exception.Message;
        }
    }
}
