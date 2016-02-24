using System;
using System.IO;

namespace Stj.Utilities
{
    public class Files
    {
        public static string MakeTempFile(string dirname="", string name="", string ext="")
        {
            if (String.IsNullOrWhiteSpace(dirname))
                dirname = Path.GetTempPath();
            if (String.IsNullOrWhiteSpace(name))
                name = Guid.NewGuid().ToString();
            if (!String.IsNullOrWhiteSpace(ext))
                ext = "." + ext.Trim();
            return Path.Combine(dirname, name + ext);
        }

        public static string TryMakeTempFile(string name, string dirname = "", string ext = "")
        {
            if (String.IsNullOrWhiteSpace(dirname))
                dirname = Path.GetTempPath();
            if (!String.IsNullOrWhiteSpace(ext))
                ext = "." + ext.Trim();
            var path = Path.Combine(dirname, name + ext);
            while (File.Exists(path))
                path = Path.Combine(dirname, name + 
                    "_" +
                    Guid.NewGuid().ToString().Substring(0, 8) + 
                    ext);
            return path;
        }
    }
}
