using RazorEngine.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Stj.Utilities.RazorEngine
{
    /// <summary>
    /// A view engine that uses the Razor engine to render a templates loaded directly from the
    /// file system. This means it will work outside of ASP.NET.
    /// </summary>
    public class FileSystemRazorViewEngine : IViewEngine
    {
        readonly string viewsPathRoot;
        readonly string applicationPathRoot;
        readonly Dictionary<string, string> templatesFiles;

        /// <summary>
        /// Creates a new <see cref="FileSystemRazorViewEngine"/> that finds views within the given path.
        /// </summary>
        /// <param name="viewsPathRoot">The root directory that contains views.</param>
        public FileSystemRazorViewEngine(string applicationPathRoot, string viewsPathRoot)
        {
            this.viewsPathRoot = viewsPathRoot;
            this.applicationPathRoot = applicationPathRoot;
            this.templatesFiles = new Dictionary<string, string>();
            var base_uri = new Uri(this.applicationPathRoot);
            foreach (var path in Glob.Expand(viewsPathRoot + @"\**\*.{cshtml, vbhtml}"))
            {
                var key = base_uri.MakeRelativeUri(new Uri(path.FullName));
                templatesFiles.Add("~/" + key.OriginalString, path.FullName);
            }
        }

        string GetViewFullPath(string path)
        {
            return Path.Combine(viewsPathRoot, path);
        }

        /// <summary>
        /// Tries to find a razor view (.cshtml or .vbhtml files).
        /// </summary>
        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            partialViewName = this.GetViewFullPath(partialViewName);
            var relative_uri = new Uri(this.applicationPathRoot).MakeRelativeUri(new Uri(partialViewName));
            var cshtml_key = "~/" + relative_uri.OriginalString + ".cshtml";
            var vbhtml_key = "~/" + relative_uri.OriginalString + ".vbhtml";
            foreach (var pair in this.templatesFiles) {
                if (pair.Key == cshtml_key)
                    return new ViewEngineResult(new FileSystemRazorView(cshtml_key, this.applicationPathRoot, this.templatesFiles), this);
                if (pair.Key == vbhtml_key)
                    return new ViewEngineResult(new FileSystemRazorView(cshtml_key, this.applicationPathRoot, this.templatesFiles), this);
            }
            return new ViewEngineResult(new List<string>() { viewsPathRoot });
        }

        /// <summary>
        /// Tries to find a razor view (.cshtml or .vbhtml files).
        /// </summary>
        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            return FindPartialView(controllerContext, viewName, useCache);
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            // Nothing to do here - FileSystemRazorView does not need disposing.
        }
    }
}