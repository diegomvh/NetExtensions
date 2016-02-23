using System.IO;
using System.Linq;
using System.Web.Mvc;
using RazorEngine.Configuration;
using System.Collections.Generic;
using RazorEngine.Templating;

namespace Stj.Utilities.RazorEngine
{
    /// <summary>
    /// A view that uses the Razor engine to render a templates loaded directly from the
    /// file system. This means it will work outside of ASP.NET.
    /// </summary>
    public class FileSystemRazorView : IView
    {
        readonly string Name;
        readonly string ApplicationPathRoot;
        readonly Dictionary<string, string> Templates;

        /// <summary>
        /// Creates a new <see cref="FileSystemRazorView"/> using the given view filename.
        /// </summary>
        /// <param name="name">The filename of the view.</param>
        public FileSystemRazorView(string name, string applicationPathRoot, Dictionary<string, string> templates)
        {
            this.Name = name;
            this.Templates = templates;
            this.ApplicationPathRoot = applicationPathRoot;
        }

        /// <summary>
        /// Renders the view into the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="viewContext">The <see cref="ViewContext"/> that contains the view data model.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the rendered output.</param>
        public void Render(ViewContext viewContext, TextWriter writer)
        {
            if (!viewContext.ViewData.ContainsKey("ApplicationPathRoot"))
                viewContext.ViewData.Add("ApplicationPathRoot", this.ApplicationPathRoot);
            using (var service = RazorEngineService.Create(this.GetTemplateServiceConfiguration()))
            {
                service.RunCompile(service.GetKey(this.Name), writer, null, viewContext.ViewData.Model, null);
                writer.Flush();
            }
        }

        private TemplateServiceConfiguration GetTemplateServiceConfiguration()
        {
            var config = new TemplateServiceConfiguration()
            {
                BaseTemplateType = typeof(RazorEngineTemplateBase<>),
                ReferenceResolver = new ReferenceResolver(),
                TemplateManager = new TemplateManager(this.Templates),
                CachingProvider = new DefaultCachingProvider(),
            };
            config.Namespaces.Add("Stj.Utilities.RazorEngine");
            return config;
        }
    }
}