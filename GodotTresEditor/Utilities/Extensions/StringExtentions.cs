using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GodotTresEditor.Utilities.Extensions
{
    public class StringExtentions
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

    }
}
