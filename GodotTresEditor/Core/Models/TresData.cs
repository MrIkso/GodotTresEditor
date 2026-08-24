namespace GodotTresEditor.Core.Models
{
    public class ExtResourceData
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public Dictionary<string, string> Attributes { get; set; } = new();
    }

    public class TresData
    {
        public string? BaseType { get; set; }
        public string? ScriptClass { get; set; }
        public int Format { get; set; }
        public string? ScriptPath { get; set; }

        public List<ExtResourceData> ExtResources { get; set; } = new();

        public Dictionary<string, object> Properties { get; set; } = new();

        public T? GetProperty<T>(string key)
        {
            if (Properties.TryGetValue(key, out var val) && val is T typedVal)
                return typedVal;
            return default;
        }
    }
}
