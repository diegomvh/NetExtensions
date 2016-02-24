using System.Dynamic;
using System.Web.Mvc;

namespace Stj.Utilities.RazorEngine
{
    public class RazorModel : DynamicObject, IViewDataContainer
    {
        public ViewDataDictionary ViewData { get; set; }

        protected RazorModel()
        {
            this.ViewData = new ViewDataDictionary(this);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this.ViewData[binder.Name] = value;
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return ViewData.TryGetValue(binder.Name, out result);
        }
    }
}
