using System;
using System.Collections.ObjectModel;

namespace Stj.Utilities.Mail
{
    [Serializable]
    public class SerializableAlternateViewCollection : Collection<SerializableAlternateView>, IDisposable
    {
        public void Dispose()
        {
        }
    }
}