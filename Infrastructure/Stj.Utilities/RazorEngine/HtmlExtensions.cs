using PostalEmail = Postal.Email;
using PostalImageEmbedder = Postal.ImageEmbedder;
using System;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace Stj.Utilities.RazorEngine
{
    public static class HtmlExtensions
    {
        /// <summary>
        /// Embeds the given image into the email and returns an HTML &lt;img&gt; tag referencing the image.
        /// </summary>
        /// <param name="html">The <see cref="HtmlHelper"/>.</param>
        /// <param name="imagePathOrUrl">An image file path or URL. A file path can be relative to the web application root directory.</param>
        /// <param name="alt">The content for the &lt;img alt&gt; attribute.</param>
        /// <returns>An HTML &lt;img&gt; tag.</returns>
        public static IHtmlString EmbedImage(this HtmlHelper html, string imagePathOrUrl, string alt = "")
        {

            if (string.IsNullOrWhiteSpace(imagePathOrUrl)) throw new ArgumentException("Path or URL required", "imagePathOrUrl");
            var model = (PostalEmail)html.ViewData.Model;
            var imageEmbedder = (PostalImageEmbedder)model.ViewData["Postal.ImageEmbedder"];
            var applicationRoot = (string)model.ViewData["ApplicationPathRoot"];
            if (IsApplicationPath(imagePathOrUrl))
            {
                imagePathOrUrl = new Uri(applicationRoot + imagePathOrUrl.Substring(2)).LocalPath;
            }
            else if (IsFileName(imagePathOrUrl))
            {
                imagePathOrUrl = html.ViewContext.HttpContext.Server.MapPath(imagePathOrUrl);
            }
            var resource = imageEmbedder.ReferenceImage(imagePathOrUrl);
            return new HtmlString(string.Format("<img src=\"cid:{0}\" alt=\"{1}\"/>", resource.ContentId, html.AttributeEncode(alt)));
        }

        public static IHtmlString Image(this HtmlHelper html, string imagePathOrUrl, string alt = "")
        {
            if (string.IsNullOrWhiteSpace(imagePathOrUrl)) throw new ArgumentException("Path or URL required", "imagePathOrUrl");
            var model = (RazorModel)html.ViewData.Model;
            var applicationRoot = (string)model.ViewData["ApplicationPathRoot"];

            if (IsApplicationPath(imagePathOrUrl)) {
                imagePathOrUrl = new Uri(applicationRoot + imagePathOrUrl.Substring(2)).OriginalString;
            }
            else if (IsFileName(imagePathOrUrl))
            {
                imagePathOrUrl = html.ViewContext.HttpContext.Server.MapPath(imagePathOrUrl);
            }
            return new HtmlString(string.Format("<img src=\"{0}\" alt=\"{1}\"/>", imagePathOrUrl, html.AttributeEncode(alt)));
        }

        public static IHtmlString EmbedRawFile(this HtmlHelper html, string filePathOrUrl)
        {

            if (string.IsNullOrWhiteSpace(filePathOrUrl)) throw new ArgumentException("Path or URL required", "imagePathOrUrl");
            dynamic model = html.ViewData.Model;
            var applicationRoot = (string)model.ViewData["ApplicationPathRoot"];

            if (IsApplicationPath(filePathOrUrl))
            {
                filePathOrUrl = new Uri(applicationRoot + filePathOrUrl.Substring(2)).LocalPath;
                var content = File.ReadAllText(filePathOrUrl);
                return html.Raw(content);
            }
            return html.Raw("");
        }

        static bool IsFileName(string pathOrUrl)
        {
            return !(pathOrUrl.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
                     || pathOrUrl.StartsWith("https:", StringComparison.OrdinalIgnoreCase));
        }

        static bool IsApplicationPath(string pathOrUrl)
        {
            return pathOrUrl.StartsWith("~/", StringComparison.OrdinalIgnoreCase);
        }
    }
}