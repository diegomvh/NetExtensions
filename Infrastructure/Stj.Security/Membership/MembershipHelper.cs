using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Web.Security;
using System.Security.Principal;
using Stj.Security.Principal;

namespace Stj.Security
{
    public static class MembershipHelper
    {
        #region Get user
        public static MembershipUser GetUser(object identifier)
        {
            MembershipUser muser = null;
            foreach (MembershipProvider provider in System.Web.Security.Membership.Providers)
            {
                try
                {
                    muser = provider.GetUser(identifier, false);
                    if (muser != null)
                        return muser;
                }
                catch { }
            }
            return null;
        }

        public static MembershipUser GetUser(string username)
        {
            MembershipUser muser = null;
            foreach (MembershipProvider provider in System.Web.Security.Membership.Providers)
            {
                try
                {
                    muser = provider.GetUser(username, false);
                    if (muser != null)
                        return muser;
                }
                catch { }
            }
            return null;
        }

        public static MembershipUser GetUserByEmail(string email) {
            MembershipUser muser = null;
            foreach (MembershipProvider provider in System.Web.Security.Membership.Providers)
            {
                try
                {
                    string username = provider.GetUserNameByEmail(email);
                    if (username != null)
                    {
                        muser = provider.GetUser(username, false);
                        if (muser != null)
                            return muser;
                    }
                }
                catch { }
            }
            return null;
        }

        #endregion

        #region Update user
        public static void UpdateUser(DirectoryMembershipUser user)
        {
            System.Web.Security.Membership.UpdateUser(user);
            user.Save();
        }

        #endregion
        public static MembershipUser CreateUser(string username, string password)
        {
            return System.Web.Security.Membership.CreateUser(username, password);
        }

        public static MembershipUser CreateUser(string username, string password, string email)
        {
            return System.Web.Security.Membership.CreateUser(username, password, email);
        }

        public static bool DeleteUser(string username)
        {
            return System.Web.Security.Membership.DeleteUser(username);
        }

        public static bool ChangePassword(string username, string password)
        {
            foreach (MembershipProvider provider in System.Web.Security.Membership.Providers)
            {
                var dp = provider as DirectoryMembershipProvider;
                if (dp != null)
                    return dp.ChangePassword(username, dp.ResetPassword(username, ""), password);
            }
            return false;
        }

        public static bool ValidateUser(string username, string password) {
            foreach (MembershipProvider provider in System.Web.Security.Membership.Providers)
                if (provider.ValidateUser(username, password))
                    return true;
            return false;
        }

        public static AzManPrincipal ConvertToAzManPrincipal(DirectoryMembershipUser user)
        {
            IIdentity identity = new GenericIdentity(user.UserName);
            AzManPrincipal principal = new AzManPrincipal(identity);
            return principal;
        }

        public static SecurityIdentifier CreateSecurityIdentifier(string sid)
        {
            return new SecurityIdentifier(sid);
        }

        public static void AddUserToApplicationRole(MembershipUser user, string name)
        {
            foreach (MembershipProvider provider in System.Web.Security.Membership.Providers)
            {
                var dp = provider as DirectoryMembershipProvider;
                if (dp != null)
                    dp.AddUserToRole(user, name);
            }
        }

    }
}
