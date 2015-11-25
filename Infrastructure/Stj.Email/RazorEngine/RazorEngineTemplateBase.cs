using RazorEngine.Templating;
using RazorEngine.Text;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace Stj.Email.RazorEngine
{
    [RequireNamespaces("System.Web.Mvc.Html")]
    public class RazorEngineTemplateBase<T> : TemplateBase<T>, IViewDataContainer
    {
        private HtmlHelper<T> helper = null;
        private ViewDataDictionary viewdata = null;

        public HtmlHelper<T> Html
        {
            get
            {
                if (helper == null)
                {
                    var writer = this.CurrentWriter; //TemplateBase.CurrentWriter
                    var vcontext = new ViewContext() { Writer = writer, ViewData = this.ViewData };

                    helper = new HtmlHelper<T>(vcontext, this);
                }
                return helper;
            }
        }
        public ViewDataDictionary ViewData
        {
            get
            {
                if (viewdata == null)
                {
                    viewdata = new ViewDataDictionary();
                    viewdata.TemplateInfo = new TemplateInfo() { HtmlFieldPrefix = string.Empty };

                    if (this.Model != null)
                    {
                        viewdata.Model = Model;
                    }
                }
                return viewdata;
            }
            set
            {
                viewdata = value;
            }
        }

        protected override ITemplate ResolveLayout(string name)
        {
            name = System.Web.Hosting.HostingEnvironment.MapPath(name);
            return base.ResolveLayout(name);
        }

        public override void WriteTo(TextWriter writer, object value)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            if (value == null) return;

            //try to cast to RazorEngine IEncodedString
            var encodedString = value as IEncodedString;
            if (encodedString != null)
            {
                writer.Write(encodedString);
            }
            else
            {
                //try to cast to IHtmlString (Could be returned by Mvc Html helper methods)
                var htmlString = value as IHtmlString;
                if (htmlString != null) writer.Write(htmlString.ToHtmlString());
                else
                {
                    base.WriteTo(writer, value);
                }
            }
        }
    }
}