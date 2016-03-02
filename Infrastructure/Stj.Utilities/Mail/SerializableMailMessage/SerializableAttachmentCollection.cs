using System;
using System.Collections.ObjectModel;

namespace Stj.Utilities.Mail
{
    [Serializable]
    public class SerializableAttachmentCollection : Collection<SerializableAttachment>, IDisposable
    {
        public void Dispose()
        {
        }
    }
}