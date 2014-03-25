namespace Stj.Security
{
    using System;
    using System.Web.Security;
    using System.Security.Principal;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Configuration;
    using System.Configuration.Provider;
    using Stj.DirectoryServices;

    public class DirectoryMembershipProvider : ActiveDirectoryMembershipProvider
    {
        private bool initialized = false;
        DirectoryEntry _rootDSE;

        string _server;
        string _adsPath;
        string _defaultDomain;

        string _username;
        string _password;

        private PrincipalContext principalcontext = null;

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {

            string temp = config["connectionStringName"];
            string adConnectionString = ConfigurationManager.ConnectionStrings[temp].ConnectionString;

            try
            {
                _server = LdapUtils.GetServerFromAdsPath(adConnectionString);
            }
            catch {
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
            this.principalcontext = new PrincipalContext(
                (_dirType == DirectoryType.AD) ? ContextType.Domain : ContextType.ApplicationDirectory,
                server_container[2],
                server_container[3],
                System.DirectoryServices.AccountManagement.ContextOptions.SimpleBind,
                _username,
                _password);

            base.Initialize(name, config);
            this.initialized = true;
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            ActiveDirectoryMembershipUser admuser = base.CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, providerUserKey, out status) as ActiveDirectoryMembershipUser;
            DirectoryUserPrincipal dsuser = DirectoryUserPrincipal.FindByIdentity(this.principalcontext, IdentityType.Name, username);
            if (admuser != null)
                return new DirectoryMembershipUser(admuser, dsuser);
            return admuser;
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            ActiveDirectoryMembershipUser admuser = base.GetUser(username, userIsOnline) as ActiveDirectoryMembershipUser;
            DirectoryUserPrincipal dsuser = DirectoryUserPrincipal.FindByIdentity(this.principalcontext, IdentityType.Name, username);
            if (admuser != null)
                return new DirectoryMembershipUser(admuser, dsuser);
            return admuser;
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            ActiveDirectoryMembershipUser admuser = base.GetUser(providerUserKey, userIsOnline) as ActiveDirectoryMembershipUser;
            DirectoryUserPrincipal dsuser = DirectoryUserPrincipal.FindByIdentity(this.principalcontext, IdentityType.Sid, providerUserKey.ToString());
            if (admuser != null)
                return new DirectoryMembershipUser(admuser, dsuser);
            return admuser;
        }
    }
}
