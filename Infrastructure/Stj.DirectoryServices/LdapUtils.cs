using System;
using System.Collections.Generic;
using System.Text;
using System.DirectoryServices;

namespace Stj.DirectoryServices
{
    public sealed class LdapUtils
    {
        /// <summary>
        /// Parse the path name to get the server so we can determine what we are working with.
        /// We need a server for a variety of reasons, but chief amongst them is that it just
        /// doesn't work well without it.
        /// </summary>
        /// <returns></returns>
        public static string GetServerFromAdsPath(string adsPath)
        {
            const int E_ADS_BAD_PATHNAME = -2147463168;

            NativeComInterfaces.IAdsPathname pathCracker =
                (NativeComInterfaces.IAdsPathname)new NativeComInterfaces.Pathname();

            try
            {
                pathCracker.Set(adsPath, NativeComInterfaces.ADS_SETTYPE_FULL);
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                if (ex.ErrorCode == E_ADS_BAD_PATHNAME)
                {
                    throw new InvalidOperationException("Invalid ADS Path Specified");
                }
                throw; //otherwise let it bubble...
            }

            try
            {
                return pathCracker.Retrieve(NativeComInterfaces.ADS_FORMAT_SERVER);
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                if (ex.ErrorCode == E_ADS_BAD_PATHNAME)
                {
                    return null;
                }
                throw; //otherwise let it bubble...
            }

            //could possible parse servername for port info too
        }

        public static string BuildFilterOctetString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.AppendFormat("\\{0}", b.ToString("x2"));
            }
            return sb.ToString();
        }

        public static string GetDefaultADAMPartition(string adsPath, DirectoryEntry rootDSE)
        {
            if (rootDSE.Properties.Contains("defaultNamingContext"))
                return (string)rootDSE.Properties["defaultNamingContext"].Value;

            //sometimes people don't set the defaultnamingcontext
            foreach (string ctx in rootDSE.Properties["namingContexts"])
            {
                if (adsPath.EndsWith(ctx, StringComparison.InvariantCultureIgnoreCase))
                {
                    return ctx;
                }
            }
            return null;
        }

        public static DirectoryEntry CreateDirectoryEntry(
            string path,
            string server,
            ConnectionProtection protection,
            string username,
            string password
            )
        {
            AuthenticationTypes bindingAuth = AuthenticationTypes.Secure;

            switch (protection)
            {
                //typical for non-SSL ADAM - so assuming server specified
                case ConnectionProtection.None:
                    bindingAuth = AuthenticationTypes.None;
                    break;

                //typical for AD
                case ConnectionProtection.Secure:
                    bindingAuth = AuthenticationTypes.Secure | AuthenticationTypes.Sealing | AuthenticationTypes.Signing;
                    break;

                //typical for SSL-ADAM (assuming server specified as well)
                case ConnectionProtection.SecureSocketsLayer:
                    bindingAuth = AuthenticationTypes.SecureSocketsLayer;
                    break;
            }

            if (!String.IsNullOrEmpty(server))
                bindingAuth = bindingAuth | AuthenticationTypes.ServerBind;

            return new DirectoryEntry(
                String.Format("LDAP://{0}{1}", String.IsNullOrEmpty(server) ? String.Empty : server + "/", path),
                username,
                password,
                bindingAuth
                );
        }

        public static DirectoryEntry CreateDirectoryEntry(string path, string server, ConnectionProtection protection)
        {
            return CreateDirectoryEntry(path, server, protection, null, null);
        }

        public static DirectoryEntry CreateDirectoryEntry(string path, string server)
        {
            return CreateDirectoryEntry(path, server, ConnectionProtection.Secure, null, null);
        }

        public static DirectoryEntry CreateDirectoryEntry(string path)
        {
            return CreateDirectoryEntry(path, null, ConnectionProtection.Secure, null, null);
        }

        //detects which ldap server we are using
        public static DirectoryType GetDirectoryType(DirectoryEntry rootDSE)
        {
            const string ADAM_OID = "1.2.840.113556.1.4.1851";
            const string AD_OID = "1.2.840.113556.1.4.800";

            foreach (string s in rootDSE.Properties["supportedCapabilities"])
            {
                if (s == AD_OID)
                    return DirectoryType.AD;

                if (s == ADAM_OID)
                    return DirectoryType.ADAM;
            }
            return DirectoryType.Unknown;
        }

        public static string GetNetbiosDomainName(DirectoryEntry rootDSE, string server, string username, string password)
        {
            string dnc = (string)rootDSE.Properties["defaultNamingContext"].Value;
            string cnc = (string)rootDSE.Properties["configurationNamingContext"].Value;

            DirectoryEntry searchRoot = LdapUtils.CreateDirectoryEntry(
                cnc,
                server,
                ConnectionProtection.Secure,
                username,
                password
                );

            using (searchRoot)
            {
                DirectorySearcher ds = new DirectorySearcher(
                    searchRoot,
                    String.Format("(&(objectCategory=crossRef)(nCName={0}))", dnc),
                    new string[] { "nETBIOSName" },
                    SearchScope.Subtree
                    );

                SearchResult sr = ds.FindOne();
                if (sr != null)
                {
                    if (sr.Properties.Contains("nETBIOSName"))
                    {
                        return sr.Properties["nETBIOSName"][0].ToString();
                    }
                }
            }
            return null;
        }

    }
}
