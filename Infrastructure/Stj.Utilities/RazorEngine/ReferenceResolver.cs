using RazorEngine.Compilation.ReferenceResolver;
using System.Collections.Generic;
using RazorEngine.Compilation;
using System.Reflection;
using System.Configuration;
using System.Linq;
using System.Collections.Specialized;
using System.IO;

namespace Stj.Utilities.RazorEngine
{
    public class ReferenceResolver : IReferenceResolver
    {
        private static string RazzorAssemblies = "RazorAssemblies";

        public IEnumerable<CompilerReference> GetReferences(TypeContext context, IEnumerable<CompilerReference> includeAssemblies)
        {
            IEnumerable<string> loadedAssemblies = (new UseCurrentAssembliesReferenceResolver())
                .GetReferences(context, includeAssemblies)
                .Select(r => r.GetFile())
                .ToArray();

            var section = ConfigurationManager.GetSection(RazzorAssemblies) as NameValueCollection;
            var directory = System.AppDomain.CurrentDomain.BaseDirectory;

            /* yield return CompilerReference.From(FindLoaded(loadedAssemblies, "mscorlib.dll"));
            yield return CompilerReference.From(FindLoaded(loadedAssemblies, "System.dll"));
            yield return CompilerReference.From(FindLoaded(loadedAssemblies, "System.Core.dll"));
            yield return CompilerReference.From(FindLoaded(loadedAssemblies, "System.Web.Mvc.dll"));
            yield return CompilerReference.From(FindLoaded(loadedAssemblies, "System.Web.dll"));
            yield return CompilerReference.From(FindLoaded(loadedAssemblies, "Microsoft.CSharp.dll"));
            */
            foreach (var key in section.AllKeys)
            {
                string assemblyLoaded = FindLoaded(loadedAssemblies, section[key]);
                if (string.IsNullOrEmpty(assemblyLoaded))
                {
                    var assemblyFile = section[key];
                    if (assemblyFile.StartsWith("~/"))
                        assemblyFile = Path.GetFullPath(Path.Combine(directory, assemblyFile.Substring(2)));
                    yield return CompilerReference.From(Assembly.LoadFrom(assemblyFile));
                }
                else
                {
                    yield return CompilerReference.From(assemblyLoaded);
                }
            }
        }

        public string FindLoaded(IEnumerable<string> refs, string find)
        {
            return refs.FirstOrDefault(r => r.EndsWith(Path.DirectorySeparatorChar + find));
        }
    }
}