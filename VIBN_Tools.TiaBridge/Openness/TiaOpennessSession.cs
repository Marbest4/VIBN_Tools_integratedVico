using System.Reflection;
using System.Text.RegularExpressions;
using VIBN_Tools.Tia.Contracts;

namespace VIBN_Tools.TiaBridge.Openness;

public sealed class TiaOpennessSession : ITiaOpennessSession
{
    private static readonly Regex VersionPattern = new("^V(1[5-9]|2[0-2])$", RegexOptions.IgnoreCase);

    private Assembly? _engineeringAssembly;
    private dynamic? _portal;
    private dynamic? _project;
    private string? _engineeringDllPath;
    private string? _selectedVersion;
    private int? _selectedPlcIndex;

    public void SelectVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version) || !VersionPattern.IsMatch(version))
            throw new ArgumentException($"Unsupported TIA version: {version}", nameof(version));

        var normalized = version.ToUpperInvariant();
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Siemens",
            "Automation",
            $"Portal {normalized}",
            "PublicAPI",
            normalized,
            "Siemens.Engineering.dll");

        if (!File.Exists(path))
            throw new FileNotFoundException($"TIA Openness assembly for {normalized} was not found.", path);

        DisposePortal();
        _selectedVersion = normalized;
        _engineeringDllPath = path;
        _engineeringAssembly = Assembly.LoadFrom(path);
    }

    public void Attach()
    {
        var assembly = RequireAssembly();
        var portalType = assembly.GetType("Siemens.Engineering.TiaPortal", throwOnError: true);
        var getProcesses = portalType.GetMethod("GetProcesses", BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException(portalType.FullName, "GetProcesses");

        var processes = ((System.Collections.IEnumerable)getProcesses.Invoke(null, null))
            .Cast<object>()
            .ToArray();

        if (processes.Length == 0)
            throw new InvalidOperationException($"Keine geöffnete TIA-Portal-Instanz {_selectedVersion} gefunden.");

        var candidates = processes
            .Select(process => new PortalProcess(
                process,
                ReadStringMember(process, "ProjectPath"),
                ReadStringMember(process, "Id", "ProcessId")))
            .Where(process => !string.IsNullOrWhiteSpace(process.ProjectPath))
            .ToArray();
        if (candidates.Length > 1)
        {
            var projects = string.Join(" | ", candidates.Select(candidate =>
                $"{candidate.ProjectPath} (PID {candidate.ProcessId})"));
            throw new InvalidOperationException(
                $"Mehrere TIA-Projekte sind geöffnet. Nur ein Projekt in {_selectedVersion} geöffnet lassen: {projects}");
        }
        if (candidates.Length == 0 && processes.Length > 1)
            throw new InvalidOperationException(
                $"Mehrere TIA-Portal-Instanzen {_selectedVersion} sind geöffnet, aber keine meldet einen ProjectPath. Nur die Instanz mit dem gewünschten Projekt geöffnet lassen.");

        // Older releases and a few project types do not expose ProjectPath.
        // With a single process attaching is still unambiguous.
        var selected = candidates.FirstOrDefault()?.Process ?? processes[0];
        dynamic selectedProcess = selected;
        _portal = selectedProcess.Attach();

        // TIA's process information is a snapshot and the Openness firewall
        // confirmation can take noticeably longer than the former ten-second
        // window. Also support an already opened Multiuser local session: its
        // project is exposed through LocalSessions[n].Project, not Projects[0].
        const int maximumProjectWaits = 360;
        for (var attempt = 0; attempt < maximumProjectWaits; attempt++)
        {
            _project = TryResolveOpenProject(_portal);
            if (_project is not null)
                break;
            Thread.Sleep(250);
        }
        if (_project is null)
        {
            var selectedPath = candidates.FirstOrDefault()?.ProjectPath;
            throw new InvalidOperationException(
                $"Die verbundene TIA-Instanz {_selectedVersion} stellt nach 90 Sekunden weder ein Einzelprojekt noch eine geöffnete Multiuser-Local-Session über Openness bereit." +
                (string.IsNullOrWhiteSpace(selectedPath) ? string.Empty : $" Gemeldeter ProjectPath: {selectedPath}.") +
                " Den Openness-Firewall-Dialog in TIA mit 'Immer zulassen' bestätigen und TIA sowie VIBN Tools nach einer Änderung der Gruppe 'Siemens TIA Openness' neu anmelden.");
        }
        _selectedPlcIndex = null;
    }

    private static object? TryResolveOpenProject(object portal)
    {
        var projects = ReadEnumerableProperty(portal, "Projects");
        var project = projects.FirstOrDefault();
        if (project is not null)
            return project;

        foreach (var localSession in ReadEnumerableProperty(portal, "LocalSessions"))
        {
            try
            {
                var sessionProject = localSession.GetType().GetProperty("Project")?.GetValue(localSession, null);
                if (sessionProject is not null)
                    return sessionProject;
            }
            catch (Exception)
            {
                // A session can disappear while TIA is switching views.
            }
        }

        return null;
    }

    private static IReadOnlyList<object> ReadEnumerableProperty(object target, string propertyName)
    {
        try
        {
            var value = target.GetType().GetProperty(propertyName)?.GetValue(target, null);
            return value is System.Collections.IEnumerable enumerable
                ? enumerable.Cast<object>().Where(item => item is not null).ToArray()
                : Array.Empty<object>();
        }
        catch (Exception)
        {
            return Array.Empty<object>();
        }
    }

    public IReadOnlyList<TiaPlcInfo> ListPlcs()
    {
        var project = RequireProject();
        var plcs = new List<TiaPlcInfo>();

        for (var index = 0; index < project.Devices.Count; index++)
        {
            dynamic device = project.Devices[index];
            if (TryGetSoftware(index) == null)
                continue;

            plcs.Add(new TiaPlcInfo
            {
                Index = index,
                Name = Convert.ToString(device.Name) ?? string.Empty,
                TypeIdentifier = Convert.ToString(device.TypeIdentifier) ?? string.Empty
            });
        }

        return plcs;
    }

    public void SelectPlc(int plcIndex)
    {
        var project = RequireProject();
        if (plcIndex < 0 || plcIndex >= project.Devices.Count)
            throw new ArgumentOutOfRangeException(nameof(plcIndex));

        if (TryGetSoftware(plcIndex) == null)
            throw new InvalidOperationException($"Device at index {plcIndex} has no PLC software container.");

        _selectedPlcIndex = plcIndex;
    }

    /// <summary>
    /// Enumerates the selected PLC first and then every device tree in the open
    /// project, reading the input/output address compositions exposed by TIA
    /// Openness. Address offsets are kept in bytes; no project data is modified.
    /// </summary>
    public IReadOnlyList<TiaHardwareModuleInfo> ListHardware()
    {
        var project = RequireProject();
        if (!_selectedPlcIndex.HasValue)
            throw new InvalidOperationException("Select a PLC before reading hardware.");

        var modules = new List<TiaHardwareModuleInfo>();
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // PROFINET/PROFIBUS IO devices are independent entries in
        // Project.Devices, not children of the selected PLC rack. Traversing
        // only project.Devices[selectedPlc] therefore found CPU modules but
        // missed the process addresses of robots and special devices.
        var selectedDeviceIndex = _selectedPlcIndex.Value;
        var deviceCount = Convert.ToInt32((object)project.Devices.Count);
        var deviceOrder = Enumerable.Range(0, deviceCount)
            .OrderBy(index => index == selectedDeviceIndex ? 0 : 1)
            .ThenBy(index => index);
        foreach (var deviceIndex in deviceOrder)
        {
            dynamic device = project.Devices[deviceIndex];
            var deviceName = ReadStringMember(device, "Name");
            TraverseHardwareItems(device, deviceIndex, deviceName, modules, identities);
        }

        return modules
            .OrderBy(module => module.DeviceIndex == selectedDeviceIndex ? 0 : 1)
            .ThenBy(module => module.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(module => module.Slot < 0 ? int.MaxValue : module.Slot)
            .ThenBy(module => module.ModuleName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public TiaProjectTree ListProgramBlocks()
    {
        dynamic software = RequireSelectedSoftware();
        var tree = new TiaProjectTree();
        TraverseGroup(software.BlockGroup, string.Empty, "Blocks", tree);
        return tree;
    }

    public TiaProjectTree ListDataTypes()
    {
        dynamic software = RequireSelectedSoftware();
        var tree = new TiaProjectTree();
        TraverseGroup(software.TypeGroup, string.Empty, "Types", tree);
        return tree;
    }

    public void ImportBlock(TiaTransferPayload payload)
    {
        ValidateImportPayload(payload);
        dynamic software = RequireSelectedSoftware();
        dynamic group = ResolveGroup(software.BlockGroup, payload.FolderPath);
        Import(group.Blocks, payload.FilePath);
    }

    public void ExportBlock(TiaTransferPayload payload)
    {
        ValidateExportPayload(payload);
        dynamic software = RequireSelectedSoftware();
        dynamic group = ResolveGroup(software.BlockGroup, payload.FolderPath);
        Export(FindByName(group.Blocks, payload.ItemName), payload.FilePath);
    }

    public void ImportDataType(TiaTransferPayload payload)
    {
        ValidateImportPayload(payload);
        dynamic software = RequireSelectedSoftware();
        dynamic group = ResolveGroup(software.TypeGroup, payload.FolderPath);
        Import(group.Types, payload.FilePath);
    }

    public void ExportDataType(TiaTransferPayload payload)
    {
        ValidateExportPayload(payload);
        dynamic software = RequireSelectedSoftware();
        dynamic group = ResolveGroup(software.TypeGroup, payload.FolderPath);
        Export(FindByName(group.Types, payload.ItemName), payload.FilePath);
    }

    public void CreateBlockFolder(TiaFolderPayload payload)
    {
        ValidateFolderPayload(payload);
        dynamic software = RequireSelectedSoftware();
        CreateFolder(ResolveGroup(software.BlockGroup, payload.ParentPath), payload.Name);
    }

    public void CreateDataTypeFolder(TiaFolderPayload payload)
    {
        ValidateFolderPayload(payload);
        dynamic software = RequireSelectedSoftware();
        CreateFolder(ResolveGroup(software.TypeGroup, payload.ParentPath), payload.Name);
    }

    public IReadOnlyList<TiaAxisInfo> ConfigureAxes()
    {
        dynamic software = RequireSelectedSoftware();
        var axes = new List<TiaAxisInfo>();
        ProcessTechnologyGroup(software.TechnologicalObjectGroup, axes);
        return axes;
    }

    public void Save()
    {
        RequireProject().Save();
    }

    public void Dispose()
    {
        DisposePortal();
    }

    private Assembly RequireAssembly() => _engineeringAssembly
        ?? throw new InvalidOperationException("Select a TIA version before attaching.");

    private dynamic RequireProject() => _project
        ?? throw new InvalidOperationException("TIA Portal is not attached.");

    private dynamic RequireSelectedSoftware()
    {
        if (!_selectedPlcIndex.HasValue)
            throw new InvalidOperationException("Select a PLC first.");

        return TryGetSoftware(_selectedPlcIndex.Value)
            ?? throw new InvalidOperationException("Selected PLC software is no longer available.");
    }

    private dynamic? TryGetSoftware(int deviceIndex)
    {
        if (_project == null || _engineeringAssembly == null)
            return null;

        var engineeringAssembly = _engineeringAssembly
            ?? throw new InvalidOperationException("Select a TIA version first.");
        object projectObject = _project
            ?? throw new InvalidOperationException("Attach to a TIA project first.");
        dynamic project = projectObject;

        var softwareContainerType = engineeringAssembly.GetType(
            "Siemens.Engineering.HW.Features.SoftwareContainer",
            throwOnError: true)!;

        foreach (dynamic deviceItem in project.Devices[deviceIndex].DeviceItems)
        {
            var getService = deviceItem.GetType().GetMethod("GetService")?.MakeGenericMethod(softwareContainerType);
            if (getService == null)
                continue;

            dynamic? container = getService.Invoke(deviceItem, null);
            if (container != null)
                return container.Software;
        }

        return null;
    }

    private static void TraverseHardwareItems(
        object parent,
        int deviceIndex,
        string deviceName,
        ICollection<TiaHardwareModuleInfo> modules,
        ISet<string> identities,
        string? deviceItemName = null)
    {
        foreach (var item in GetChildDeviceItems(parent))
        {
            var moduleName = ReadStringMember(item, "Name");
            var rootDeviceItemName = string.IsNullOrWhiteSpace(deviceItemName)
                ? moduleName
                : deviceItemName;
            var displayDeviceName = string.IsNullOrWhiteSpace(rootDeviceItemName) ||
                                    string.Equals(deviceName, rootDeviceItemName, StringComparison.OrdinalIgnoreCase)
                ? deviceName
                : $"{deviceName} ({rootDeviceItemName})";
            var typeIdentifier = ReadStringMember(item, "TypeIdentifier");
            var moduleType = ReadStringMember(item, "TypeName", "Classification");
            var firmwareVersion = ReadStringMember(item, "FirmwareVersion");
            if (string.IsNullOrWhiteSpace(firmwareVersion))
                firmwareVersion = ExtractFirmwareVersion(typeIdentifier);
            var slot = ReadIntMember(item, "PositionNumber", "Slot");
            var inputStart = -1;
            var inputLength = 0;
            var outputStart = -1;
            var outputLength = 0;

            foreach (var address in GetAddresses(item))
            {
                var ioType = ReadAddressIoType(address);
                var start = ReadAddressStart(address);
                var length = Math.Max(0, ReadAddressLength(address));
                if (ioType.IndexOf("Input", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    MergeAddressRange(ref inputStart, ref inputLength, start, length);
                }
                else if (ioType.IndexOf("Output", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    MergeAddressRange(ref outputStart, ref outputLength, start, length);
                }
            }

            var identity = $"{deviceIndex}|{slot}|{moduleName}|{typeIdentifier}";
            if (identities.Add(identity))
            {
                modules.Add(new TiaHardwareModuleInfo
                {
                    DeviceIndex = deviceIndex,
                    Slot = slot,
                    DeviceName = displayDeviceName,
                    ModuleName = moduleName,
                    ModuleType = moduleType,
                    TypeIdentifier = typeIdentifier,
                    FirmwareVersion = firmwareVersion,
                    InputStartByte = inputStart,
                    InputLength = inputLength,
                    OutputStartByte = outputStart,
                    OutputLength = outputLength
                });
            }

            TraverseHardwareItems(item, deviceIndex, deviceName, modules, identities, rootDeviceItemName);
        }
    }

    private static IReadOnlyList<object> GetChildDeviceItems(object target)
        => ReadEnumerableMember(target, "DeviceItems");

    private static IReadOnlyList<object> GetAddresses(object target)
        => ReadEnumerableMember(target, "Addresses");

    /// <summary>
    /// Openness proxy objects frequently expose compositions through an
    /// explicit interface or a public base class. A plain dynamic access can
    /// therefore fail although DeviceItems/Addresses are available. Resolve
    /// the complete runtime type hierarchy and implemented interfaces.
    /// </summary>
    private static IReadOnlyList<object> ReadEnumerableMember(object target, string memberName)
    {
        var value = ReadMemberValue(target, memberName);
        return value is System.Collections.IEnumerable enumerable
            ? enumerable.Cast<object>().Where(item => item is not null).ToArray()
            : Array.Empty<object>();
    }

    private static string ReadAddressIoType(object address)
        => ReadStringMember(address, "IoType", "IOType", "AddressType");

    private static int ReadAddressStart(object address)
        => ReadIntMember(address, "StartAddress", "StartAdress", "StartByte");

    private static int ReadAddressLength(object address)
        => ReadIntMember(address, "Length", "ByteLength", "Size");

    private static void MergeAddressRange(
        ref int currentStart,
        ref int currentLength,
        int candidateStart,
        int candidateLength)
    {
        if (candidateStart < 0)
            return;
        if (currentStart < 0)
        {
            currentStart = candidateStart;
            currentLength = candidateLength;
            return;
        }

        var end = Math.Max(currentStart + currentLength, candidateStart + candidateLength);
        currentStart = Math.Min(currentStart, candidateStart);
        currentLength = Math.Max(0, end - currentStart);
    }

    private static string ExtractFirmwareVersion(string typeIdentifier)
    {
        var match = Regex.Match(typeIdentifier ?? string.Empty, @"/(?<version>V[^/]+)$", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["version"].Value.Trim() : string.Empty;
    }

    private static string ReadStringMember(object target, params string[] names)
    {
        foreach (var name in names)
        {
            try
            {
                var value = ReadMemberValue(target, name);
                if (value is not null)
                    return Convert.ToString(value) ?? string.Empty;
            }
            catch (Exception)
            {
                // Dynamic Openness attributes are not supported by every device item.
            }
        }
        return string.Empty;
    }

    private static int ReadIntMember(object target, params string[] names)
    {
        foreach (var name in names)
        {
            try
            {
                var value = ReadMemberValue(target, name);
                if (value is not null && int.TryParse(Convert.ToString(value), out var number))
                    return number;
            }
            catch (Exception)
            {
                // Some module types do not expose an address or slot.
            }
        }
        return -1;
    }

    private static object? ReadMemberValue(object target, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                   BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        for (var type = target.GetType(); type is not null; type = type.BaseType)
        {
            var property = type.GetProperties(flags).FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
            if (property is not null)
            {
                try
                {
                    return property.GetValue(target, null);
                }
                catch (Exception)
                {
                    // Explicit Openness interfaces are attempted below.
                }
            }
        }

        foreach (var interfaceType in target.GetType().GetInterfaces())
        {
            var property = interfaceType.GetProperties().FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
            if (property is null)
                continue;
            try
            {
                return property.GetValue(target, null);
            }
            catch (Exception)
            {
                // Not every Openness proxy supports every interface member.
            }
        }

        try
        {
            return ReadEngineeringAttribute(target, name);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static object? ReadEngineeringAttribute(object target, string name)
    {
        var publicMethod = target.GetType().GetMethod("GetAttribute", new[] { typeof(string) });
        if (publicMethod is not null)
            return publicMethod.Invoke(target, new object[] { name });

        var engineeringInterface = target.GetType().GetInterfaces().FirstOrDefault(type =>
            string.Equals(type.FullName, "Siemens.Engineering.IEngineeringObject", StringComparison.Ordinal));
        return engineeringInterface?.GetMethod("GetAttribute", new[] { typeof(string) })
            ?.Invoke(target, new object[] { name });
    }

    private sealed class PortalProcess
    {
        public PortalProcess(object process, string projectPath, string processId)
        {
            Process = process;
            ProjectPath = projectPath;
            ProcessId = processId;
        }

        public object Process { get; }
        public string ProjectPath { get; }
        public string ProcessId { get; }
    }

    private static void TraverseGroup(dynamic group, string parentPath, string itemCollection, TiaProjectTree tree)
    {
        dynamic items = GetProperty(group, itemCollection);
        foreach (dynamic item in items)
        {
            tree.Items.Add(new TiaProgramItemInfo
            {
                Name = Convert.ToString(item.Name) ?? string.Empty,
                FolderPath = parentPath
            });
        }

        foreach (dynamic child in group.Groups)
        {
            var name = Convert.ToString(child.Name) ?? string.Empty;
            var path = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
            tree.Folders.Add(new TiaFolderInfo { Name = name, Path = path });
            TraverseGroup(child, path, itemCollection, tree);
        }
    }

    private static dynamic ResolveGroup(dynamic root, string path)
    {
        dynamic current = root;
        foreach (var segment in path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
        {
            dynamic? next = null;
            foreach (dynamic group in current.Groups)
            {
                if (string.Equals(Convert.ToString(group.Name), segment, StringComparison.OrdinalIgnoreCase))
                {
                    next = group;
                    break;
                }
            }

            current = next ?? throw new InvalidOperationException($"TIA folder was not found: {path}");
        }

        return current;
    }

    private void Import(dynamic composition, string filePath)
    {
        var assembly = RequireAssembly();
        var optionsType = assembly.GetType("Siemens.Engineering.ImportOptions", throwOnError: true);
        dynamic options = Activator.CreateInstance(optionsType);

        optionsType.GetProperty("OverwriteExisting")?.SetValue(options, true, null);
        optionsType.GetProperty("OverrideExisting")?.SetValue(options, true, null);
        optionsType.GetProperty("KeepOriginalName")?.SetValue(options, true, null);
        composition.Import(new FileInfo(filePath), options);
    }

    private void Export(dynamic item, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        if (File.Exists(filePath))
            File.Delete(filePath);

        var optionsType = RequireAssembly().GetType("Siemens.Engineering.ExportOptions", throwOnError: true);
        dynamic options = Activator.CreateInstance(optionsType);
        item.Export(new FileInfo(filePath), options);
    }

    private static dynamic FindByName(dynamic composition, string name)
    {
        foreach (dynamic item in composition)
        {
            if (string.Equals(Convert.ToString(item.Name), name, StringComparison.OrdinalIgnoreCase))
                return item;
        }

        throw new InvalidOperationException($"TIA item was not found: {name}");
    }

    private static void CreateFolder(dynamic parent, string name)
    {
        foreach (dynamic child in parent.Groups)
        {
            if (string.Equals(Convert.ToString(child.Name), name, StringComparison.OrdinalIgnoreCase))
                return;
        }

        parent.Groups.Create(name);
    }

    private static void ProcessTechnologyGroup(dynamic group, ICollection<TiaAxisInfo> axes)
    {
        if (HasProperty(group, "TechnologicalObjects"))
        {
            foreach (dynamic technologyObject in group.TechnologicalObjects)
            {
                var technologyType = Convert.ToString(technologyObject.OfSystemLibElement) ?? string.Empty;
                if (technologyType.IndexOf("Axis", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var name = Convert.ToString(technologyObject.Name) ?? string.Empty;
                axes.Add(new TiaAxisInfo { Name = name, TechnologyType = technologyType });
                ConfigureAxisParameters(technologyObject, name);
            }
        }

        if (!HasProperty(group, "Groups"))
            return;

        foreach (dynamic child in group.Groups)
            ProcessTechnologyGroup(child, axes);
    }

    private static void ConfigureAxisParameters(dynamic technologyObject, string axisName)
    {
        var linear = axisName.IndexOf("X", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     axisName.IndexOf("Y", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     axisName.IndexOf("Z", StringComparison.OrdinalIgnoreCase) >= 0;
        var motionType = linear ? 0 : 1;

        var parameterValues = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["_Properties.MotionType"] = motionType,
            ["Modulo.Enable"] = 0,
            ["Actor.DataAdaption"] = 0,
            ["Sensor[1].DataAdaption"] = 0,
            ["Sensor[1].MountingMode"] = motionType,
            ["Simulation.Mode"] = 1,
            ["Sensor[1].Type"] = 2,
            ["TorqueLimiting.PositionBasedMonitorings"] = 0,
            ["FollowingError.EnableMonitoring"] = 0,
            ["PositionControl.EnableDSC"] = 0
        };

        foreach (dynamic parameter in technologyObject.Parameters)
        {
            try
            {
                var name = Convert.ToString(parameter.GetAttribute("Name"));
                object value = null!;
                if (name != null && parameterValues.TryGetValue(name, out value))
                    parameter.Value = value;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Could not configure axis parameter: {exception.Message}");
            }
        }
    }

    private static object GetProperty(object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName)?.GetValue(target, null)
            ?? throw new InvalidOperationException($"TIA object has no property '{propertyName}'.");
    }

    private static bool HasProperty(object target, string propertyName) =>
        target != null && target.GetType().GetProperty(propertyName) != null;

    private static void ValidateImportPayload(TiaTransferPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.FilePath) || !File.Exists(payload.FilePath))
            throw new FileNotFoundException("Import file was not found.", payload.FilePath);
    }

    private static void ValidateExportPayload(TiaTransferPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.ItemName))
            throw new ArgumentException("An item name is required.", nameof(payload));
        if (string.IsNullOrWhiteSpace(payload.FilePath))
            throw new ArgumentException("An export path is required.", nameof(payload));
    }

    private static void ValidateFolderPayload(TiaFolderPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Name))
            throw new ArgumentException("A folder name is required.", nameof(payload));
    }

    private void DisposePortal()
    {
        try
        {
            if (_portal is IDisposable disposable)
                disposable.Dispose();
        }
        finally
        {
            _selectedPlcIndex = null;
            _project = null;
            _portal = null;
            _engineeringAssembly = null;
            _engineeringDllPath = null;
        }
    }
}
