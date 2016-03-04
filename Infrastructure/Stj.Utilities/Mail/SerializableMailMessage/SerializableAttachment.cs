using System;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;

namespace Stj.Utilities.Mail
{
    [Serializable]
    public class SerializableAttachment
    {
        private SerializableAttachment(Attachment attachment)
        {
            Name = attachment.Name;
            NameEncoding = attachment.NameEncoding;
            ContentDisposition = attachment.ContentDisposition;
            ContentId = attachment.ContentId;

            //Seek and copy
            attachment.ContentStream.Seek(0, SeekOrigin.Begin);
            this.ContentStream = new MemoryStream();
            attachment.ContentStream.CopyTo(ContentStream);
            attachment.ContentStream.Seek(0, SeekOrigin.Begin);

            ContentType = attachment.ContentType;
            TransferEncoding = attachment.TransferEncoding;
        }

        public SerializableContentDisposition ContentDisposition { get; }
        public string Name { get; set; }
        public System.Text.Encoding NameEncoding { get; set; }

        public string ContentId { get; set; }
        public Stream ContentStream { get; }
        public SerializableContentType ContentType { get; set; }
        public TransferEncoding TransferEncoding { get; set; }

        public static implicit operator Attachment(SerializableAttachment attachment)
        {
            if (attachment == null)
                return null;
            attachment.ContentStream.Seek(0, SeekOrigin.Begin);
            var a = new Attachment(attachment.ContentStream, attachment.Name);
            a.NameEncoding = attachment.NameEncoding;
            a.Name = attachment.Name;
            a.ContentId = attachment.ContentId;
            a.ContentType = attachment.ContentType;
            a.TransferEncoding = attachment.TransferEncoding;
            attachment.ContentDisposition.CopyTo(a.ContentDisposition);
            return a;
        }

        public static implicit operator SerializableAttachment(Attachment attachment)
            => attachment == null ? null : new SerializableAttachment(attachment);
    }
}