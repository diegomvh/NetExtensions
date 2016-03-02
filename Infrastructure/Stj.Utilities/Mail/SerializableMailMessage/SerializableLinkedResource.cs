using System;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;

namespace Stj.Utilities.Mail
{
    [Serializable]
    public class SerializableLinkedResource
    {
        private SerializableLinkedResource(LinkedResource resource)
        {
            ContentLink = resource.ContentLink;
            ContentId = resource.ContentId;
            resource.ContentStream.Position = 0;

            this.ContentStream = new MemoryStream();
            resource.ContentStream.CopyTo(this.ContentStream);
            resource.ContentStream.Seek(0, SeekOrigin.Begin);
            ContentType = resource.ContentType;
            TransferEncoding = resource.TransferEncoding;

            ContentType = resource.ContentType;
            TransferEncoding = resource.TransferEncoding;
        }

        public Uri ContentLink { get; set; }

        public string ContentId { get; set; }

        public Stream ContentStream { get; }

        public SerializableContentType ContentType { get; set; }

        public TransferEncoding TransferEncoding { get; set; }

        public static implicit operator LinkedResource(SerializableLinkedResource resource)
        {
            if (resource == null)
            return null;

            resource.ContentStream.Seek(0, SeekOrigin.Begin);
            var r = new LinkedResource(resource.ContentStream)
            {
                ContentLink = resource.ContentLink,
                ContentId = resource.ContentId,
                ContentType = resource.ContentType,
                TransferEncoding = resource.TransferEncoding
            };

            return r;
        }

        public static implicit operator SerializableLinkedResource(LinkedResource resource)
            => resource == null ? null : new SerializableLinkedResource(resource);
    }
}