#region Using

using System;

#endregion Using

namespace Stj.Security.Management
{
    public class WebServiceErrorEvent : System.Web.Management.WebServiceErrorEvent
    {
        protected internal WebServiceErrorEvent(string message, object eventSource, Exception exception) : base(message, eventSource, exception) { }
    }
}
