using System.IO;
using System.Xml.Serialization;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;

namespace VIBN_Tools.ContainerGeneration.Models
{
    public class SavedData
    {
        public List<ContainerEntry> FilteredEntries { get; set; } = new List<ContainerEntry>();
        public List<ContainerEntry> UnassignedEntries { get; set; } = new List<ContainerEntry>();
        public List<ContainerData> ContainerList { get; set; } = new List<ContainerData>();
        public List<SavedEntryState> EntryStates { get; set; } = new List<SavedEntryState>();
        public List<WorkspaceActivityLogEntry> ActivityLog { get; set; } =
            new List<WorkspaceActivityLogEntry>();

        [XmlIgnore]
        public string FilePath = "";

        public static SavedData DeserializeProject(string filename)
        {
            // Create an instance of the XmlSerializer class;
            // specify the type of object to be deserialized.
            XmlSerializer serializer = new XmlSerializer(typeof(SavedData));
            /* If the XML document has been altered with unknown 
            nodes or attributes, handle them with the 
            UnknownNode and UnknownAttribute events.*/
            serializer.UnknownNode += new
            XmlNodeEventHandler(Serializer_UnknownNode);
            serializer.UnknownAttribute += new
            XmlAttributeEventHandler(Serializer_UnknownAttribute);

            // A FileStream is needed to read the XML document.
            using FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            /* Use the Deserialize method to restore the object's state with
            data from the XML document. */
            SavedData Project = serializer.Deserialize(fs) as SavedData
                ?? throw new InvalidDataException("The selected file does not contain a valid VIBN Tools workspace.");

            Project.FilePath = filename;
            Project.ApplyEntryStates();

            return Project;
        }

        private static void Serializer_UnknownNode(object? sender, XmlNodeEventArgs e)
        {
            Console.WriteLine("Unknown Node:" + e.Name + "\t" + e.Text);
        }

        private static void Serializer_UnknownAttribute
        (object? sender, XmlAttributeEventArgs e)
        {
            System.Xml.XmlAttribute attr = e.Attr;
            Console.WriteLine("Unknown attribute " +
            attr.Name + "='" + attr.Value + "'");
        }


        public void SetSettings()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
                throw new InvalidOperationException("No save path was selected.");

            XmlSerializer serializer = new XmlSerializer(typeof(SavedData));
            using TextWriter writer = new StreamWriter(FilePath);
            serializer.Serialize(writer, this);
        }

        public void CaptureEntryStates()
        {
            EntryStates = EnumerateEntries()
                .Select(entry => new SavedEntryState
                {
                    SignalId = entry.EnsureSignalId(),
                    PrimaryKey = GenerationWorkspaceReconciler.CreatePrimaryKey(entry),
                    SourceFingerprint = GenerationWorkspaceReconciler.CreateSourceFingerprint(entry),
                    IsManuallyEdited = entry.IsManuallyEdited,
                    ReviewState = entry.ReviewState,
                    ReviewMessage = entry.ReviewMessage
                })
                .ToList();
        }

        private void ApplyEntryStates()
        {
            var states = EntryStates
                .GroupBy(state => (state.PrimaryKey, state.SourceFingerprint))
                .ToDictionary(
                    group => group.Key,
                    group => new Queue<SavedEntryState>(group));

            foreach (var entry in EnumerateEntries())
            {
                var key = (
                    GenerationWorkspaceReconciler.CreatePrimaryKey(entry),
                    GenerationWorkspaceReconciler.CreateSourceFingerprint(entry));

                if (!states.TryGetValue(key, out var candidates) || candidates.Count == 0)
                    continue;

                var state = candidates.Dequeue();
                entry.SignalId = state.SignalId;
                entry.IsManuallyEdited = state.IsManuallyEdited;
                entry.ReviewState = state.ReviewState;
                entry.ReviewMessage = state.ReviewMessage;
            }

            foreach (var entry in EnumerateEntries())
                entry.EnsureSignalId();

            foreach (var container in ContainerList)
                container.RefreshReimportStatus();
        }

        private IEnumerable<ContainerEntry> EnumerateEntries() =>
            ContainerList.SelectMany(container => container.DataList)
                .Concat(UnassignedEntries)
                .Concat(FilteredEntries);
    }

    public class SavedEntryState
    {
        public string SignalId { get; set; } = string.Empty;
        public string PrimaryKey { get; set; } = string.Empty;
        public string SourceFingerprint { get; set; } = string.Empty;
        public bool IsManuallyEdited { get; set; }
        public ContainerEntryReviewState ReviewState { get; set; }
        public string ReviewMessage { get; set; } = string.Empty;
    }
}
