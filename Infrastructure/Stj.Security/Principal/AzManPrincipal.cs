#region Using

using System;
using System.Linq;
using System.Security.Principal;
using System.Runtime.Serialization;

#endregion Using

namespace Stj.Security.Principal
{
    public class AzManPrincipal : IPrincipal
    {

        #region Members

        private IIdentity _identity = null;
        private string[] _roles = null;
        private string[] _operations = null;
        private string[] _tasks = null;

        #endregion Members

        #region Properties
        public string Name { get { return this.Identity.Name;  } }
        public IIdentity Identity { get { return _identity; } }

        #endregion Properties

        #region Constructors

        public AzManPrincipal(IIdentity identity) : this(identity, null, null, null) { }

        public AzManPrincipal(IIdentity identity, string[] roles, string[] operations, string[] tasks)
        {
            _identity = identity;
            _roles = roles;
            _operations = operations;
            _tasks = tasks;
        }

        #endregion Constructors

        #region Methods

        #region Public

        public bool IsInRole(string role)
        {
            var isInRole = false;
            if (_roles != null)
            {
                isInRole = _roles.Contains(role);
            }
            return isInRole;
        }

        public bool HasRequiredOperations(string[] requiredOperations)
        {
            if (requiredOperations == null || requiredOperations.Length == 0) return true;
            if (_operations == null || _operations.Length == 0) return false;

            return requiredOperations.All(t => _operations.Contains(t));
        }

        public bool HasRequiredTasks(string[] requiredTasks)
        {
            if (requiredTasks == null || requiredTasks.Length == 0) return true;
            if (_tasks == null || _tasks.Length == 0) return false;

            return requiredTasks.All(t => _tasks.Contains(t));
        }

        #endregion Public

        #endregion Methods
    }
}
