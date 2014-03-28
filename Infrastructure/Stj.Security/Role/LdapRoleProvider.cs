using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web.Configuration;
using System.Configuration.Provider;

using Novell.Directory.Ldap;

namespace Stj.Security
{
	public class LdapRoleProvider : System.Web.Security.RoleProvider
	{
		#region Properties
		private string pApplicationName;
		public override string ApplicationName
		{
			get { return pApplicationName;	}
			set	{ pApplicationName = value;	}
		}
		#endregion

		// LDAP options
		private string pServer;
		private string pSearchBase;
		private string pUserSearchBase;
		private string pGroupRdnAttribute;
		private string pUserRdnAttribute;
		private int pServerPort;

		private LdapConnection pSearchConnection;

		public override void Initialize(string name, NameValueCollection config)
		{
			base.Initialize(name, config);

			// read property values
			pApplicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);

			// read ldap attributes
			pServer = GetConfigValue(config["server"], "localhost");
			pServerPort = Convert.ToInt32(GetConfigValue(config["serverPort"], "0"));
			
			pSearchBase = config["searchBase"];
			pUserSearchBase = GetConfigValue(config["userSearchBase"], pSearchBase);
			
			pGroupRdnAttribute = GetConfigValue(config["rdn"], "cn");
			pUserRdnAttribute = GetConfigValue(config["userRdn"], "uid");

			string searchUser = config["searchUser"];
			string searchPwd = config["searchPassword"];

			// connect to LDAP
			pSearchConnection = new LdapConnection();
			pSearchConnection.Connect(pServer, pServerPort);
			pSearchConnection.Bind(searchUser, searchPwd);
		}

		public override string[] FindUsersInRole(string roleName, string usernameToMatch)
		{
			var r = from s in GetRole(roleName).getAttribute("member").StringValueArray
					where s.Contains(usernameToMatch)
					select s;
			return r.ToArray();
		}

		public override string[] GetAllRoles()
		{
			var r = from e in SearchAllRoles()
					select e.getAttribute(pGroupRdnAttribute).StringValue;
			return r.ToArray();
		}

		public override string[] GetRolesForUser(string username)
		{
			String userDN = GetUserDN(username);
			var r = from e in SearchAllRoles()
					where e.getAttribute("member").StringValueArray.Any(s => s == userDN)
					select e.getAttribute(pGroupRdnAttribute).StringValue;
			return r.ToArray();
		}

		public override string[] GetUsersInRole(string roleName)
		{
			return FindUsersInRole(roleName, "");
		}

		public override bool IsUserInRole(string username, string roleName)
		{
			String userDN = GetUserDN(username);
			return GetRole(roleName).getAttribute("member").StringValueArray.Any(s => s.Equals(userDN));
		}

		public override bool RoleExists(string roleName)
		{
			return GetRole(roleName) != null;
		}

		private string GetConfigValue(string configValue, string defaultValue)
		{
			if (String.IsNullOrEmpty(configValue))
				return defaultValue;

			return configValue;
		}

		private string GetUserDN(string username)
		{
			try {
				LdapSearchResults results = pSearchConnection.Search(pUserSearchBase, LdapConnection.SCOPE_SUB, "(&(objectClass=user)(" + pUserRdnAttribute + "=" + username + "))", null, false);
				if (results.hasMore()) {
					return results.next().DN;
				} else {
					return null;
				}
			} catch (LdapException) {
				return null;
			}
		}

		private LdapSearchResults SearchAllRoles()
		{
			return pSearchConnection.Search(pSearchBase, LdapConnection.SCOPE_SUB, "(objectClass=groupOfNames)", null, false);
		}

		private LdapEntry GetRole(string rolename)
		{
			return pSearchConnection.Search(pSearchBase, LdapConnection.SCOPE_SUB, "(&(objectClass=groupOfNames)(" + pGroupRdnAttribute + "=" + rolename + "))", null, false).FirstOrDefault();
		}

		#region Not implemented methods
		public override void CreateRole(string roleName)
		{
			throw new NotImplementedException();
		}

		public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
		{
			throw new NotImplementedException();
		}

		public override void AddUsersToRoles(string[] usernames, string[] roleNames)
		{
			throw new NotImplementedException();
		}

		public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}