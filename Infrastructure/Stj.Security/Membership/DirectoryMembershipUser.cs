namespace Stj.Security
{
    using System;
    using System.Linq;
    using System.Security.Principal;
    using System.Collections.Generic;
    using System.Web.Security;
    using Stj.DirectoryServices;

    public class DirectoryMembershipUser : MembershipUser
    {
        public override object ProviderUserKey { get { return this._admuser.ProviderUserKey; } }
        public string DistinguishedName { get { return this._dsuser.DistinguishedName; } }
        public string Nombres { get { return this._dsuser.Nombres; } set { this._dsuser.Nombres = value; } }
        public string Apellidos { get { return this._dsuser.Apellidos; } set { this._dsuser.Apellidos = value; } }
        public string Dni { get { return this._dsuser.Dni; } set { this._dsuser.Dni = value; } }
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
    }
}
