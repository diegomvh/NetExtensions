#region Using

using System;
using System.Runtime.InteropServices;
using AZROLESLib;
using Stj.Security.Identity;

#endregion Using

namespace Stj.Security
{
    internal class AzManStore : IDisposable
    {
        //TODO: Rename to AzManContext
        public AzAuthorizationStore Store { get; private set; }
        public IAzApplication Application { get; private set; }
        public Impersonation Impersonation { get; private set; }

        public AzManStore(string applicationName, string connectionString, string connectionUsername = null, string connectionPassword = null, string connectionDomain = null)
        {
            if (connectionUsername != null)
            {
                try
                {
                    Impersonation = Impersonation.LogonUser(connectionDomain, connectionUsername, connectionPassword, LogonType.Interactive);
                }
                catch
                {
                }
            }
            if (string.IsNullOrEmpty(applicationName)) throw new AzManProviderException(Resources.MessageAzManApplicationNameNotSpecified);

            try
            {
                Store = new AzAuthorizationStore();
                Store.Initialize(0, connectionString, null);
                Application = Store.OpenApplication(applicationName, null);
            }
            catch (COMException ex)
            {
                throw new AzManProviderException(Resources.MessageAzManHelperInitializeFailed, ex);
            }
            catch (Exception ex)
            {
                throw new AzManProviderException(string.Format(Resources.MessageAzManInvalidConnectionString, connectionString), ex);
            }
        }

        public void Dispose()
        {
            if (this.Impersonation != null) Impersonation.Dispose();
            if (this.Application == null) return;
            

            Marshal.FinalReleaseComObject(Application);
            Marshal.FinalReleaseComObject(Store);

            Application = null;
            Store = null;
        }
    }
}
