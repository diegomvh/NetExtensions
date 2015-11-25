using RazorEngine.Templating;
using System;

namespace Stj.Email.RazorEngine
{
    public class TemplateManager : ITemplateManager
    {
        public void AddDynamic(ITemplateKey key, ITemplateSource source)
        {
            throw new NotImplementedException();
        }

        public ITemplateKey GetKey(string name, ResolveType resolveType, ITemplateKey context)
        {
            throw new NotImplementedException();
        }

        public ITemplateSource Resolve(ITemplateKey key)
        {
            throw new NotImplementedException();
        }
    }
}