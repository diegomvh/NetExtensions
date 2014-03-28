using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web.Configuration;
using System.Configuration.Provider;

using Novell.Directory.Ldap;

namespace System.Web.Security
{
	public class LdapMembershipProvider : System.Web.Security.MembershipProvider
	{
		#region Properties
		private string pApplicationName;
		private bool pEnablePasswordReset;
		private bool pEnablePasswordRetrieval;
		private bool pRequiresQuestionAndAnswer;
		private int pMaxInvalidPasswordAttempts;
		private int pPasswordAttemptWindow;
		private int pMinRequiredNonAlphanumericCharacters;
		private int pMinRequiredPasswordLength;
		private string pPasswordStrengthRegularExpression;

		private MembershipPasswordFormat pPasswordFormat;

		public override string ApplicationName
		{
			get { return pApplicationName; }
			set { pApplicationName = value; }
		}

		public override bool EnablePasswordReset
		{
			get { return pEnablePasswordReset; }
		}

		public override bool EnablePasswordRetrieval
		{
			get { return pEnablePasswordRetrieval; }
		}

		public override int MaxInvalidPasswordAttempts
		{
			get { return pMaxInvalidPasswordAttempts; }
		}

		public override int MinRequiredNonAlphanumericCharacters
		{
			get { return pMinRequiredNonAlphanumericCharacters;  }
		}

		public override int MinRequiredPasswordLength
		{
			get { return pMinRequiredPasswordLength; }
		}

		public override int PasswordAttemptWindow
		{
			get { return pPasswordAttemptWindow; }
		}

		public override MembershipPasswordFormat PasswordFormat
		{
			get { return pPasswordFormat; }
		}

		public override string PasswordStrengthRegularExpression
		{
			get { return pPasswordStrengthRegularExpression; }
		}

		public override bool RequiresQuestionAndAnswer
		{
			get { return pRequiresQuestionAndAnswer; }
		}

		public override bool RequiresUniqueEmail
		{
			get { return true; }
		}
		#endregion

		// LDAP options
		private string pServer;
		private string pSearchBase;
		private string pUserRdnAttribute;
		private int pServerPort;

		private LdapConnection pSearchConnection;

		//
		// System.Configuration.Provider.ProviderBase.Initialize Method
		//
		public override void Initialize(string name, NameValueCollection config)
		{
			base.Initialize(name, config);

			//
			// Initialize values from web.config.
			//
			if (config == null) {
				throw new ArgumentNullException("config");
			}

			if (name == null || name.Length == 0) {
				name = "LdapMembershipProvider";
			}

			if (String.IsNullOrEmpty(config["description"])) {
				config.Remove("description");
				config.Add("description", "LDAP Membership provider");
			}

			pApplicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
			pMaxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
			pPasswordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
			pMinRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphanumericCharacters"], "1"));
			pMinRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
			pPasswordStrengthRegularExpression = Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], ""));
			pEnablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
			pEnablePasswordRetrieval = Convert.ToBoolean(GetConfigValue(config["enablePasswordRetrieval"], "true"));
			pRequiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));

			string temp_format = GetConfigValue(config["passwordFormat"], "Hashed");
			switch (temp_format)
			{
				case "Hashed":
					pPasswordFormat = MembershipPasswordFormat.Hashed;
					break;
				case "Encrypted":
					pPasswordFormat = MembershipPasswordFormat.Encrypted;
					break;
				case "Clear":
					pPasswordFormat = MembershipPasswordFormat.Clear;
					break;
				default:
					throw new ProviderException("Password format not supported.");
			}

			// read ldap attributes
			pServer = GetConfigValue(config["server"], "localhost");
			pServerPort = Convert.ToInt32(GetConfigValue(config["serverPort"], "0"));
			pSearchBase = config["searchBase"];
			pUserRdnAttribute = GetConfigValue(config["rdn"], "uid");
			string searchUser = config["searchUser"];
			string searchPwd = config["searchPassword"];			

			// connect to LDAP
			pSearchConnection = new LdapConnection();
			pSearchConnection.Connect(pServer, pServerPort);
			pSearchConnection.Bind(searchUser, searchPwd);
		}

		public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
		{
			MembershipUserCollection result = new MembershipUserCollection();
			
			LdapSearchResults results = pSearchConnection.Search(pSearchBase, LdapConnection.SCOPE_SUB, "(objectClass=user)", null, false);
			while(results.hasMore()) {
				try {
					result.Add(createMembershipUser(results.next()));
				} catch(LdapException) {
					continue;
				}
			}

			totalRecords = result.Count;
			return result;
		}

		public override MembershipUser GetUser(string username, bool userIsOnline)
		{
			try {
				LdapSearchResults results = pSearchConnection.Search(pSearchBase, LdapConnection.SCOPE_SUB, "(&(objectClass=user)(" + pUserRdnAttribute + "=" + username + "))", null, false);
				if (results.hasMore()) {
					return createMembershipUser(results.next());
				} else {
					return null;
				}
			} catch (LdapException) {
				return null;
			}
		}

		public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
		{
			return GetUser(providerUserKey.ToString(), userIsOnline);
		}

		public override string GetUserNameByEmail(string email)
		{
			string userName = null;
			try {
				LdapSearchResults results = pSearchConnection.Search(pSearchBase, LdapConnection.SCOPE_SUB, "(&(objectClass=user)(mail=" + email + "))", null, false);
				if(results.hasMore())
				{
					LdapEntry e = results.next();
					userName = e.getAttribute(pUserRdnAttribute).StringValue;
				}

				return userName;
			} catch (LdapException) {
				return null;
			}
		}

		public override bool ValidateUser(string username, string password)
		{
			bool success = false;
			try {
				LdapSearchResults results = pSearchConnection.Search(pSearchBase, LdapConnection.SCOPE_SUB, "(&(objectClass=user)(" + pUserRdnAttribute + "=" + username + "))", null, false);
				if (results.hasMore()) {
					LdapEntry e = results.next();

					LdapConnection authConnection = new LdapConnection();
					authConnection.Connect(pServer, pServerPort);
					authConnection.Bind(e.DN, password);
					success = authConnection.Bound;
					authConnection.Disconnect();
				}
				
				return success;
			} catch (LdapException) {
				return false;
			}
		}

		private string GetConfigValue(string configValue, string defaultValue)
		{
			if (String.IsNullOrEmpty(configValue))
				return defaultValue;

			return configValue;
		}

		private MembershipUser createMembershipUser(LdapEntry entry)
		{
			return new MembershipUser(this.Name, entry.getAttribute(pUserRdnAttribute).StringValue, entry.getAttribute(pUserRdnAttribute).StringValue, entry.getAttribute("mail").StringValue, "", "", true, false, DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now);
		}

		#region Not implemented methods
		public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
		{
			throw new NotImplementedException();
		}

		public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
		{
			throw new NotImplementedException();
		}
		public override string ResetPassword(string username, string answer)
		{
			throw new NotImplementedException();
		}

		public override bool UnlockUser(string userName)
		{
			throw new NotImplementedException();
		}

		public override void UpdateUser(MembershipUser user)
		{
			throw new NotImplementedException();
		}
		public override int GetNumberOfUsersOnline()
		{
			throw new NotImplementedException();
		}

		public override string GetPassword(string username, string answer)
		{
			throw new NotImplementedException();
		}
		public override bool ChangePassword(string username, string oldPassword, string newPassword)
		{
			throw new NotImplementedException();
		}

		public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
		{
			throw new NotImplementedException();
		}

		public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
		{
			throw new NotImplementedException();
		}

		public override bool DeleteUser(string username, bool deleteAllRelatedData)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
