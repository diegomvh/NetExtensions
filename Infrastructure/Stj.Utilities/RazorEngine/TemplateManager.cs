using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.IO;

namespace Stj.Utilities.RazorEngine
{
    public class TemplateManager : ITemplateManager
    {
        readonly Dictionary<string, string> Files;

        public TemplateManager(Dictionary<string, string> files) {
            this.Files = files;
        }

        public void AddDynamic(ITemplateKey key, ITemplateSource source)
        {
            throw new NotImplementedException("dynamic templates are not supported!");
        }

        public ITemplateKey GetKey(string name, ResolveType resolveType, ITemplateKey context)
        {
            return new NameOnlyTemplateKey(name, resolveType, context);
        }

        public ITemplateSource Resolve(ITemplateKey key)
        {
            var file_path = this.Files[key.Name];
            return new LoadedTemplateSource(File.ReadAllText(file_path), file_path);
        }
    }
}