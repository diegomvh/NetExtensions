#region Using

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.Security;
using AZROLESLib;
using System.Reflection;

#endregion Using

namespace Stj.Security
{
    public class AzManRoleProvider : RoleProvider
    {

        private const string OperationContextPrefix = "O:";
        private const string StoreLocationTargetToken = "{currentPath}";
        private const string StoreLocationAppBaseToken = "{baseDirectory}";

        #region Properties

        public string ConnectionUsername { get; private set; }
        public string ConnectionPassword { get; private set; }
        public string ConnectionDomain { get; private set; }
        public override string ApplicationName { get; set; }
        public string StoreLocation { get; private set; }
        public string AuditIdentifierPrefix { get; private set; }
        public string ScopeName { get; private set; }

        #endregion Properties

        #region Constructors

        public AzManRoleProvider()
        {
        }

        #endregion Constructors

        #region Methods

        #region Provider

        /// <summary>
        /// Initializes the Provider.
        /// </summary>
        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);

            AuditIdentifierPrefix = config["auditIdentifierPrefix"];
            if (string.IsNullOrEmpty(AuditIdentifierPrefix))
            {
                AuditIdentifierPrefix = "";
            }

            ScopeName = config["scopeName"];
            if (string.IsNullOrEmpty(ScopeName))
            {
                ScopeName = "";
            }

            ApplicationName = config["applicationName"];
            if (string.IsNullOrEmpty(ApplicationName))
            {
                throw new AzManProviderException(Resources.MessageAzManApplicationNameNotSpecified);
            }

            ConnectionUsername = config["connectionUsername"];
            ConnectionPassword = config["connectionPassword"];
            ConnectionDomain = config["connectionDomain"];
            
            // we need the connectionString and it must have a server
            if (String.IsNullOrEmpty(config["connectionStringName"]))
                throw new ProviderException("connectionStringName must be configured");

            string connStringName = config["connectionStringName"];
            config.Remove("connectionStringName");

            var connectionString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new AzManProviderException(Resources.MessageAzManConnectionStringNotSpecified);
            }

            StoreLocation = GetStoreLocationPath(connectionString);
        }  

        /// <summary>
        /// Adds the specified user names to the specified roles for the configured Application Name.
        /// </summary>
        /// <param name="userNames">A string array of user names to be added to the specified roles.</param>
        /// <param name="roleNames">A string array of the role names to add the specified user names to.</param>
        /// <exception cref="ArgumentNullException">If the specified Role Names or User Names are null.</exception>
        /// <exception cref="ArgumentException">If the specified Role Names or User Names are empty.</exception>
        /// <exception cref="ProviderException">If the specified Role Names or User Names already exist for the configured Application Name.</exception>
        /// <exception cref="AzManProviderException">If another exception occurs.</exception>
        public override void AddUsersToRoles(string[] userNames, string[] roleNames)
        {
            CheckArrayParameter(ref roleNames, true, true, true, 0, "roleNames");
            CheckArrayParameter(ref userNames, true, true, true, 0, "userNames");

            var roles = new object[roleNames.Length];
            int i = 0;
            foreach (var roleName in roleNames)
            {
                var role = GetRole(roleName);
                if (role == null)
                {
                    throw new ProviderException(string.Format(Resources.MessageAzManRoleDoesNotExist, roleName));
                }
                roles[i++] = role;
            }

            using (var store = new AzManStore(ApplicationName, StoreLocation, ConnectionUsername, ConnectionPassword, ConnectionDomain))
            {
                try
                {
                    foreach (var userName in userNames)
                    {
                        var user = MembershipHelper.GetUser(userName);
                        if (user == null)
                        {
                            throw new ProviderException(string.Format(Resources.MessageAzManUserDoesNotExist, userName));
                        }
                        var sid = user.ProviderUserKey.ToString();

                        try
                        {
                            foreach (IAzRole role in roles)
                            {
                                role.AddMember(sid, null);
                                role.Submit(0, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new AzManProviderException(Resources.MessageAzManAnExceptionOccured, ex);
                        }
                    }
                }
                finally
                {
                    foreach (var role in roles)
                    {
                        Marshal.FinalReleaseComObject(role);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new role to the data source for the configured Application Name. 
        /// </summary>
        /// <param name="roleName">A valid Role Name.</param>
        /// <exception cref="ArgumentNullException">If the specified Role Name is null.</exception>
        /// <exception cref="ArgumentException">If the specified Role Name is empty.</exception>
        /// <exception cref="ProviderException">If the specified Role Name already exists for the configured Application Name.</exception>
        /// <exception cref="AzManProviderException">If another exception occurs.</exception>
        public override void CreateRole(string roleName)
        {
            CheckParameter(ref roleName, true, true, true, 0, "roleName");

            using (var store = new AzManStore(ApplicationName, StoreLocation, ConnectionUsername, ConnectionPassword, ConnectionDomain))
            {
                if (RoleExists(roleName))
                {
                    throw new ProviderException(string.Format(Resources.MessageAzManRoleAlreadyExists, roleName));
                }

                try
                {
                    var task = store.Application.CreateTask(roleName, null);
                    task.IsRoleDefinition = 1;
                    task.Submit(0, null);
                    var role = store.Application.CreateRole(roleName, null);
                    role.AddTask(roleName, null);
                    role.Submit(0, null);
                }
                catch (Exception ex)
                {
                    throw new AzManProviderException(Resources.MessageAzManAnExceptionOccured, ex);
                }
            }
        }

        /// <summary>
        /// Removes a role from the data source for the configured Application Name.
        /// </summary>
        /// <param name="roleName">The name of the role to delete.</param>
        /// <param name="throwOnPopulatedRole">If true, throw an exception if roleName has one or more members and do not delete roleName.</param>
        /// <returns>true if the role was successfully deleted; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">If the specified Role Name is null.</exception>
        /// <exception cref="ArgumentException">If the specified Role Name is empty.</exception>
        /// <exception cref="ProviderException">If throwOnPopulatedRole true, and the specified Role Name has one or more members.</exception>
        /// <exception cref="AzManProviderException">If another exception occurs.</exception>
        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            CheckParameter(ref roleName, true, true, true, 0, "roleName");

            if (RoleExists(roleName) == false)
            {
                return false;
            }

            if (throwOnPopulatedRole)
            {
                string[] usersInRole;
                try
                {
                    usersInRole = GetUsersInRole(roleName);
                }
                catch
                {
                    return false;
                }
                if (usersInRole.Length != 0)
                {
                    throw new ProviderException(string.Format(Resources.MessageAzManRoleIsNotEmpty, roleName));
                }
            }

            using (var store = new AzManStore(ApplicationName, StoreLocation, ConnectionUsername, ConnectionPassword, ConnectionDomain))
            {
                try
                {
                    store.Application.DeleteTask(roleName, null);
                    store.Application.DeleteRole(roleName, null);
                }
                catch (Exception ex)
                {
                    throw new AzManProviderException(Resources.MessageAzManAnExceptionOccured, ex);
                }
            }

            return true;
        }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        public override string[] FindUsersInRole(string roleName, string userNameToMatch)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a list of all the roles for the configured Application Name.
        /// </summary>
        /// <returns>A string array containing the names of all the roles stored in the data source for the configured Application Name.</returns>
        /// <exception cref="AzManProviderException">If an exception occurs.</exception>
        public override string[] GetAllRoles()
        {
            var list = new string[0];

            using (var store = new AzManStore(ApplicationName, StoreLocation, ConnectionUsername, ConnectionPassword, ConnectionDomain))
            {
                try
                {
                    var roles = store.Application.Roles;
                    list = (from IAzRole role in roles select role.Name).ToArray();
                }
                catch (Exception ex)
                {
                    throw new AzManProviderException(Resources.MessageAzManAnExceptionOccured, ex);
                }
            }

            return list;
        }

        /// <summary>
        /// Returns a list of the roles that a specified user is in for the configured Application Name. 
        /// Returns a string array with no element, if no role exists for the specified user for the configured Application Name.
        /// </summary>
        /// <param name="userName">A valid User Name</param>
        /// <returns>The list of User Roles.</returns>
        /// <exception cref="ArgumentNullException">If the specified User Name is null.</exception>
        /// <exception cref="ProviderException">If the specified User Name does not exist for the configured Application Name.</exception>
        /// <exception cref="AzManProviderException">If another exception occurs.</exception>
        public override string[] GetRolesForUser(string userName)
        {
            this.CheckParameter(ref userName, true, false, true, 0, "userName");
            if (userName.Length < 1)
            {
                return new string[0];
            }
            return GetRolesForUserCore(userName);
        }

        private string[] GetRolesForUserCore(string userName)
        {
            var list = new string[0];

            using (var store = new AzManStore(ApplicationName, StoreLocation, ConnectionUsername, ConnectionPassword, ConnectionDomain))
            {
                try
                {
                    var context = GetClientContext2(store, userName);
                    if (context != null)
                    {
                        var roles = (object[])context.GetRoles(this.ScopeName);
                        list = (from string role in roles select role).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    throw new AzManProviderException(Resources.MessageAzManAnExceptionOccured, ex);
                }
            }

            return list;
        }

        /// <summary>
        /// Gets a list of users in the specified role for the configured Application Name.
        /// </summary>
        /// <param name="roleName">The name of the role to get the list of users for.</param>
        /// <returns>A string array containing the names of all the users who are members of the specified role for the configured Application Name.</returns>
        /// <exception cref="ArgumentNullException">If the specified Role Name is null.</exception>
        /// <exception cref="ArgumentException">If the specified Role Name is empty.</exception>
        /// <exception cref="ProviderException">If the specified Role Name already exists for the configured Application Name.</exception>
        /// <exception cref="AzManProviderException">If another exception occurs.</exception>
        public override string[] GetUsersInRole(string roleName)
        {
            CheckParameter(ref roleName, true, true, true, 0, "roleName");

            var list = new string[0];

            var role = GetRole(roleName);
            if (role == null)
            {
                throw new ProviderException(string.Format(Resources.MessageAzManRoleDoesNotExist, roleName));
            }

            try
            {
                object members = null;
                try
                {
                    members = role.MembersName;
                }
                finally
                {
                    Marshal.FinalReleaseComObject(role);
                }

                list = (from string member in (IEnumerable)members select member).ToArray();
            }
            catch (Exception ex)
            {
                throw new AzManProviderException(Resources.MessageAzManAnExceptionOccured, ex);
            }

            return list;
        }

        /// <summary>
        /// Gets a value indicating whether the specified user is in the specified role for the configured Application Name. 
        /// </summary>
        /// <param name="userName">A valid User Name.</param>
        /// <param name="roleName">A valid Role Name.</param>
        /// <returns>Flag that indicates whether the specified user in the specified role.</returns>
        /// <exception cref="ArgumentNullException">If the specified User Name or Role Name is null.</exception>
        /// <exception cref="ProviderException">If the specified User Name or Role Name does not exist for the configured Application Name.</exception>
        /// <exception cref="AzManProviderException">If another exception occurs.</exception>
        public override bool IsUserInRole(string userName, string roleName)
        {
            CheckParameter(ref userName, true, false, true, 0, "userName");
            if (userName.Length < 1)
            {
                return false;
            }
            CheckParameter(ref roleName, true, true, true, 0, "roleName");
            return IsUserInRoleCore(userName, roleName);
        }

        private bool IsUserInRoleCore(string userName, string roleName)
        {
            if (RoleExists(roleName) == false)
            {
                throw new ProviderException(string.Format(Resources.MessageAzManRoleDoesNotExist, roleName));
            }

            var roles = GetRolesForUser(userName);
            var role = roles.Where(r => r == roleName).FirstOrDefault();

            return (role != null);
        }

        /// <summary>
        /// Removes the specified user names from the specified roles for the configured Application Name.
        /// </summary>
        /// <param name="userNames">A string array of user names to be removed from the specified roles.</param>
        /// <param name="roleNames">A string array of the role names to remove the specified user names from.</param>
        /// <exception cref="ArgumentNullException">If the specified Role Names or User Names are null.</exception>
        /// <exception cref="ArgumentException">If the specified Role Names or User Names are empty.</exception>
        /// <exception cref="ProviderException">If the specified Role Names or User Names already exist for the configured Application Name.</exception>
        /// <exception cref="AzManProviderException">If another exception occurs.</exception>
        public override void RemoveUsersFromRoles(string[] userNames, string[] roleNames)
        {
            CheckArrayParameter(ref roleNames, true, true, true, 0, "roleNames");
            CheckArrayParameter(ref userNames, true, true, true, 0, "userNames");

            var roles = new object[roleNames.Length];
            int i = 0;
            foreach (var roleName in roleNames)
            {
                var role = GetRole(roleName);
                if (role == null)
                {
                    throw new ProviderException(string.Format(Resources.MessageAzManRoleDoesNotExist, roleName));
                }
                roles[i++] = role;
            }

            using (var store = new AzManStore(ApplicationName, StoreLocation, ConnectionUsername, ConnectionPassword, ConnectionDomain))
            {
                try
                {
                    foreach (var userName in userNames)
                    {
                        var user = MembershipHelper.GetUser(userName);
                        if (user == null)
                        {
                            throw new ProviderException(string.Format(Resources.MessageAzManUserDoesNotExist, userName));
                        }
                        var sid = user.ProviderUserKey.ToString();

                        try
                        {
                            foreach (IAzRole role in roles)
                            {
                                if (IsUserInRole(userName, role.Name))
                                {
                                    role.DeleteMember(sid, null);
                                    role.Submit(0, null);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new AzManProviderException(Resources.MessageAzManAnExceptionOccured, ex);
                        }
                    }
                }
                finally
                {
                    foreach (var role in roles)
                    {
                        Marshal.FinalReleaseComObject(role);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the specified role name already exists in the role data source for the configured Application Name.
        /// </summary>
        /// <param name="roleName">The name of the role to search for in the data source.</param>
        /// <returns>true if the role name already exists in the data source for the configured applicationName; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">If the specified Role Name is null.</exception>
        /// <exception cref="ArgumentException">If the specified Role Name is empty.</exception>
        /// <exception cref="AzManProviderException">If another exception occurs.</exception>
        public override bool RoleExists(string roleName)
        {
            CheckParameter(ref roleName, true, true, true, 0, "roleName");

            var success = false;

            object role = null;
            try
            {
                role = GetRole(roleName);
                success = (role != null);
            }
            catch (Exception ex)
            {
                throw new AzManProviderException(Resources.MessageAzManAnExceptionOccured, ex);
            }
            finally
            {
                if (role != null) Marshal.FinalReleaseComObject(role);
            }

            return success;
        }

        /// <summary>
        /// Returns a list of the operations that a specified user is in for the configured Application Name. 
        /// Returns a string array with no element, if no role exists for the specified user for the configured Application Name.
        /// </summary>
        /// <param name="userName">A valid User Name</param>
        /// <returns>The list of User Roles.</returns>
        /// <exception cref="ArgumentNullException">If the specified User Name is null.</exception>
        /// <exception cref="ProviderException">If the specified User Name does not exist for the configured Application Name.</exception>
        /// <exception cref="AzManProviderException">If another exception occurs.</exception>
        public string[] GetOperationsForUser(string userName, Dictionary<string, object> parameters = null)
        {
            CheckParameter(ref userName, true, false, true, 0, "userName");
            if (userName.Length < 1)
            {
                return new string[0];
            }
            return GetOperationsForUserCore(userName, parameters);
        }

        private string[] GetOperationsForUserCore(string userName, Dictionary<string, object> parameters)
        {
            //TODO: Algo mejor para los parameters
            var operations = new List<string>();
            string[] scopes = new string[] { this.ScopeName };

            using (var store = new AzManStore(ApplicationName, StoreLocation, ConnectionUsername, ConnectionPassword, ConnectionDomain ))
            {
                try
                {
                    var context = GetClientContext2(store, userName);
                    if (context != null)
                    {
                        /* Internal Scope */
                        object[] internalScopes = new object[1];
                        internalScopes[0] = (object)scopes[0];

                        /* Internal interfaces */
                        object[] parameterNames = new object[0];
                        object[] parameterValues = new object[0];
                        if (parameters != null)
                        {
                            //Update cache for new parameters
                            store.Store.UpdateCache();
                            parameterNames = parameters.Keys.OrderBy( k => k).ToArray<object>();
                            parameterValues = new object[parameterNames.Length];
                            for (var i = 0; i < parameterNames.Length; i++)
                                parameterValues[i] = parameters[parameterNames[i].ToString()];
                        }

                        object[] operationIds = (from IAzOperation operation in store.Application.Operations select operation.OperationID).Cast<object>().ToArray();

                        object[] results = (object[])context.AccessCheck("GetOperationsForUser:" + userName,
                                                                   internalScopes, operationIds, parameterNames, parameterValues, null, null, null);
                        for (var i = 0; i < results.Length; i++)
                            if ((int)results[i] == 0)
                                operations.Add(store.Application.Operations[i + 1].Name);
                    }
                }
                catch (Exception ex)
                {
                    throw new AzManProviderException(Resources.MessageAzManAnExceptionOccured, ex);
                }
            }

            return operations.ToArray();
        }

        public string[] GetTasksForUser(string userName, Dictionary<string, object> parameters = null)
        {
            CheckParameter(ref userName, true, false, true, 0, "userName");
            if (userName.Length < 1)
            {
                return new string[0];
            }
            return GetTasksForUserCore(userName, parameters);
        }

        private string[] GetTasksForUserCore(string userName, Dictionary<string, object> parameters)
        {
            var tasks = new List<string>();
            string[] scopes = new string[] { this.ScopeName };

            using (var store = new AzManStore(ApplicationName, StoreLocation, ConnectionUsername, ConnectionPassword, ConnectionDomain))
            {
                try
                {
                    var context = GetClientContext2(store, userName);
                    if (context != null)
                    {
                        /* Internal Scope */
                        object[] internalScopes = new object[1];
                        internalScopes[0] = scopes[0];

                        /* Internal interfaces */
                        object[] parameterNames = new object[0];
                        object[] parameterValues = new object[0];
                        if (parameters != null)
                        {
                            //Update cache for new parameters
                            store.Store.UpdateCache();
                            parameterNames = parameters.Keys.OrderBy(k => k).ToArray<object>();
                            parameterValues = new object[parameterNames.Length];
                            for (var i = 0; i < parameterNames.Length; i++)
                                parameterValues[i] = parameters[parameterNames[i].ToString()];
                        }
                        
                        foreach (IAzTask task in store.Application.Tasks)
                        {
                            object[] operationIds = GetTaskOperations(store, new string[] { task.Name });
                            if (operationIds.Length != 0)
                            {
                                object[] results = (object[])context.AccessCheck("GetTasksForUser:" + userName,
                                                                   internalScopes, operationIds, parameterNames, parameterValues, null, null, null);
                                if ((from int result in results select result).All(r => r == 0))
                                    tasks.Add(task.Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new AzManProviderException(Resources.MessageAzManAnExceptionOccured, ex);
                }
            }

            return tasks.ToArray();
        }

        /// <summary>
        /// Gets a value indicating whether the specified user can access the specified operation for the configured Application Name. 
        /// </summary>
        /// <param name="userName">A valid User Name.</param>
        /// <param name="operationName">A valid Operation Name.</param>
        /// <returns>Flag that indicates whether the specified user in the specified operation.</returns>
        /// <exception cref="ArgumentNullException">If the specified User Name or Operation Name is null.</exception>
        /// <exception cref="ArgumentException">If the specified User Name or Operation Name is empty.</exception>
        /// <exception cref="ProviderException">If the specified User Name or Operation Name does not exist for the configured Application Name.</exception>
        /// <exception cref="AzManProviderException">If another exception occurs.</exception>
        public bool CanUserAccessOperation(string userName, string operationName)
        {
            CheckParameter(ref userName, true, false, true, 0, "userName");
            if (userName.Length < 1)
            {
                return false;
            }
            CheckParameter(ref operationName, true, true, true, 0, "operationName");
            return this.CanUserAccessOperationCore(userName, operationName);
        }

        private bool CanUserAccessOperationCore(string userName, string operationName)
        {
            CheckParameter(ref operationName, true, true, true, 0x100, "operationName");

            // TODO: Ensure Operation exists

            var operations = GetOperationsForUser(userName);
            var operation = operations.Where(o => o == operationName).FirstOrDefault();
            return (operation != null);
        }

        public bool CanUserAccessTask(string userName, string taskName)
        {
            CheckParameter(ref userName, true, false, true, 0, "userName");
            if (userName.Length < 1)
            {
                return false;
            }
            CheckParameter(ref taskName, true, true, true, 0, "taskName");
            return this.CanUserAccessTaskCore(userName, taskName);
        }

        private bool CanUserAccessTaskCore(string userName, string taskName)
        {
            CheckParameter(ref taskName, true, true, true, 0x100, "taskName");

            // TODO: Ensure Tasts exists

            var operations = GetTasksForUser(userName);
            var operation = operations.Where(o => o == taskName).FirstOrDefault();
            return (operation != null);
        }

        #region Authorize API
        public bool Authorize(string userName, string context)
        {
            string auditIdentifier = this.AuditIdentifierPrefix + userName + ":" + context;

            bool result = false;
            bool operation = false;
            if (context.IndexOf(OperationContextPrefix) == 0)
            {
                operation = true;
                context = context.Substring(OperationContextPrefix.Length);
            }

            if (operation)
            {
                string[] operations = new string[] { context };
                result = CheckAccessOperations(auditIdentifier, userName, operations);
            }
            else
            {
                string[] tasks = new string[] { context };
                result = CheckAccessTasks(auditIdentifier, userName, tasks);
            }
            
            //TODO: Cache
            return result;
        }

        private bool CheckAccessTasks(string auditIdentifier, string userName, string[] tasks)
        {
            string[] scopes = new string[] { this.ScopeName };

            using (var store = new AzManStore(ApplicationName, StoreLocation, ConnectionUsername, ConnectionPassword, ConnectionDomain))
            {
                try
                {
                    IAzClientContext2 clientCtx = GetClientContext2(store, userName);
                    object[] operationIds = GetTaskOperations(store, tasks);

                    object[] internalScopes = null;
                    if (scopes != null)
                    {
                        internalScopes = new object[1];
                        internalScopes[0] = scopes[0];
                    }

                    object[] result = (object[])clientCtx.AccessCheck(auditIdentifier,
                                                                       internalScopes, operationIds, null, null, null, null, null);
                    foreach (int accessAllowed in result)
                    {
                        if (accessAllowed != 0)
                        {
                            return false;
                        }
                    }
                }
                catch (COMException comEx)
                {
                    throw new AzManProviderException(comEx.Message, comEx);
                }
            }
            return true;
        }

        private object[] GetTaskOperations(AzManStore store, string[] tasks)
        {
            string[] scopes = new string[] { this.ScopeName };
            StringCollection operations = new StringCollection();
            foreach (String task in tasks)
            {
                IAzScope scope = null;
                if ((scopes != null) && (scopes[0].Length > 0))
                {
                    scope = store.Application.OpenScope(scopes[0], null);
                }

                IAzTask azTask = null;
                if (scope != null)
                {
                    azTask = scope.OpenTask(task, null);
                }
                else
                {
                    azTask = store.Application.OpenTask(task, null);
                }

                Array ops = azTask.Operations as Array;
                foreach (String op in ops)
                {
                    operations.Add(op);
                }
            }

            object[] operationIds = new object[operations.Count];
            for (int index = 0; index < operations.Count; index++)
            {
                operationIds[index] = store.Application.OpenOperation(operations[index], null).OperationID;
            }

            return operationIds;
        }

        private bool CheckAccessOperations(string auditIdentifier, string userName, string[] operations)
        {
            string[] scopes = new string[] { this.ScopeName };

            using (var store = new AzManStore(ApplicationName, StoreLocation, ConnectionUsername, ConnectionPassword, ConnectionDomain))
            {
                try
                {
                    IAzClientContext2 clientCtx = GetClientContext2(store, userName);
                    object[] operationIds = new object[operations.Length];
                    for (int index = 0; index < operations.Length; index++)
                    {
                        operationIds[index] = store.Application.OpenOperation(operations[index], null).OperationID;
                    }

                    object[] internalScopes = null;
                    if (scopes != null)
                    {
                        internalScopes = new object[1];
                        internalScopes[0] = scopes[0];
                    }

                    object[] result = (object[])clientCtx.AccessCheck(auditIdentifier,
                                                                       internalScopes, operationIds, null, null, null, null, null);
                    foreach (int accessAllowed in result)
                    {
                        if (accessAllowed != 0)
                        {
                            return false;
                        }
                    }
                }
                catch (COMException comEx)
                {
                    throw new AzManProviderException(comEx.Message, comEx);
                }
            }
            return true;
        }
        #endregion

        public static string GetStoreLocationPath(string storeLocation)
        {
            string store = storeLocation;
            if (store.IndexOf(StoreLocationTargetToken) > -1)
            {
                string dir = Directory.GetCurrentDirectory().Replace(@"\", "/");
                store = store.Replace(StoreLocationTargetToken, dir);
            }
            if (store.IndexOf(StoreLocationAppBaseToken) > -1)
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory.Replace(@"\", "/");
                store = store.Replace(StoreLocationAppBaseToken, dir);
            }

            return store;
        }

        #endregion Provider

        #region Utility

        private void CheckParameter(ref string param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize, string paramName)
        {
            if (param == null)
            {
                if (checkForNull)
                {
                    throw new ArgumentNullException(paramName);
                }
            }
            else
            {
                param = param.Trim();
                if (checkIfEmpty && (param.Length < 1))
                {
                    throw new ArgumentException(string.Format(Resources.MessageAzManParameterIsEmpty, paramName));
                }
                if ((maxSize > 0) && (param.Length > maxSize))
                {
                    throw new ArgumentException(string.Format(Resources.MessageAzManParameterTooLong, paramName, maxSize));
                }
                if (checkForCommas && param.Contains(","))
                {
                    throw new ArgumentException(string.Format(Resources.MessageAzManParameterCannotContainCommas, paramName));
                }

            }
        }

        private void CheckArrayParameter(ref string[] param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize, string paramName)
        {
            if (param == null)
            {
                throw new ArgumentNullException(paramName);
            }
            if (param.Length < 1)
            {
                throw new ArgumentException(string.Format(Resources.MessageAzManParameterArrayCannotBeEmpty, paramName));
            }
            var hashtable = new Hashtable(param.Length);
            for (int i = param.Length - 1; i >= 0; i--)
            {
                CheckParameter(ref param[i], checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName);
                if (hashtable.Contains(param[i]))
                {
                    throw new ArgumentException(string.Format(Resources.MessageAzManParameterArrayCannotContainDuplicates, paramName));
                }
                hashtable.Add(param[i], param[i]);
            }
        }

        private IAzClientContext3 GetClientContext3(AzManStore store, string userName)
        {
            var user = MembershipHelper.GetUser(userName);
            if (user == null)
            {
                throw new ProviderException(string.Format(Resources.MessageAzManUserDoesNotExist, userName)); 
            }
            return (IAzClientContext3)store.Application.InitializeClientContextFromStringSid(user.ProviderUserKey.ToString(), (int)tagAZ_PROP_CONSTANTS.AZ_CLIENT_CONTEXT_SKIP_GROUP, null);
        }

        private IAzClientContext2 GetClientContext2(AzManStore store, string userName)
        {
            var user = MembershipHelper.GetUser(userName);
            if (user == null)
            {
                throw new ProviderException(string.Format(Resources.MessageAzManUserDoesNotExist, userName));
            }
            IAzClientContext2 clientCtx = ((IAzApplication2)store.Application).InitializeClientContext2("Usuario");
            List<object> userSids = new List<object>();
            userSids.Add((object)user.ProviderUserKey.ToString());

            if (user is DirectoryMembershipUser)
            {
                foreach (string GroupSid in ((DirectoryMembershipUser)user).GetGroupSids())
                    userSids.Add((object)GroupSid);
            }
            clientCtx.AddStringSids(userSids.ToArray());
            //Set LDAP QueryDN for adam user. This is needed if LDAP query groups are involved.
            // todo traer el host del usuario por si es de un ad o de un lds
            //clientCtx.LDAPQueryDN = "ldap://" + identity.AuthenticationServer + "/" + identity.DistinguishedName;

            return clientCtx;
        }

        private IAzRole GetRole(string roleName)
        {
            IAzRole role = null;

            using (var store = new AzManStore(ApplicationName, StoreLocation, ConnectionUsername, ConnectionPassword, ConnectionDomain))
            {
                try
                {
                    role = store.Application.OpenRole(roleName, null);
                }
                catch (COMException ex)
                { 
                    // Role does not exist
                    if (ex.ErrorCode == -2147023728) return null;
                    throw;
                }
            }

            return role;
        }

        #endregion Utility

        #endregion Methods

    }
}
