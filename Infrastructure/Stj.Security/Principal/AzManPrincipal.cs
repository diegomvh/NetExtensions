#region Using

using System;
using System.Linq;
using System.Security.Principal;
using System.Runtime.Serialization;

#endregion Using

namespace Stj.Security.Principal
{
    [Serializable]
    public class AzManPrincipal : IUserPrincipal
    {

        #region Properties
        public string Name { get { return this.Identity.Name; } }
        public bool IsAuthenticated { get { return this.Identity.IsAuthenticated; } }
        public IIdentity Identity { get; private set; }
        public string[] Roles { get; set; }
        public string[] Operations { get; set; }
        public string[] Tasks { get; set; }

        #endregion Properties

        #region Constructors

        public AzManPrincipal(IIdentity identity) : this(identity, null, null, null) { }

        public AzManPrincipal(IIdentity identity, string[] roles, string[] operations, string[] tasks)
        {
            Identity = identity;
            Roles = roles;
            Operations = operations;
            Tasks = tasks;
        }

        #endregion Constructors

        #region Methods

        #region Public

        public bool IsInRole(string role)
        {
            var isInRole = false;
            if (Roles != null)
            {
                isInRole = Roles.Contains(role);
            }
            return isInRole;
        }

        public bool HasRequiredOperations(string[] requiredOperations)
        {
            if (requiredOperations == null || requiredOperations.Length == 0) return true;
            if (Operations == null || Operations.Length == 0) return false;

            return requiredOperations.All(t => Operations.Contains(t));
        }

        public bool HasRequiredTasks(string[] requiredTasks)
        {
            if (requiredTasks == null || requiredTasks.Length == 0) return true;
            if (Tasks == null || Tasks.Length == 0) return false;

            return requiredTasks.All(t => Tasks.Contains(t));
        }

        public bool Can(string permission)
        { 
            //TODO: Cosas locas como (algo1 && algo2) || algo3  :)
            var permissions = new string[] { permission };
            return this.HasRequiredOperations(permissions) || this.HasRequiredTasks(permissions);
        }

        #endregion Public

        #endregion Methods

    }
}
