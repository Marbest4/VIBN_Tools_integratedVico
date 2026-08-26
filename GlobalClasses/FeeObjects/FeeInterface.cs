using System.Diagnostics;
using FS.SDK.Extensibility.Contracts;
using FS.SDK.Extensibility.Interfaces;
using FS.SDK.Io;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeInterface : FeeAbstractObject, IPlausibilityCheck<List<FeeBasicFrame>>
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================


        public Guid ProviderGuid { get; set; }
        public string ProviderName { get; set; }


        private string _ipAddress;
        public string IpAddress
        {
            get => _ipAddress;
            set => SetPropertyChange(ref _ipAddress, value);
        }

        private string _port;
        public string Port
        {
            get => _port;
            set => SetPropertyChange(ref _port, value);
        }


        public List<FeeInterfaceSignal> Signals { get; set; }




        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeInterface()
        {
            Guid = Guid.NewGuid();
            ProviderGuid = Defines.GrobGenerationInterfaceProviderGuid;        // Standard Interface is Grob Generation Interface
        }

        //public FeeInterface(string interfaceName)
        //{
        //    Guid = Guid.NewGuid();
        //    ProviderGuid = Defines.GrobGenerationInterfaceProviderGuid;
        //    Name = interfaceName;
        //}

        //public FeeInterface(Guid interfaceProviderGuid, string interfaceName)
        //{
        //    Guid = Guid.NewGuid();
        //    ProviderGuid = interfaceProviderGuid;
        //    Name = interfaceName;
        //}

        public FeeInterface(Guid interfaceProviderGuid, Guid interfaceGuid, string interfaceName)
        {
            Guid = interfaceGuid;
            ProviderGuid = interfaceProviderGuid;
            Name = interfaceName;
        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        /// <summary>
        /// Function createas an Interface
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CreateInterfaceAsync()
        {
            await Services.ApiInstance.Interface.UpdateOrCreateInterfacePluginAsync(ProviderGuid, Guid, new Dictionary<string, object>
            {
                { "InterfaceName", Name}
            });

            // Check for interface existing

            var stopWatch = Stopwatch.StartNew();

            while(stopWatch.Elapsed < TimeSpan.FromSeconds(5))
            {
                var currentInterfaces = await Services.ApiInstance.Interface.GetAllInterfacesAsync();

                if (currentInterfaces.Any(value =>
                        System.Guid.TryParse(value, out var parsedGuid) && parsedGuid == Guid))
                    return true;

                await Task.Delay(10);
            }

            // Do not continue with variable creation against an interface that
            // the SDK has not made visible yet.
            return false;
        }




        /// <summary>
        /// Function reads all present interfaces and return a list with the interface information
        /// </summary>
        /// <returns></returns>
        public async static Task<List<FeeInterface>> GetAllInterfacesAsync()
        {
            List<FeeInterface> interfaceList = new List<FeeInterface>();

            var interfaceGuids = await Services.ApiInstance.Interface.GetAllInterfacesAsync();

            foreach (var guid in interfaceGuids)
            {
                var properties = await Services.ApiInstance.Interface.GetInterfacePropertiesAsync(Guid.Parse(guid));

                FeeInterface tempInterface = new FeeInterface()
                {
                    Guid = Guid.Parse(guid),
                    Name = properties.FirstOrDefault(x => x.PropertyName == "InterfaceName")?.PropertyValue,
                    ProviderGuid = Guid.Parse(properties.FirstOrDefault(x => x.PropertyName == "InterfaceProviderGuid")?.PropertyValue),
                    ProviderName = properties.FirstOrDefault(x => x.PropertyName == "InterfaceProvider")?.PropertyValue,
                    IpAddress = properties.FirstOrDefault(x => x.PropertyName == "IpAddress")?.PropertyValue,
                    Port = properties.FirstOrDefault(x => x.PropertyName == "Port")?.PropertyValue,
                };

                await tempInterface.LoadSignalsAsync();

                interfaceList.Add(tempInterface);
            }

            return interfaceList;

        }


        public async Task LoadSignalsAsync()
        {
            var variables = await Services.ApiInstance.Interface.GetAllVariablesAsync();

            Signals = variables
                .Where(x => x.InterfaceGuid == Guid)
                .Select(signal => new FeeInterfaceSignal
                {
                    Guid = signal.VariableGuid,
                    Tag = signal.Tag,
                    Address = signal.Address,
                    Path = signal.Path,
                    IOType = signal.Type,
                    Comment = signal.Comment,
                    Usage = signal.Usage,
                    References = signal.References,
                    ParentInterface = this
                }).ToList();
        }


        public async static Task<List<FeeInterfaceSignal>> GetAllSignalsFromInterfaceAsync(Guid interfaceGuid)
        {
            var allSignals = await Services.ApiInstance.Interface.GetAllVariablesAsync();

            var signals = allSignals.Where(x => x.InterfaceGuid.Equals(interfaceGuid))
                .Select(x => new FeeInterfaceSignal
                {
                    Guid = x.VariableGuid,
                    Tag = x.Tag,
                    Comment = x.Comment,
                    Address = x.Address,
                    Path = x.Path,
                    IOType = x.Type,
                    Usage = x.Usage,
                }).ToList();

            return signals;
        }






        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {

            // Check Signals Issues
            await Parallel.ForEachAsync(Signals, async (signal, ct) =>
            {
                await signal.CheckObjectIssuesAsync(newObjects);
            });


            if (String.IsNullOrWhiteSpace(Name))
                PlausibilityIssues.Add(new PlausibilityIssue($"Interfacename fehlt", Severity.Error));

            if ((String.IsNullOrWhiteSpace(Port) || Port == "0") && !(ProviderGuid == PluginGuids.Symbolicinterface || ProviderGuid == Defines.GrobGenerationInterfaceProviderGuid))
                PlausibilityIssues.Add(new PlausibilityIssue($"Port fehlt", Severity.Error));

            if (!System.Net.IPAddress.TryParse(IpAddress, out _) && !(ProviderGuid == PluginGuids.Symbolicinterface || ProviderGuid == Defines.GrobGenerationInterfaceProviderGuid))
                PlausibilityIssues.Add(new PlausibilityIssue($"Ungültige IP - Adresse: '{IpAddress}'", Severity.Error));

            if (!Signals.All(x => x.IsPlausible))
                PlausibilityIssues.Add(new PlausibilityIssue($"Mindestens ein Signal fehlerhaft", Severity.Warning));

            if (ProviderGuid == PluginGuids.ABBRobotInterface || ProviderGuid == PluginGuids.FanucRobotInterface || ProviderGuid == PluginGuids.KukaRobotInterface)
            {
                if (Signals.Count < 7)
                    PlausibilityIssues.Add(new PlausibilityIssue($"Roboterinterface: Anzahl Signale prüfen", Severity.Warning));
            }

            if (Signals.Count == 0)
                PlausibilityIssues.Add(new PlausibilityIssue($"Interface enthält keine Signale", Severity.Error));
        }

        public async Task CheckObjectIssuesAsync(List<FeeBasicFrame> parameter)
        {
            if (!parameter.Any(x => Name.Contains(x.Name)))
            {
                PlausibilityIssues.Add(new PlausibilityIssue($"Stationsname nicht im Interfacename enthalten", Severity.Error));
            }
        }






        //===================================================================================================================
        // A D D I T I O N A L S :   C O N S T A N T S ,   D E F I N E S ,   E T C .
        //===================================================================================================================






    }
}
