#region Using

using System;
using System.Security;
using System.Security.Permissions;

#endregion Using

namespace Stj.Security.Permissions
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AzManPrincipalPermissionAttribute : CodeAccessSecurityAttribute
    {

        #region Properties

        public bool IsAuthenticated { get; set; }
        public string Operation { get; set; }
        public string Task { get; set; }

        #endregion Properties

        #region Constructors

        public AzManPrincipalPermissionAttribute(SecurityAction action)
            : base(action)
        {
            IsAuthenticated = true;
        }

        #endregion Constructors

        #region Methods

        #region Public

        public override IPermission CreatePermission()
        {
            if (base.Unrestricted)
            {
                return new AzManPrincipalPermission(PermissionState.Unrestricted);
            }
            return new AzManPrincipalPermission(IsAuthenticated, new string[] { Operation }, new string[] { Task });
        }

        #endregion Public

        #endregion Methods

    }
}
