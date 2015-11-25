using System.IO;
using System.Linq;
using System.Web.Mvc;
using RazorEngine.Configuration;
using System.Collections.Generic;
using RazorEngine.Templating;
using Stj.Email.RazorEngine;

namespace Stj.Email.Postal
{
    /// <summary>
    /// A view that uses the Razor engine to render a templates loaded directly from the
    /// file system. This means it will work outside of ASP.NET.
    /// </summary>
    public class FileSystemRazorView : IView
    {
        readonly string template;
        readonly string templateName;
        readonly string[] layouts;
        readonly string[] layoutNames;

        /// <summary>
        /// Creates a new <see cref="FileSystemRazorView"/> using the given view filename.
        /// </summary>
        /// <param name="filename">The filename of the view.</param>
        public FileSystemRazorView(string filename, params string[] layouts)
        {
            this.template = File.ReadAllText(filename);
            this.templateName = filename;
            this.layoutNames = layouts;
            this.layouts = layouts.Select(l => File.ReadAllText(l)).ToArray();

        }

        /// <summary>
        /// Renders the view into the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="viewContext">The <see cref="ViewContext"/> that contains the view data model.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the rendered output.</param>
        public void Render(ViewContext viewContext, TextWriter writer)
        {
            
            TemplateServiceConfiguration config = this.GetTemplateServiceConfiguration();
            using (var service = RazorEngineService.Create(config))
            {
                for (var i=0;i < this.layouts.Length; i++)
                {
                    service.AddTemplate(layoutNames[i], layouts[i]);
                }
                var key = service.GetKey(templateName);
                service.AddTemplate(key, template);
                service.RunCompile(key, writer, null, viewContext.ViewData.Model, null);
                writer.Flush();
            }
        }

        private TemplateServiceConfiguration GetTemplateServiceConfiguration()
        {
            ICollection<string> namespaces = new List<string>();
            namespaces.Add("Stj.Email.Postal");
            namespaces.Add("Stj.Email.RazorEngine");

            TemplateServiceConfiguration config = new TemplateServiceConfiguration()
            {
                BaseTemplateType = typeof(Stj.Email.RazorEngine.RazorEngineTemplateBase<>)
            };
            
            config.ReferenceResolver = new ReferenceResolver();

            foreach (var name in namespaces)
            {
                config.Namespaces.Add(name);
            }

            return config;
        }
    }
}