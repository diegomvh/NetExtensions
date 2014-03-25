using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

namespace Stj.DirectoryServices
{
    using DWORD = System.UInt32;
    using BOOL = System.Int32;

    internal sealed class NameUtility
    {
        private NameUtility() { }

        //we can also use unsafe code here... not sure which is faster or if there
        //are any downsides this particular method of using GCHandle
        internal static string ConvertSidToString(byte[] sid)
        {
            IntPtr pSidString = IntPtr.Zero;
            GCHandle handle = new GCHandle();

            try
            {
                //pin the sid bytes
                handle = GCHandle.Alloc(sid, GCHandleType.Pinned);

                BOOL rc = NativeMethods.ConvertSidToStringSid(handle.AddrOfPinnedObject(), out pSidString);
                Win32.CheckCall(rc);

                return Marshal.PtrToStringAuto(pSidString);
            }
            finally
            {
                if (!Win32.IsNullHandle(pSidString))
                    Win32.LocalFree(pSidString);

                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        /// <summary>
        /// Cracks Active Directory names into various formats.
        /// </summary>
        /// <param name="itemsToConvert"></param>
        /// <param name="hDS"></param>
        /// <param name="formatOffered"></param>
        /// <param name="formatDesired"></param>
        /// <returns></returns>
        internal static string[] DsCrackNamesWrapper(
            string[] itemsToConvert,
            IntPtr hDS,
            DS_NAME_FORMAT formatOffered,
            DS_NAME_FORMAT formatDesired
            )
        {
            if (Win32.IsNullHandle(hDS))
                throw new ArgumentException("Invalid Directory Handle");

            if (itemsToConvert == null || itemsToConvert.Length == 0)
                throw new ArgumentException("No items to convert specified");

            IntPtr pResult = IntPtr.Zero;
            DS_NAME_RESULT_ITEM[] dnri = null;
            Hashtable ht = new Hashtable();

            StringCollection sc = new StringCollection();

            try
            {
                DWORD rc = NativeMethods.DsCrackNames(
                    hDS,
                    DS_NAME_FLAGS.DS_NAME_NO_FLAGS,
                    formatOffered,
                    formatDesired,
                    (uint)itemsToConvert.Length,
                    itemsToConvert,
                    out pResult
                    );

                if (rc != Win32.ERROR_SUCCESS)
                {
                    Win32.SetLastError(rc);
                    Win32.ThrowLastError();
                }

                DS_NAME_RESULT dnr = (DS_NAME_RESULT)Marshal.PtrToStructure(pResult, typeof(DS_NAME_RESULT));

                //define the array with size to match				
                dnri = new DS_NAME_RESULT_ITEM[dnr.cItems];

                //point to our current DS_NAME_RESULT_ITEM structure
                IntPtr pidx = dnr.rItems;

                for (int idx = 0; idx < dnr.cItems; idx++)
                {
                    //marshall back the structure
                    dnri[idx] = (DS_NAME_RESULT_ITEM)Marshal.PtrToStructure(pidx, typeof(DS_NAME_RESULT_ITEM));
                    //update the current pointer idx to next structure
                    pidx = (IntPtr)(pidx.ToInt32() + Marshal.SizeOf(dnri[idx]));
                }

                for (int i = 0; i < dnri.Length; i++)
                {
                    //we will intentionally ignore any that did not resolve
                    //and we will only keep the unique names
                    if (dnri[i].status == 0 && !ht.ContainsKey(dnri[i].pName))
                    {
                        sc.Add(dnri[i].pName);
                        ht.Add(dnri[i].pName, null);
                    }
                }

                string[] names = new string[sc.Count];
                sc.CopyTo(names, 0);

                return names;
            }
            finally
            {
                if (!Win32.IsNullHandle(pResult))
                    NativeMethods.DsFreeNameResult(pResult);
            }
        }

    }
}
