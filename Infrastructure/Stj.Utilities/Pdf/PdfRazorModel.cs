using Stj.Utilities.RazorEngine;

namespace Stj.Utilities.Pdf
{
    public class PdfRazorModel : RazorModel
    {
        public readonly string ViewName;
        public PdfRazorModel(string viewName) : base(){
            this.ViewName = viewName;
        }
    }
}
