using GodotTresEditor.Core.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GodotTresEditor.Core;

internal static class TresParser
{
    private const string ResourceToken = "[resource]";
    private const string BaseTypeRegexStr = @"^\[gd_resource type=""(?<BaseType>.+?)"".+?format=(?<Format>.+?)";
    private const string ScriptPathRegexStr = @"^\[ext_resource type=""Script"".+?path=""(?<Path>.+?)"".+?id=""(?<Id>.+?)""";
    private const string ScriptUsageRegexStr = @"^script = ExtResource\(""(?<Id>.+?)""\)";

    private static readonly Regex BaseTypeRegex = new(BaseTypeRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    private static readonly Regex ScriptPathRegex = new(ScriptPathRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    private static readonly Regex ScriptUsageRegex = new(ScriptUsageRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    public static TresData Parse(string tresPath)
    {
        var result = new TresData();

        Dictionary<string, string> scriptPaths = new();
        bool resourceSectionFound = false;

        foreach (var line in File.ReadLines(tresPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (result.BaseType == null)
            {
                var match = BaseTypeRegex.Match(line);
                if (match.Success)
                {
                    result.BaseType = match.Groups["BaseType"].Value;
                    result.Format = int.Parse(match.Groups["Format"].Value);
                    continue;
                }
            }

            if (line.Trim() == ResourceToken)
            {
                resourceSectionFound = true;
                continue;
            }

            if (!resourceSectionFound)
            {
                var match = ScriptPathRegex.Match(line);
                if (match.Success)
                {
                    var path = match.Groups["Path"].Value;
                    var id = match.Groups["Id"].Value;
                    if (path.EndsWith(".cs"))
                    {
                        scriptPaths[id] = path;
                    }
                }
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
                        result.ScriptType = path;
                        //MiniCsScraper.GetType(compilation, path);
                    }
                    continue;
                }

                int equalIndex = line.IndexOf('=');
                if (equalIndex > 0)
                {
                    string key = line.Substring(0, equalIndex).Trim();
                    string rawValue = line.Substring(equalIndex + 1).Trim();

                    object parsedValue = ParseValue(rawValue);
                    result.Properties[key] = parsedValue;
                }
            }
        }

        return result;
    }

    private static object ParseValue(string value)
    {
        if (value.StartsWith("\"") && value.EndsWith("\""))
        {
            return value.Trim('"');
        }

        if (value.StartsWith("PackedInt32Array("))
        {
            return ParseInt32Array(value);
        }

        if (value.StartsWith("PackedByteArray("))
        {
            return ParseByteArray(value);
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

    private static byte[] ParseByteArray(string raw)
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

        if (content.EndsWith("=="))
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