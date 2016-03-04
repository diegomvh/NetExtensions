using System;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;

namespace Stj.Utilities.Mail
{
    [Serializable]
    public class SerializableAlternateView
    {
        private SerializableAlternateView(AlternateView view)
        {
            BaseUri = view.BaseUri;
            LinkedResources = new SerializableLinkedResourceCollection();
            foreach (var res in view.LinkedResources)
            LinkedResources.Add(res);
            ContentId = view.ContentId;

            // Seek and copy
            view.ContentStream.Seek(0, SeekOrigin.Begin);
            this.ContentStream = new MemoryStream();
            view.ContentStream.CopyTo(this.ContentStream);
            view.ContentStream.Seek(0, SeekOrigin.Begin);

            ContentType = view.ContentType;
            TransferEncoding = view.TransferEncoding;
        }

        public Uri BaseUri { get; set; }

        public SerializableLinkedResourceCollection LinkedResources { get; }

        public string ContentId { get; set; }

        public Stream ContentStream { get; }

        public SerializableContentType ContentType { get; set; }

        public TransferEncoding TransferEncoding { get; set; }

        public static implicit operator AlternateView(SerializableAlternateView view)
        {
            if (view == null)
                return null;
            view.ContentStream.Seek(0, SeekOrigin.Begin);
            var v = new AlternateView(view.ContentStream) {BaseUri = view.BaseUri};

            foreach (var res in view.LinkedResources)
                v.LinkedResources.Add(res);
            v.ContentId = view.ContentId;
            v.ContentType = view.ContentType;
            v.TransferEncoding = view.TransferEncoding;
            return v;
        }

        public static implicit operator SerializableAlternateView(AlternateView view)
            => view == null ? null : new SerializableAlternateView(view);
    }
}