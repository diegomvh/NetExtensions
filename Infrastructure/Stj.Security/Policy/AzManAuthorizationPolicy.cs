#region Using

using System;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using Stj.Security.Principal;
using System.Collections.Generic;
using AZROLESLib;

#endregion Using

namespace Stj.Security.Policy
{
    public class AzManAuthorizationPolicy : IAuthorizationPolicy
    {

        #region Members

        private Guid _id = Guid.NewGuid();

        #endregion Members

        #region Properties

        public string Id { get { return _id.ToString(); } }
        public ClaimSet Issuer { get { return ClaimSet.System; } }
        public Dictionary<string, object> Parameters { get; set; }

        #endregion Properties

        #region Methods

        #region Public

        public bool Evaluate(EvaluationContext evaluationContext, ref object state)
        {
            var success = false;

            var identity = GetClientIdentity(evaluationContext);
            if (identity != null)
            {
                evaluationContext.Properties["Principal"] = MembershipHelper.ToPrincipal(identity, Parameters);
                success = true;
            }

            return success;
        }

        public static List<IAuthorizationPolicy> PoliciesFactory() {
            List<IAuthorizationPolicy> policies = new List<IAuthorizationPolicy>();
            AzManAuthorizationPolicy policy = new AzManAuthorizationPolicy();

            policy.Parameters = AzManAuthorizationPolicy.ParametersFactory();
            policies.Add(policy);
            return policies;
        }

        public static Dictionary<string, object> ParametersFactory() {
            /* Parameters */
            var ipAddress = HttpContext.Current.Request.UserHostAddress;
            Dictionary<string, object>  parameters = new Dictionary<string, object>();
            parameters["Ip"] = ipAddress;
            parameters["IsPrivateIp"] = false;
            parameters["IsLocalIp"] = HttpContext.Current.Request.IsLocal;
            parameters["IsSecureConnection"] = HttpContext.Current.Request.IsSecureConnection;
            try
            {
                String[] sparts = ipAddress.Split(new String[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                parameters["IsPrivateIp"] = (int.Parse(sparts[0]) == 10 || (int.Parse(sparts[0]) == 192 && int.Parse(sparts[1]) == 168) || (int.Parse(sparts[0]) == 172 && (int.Parse(sparts[1]) >= 16 && int.Parse(sparts[1]) <= 31)));
            }
            catch { }
            return parameters;
        }

        #endregion Public

        #region Private

        private IIdentity GetClientIdentity(EvaluationContext evaluationContext)
        {
            var identity = HttpContext.Current.User.Identity;
            if (identity == null || string.IsNullOrEmpty(identity.Name)) return null;
            return identity;
        }

        #endregion

        #endregion Methods

    }
}
