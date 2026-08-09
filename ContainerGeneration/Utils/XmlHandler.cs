using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;

namespace VIBN_Tools.ContainerGeneration.Utils
{
    /// <summary>
    /// Static class containing read and write methods for XML files relevant to the application.
    /// </summary>
    public static class XmlHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Writes the container data to an XML file.
        /// This method creates a directory if it does not exist, serializes the container data to XML,
        /// validates the XML against a schema, and logs the success or failure of the operation.
        /// </summary>
        /// <param name="containerList">The list of component containers to write.</param>
        /// <param name="filePath">The file path where the XML file will be created.</param>
        /// <param name="autoCrateFileName">The name of the auto-create file (optional).</param>
        /// <param name="zuliFileName">The name of the Zuli file (optional).</param>
        public static Result<string> WriteContainerXml(
            List<ComponentContainer> containerList,
            string filePath,
            string? autoCrateFileName = null,
            string? zuliFileName = null)
        {
            string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Version? version = Assembly.GetExecutingAssembly().GetName().Version;

            CAAMergeResult cAAMergeResult = new()
            {
                Version = version == null ? string.Empty : version.ToString(),
                CreatedAt = currentDateTime,
                AutoCreateFile = autoCrateFileName ?? string.Empty,
                Zuli = zuliFileName ?? string.Empty,
                ContainerList = containerList
            };

            var dirName = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
            {
                Logger.Debug("Create directory {path}", filePath);
                Directory.CreateDirectory(dirName);
            }

            var resultSerialize = SerializeToXml(cAAMergeResult, filePath);
            if (resultSerialize.IsSuccess)
            {
                var resultRead = Read(resultSerialize.Value);
                if (resultRead.IsSuccess)
                {
                    using Stream stream = ResourceHandler.GetEmbeddedResourceStream(ResourceHandler.CAARESULT_SCHEMA);
                    if (!Validate(resultRead.Value, stream))
                        resultSerialize.SetFailed("Container XML schema validation failed");
                    else
                        Logger.Info("Container XML at {path} created successfully", filePath);
                }
                else
                {
                    resultSerialize.SetFailed($"Error while reading the container xml {resultSerialize.Value}");
                }
            }
            return resultSerialize;
        }

        /// <summary>
        /// Serializes the specified object to an XML file.
        /// This method removes default XML namespaces, serializes the object to the specified file path, and returns the result of the operation.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="root">The object to serialize.</param>
        /// <param name="filePath">The file path where the XML file will be created.</param>
        /// <returns>A <see cref="Result{T}"/> indicating the success or failure of the operation.</returns>
        public static Result<string> SerializeToXml<T>(T root, string filePath)
        {
            XmlSerializer serializer = new(typeof(T));
            // Remove the default namespaces xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"  and xmlns:xsd="http://www.w3.org/2001/XMLSchema"
            XmlSerializerNamespaces namespaces = new();
            namespaces.Add(string.Empty, string.Empty);
            try
            {
                using (StreamWriter writer = new(filePath))
                {
                    serializer.Serialize(writer, root, namespaces);
                }
                string fullPath = Path.GetFullPath(filePath);
                Logger.Debug("Successfully created XML file at {path}", filePath);
                return Result<string>.Success(fullPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while writing file {path}", filePath);
                return Result<string>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Reads an XML document from the specified file path.
        /// This method reads the XML file, logs the success or failure, and returns the result.
        /// </summary>
        /// <param name="xmlPath">The path to the XML file.</param>
        /// <returns>A <see cref="Result{T}"/> containing the XML document or an error message.</returns>
        public static Result<XDocument> Read(string xmlPath)
        {
            XDocument doc = new();

            try
            {
                using var stream = new FileStream(
                    xmlPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);
                doc = XDocument.Load(stream, LoadOptions.SetLineInfo);

                Logger.Info("XML document at {path} read successfully", xmlPath);
                return Result<XDocument>.Success(doc);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while reading {path}", xmlPath);
                return Result<XDocument>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Writes the specified XML document to a file.
        /// This method saves the XML document to the specified file path, logs the success or failure, and returns the result.
        /// </summary>
        /// <param name="doc">The XML document to write.</param>
        /// <param name="filePath">The file path where the XML document will be saved.</param>
        /// <returns>A <see cref="Result{T}"/> indicating the success or failure of the operation.</returns>
        public static Result<string> Write(XDocument doc, string filePath)
        {
            try
            {
                string fullPath = Path.GetFullPath(filePath);
                doc.Save(fullPath);
                Logger.Info("Wrote XML document to {path} successfully", fullPath);
                return Result<string>.Success(fullPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while writing {path}", filePath);
                return Result<string>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Validates the specified XML document against the provided schema.
        /// This method checks the XML document for validity against the schema and logs any validation errors.
        /// </summary>
        /// <param name="xml">The XML document to validate.</param>
        /// <param name="schema">The schema to validate against.</param>
        /// <returns><c>true</c> if the XML document is valid; otherwise, <c>false</c>.</returns>
        public static bool Validate(XDocument xml, Stream schema)
        {
            XmlSchemaSet schemaSet = new();
            using (schema)
            {
                using XmlReader schemaReader = XmlReader.Create(schema);
                schemaSet.Add(null, schemaReader);
            }

            var isValid = true;
            xml.Validate(schemaSet, (sender, e) =>
            {
                isValid = false;
                string location = "N/A";
                if (sender is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
                    location = $"Line: {lineInfo.LineNumber}, Position: {lineInfo.LinePosition}";
                Logger.Warn("Validation failed: {severity} - Location: {location} - {message}", e.Severity, location, e.Message);
            });

            return isValid;
        }


    }
}
