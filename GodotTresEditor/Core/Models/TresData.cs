namespace GodotTresEditor.Core.Models
{
    public class TresData
    {
        public string BaseType { get; set; }

        public int Format { get; set; }
        public string ScriptType { get; set; }
        public Dictionary<string, object> Properties { get; } = new();

        public T GetProperty<T>(string key)
        {
            if (Properties.TryGetValue(key, out var value) && value is T castValue)
            {
                return castValue;
            }
            return default;
        }
    }
}
