namespace VIBN_Tools.ContainerGeneration.AI
{
    /// <summary>
    /// Vereinheitlicht Schreibweisen der ComponentType-Felder.
    /// Beispiel: "Zylinder", "ZYL", "Cylinder", "Cyl" → "zylinder"
    /// </summary>
    public sealed class ComponentTypeNormalizer
    {
        private readonly Dictionary<string, string> _map =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "zylinder", "zylinder" },
                { "zyl", "zylinder" },
                { "cyl", "zylinder" },
                { "cylinder", "zylinder" },

                { "sensor", "sensor" },
                { "sns", "sensor" },

                { "ventil", "ventil" },
                { "valve", "ventil" },

                { "greifer", "greifer" },
                { "gripper", "greifer" }
            };

        public string Normalize(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return "unknown";

            return _map.TryGetValue(type.Trim(), out var norm)
                ? norm
                : type.ToLowerInvariant();
        }
    }
}