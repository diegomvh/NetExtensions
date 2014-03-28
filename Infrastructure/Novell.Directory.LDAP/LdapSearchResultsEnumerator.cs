using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Novell.Directory.Ldap
{
	public class LdapSearchResultsEnumerator : IEnumerator<LdapEntry>, System.Collections.IEnumerator
	{
		private LdapSearchResults pResults = null;
		private LdapEntry pCurrent = null;

		public LdapSearchResultsEnumerator(LdapSearchResults results)
		{
			pResults = results;
		}

		#region IEnumerator<LdapEntry> Members

		public LdapEntry Current
		{
			get { return pCurrent; }
		}

		#endregion

		#region IEnumerator Members

		object System.Collections.IEnumerator.Current
		{
			get { return pCurrent; }
		}

		public bool MoveNext()
		{
			if (!pResults.hasMore()) {
				return false;
			}

			pCurrent = pResults.next();
			return true;
		}

		public void Reset()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
		}

		#endregion
	}
}
