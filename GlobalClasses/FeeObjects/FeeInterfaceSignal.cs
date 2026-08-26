using System.Windows;
using FS.API;
using FS.SDK.Extensibility.Contracts;
using FS.SDK.Io;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeInterfaceSignal : FeeAbstractObject, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================

        public new string Name => Tag;


        private string _tag;
        public string Tag
        {
            get => _tag;
            set => SetPropertyChange(ref _tag, value);
        }

        private string _address;
        public string Address
        {
            get => _address;
            set => SetPropertyChange(ref _address, value);
        }


        private string _path;
        public string Path
        {
            get => _path;
            set => SetPropertyChange(ref _path, value);
        }

        private string _comment;
        public string Comment
        {
            get => _comment;
            set => SetPropertyChange(ref _comment, value);
        }


        public string Value { get; set; }
        public int References { get; set; }


        private IOType _ioType;
        public IOType IOType
        {
            get { return _ioType; }
            set
            {
                _ioType = value;
                _ioTypeString = value.ToString();
            }
        }

        private string _ioTypeString;
        public string IOTypeString
        {
            get { return _ioTypeString; }
            set
            {
                _ioTypeString = value;
                _ioType = ParseIOType(value);
            }
        }

        private IOMode _usage;
        public IOMode Usage
        {
            get { return _usage; }
            set
            {
                _usage = value;
                _usageString = value.ToString();
            }
        }

        private string _usageString;
        public string UsageString
        {
            get { return _usageString; }
            set
            {
                _usageString = value;
                _usage = (value == "Read") ? IOMode.Read : IOMode.Write;
            }
        }

        public FeeInterface ParentInterface { get; set; }








        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeInterfaceSignal()
        {
            // Default Constructor
            Guid = Guid.NewGuid();
        }

        // Constructor for Interface-Signal
        public FeeInterfaceSignal(string tag, string address, string usage, string type, string comment = "")
        {
            Tag = tag;
            Address = address;
            UsageString = usage;
            IOTypeString = type;
            Comment = comment;
            Guid = Guid.NewGuid();
        }

        // Constructor for Symbolic-Signal
        public FeeInterfaceSignal(string tag, string type, string value, string comment)
        {
            Tag = tag;
            IOTypeString = type;
            Value = value;
            Comment = comment;
            Guid = Guid.NewGuid();
        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public async Task<bool> CreateSignalAsync(FeeInterface? targetInterface = null)
        {
            var tempInterface = targetInterface ?? ParentInterface;

            if (tempInterface == null)
                throw new InvalidOperationException("No FeeInterface provided!");

            if (!await Services.ApiInstance.Interface.UpdateOrCreateVariableAsync(new ApiInterfaceVariableDefinition
            {
                InterfacePluginProvider = tempInterface.ProviderGuid,
                InterfaceGuid = tempInterface.Guid,
                VariableGuid = Guid,
                Tag = Tag,
                InterfaceName = Tag,
                Address = Address,
                Path = Path,
                Type = IOType,
                Comment = Comment,
                Usage = Usage,
            }))
                return false;

            return true;
        }



        public static IOType ParseIOType(string input)
        {
            if (Enum.TryParse<IOType>(input, ignoreCase: true, out var result))
            {
                return result;
            }
            return default;

        }

        public void SetIoMode()
        {
            Usage = Address.StartsWith("A") || Address.StartsWith("PA") || Path.Contains(".Og") ? IOMode.Read : IOMode.Write;
        }


        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            if (Address != String.Empty && Path != String.Empty)
                PlausibilityIssues.Add(new PlausibilityIssue($"Sowohl Adresse als auch Pfad vorhanden", Severity.Error));

            if (Address == String.Empty && Path == String.Empty)
                PlausibilityIssues.Add(new PlausibilityIssue($"Weder Adresse noch Pfad vorhanden", Severity.Error));

            if (Tag == String.Empty)
                PlausibilityIssues.Add(new PlausibilityIssue($"Kein Tag vergeben", Severity.Warning));

            if (Usage != IOMode.Read && Usage != IOMode.Write && ParentInterface.ProviderGuid != PluginGuids.Symbolicinterface)
                PlausibilityIssues.Add(new PlausibilityIssue($"Read/Write nicht gesetzt", Severity.Error));

            if (References == 0)
                PlausibilityIssues.Add(new PlausibilityIssue($"Signal nicht verknüpft", Severity.Error));

            if (ParentInterface != null)
            {
                foreach (var other in ParentInterface.Signals)
                {
                    if (other == this) continue;
                    if (string.IsNullOrEmpty(Address)) continue;
                    if (string.IsNullOrEmpty(other.Address)) continue;

                    var myAddr = ParseAddress(Address);
                    var otherAddr = ParseAddress(other.Address);

                    if (myAddr != null && otherAddr != null && Overlaps(myAddr, otherAddr))
                    {
                        PlausibilityIssues.Add(new PlausibilityIssue($"Adresskonflikt mit Signal '{other.Tag}'", Severity.Error));
                    }
                }
            }

        }







        //===================================================================================================================
        // A D D I T I O N A L S :   C O N S T A N T S ,   D E F I N E S ,   E T C .
        //===================================================================================================================


        private record ParsedAddress(char Area, int StartByte, int Length, int? Bit = null);

        private static ParsedAddress ParseAddress(string address)
        {

            // Exclude Robot Signals
            if (address.Contains("IN") || address.Contains("OUT") || (!address.StartsWith("A") && !address.StartsWith("E")))
                return null;

            // Exclude CMVM Axis Signals
            if (address.StartsWith("Axis"))
                return null;



            char area = address[0]; // 'A' or 'E'

            if (address.Contains('.'))
            {
                // Bool
                var parts = address.Substring(1).Split('.');
                return new ParsedAddress(area, int.Parse(parts[0]), 1, int.Parse(parts[1]));
            }

            // Byte, Word, DWord
            string type = address.Substring(1, 1); // B, W, D
            int start = int.Parse(address.Substring(2));

            return type switch
            {
                "B" => new ParsedAddress(area, start, 1),
                "W" => new ParsedAddress(area, start, 2),
                "D" => new ParsedAddress(area, start, 4),
                _ => throw new ArgumentException("Unbekanntes Format: " + address)
            };
        }
            

        private static bool Overlaps(ParsedAddress a, ParsedAddress b)
        {
            if (a.Area != b.Area) return false;

            int aEnd = a.StartByte + a.Length - 1;
            int bEnd = b.StartByte + b.Length - 1;

            // Same Bit-Address
            if (a.Bit.HasValue && b.Bit.HasValue)
                return a.StartByte == b.StartByte && a.Bit.Value == b.Bit.Value;

            // Bit-Adrress overlaps with other Byte/Word/Doubleword
            if (a.Bit.HasValue || b.Bit.HasValue)
            {
                var boolAddr = a.Bit.HasValue ? a : b;
                var blockAddr = a.Bit.HasValue ? b : a;
                int blockEnd = blockAddr.StartByte + blockAddr.Length - 1;
                return boolAddr.StartByte >= blockAddr.StartByte && boolAddr.StartByte <= blockEnd;
            }

            return a.StartByte <= bEnd && b.StartByte <= aEnd;
        }
    }



    public record SignalDefinition(string Name, double Offset, IOMode Mode, IOType Type, string Comment = "");



}
