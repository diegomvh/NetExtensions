using System.Collections.Generic;
using System.Text;

namespace Stj.OpenXml.Extensions
{
    public static class StringExtensions
    {
        public static string StringConcatenate(this IEnumerable<string> source)
        {
            var sb = new StringBuilder();
            foreach (var s in source)
                sb.Append(s);
            return sb.ToString();
        }

        public static string StringConcatenate(this IEnumerable<string> source, string separator)
        {
            var sb = new StringBuilder();
            foreach (var s in source)
                sb.Append(s).Append(separator);
            return sb.ToString();
        }
    }
}
