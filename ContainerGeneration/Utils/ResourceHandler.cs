using System.IO;
using System.Reflection;

namespace VIBN_Tools.ContainerGeneration.Utils
{
    /// <summary>
    /// Class to manage embedded application resources.
    /// </summary>
    public static class ResourceHandler
    {
        /// <summary>
        /// Namespace for the AutoCreate XML schema definition.
        /// </summary>
        public const string AUTOCREATE_SCHEMA = "ContainerGeneration.XmlSchemas.AutoCreate.xsd";


        /// <summary>
        /// Namespace for the CAAResult XML schema definition.
        /// </summary>
        public const string CAARESULT_SCHEMA = "ContainerGeneration.XmlSchemas.CAAResult.xsd";

        /// <summary>
        /// Retrieves an embedded resource stream from the executing assembly.
        /// This method gets the manifest resource stream for the specified resource name
        /// and throws a <see cref="FileNotFoundException"/> if the resource is not found.
        /// </summary>
        /// <param name="resourceName">The name of the embedded resource.</param>
        /// <returns>A <see cref="Stream"/> of the embedded resource.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the resource is not found.</exception>
        public static Stream GetEmbeddedResourceStream(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{resourceName}");
            return resourceStream ?? throw new FileNotFoundException("Resource not found: " + resourceName);
        }

    }
}
