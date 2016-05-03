namespace Stj.Security
{
    using System;
    using System.Linq;
    using System.Security.Principal;
    using System.Collections.Generic;
    using System.Web.Security;
    using Stj.DirectoryServices;
    using System.DirectoryServices;
    using Stj.Security.Principal;
    
    public class DirectoryMembershipUser : MembershipUser
    {
        [Flags]
        public enum DirectoryMembershipUserFlags
        {
            // Reference - Chapter 10 (from The .NET Developer's Guide to Directory Services Programming)

            Script = 1,                                     // 0x1
            AccountDisabled = 2,                            // 0x2
            HomeDirectoryRequired = 8,                      // 0x8
            AccountLockedOut = 16,                          // 0x10
            PasswordNotRequired = 32,                       // 0x20
            PasswordCannotChange = 64,                      // 0x40
            EncryptedTextPasswordAllowed = 128,             // 0x80
            TempDuplicateAccount = 256,                     // 0x100
            NormalAccount = 512,                            // 0x200
            InterDomainTrustAccount = 2048,                 // 0x800
            WorkstationTrustAccount = 4096,                 // 0x1000
            ServerTrustAccount = 8192,                      // 0x2000
            UserDontExpirePassword = 65536,                 // 0x10000 (Also 66048 )
            MnsLogonAccount = 131072,                       // 0x20000
            SmartCardRequired = 262144,                     // 0x40000
            TrustedForDelegation = 524288,                  // 0x80000
            AccountNotDelegated = 1048576,                  // 0x100000
            UseDesKeyOnly = 2097152,                        // 0x200000
            DontRequirePreauth = 4194304,                   // 0x400000
            PasswordExpired = 8388608,                      // 0x800000 (Applicable only in Window 2000 and Window Server 2003)
            TrustedToAuthenticateForDelegation = 16777216,  // 0x1000000
            NoAuthDataRequired = 33554432                   // 0x2000000
        }

        public override object ProviderUserKey { get { return this._admuser.ProviderUserKey; } }
        public string DistinguishedName { get { return this._dsuser.DistinguishedName; } }
        public string Nombres { get { return this._dsuser.Nombres; } set { this._dsuser.Nombres = value; } }
        public string Apellidos { get { return this._dsuser.Apellidos; } set { this._dsuser.Apellidos = value; } }
        public string Dni { get { return this._dsuser.Dni; } set { this._dsuser.Dni = value; } }
        public string EmployeeId { get { return this._dsuser.EmployeeId; } set { this._dsuser.EmployeeId = value; } }
        public string Localidad { get { return this._dsuser.Localidad; } set { this._dsuser.Localidad = value; } }
        public string Provincia { get { return this._dsuser.Provincia; } set { this._dsuser.Provincia = value; } }
        public string Domicilio { get { return this._dsuser.Domicilio; } set { this._dsuser.Domicilio = value; } }
        public string Telefono { get { return this._dsuser.VoiceTelephoneNumber; } set { this._dsuser.VoiceTelephoneNumber = value; } }
        public string AuthenticationServer { get { return this._dsuser.AuthenticationServer; } }
        public SecurityIdentifier Sid { get { return (this._dsuser != null) ? this._dsuser.Sid : null; } }
        public IEnumerable<string> Organizations { get { return this._dsuser.Organizations; } }

        private ActiveDirectoryMembershipUser _admuser;
        private DirectoryUserPrincipal _dsuser;
        public DirectoryMembershipUser(ActiveDirectoryMembershipUser admuser, DirectoryUserPrincipal dsuser)
            : base(admuser.ProviderName,
                   admuser.UserName,
                        null,
                        admuser.Email,
                        admuser.PasswordQuestion,
                        admuser.Comment,
                        admuser.IsApproved,
                        admuser.IsLockedOut,
                        admuser.CreationDate,
                        DateTime.MinValue,
                        DateTime.MinValue,
                        admuser.LastPasswordChangedDate,
                        admuser.LastLockoutDate)
        {
            this._admuser = admuser;
            this._dsuser = dsuser;
        }

        public string[] GetGroupSids() {
            if (this._dsuser == null)
                return new string[] { };
            return (from System.DirectoryServices.AccountManagement.Principal g in this._dsuser.GetGroups()
                    select g.Sid.ToString()).ToArray();
        }

        public void Save() {
            this._dsuser.Save();
        }

        #region Cuenta del usuario, AzManPrincipal, permisos, roles
        private IPrincipal _account;
        public IPrincipal Account
        {
            get
            {
                if (this._account == null)
                    this._account = MembershipHelper.ToPrincipal(this);
                return this._account;
            }
            set
            {
                this._account = value;
            }
        }

        public bool IsInRole(string role)
        {
            return (this.Account != null) && this.Account.IsInRole(role);
        }

        public bool HasRequiredOperation(string operation)
        {
            return (this.Account is AzManPrincipal) && ((AzManPrincipal)this.Account).HasRequiredOperations(new string[] { operation });
        }

        public bool HasRequiredTask(string task)
        {
            return (this.Account is AzManPrincipal) && ((AzManPrincipal)this.Account).HasRequiredTasks(new string[] { task });
        }

        public bool Can(string permission)
        {
            return (this.Account is AzManPrincipal) && ((AzManPrincipal)this.Account).Can(new string[] { permission });
        }

        public string[] GetRoles()
        {
            return (this.Account is AzManPrincipal)? ((AzManPrincipal)this.Account).Roles : new string[0];
        }

        public string[] GetOperations()
        {
            return (this.Account is AzManPrincipal) ? ((AzManPrincipal)this.Account).Operations : new string[0];
        }

        public string[] GetTasks()
        {
            return (this.Account is AzManPrincipal) ? ((AzManPrincipal)this.Account).Tasks : new string[0];
        }

        #endregion

        public bool UserDontExpirePassword
        { 
            get {
                return this._dsuser.UserDontExpirePassword;
            }
            set {
                this._dsuser.UserDontExpirePassword = value;
            }
        }
        
        public void SetPassword(string password) {
            DirectoryEntry directoryEntry = this._dsuser.GetUnderlyingObject() as DirectoryEntry;
            const long ADS_OPTION_PASSWORD_PORTNUMBER = 6;
            const long ADS_OPTION_PASSWORD_METHOD = 7;

            const int ADS_PASSWORD_ENCODE_CLEAR = 1;

            try
            {
                directoryEntry.Invoke("SetOption", new object[] { ADS_OPTION_PASSWORD_PORTNUMBER, 50000 });
                directoryEntry.Invoke("SetOption", new object[]
                    {ADS_OPTION_PASSWORD_METHOD,
                     ADS_PASSWORD_ENCODE_CLEAR});
                directoryEntry.Invoke("SetPassword", new object[] { password });
                directoryEntry.RefreshCache();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:   Set password failed.");
                Console.WriteLine("         {0}.", e.Message);
                return;
            }
        }
    }
}
