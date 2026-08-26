using VIBN_Tools.GlobalClasses;
using VIBN_Tools.SpecialDevices;
using VIBN_Tools.Tia.Contracts;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace VIBN_Tools.Application.VM;

/// <summary>One selectable Special Device logic backed by the existing factory.</summary>
public sealed record SpecialDeviceLogicOption(DeviceManufacturer Manufacturer, Enum DeviceType)
{
    public string DisplayName => $"{Manufacturer} – {DeviceCatalog.GetDisplayName(DeviceType)}";

    public bool RequiresRobotType => DeviceMetadata.MetadataMap.TryGetValue(
        (Manufacturer, DeviceType),
        out var metadata) && metadata.RequiresRobotType;

    public static IReadOnlyList<SpecialDeviceLogicOption> All { get; } = DeviceFactory.DeviceFactoryMap.Keys
        .Select(key => new SpecialDeviceLogicOption(key.Item1, key.Item2))
        .OrderBy(option => option.Manufacturer)
        .ThenBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    /// <summary>
    /// Offers a conservative suggestion only when the TIA type clearly names
    /// a supported device. Ambiguous hardware deliberately remains unassigned.
    /// </summary>
    public static SpecialDeviceLogicOption? Suggest(TiaHardwareModuleInfo module)
    {
        var text = $"{module.ModuleName} {module.ModuleType} {module.TypeIdentifier}";
        return All.FirstOrDefault(option => option switch
        {
            { Manufacturer: DeviceManufacturer.Cognex } => text.Contains("COGNEX", StringComparison.OrdinalIgnoreCase),
            { Manufacturer: DeviceManufacturer.Keyence } => text.Contains("KEYENCE", StringComparison.OrdinalIgnoreCase),
            { Manufacturer: DeviceManufacturer.Ipg } => text.Contains("IPG", StringComparison.OrdinalIgnoreCase),
            { Manufacturer: DeviceManufacturer.Promess } => text.Contains("PROMESS", StringComparison.OrdinalIgnoreCase),
            { Manufacturer: DeviceManufacturer.Kuka } => text.Contains("KUKA", StringComparison.OrdinalIgnoreCase),
            { Manufacturer: DeviceManufacturer.Lenze, DeviceType: LenzeDeviceTypes.I950 } => text.Contains("I950", StringComparison.OrdinalIgnoreCase),
            { Manufacturer: DeviceManufacturer.Lenze, DeviceType: LenzeDeviceTypes.Motec8400 } => text.Contains("MOTEC", StringComparison.OrdinalIgnoreCase),
            { Manufacturer: DeviceManufacturer.Lenze, DeviceType: LenzeDeviceTypes.Protec8400 } => text.Contains("PROTEC", StringComparison.OrdinalIgnoreCase),
            { Manufacturer: DeviceManufacturer.Grob, DeviceType: GrobDeviceTypes.SafePnPn } =>
                text.Contains("SAFE", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("PN", StringComparison.OrdinalIgnoreCase),
            { Manufacturer: DeviceManufacturer.AtlasCopco, DeviceType: AtlasCopcoDeviceTypes.Sys6000_Glueing_BMW } =>
                text.Contains("ATLAS", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("BMW", StringComparison.OrdinalIgnoreCase),
            { Manufacturer: DeviceManufacturer.AtlasCopco, DeviceType: AtlasCopcoDeviceTypes.Sys6000_Glueing_VASS } =>
                text.Contains("ATLAS", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("VASS", StringComparison.OrdinalIgnoreCase),
            _ => false
        });
    }
}

/// <summary>
/// Editable staging row between TIA hardware discovery and Special Device
/// creation. The user can inspect and correct addresses before FEE is touched.
/// </summary>
public sealed class TiaHardwareDeviceRowVM : MvvmBase
{
    private bool _include;
    private string _prefix;
    private int? _inputByte;
    private int? _outputByte;
    private SpecialDeviceLogicOption? _selectedLogic;
    private RobotType? _selectedRobotType;
    private bool _isAdded;

    public TiaHardwareDeviceRowVM(TiaHardwareModuleInfo module)
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        _prefix = CreatePrefix(module.ModuleName);
        _inputByte = module.InputStartByte >= 0 ? module.InputStartByte : null;
        _outputByte = module.OutputStartByte >= 0 ? module.OutputStartByte : null;
        _selectedLogic = SpecialDeviceLogicOption.Suggest(module);
        _include = _selectedLogic is not null;
    }

    public TiaHardwareModuleInfo Module { get; }

    public int Slot => Module.Slot;

    public string DeviceName => Module.DeviceName;

    public string ModuleName => Module.ModuleName;

    public string ModuleType => Module.ModuleType;

    public string TypeIdentifier => Module.TypeIdentifier;

    public string FirmwareVersion => Module.FirmwareVersion;

    public int InputLength => Module.InputLength;

    public int OutputLength => Module.OutputLength;

    public string InputAddressRange => FormatAddressRange(InputByte, InputLength);

    public string OutputAddressRange => FormatAddressRange(OutputByte, OutputLength);

    public bool Include
    {
        get => _include;
        set
        {
            _include = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(State));
        }
    }

    public string Prefix
    {
        get => _prefix;
        set
        {
            _prefix = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public int? InputByte
    {
        get => _inputByte;
        set
        {
            _inputByte = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(InputAddressRange));
        }
    }

    public int? OutputByte
    {
        get => _outputByte;
        set
        {
            _outputByte = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OutputAddressRange));
        }
    }

    public SpecialDeviceLogicOption? SelectedLogic
    {
        get => _selectedLogic;
        set
        {
            _selectedLogic = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RequiresRobotType));
        }
    }

    public RobotType? SelectedRobotType
    {
        get => _selectedRobotType;
        set
        {
            _selectedRobotType = value;
            OnPropertyChanged();
        }
    }

    public bool RequiresRobotType => SelectedLogic?.RequiresRobotType == true;

    public bool IsAdded
    {
        get => _isAdded;
        set
        {
            _isAdded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(State));
        }
    }

    public string State => IsAdded ? "In Warteschlange" : Include ? "Ausgewählt" : "Nicht ausgewählt";

    public bool TryCreate(out SpecialDevice? device, out string error)
    {
        device = null;
        error = string.Empty;
        if (!Include || IsAdded)
            return false;
        if (SelectedLogic is null)
        {
            error = $"{ModuleName}: Bitte eine Logik auswählen.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Prefix))
        {
            error = $"{ModuleName}: Ein Präfix ist erforderlich.";
            return false;
        }
        if (InputByte is null || OutputByte is null)
        {
            error = $"{ModuleName}: Eingangs- und Ausgangsbyte müssen bekannt oder manuell ergänzt sein.";
            return false;
        }
        if (RequiresRobotType && SelectedRobotType is null)
        {
            error = $"{ModuleName}: Für diese Logik ist ein Robotertyp erforderlich.";
            return false;
        }

        device = DeviceFactory.Create(
            SelectedLogic.Manufacturer,
            SelectedLogic.DeviceType,
            Prefix.Trim(),
            new SpecialDeviceAddresses(InputByte.Value, OutputByte.Value),
            SelectedRobotType);
        return true;
    }

    private static string CreatePrefix(string value)
    {
        var result = new string((value ?? string.Empty)
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray())
            .Trim('_');
        return result.Length == 0 ? "Device" : result;
    }

    private static string FormatAddressRange(int? start, int length)
    {
        if (start is null)
            return "—";
        var end = length > 0 ? start.Value + length - 1 : start.Value;
        return end > start.Value ? $"{start.Value}–{end}" : start.Value.ToString();
    }
}
