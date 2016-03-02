using System;
using System.Collections.ObjectModel;

namespace Stj.Utilities.Mail
{
    [Serializable]
    public class SerializableLinkedResourceCollection : Collection<SerializableLinkedResource>, IDisposable
    {
        public void Dispose()
        {
        }
    }
}