using GodotTresEditor.Core.Models;
using GodotTresEditor.Utilities.Extensions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GodotTresEditor.Core;

internal static class TresParser
{
    private const string ResourceToken = "[resource]";
    private static readonly Regex TypeRegex = new(@"type=""(?<Type>[^""]+)""", RegexOptions.Compiled);
    private static readonly Regex FormatRegex = new(@"format=(?<Format>\d+)", RegexOptions.Compiled);
    private static readonly Regex ScriptClassRegex = new(@"script_class=""(?<Class>[^""]+)""", RegexOptions.Compiled);
    private static readonly Regex AttributesRegex = new(@"(?<Key>[a-zA-Z0-9_]+)=""(?<Value>[^""]*)""", RegexOptions.Compiled);
    private static readonly Regex ScriptUsageRegex = new(@"^script\s*=\s*ExtResource\(\s*""?(?<Id>[^""\s)]+)""?\s*\)", RegexOptions.Compiled);

    public static TresData Parse(string tresPath)
    {
        var result = new TresData();
        var scriptPaths = new Dictionary<string, string>();
        bool resourceSectionFound = false;

        string[] lines = File.ReadAllLines(tresPath);
        int i = 0;

        while (i < lines.Length)
        {
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            if (result.BaseType == null && line.StartsWith("[gd_resource"))
            {
                var typeMatch = TypeRegex.Match(line);
                var formatMatch = FormatRegex.Match(line);
                if (typeMatch.Success && formatMatch.Success)
                {
                    result.BaseType = typeMatch.Groups["Type"].Value;
                    result.Format = int.Parse(formatMatch.Groups["Format"].Value);
                }

                var classMatch = ScriptClassRegex.Match(line);
                if (classMatch.Success)
                {
                    result.ScriptClass = classMatch.Groups["Class"].Value;
                }

                i++;
                continue;
            }

            if (line.Trim() == ResourceToken)
            {
                resourceSectionFound = true;
                i++;
                continue;
            }

            if (!resourceSectionFound)
            {
                if (line.StartsWith("[ext_resource"))
                {
                    var attributes = new Dictionary<string, string>();
                    var matches = AttributesRegex.Matches(line);

                    foreach (Match match in matches)
                    {
                        string key = match.Groups["Key"].Value;
                        string value = match.Groups["Value"].Value;
                        attributes[key] = value;
                    }

                    if (attributes.TryGetValue("id", out var id))
                    {
                        var extResource = new ExtResourceData
                        {
                            Id = id,
                            Type = attributes.GetValueOrDefault("type", string.Empty),
                            Path = attributes.GetValueOrDefault("path", string.Empty),
                            Attributes = attributes
                        };

                        result.ExtResources.Add(extResource);

                        if (extResource.Type == "Script")
                        {
                            scriptPaths[id] = extResource.Path;
                        }
                    }
                }
                i++;
                continue;
            }

            if (resourceSectionFound)
            {
                var scriptMatch = ScriptUsageRegex.Match(line);
                if (scriptMatch.Success)
                {
                    var id = scriptMatch.Groups["Id"].Value;
                    if (scriptPaths.TryGetValue(id, out var path))
                    {
                        result.ScriptPath = path;
                    }
                    i++;
                    continue;
                }

                int equalIndex = line.IndexOf('=');
                if (equalIndex > 0)
                {
                    string key = line.Substring(0, equalIndex).Trim();
                    string rawValue = line.Substring(equalIndex + 1).Trim();

                    if (rawValue.StartsWith("\"") && !StringExtentions.IsStringClosed(rawValue))
                    {
                        var sb = new StringBuilder(rawValue);
                        i++;
                        while (i < lines.Length)
                        {
                            sb.Append("\n").Append(lines[i]);
                            if (StringExtentions.IsStringClosed(lines[i]))
                            {
                                break;
                            }
                            i++;
                        }
                        rawValue = sb.ToString();
                    }

                    object parsedValue = ParseValue(rawValue, result.Format);
                    result.Properties[key] = parsedValue;
                }
            }

            i++;
        }

        return result;
    }

    private static object ParseValue(string value, int version)
    {
        if (value.StartsWith("\"") && value.EndsWith("\""))
        {
            return value.UnescapeString();
        }

        if (value.StartsWith("PackedInt32Array("))
        {
            return ParseInt32Array(value);
        }

        if (value.StartsWith("PackedByteArray("))
        {
            return ParseByteArray(value, version);
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intVal))
        {
            return intVal;
        }

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatVal))
        {
            return floatVal;
        }

        return value;
    }

    private static int[] ParseInt32Array(string raw)
    {
        int start = raw.IndexOf('(') + 1;
        int end = raw.LastIndexOf(')');

        if (start <= 0 || end <= start)
            return Array.Empty<int>();

        var content = raw.Substring(start, end - start);

        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<int>();

        return content.Split(',')
                      .Select(s => int.Parse(s.Trim(), CultureInfo.InvariantCulture))
                      .ToArray();
    }

    private static byte[] ParseByteArray(string raw, int version)
    {
        int start = raw.IndexOf('(') + 1;
        int end = raw.LastIndexOf(')');

        if (start <= 0 || end <= start)
            return Array.Empty<byte>();

        var content = raw.Substring(start, end - start);
        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<byte>();

        if (content.StartsWith("\"") && content.EndsWith("\""))
        {
            content = content.Trim('"');
        }

        if (version == 4)
        {
            return Convert.FromBase64String(content);
        }
        else
        {
            return content.Split(',')
                          .Select(s => byte.Parse(s.Trim(), CultureInfo.InvariantCulture))
                          .ToArray();
        }
    }
}