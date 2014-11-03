namespace Stj.Security
{
    using System;
    using System.Configuration;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Web.Security;
    using Stj.DirectoryServices;
    using Stj.Security.Identity;
    
    public class DirectoryMembershipProvider : ActiveDirectoryMembershipProvider
    {
        private bool initialized = false;
        DirectoryEntry _rootDSE;

        string _server;
        string _adsPath;
        string _defaultDomain;

        string _username;
        string _password;

        private PrincipalContext RootContext { get; set;}
        private PrincipalContext RolesContext { get; set;}

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {

            string connectionUsersStringName = config["connectionStringName"];
            var connectionRolesStringName = connectionUsersStringName.Replace("Users", "Roles");
            string adConnectionString = ConfigurationManager.ConnectionStrings[connectionUsersStringName].ConnectionString;

            try
            {
                _server = LdapUtils.GetServerFromAdsPath(adConnectionString);
            }
            catch
            {
                _server = adConnectionString.Split('/')[2];
            }
            if (String.IsNullOrEmpty(_server))
            {
                //we only allow server binds here (or ADAM would be screwed)...
                throw new InvalidOperationException("Server must be specified");
            }

            _username = config["connectionUsername"];
            _password = config["connectionPassword"];

            //should not fail...
            _rootDSE = LdapUtils.CreateDirectoryEntry("rootDSE", _server, ConnectionProtection.None, _username, _password);
            _rootDSE.RefreshCache();

            DirectoryType _dirType = LdapUtils.GetDirectoryType(_rootDSE);
            if (_dirType == DirectoryType.Unknown)
                throw new InvalidOperationException("Only Active Directory and ADAM are supported");

            try
            {
                //this will include the trailing \ if it works
                if (_dirType == DirectoryType.AD)
                    _defaultDomain = LdapUtils.GetNetbiosDomainName(_rootDSE, _server, _username, _password);
            }
            catch { }

            string[] server_container = adConnectionString.Split('/');

            var server = server_container[2];
            var container = server_container[3];

            this.RootContext = new PrincipalContext(
                (_dirType == DirectoryType.AD) ? ContextType.Domain : ContextType.ApplicationDirectory,
                server,
                container,
                System.DirectoryServices.AccountManagement.ContextOptions.SimpleBind,
                _username,
                _password);

            if (ConfigurationManager.ConnectionStrings[connectionRolesStringName] != null)
            {
                server_container = ConfigurationManager.ConnectionStrings[connectionRolesStringName].ConnectionString.Split('/');

                server = server_container[2];
                container = server_container[3];

                this.RolesContext = new PrincipalContext(
                    (_dirType == DirectoryType.AD) ? ContextType.Domain : ContextType.ApplicationDirectory,
                    server,
                    container,
                    System.DirectoryServices.AccountManagement.ContextOptions.SimpleBind,
                    _username,
                    _password);
            }

            base.Initialize(name, config);
            this.initialized = true;
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            ActiveDirectoryMembershipUser admuser = base.CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, providerUserKey, out status) as ActiveDirectoryMembershipUser;
            if (admuser != null)
            {
                DirectoryUserPrincipal dsuser = DirectoryUserPrincipal.FindByIdentity(this.RootContext, IdentityType.Sid, admuser.ProviderUserKey.ToString());
                if (dsuser != null)
                    return new DirectoryMembershipUser(admuser, dsuser);
            }
            return admuser;
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            ActiveDirectoryMembershipUser admuser = base.GetUser(username, userIsOnline) as ActiveDirectoryMembershipUser;
            if (admuser != null)
            {
                DirectoryUserPrincipal dsuser = DirectoryUserPrincipal.FindByIdentity(this.RootContext, IdentityType.Sid, admuser.ProviderUserKey.ToString());
                if (dsuser != null)
                    return new DirectoryMembershipUser(admuser, dsuser);
            }
            return admuser;
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            ActiveDirectoryMembershipUser admuser = base.GetUser(providerUserKey, userIsOnline) as ActiveDirectoryMembershipUser;
            if (admuser != null)
            {
                DirectoryUserPrincipal dsuser = DirectoryUserPrincipal.FindByIdentity(this.RootContext, IdentityType.Sid, providerUserKey.ToString());
                if (dsuser != null)
                    return new DirectoryMembershipUser(admuser, dsuser);
            }
            return admuser;
        }

        public void AddUserToRole(MembershipUser user, string name)
        {
            GroupPrincipal grp = GroupPrincipal.FindByIdentity(this.RolesContext,
                                                    IdentityType.Name,
                                                    name);

            if (grp != null)
            {
                grp.Members.Add(this.RootContext, IdentityType.Name, user.UserName);
                grp.Save();
                grp.Dispose();
            }
        }
        
        public string GetErrorMessage(MembershipCreateStatus status)
        {
            switch (status)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "Username already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A username for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
    }
}
