using FS.API;
using System.ComponentModel;
using System.Windows;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.Settings;

namespace VIBN_Tools.GlobalClasses
{
    public static class Services
    {
        public static CoreApi ApiInstance { get; private set; }
        public static FeeConnectionService Connection { get; private set; }
        public static FeeObjectService FeeObjects { get; private set; }
        public static ProjectSettings ProjectSettings { get; } = new ProjectSettings();





        public static void Initialize()
        {
            ApiInstance = new CoreApi();

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Connection = new FeeConnectionService();
            }

            FeeObjects = new FeeObjectService();

            // Load Fee Data only once
            //if (Connection != null)
            //{
            //    Action handler = null;

            //    handler = async () =>
            //    {
            //        if (Connection.IsConnected)
            //        {
            //            Connection.Connected -= handler;
            //            await FeeObjects.UpdateFeeDataAsync();
            //        }
            //    };
            //    Connection.Connected += handler;
            //}

            // Load Fee Data on every connect
            if (Connection != null)
            {
                Connection.Connected += async () =>
                {
                    if (Connection.LoadFeeDataOnConnect)
                    {
                        await FeeObjects.GetInitialFeeDataAsync();
                    }
                    
                };
            }


        }
    }
}
