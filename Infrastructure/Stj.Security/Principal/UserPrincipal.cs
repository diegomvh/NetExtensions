using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;

namespace Stj.Security.Principal
{
    public interface IUserPrincipal : IPrincipal
    {
        bool IsAuthenticated { get; }
        string Name { get; }
    }
}
