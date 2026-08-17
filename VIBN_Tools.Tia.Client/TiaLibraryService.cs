using System.Xml.Linq;
using VIBN_Tools.Tia.Contracts;

namespace VIBN_Tools.Tia.Client;

public sealed record TiaLibraryProgress(int Completed, int Total, string Operation);

public interface ITiaLibraryService
{
    Task ImportAsync(
        string libraryPath,
        bool configureAxes,
        string tiaVersion,
        IProgress<TiaLibraryProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<string> ExportAsync(
        string libraryName,
        string destinationRoot,
        string tiaVersion,
        IProgress<TiaLibraryProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class TiaLibraryService : ITiaLibraryService
{
    private readonly ITiaBridgeClient _client;

    public TiaLibraryService(ITiaBridgeClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task ImportAsync(
        string libraryPath,
        bool configureAxes,
        string tiaVersion,
        IProgress<TiaLibraryProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(libraryPath))
            throw new DirectoryNotFoundException($"ViCo library was not found: {libraryPath}");

        var programRoot = Path.Combine(libraryPath, "_Programm");
        var dataTypeRoot = Path.Combine(libraryPath, "_Datatype");

        if (configureAxes)
        {
            var axes = await _client.ConfigureAxesAsync(cancellationToken);
            if (axes.Count > 0)
            {
                var axisFolder = ResolveAxisFolder(programRoot);
                Directory.CreateDirectory(axisFolder);
                TiaAxisXmlGenerator.CreateGlobalDataBlock(
                    axes.Select(axis => axis.Name),
                    Path.Combine(axisFolder, "AxisDB.xml"),
                    tiaVersion);
                TiaAxisXmlGenerator.CreateConnectionFunction(
                    axes.Select(axis => axis.Name),
                    Path.Combine(axisFolder, "AxisFC.xml"),
                    tiaVersion);
            }
        }

        var operations = CountXmlFiles(programRoot) + CountXmlFiles(dataTypeRoot);
        var completed = 0;

        completed = await ImportSectionAsync(
            programRoot,
            blocks: true,
            completed,
            operations,
            progress,
            cancellationToken);
        await ImportSectionAsync(
            dataTypeRoot,
            blocks: false,
            completed,
            operations,
            progress,
            cancellationToken);

        await _client.SaveAsync(cancellationToken);
    }

    public async Task<string> ExportAsync(
        string libraryName,
        string destinationRoot,
        string tiaVersion,
        IProgress<TiaLibraryProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(libraryName))
            throw new ArgumentException("A ViCo library folder name is required.", nameof(libraryName));
        if (string.IsNullOrWhiteSpace(destinationRoot))
            throw new ArgumentException("An export destination is required.", nameof(destinationRoot));

        var exportRoot = Path.Combine(destinationRoot, $"{SanitizeName(libraryName)}_{tiaVersion}");
        Directory.CreateDirectory(exportRoot);

        var blocks = await _client.ListProgramBlocksAsync(cancellationToken);
        var dataTypes = await _client.ListDataTypesAsync(cancellationToken);
        var blockRoot = FindLibraryRoot(blocks, libraryName);
        var dataTypeRoot = FindLibraryRoot(dataTypes, libraryName);
        var selectedBlocks = SelectLibraryItems(blocks, blockRoot).ToArray();
        var selectedDataTypes = SelectLibraryItems(dataTypes, dataTypeRoot).ToArray();
        var total = selectedBlocks.Length + selectedDataTypes.Length;
        var completed = 0;

        foreach (var item in selectedBlocks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var output = GetExportPath(exportRoot, "_Programm", item);
            await _client.ExportBlockAsync(item.FolderPath, item.Name, output, cancellationToken);
            progress?.Report(new TiaLibraryProgress(++completed, total, $"Export block {item.Name}"));
        }

        foreach (var item in selectedDataTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var output = GetExportPath(exportRoot, "_Datatype", item);
            await _client.ExportDataTypeAsync(item.FolderPath, item.Name, output, cancellationToken);
            progress?.Report(new TiaLibraryProgress(++completed, total, $"Export data type {item.Name}"));
        }

        return exportRoot;
    }

    private async Task<int> ImportSectionAsync(
        string sectionRoot,
        bool blocks,
        int completed,
        int total,
        IProgress<TiaLibraryProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(sectionRoot))
            return completed;

        var tree = blocks
            ? await _client.ListProgramBlocksAsync(cancellationToken)
            : await _client.ListDataTypesAsync(cancellationToken);
        var knownFolders = tree.Folders
            .Select(folder => NormalizeRemotePath(folder.Path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var folders = Directory.EnumerateDirectories(sectionRoot, "*", SearchOption.AllDirectories)
            .Prepend(sectionRoot)
            .Select(path => new
            {
                LocalPath = path,
                RemotePath = NormalizeRemotePath(Path.GetRelativePath(sectionRoot, path))
            })
            .Where(folder => folder.RemotePath.Length > 0)
            .OrderBy(folder => folder.RemotePath.Count(character => character == '/'))
            .ToArray();

        foreach (var folder in folders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (knownFolders.Contains(folder.RemotePath))
                continue;

            var parent = GetParentPath(folder.RemotePath);
            var name = GetName(folder.RemotePath);
            if (blocks)
                await _client.CreateBlockFolderAsync(parent, name, cancellationToken);
            else
                await _client.CreateDataTypeFolderAsync(parent, name, cancellationToken);
            knownFolders.Add(folder.RemotePath);
        }

        var files = Directory.EnumerateFiles(sectionRoot, "*.xml", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetFileName(path).Contains("IDB", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var folder = NormalizeRemotePath(Path.GetRelativePath(sectionRoot, Path.GetDirectoryName(file)!));
            if (blocks)
                await _client.ImportBlockAsync(folder, file, cancellationToken);
            else
                await _client.ImportDataTypeAsync(folder, file, cancellationToken);

            progress?.Report(new TiaLibraryProgress(++completed, total, $"Import {Path.GetFileName(file)}"));
        }

        return completed;
    }

    private static IEnumerable<TiaProgramItemInfo> SelectLibraryItems(TiaProjectTree tree, string rootPath) =>
        tree.Items.Where(item =>
            string.Equals(item.FolderPath, rootPath, StringComparison.OrdinalIgnoreCase) ||
            item.FolderPath.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase));

    private static string FindLibraryRoot(TiaProjectTree tree, string libraryName)
    {
        return tree.Folders
            .Where(folder => string.Equals(folder.Name, libraryName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(folder => folder.Path.Count(character => character == '/'))
            .Select(folder => folder.Path)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"TIA library folder was not found: {libraryName}");
    }

    private static string GetExportPath(string root, string section, TiaProgramItemInfo item)
    {
        var folder = Path.Combine(
            root,
            section,
            item.FolderPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, $"{SanitizeName(item.Name)}.xml");
    }

    private static string ResolveAxisFolder(string programRoot)
    {
        Directory.CreateDirectory(programRoot);
        var libraryRoot = Directory.EnumerateDirectories(programRoot).FirstOrDefault();
        return Path.Combine(libraryRoot ?? programRoot, "Axis");
    }

    private static int CountXmlFiles(string path) =>
        Directory.Exists(path)
            ? Directory.EnumerateFiles(path, "*.xml", SearchOption.AllDirectories).Count()
            : 0;

    private static string NormalizeRemotePath(string path) =>
        path == "."
            ? string.Empty
            : path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/').Trim('/');

    private static string GetParentPath(string path)
    {
        var separator = path.LastIndexOf('/');
        return separator < 0 ? string.Empty : path[..separator];
    }

    private static string GetName(string path)
    {
        var separator = path.LastIndexOf('/');
        return separator < 0 ? path : path[(separator + 1)..];
    }

    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}

internal static class TiaAxisXmlGenerator
{
    public static void CreateGlobalDataBlock(IEnumerable<string> axes, string outputPath, string tiaVersion)
    {
        XNamespace interfaceNamespace = "http://www.siemens.com/automation/Openness/SW/Interface/v5";
        var section = new XElement(interfaceNamespace + "Section", new XAttribute("Name", "Static"));
        foreach (var axis in axes)
        {
            section.Add(new XElement(
                interfaceNamespace + "Member",
                new XAttribute("Name", axis),
                new XAttribute("Datatype", "Real"),
                new XElement(interfaceNamespace + "AttributeList",
                    BooleanAttribute(interfaceNamespace, "ExternalAccessible"),
                    BooleanAttribute(interfaceNamespace, "ExternalVisible"),
                    BooleanAttribute(interfaceNamespace, "ExternalWritable"))));
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Document",
                new XElement("Engineering", new XAttribute("version", tiaVersion)),
                CreateDocumentInfo(tiaVersion),
                new XElement("SW.Blocks.GlobalDB",
                    new XAttribute("ID", "0"),
                    new XElement("AttributeList",
                        new XElement("AutoNumber", "false"),
                        new XElement("Interface", new XElement(interfaceNamespace + "Sections", section)),
                        new XElement("MemoryLayout", "Standard"),
                        new XElement("Name", "viCo_Axes_DB"),
                        new XElement("Number", "7957"),
                        new XElement("ProgrammingLanguage", "DB")),
                    new XElement("ObjectList"))));

        Save(document, outputPath);
    }

    public static void CreateConnectionFunction(IEnumerable<string> axes, string outputPath, string tiaVersion)
    {
        XNamespace structuredText = "http://www.siemens.com/automation/Openness/SW/NetworkSource/StructuredText/v3";
        var source = new XElement(structuredText + "StructuredText");
        var uid = 21;
        foreach (var axis in axes)
        {
            source.Add(
                Access(structuredText, uid, "viCo_Axes_DB", axis),
                new XElement(structuredText + "Blank", new XAttribute("UId", uid + 5)),
                new XElement(structuredText + "Token", new XAttribute("Text", ":="), new XAttribute("UId", uid + 6)),
                new XElement(structuredText + "Blank", new XAttribute("UId", uid + 7)),
                Access(structuredText, uid + 8, axis, "ActualPosition"),
                new XElement(structuredText + "Token", new XAttribute("Text", ";"), new XAttribute("UId", uid + 13)),
                new XElement(structuredText + "NewLine", new XAttribute("UId", uid + 14)));
            uid += 15;
        }

        XNamespace interfaceNamespace = "http://www.siemens.com/automation/Openness/SW/Interface/v5";
        var sections = new XElement(interfaceNamespace + "Sections",
            new XElement(interfaceNamespace + "Section", new XAttribute("Name", "Input")),
            new XElement(interfaceNamespace + "Section", new XAttribute("Name", "Output")),
            new XElement(interfaceNamespace + "Section", new XAttribute("Name", "InOut")),
            new XElement(interfaceNamespace + "Section", new XAttribute("Name", "Temp")),
            new XElement(interfaceNamespace + "Section", new XAttribute("Name", "Constant")),
            new XElement(interfaceNamespace + "Section", new XAttribute("Name", "Return"),
                new XElement(interfaceNamespace + "Member",
                    new XAttribute("Name", "Ret_Val"),
                    new XAttribute("Datatype", "Void"))));

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Document",
                new XElement("Engineering", new XAttribute("version", tiaVersion)),
                CreateDocumentInfo(tiaVersion),
                new XElement("SW.Blocks.FC",
                    new XAttribute("ID", "0"),
                    new XElement("AttributeList",
                        new XElement("AutoNumber", "false"),
                        new XElement("Interface", sections),
                        new XElement("MemoryLayout", "Optimized"),
                        new XElement("Name", "viCo_AxisConnect_FC"),
                        new XElement("Number", "7980"),
                        new XElement("ProgrammingLanguage", "SCL"),
                        new XElement("SetENOAutomatically", "false")),
                    new XElement("ObjectList",
                        new XElement("SW.Blocks.CompileUnit",
                            new XAttribute("ID", "6"),
                            new XAttribute("CompositionName", "CompileUnits"),
                            new XElement("AttributeList",
                                new XElement("NetworkSource", source),
                                new XElement("ProgrammingLanguage", "SCL")),
                            new XElement("ObjectList"))))));

        Save(document, outputPath);
    }

    private static XElement BooleanAttribute(XNamespace ns, string name) =>
        new(ns + "BooleanAttribute",
            new XAttribute("Name", name),
            new XAttribute("SystemDefined", "true"),
            "false");

    private static XElement Access(XNamespace ns, int uid, string first, string second) =>
        new(ns + "Access",
            new XAttribute("Scope", "GlobalVariable"),
            new XAttribute("UId", uid),
            new XElement(ns + "Symbol",
                new XAttribute("UId", uid + 1),
                new XElement(ns + "Component", new XAttribute("Name", first), new XAttribute("UId", uid + 2)),
                new XElement(ns + "Token", new XAttribute("Text", "."), new XAttribute("UId", uid + 3)),
                new XElement(ns + "Component", new XAttribute("Name", second), new XAttribute("UId", uid + 4))));

    private static XElement CreateDocumentInfo(string version) =>
        new("DocumentInfo",
            new XElement("Created", DateTime.UtcNow.ToString("O")),
            new XElement("ExportSetting", "None"),
            new XElement("InstalledProducts",
                new XElement("Product",
                    new XElement("DisplayName", "Totally Integrated Automation Portal"),
                    new XElement("DisplayVersion", version))));

    private static void Save(XDocument document, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        document.Save(outputPath, SaveOptions.DisableFormatting);
    }
}
