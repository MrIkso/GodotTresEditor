using System.Text;

namespace GodotTresEditor.Utilities.Extensions;

public static class StringExtentions
{

    public static string ConvertNewlinesToMarkers(string text)
    {
        return string.IsNullOrEmpty(text) ? text :
            text.Replace("\r\n", "\\r\\n").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }

    public static string ConvertMarkersToNewlines(string text)
    {
        return string.IsNullOrEmpty(text) ? text :
            text.Replace("\\r\\n", "\r\n").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
    }


    public static string EscapeString(this String s)
    {
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': 
                    sb.Append(@"\\");
                    break;
                case '"':
                    sb.Append(@"\""");
                    break;
                case '\n': 
                    sb.Append(@"\n");
                    break;
                case '\r': 
                    sb.Append(@"\r");
                    break;
                case '\t': 
                    sb.Append(@"\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    public static string UnescapeString(this String s)
    {
        if (s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
        {
            s = s.Substring(1, s.Length - 2);
        }

        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                char next = s[i + 1];
                switch (next)
                {
                    case 'n': 
                        sb.Append('\n');
                        break;
                    case 'r':
                        sb.Append('\r');
                        break;
                    case 't':
                        sb.Append('\t');
                        break;
                    case '"': 
                        sb.Append('"');
                        break;
                    case '\\':
                        sb.Append('\\');
                        break;
                    default: 
                        sb.Append(next);
                        break;
                }
                i++;
            }
            else
            {
                sb.Append(s[i]);
            }
        }
        return sb.ToString();
    }

    public static bool IsStringClosed(string s)
    {
        s = s.Trim();
        if (!s.EndsWith("\"") || s.Length < 2)
            return false;

        int backslashCount = 0;
        for (int j = s.Length - 2; j >= 0; j--)
        {
            if (s[j] == '\\')
                backslashCount++;
            else
                break;
        }

        return backslashCount % 2 == 0;
    }
}
