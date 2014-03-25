namespace Stj.DirectoryServices
{
    using System;
    using System.Security.Principal;
    using System.Collections.Generic;
    using System.DirectoryServices.AccountManagement;

    [DirectoryRdnPrefix("CN")]
    [DirectoryObjectClass("User")]
    public class DirectoryUserPrincipal : System.DirectoryServices.AccountManagement.UserPrincipal
    {
        // Inplement the constructor using the base class constructor. 
        public DirectoryUserPrincipal(PrincipalContext context)
            : base(context)
        { }

        // Implement the constructor with initialization parameters.    
        public DirectoryUserPrincipal(PrincipalContext context,
                             string samAccountName,
                             string password,
                             bool enabled)
            : base(context, samAccountName, password, enabled)
        { }

        #region Mapeando algunas propiedades
        public string Nombres
        {
            get
            {
                return this.GivenName != null ? this.GivenName : "";
            }
            set
            {
                this.GivenName = value;
            }
        }
        public string Apellidos
        {
            get
            {
                return this.Surname != null ? this.Surname : "";
            }
            set
            {
                this.Surname = value;
            }
        }
        public string Email
        {
            get
            {
                return this.EmailAddress;
            }
            set
            {
                this.EmailAddress = value;
            }
        }
        public string AuthenticationServer
        {
            get
            {
                return this.Context.Name;
            }
        }
        private IEnumerable<string> _organizations = null;
        public IEnumerable<string> Organizations
        {
            get
            {
                if (this._organizations == null)
                    this._organizations = DirectoryUserPrincipal._ParseOrganizations(this);
                return this._organizations;
            }
            set
            {
                this._organizations = value;
            }
        }
        #endregion

        #region Extensiones
        [DirectoryProperty("title")]
        public string Title
        {
            get
            {
                if (ExtensionGet("title").Length != 1)
                    return string.Empty;

                return (string)ExtensionGet("title")[0];
            }
            set { ExtensionSet("title", value); }
        }
        [DirectoryProperty("st")]
        public string Provincia
        {
            get
            {
                if (ExtensionGet("st").Length != 1)
                    return string.Empty;

                return (string)ExtensionGet("st")[0];
            }
            set { ExtensionSet("st", value); }
        }
        [DirectoryProperty("l")]
        public string Ciudad
        {
            get
            {
                if (ExtensionGet("l").Length != 1)
                    return string.Empty;

                return (string)ExtensionGet("l")[0];
            }
            set { ExtensionSet("l", value); }
        }
        [DirectoryProperty("street")]
        public string Domicilio
        {
            get
            {
                if (ExtensionGet("street").Length != 1)
                    return string.Empty;

                return (string)ExtensionGet("street")[0];
            }
            set { ExtensionSet("street", value); }
        }

        [DirectoryProperty("DNI")]
        public string Dni
        {
            get
            {
                if (ExtensionGet("DNI").Length > 0)
                    return (string)ExtensionGet("DNI")[0];
                /* Probamos las normativas de stj :P */
                if (ExtensionGet("wWWHomePage").Length > 0)
                    return (string)ExtensionGet("wWWHomePage")[0];
                return this.EmployeeId != null ? this.EmployeeId : string.Empty;
            }
            set
            {
                this.EmployeeId = value;
                ExtensionSet("DNI", value);
            }
        }
        #endregion

        public string GetFullName()
        {
            var s = new System.Text.StringBuilder();
            if (this.Nombres != "")
            {
                s.Append(this.Nombres);
                s.Append(" ");
                if (this.Apellidos != "")
                    s.Append(this.Apellidos);
            }
            return s.ToString();
        }

        #region Estaticos
        // Implement the overloaded search method FindByIdentity.
        public static new DirectoryUserPrincipal FindByIdentity(PrincipalContext context, string identityValue)
        {
            return (DirectoryUserPrincipal)FindByIdentityWithType(context, typeof(DirectoryUserPrincipal), identityValue);
        }

        // Implement the overloaded search method FindByIdentity. 
        public static new DirectoryUserPrincipal FindByIdentity(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            return (DirectoryUserPrincipal)FindByIdentityWithType(context, typeof(DirectoryUserPrincipal), identityType, identityValue);
        }

        private static List<string> _ParseOrganizations(DirectoryUserPrincipal principal)
        {
            var values = new List<string>();

            foreach (var token in principal.DistinguishedName.Split(','))
                if (token.IndexOf("OU") == 0)
                    values.Add(token.Substring(3, token.Length - 3));
            return values;
        }
        #endregion
    }
}
