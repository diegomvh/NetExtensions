using System;
using System.Collections.Specialized;
using System.Net.Mail;
using System.Net.Mime;

namespace Stj.Utilities.Mail
{
    [Serializable]
    public class SerializableMailMessage
    {
        private SerializableMailMessage(MailMessage m)
        {
            AlternateViews = new SerializableAlternateViewCollection();
            foreach (var a in m.AlternateViews)
                AlternateViews.Add(a);
            Attachments = new SerializableAttachmentCollection();
            foreach (var a in m.Attachments)
                Attachments.Add(a);
            Bcc = new SerializableMailAddressCollection();
            foreach (var a in m.Bcc)
                Bcc.Add(a);
            Body = m.Body;
            BodyEncoding = m.BodyEncoding;
            //BodyTransferEncoding = m.BodyTransferEncoding;
            CC = new SerializableMailAddressCollection();
            foreach (var a in m.CC)
                CC.Add(a);
            DeliveryNotificationOptions = m.DeliveryNotificationOptions;
            From = m.From;
            Headers = new NameValueCollection();
                Headers.Add(m.Headers);
            HeadersEncoding = m.HeadersEncoding;
            IsBodyHtml = m.IsBodyHtml;
            Priority = m.Priority;
            ReplyTo = m.ReplyTo;
            ReplyToList = new SerializableMailAddressCollection();
            foreach (var a in m.ReplyToList)
                ReplyToList.Add(a);
            Sender = m.Sender;
            Subject = m.Subject;
            SubjectEncoding = m.SubjectEncoding;
            To = new SerializableMailAddressCollection();
            foreach (var a in m.To)
                To.Add(a);
        }

        public SerializableAlternateViewCollection AlternateViews { get; }

        public SerializableAttachmentCollection Attachments { get; }

        public SerializableMailAddressCollection Bcc { get; }

        public string Body { get; set; }

        public System.Text.Encoding BodyEncoding { get; set; }

        public TransferEncoding BodyTransferEncoding { get; }

        public SerializableMailAddressCollection CC { get; }

        public DeliveryNotificationOptions DeliveryNotificationOptions { get; set; }

        public SerializableMailAddress From { get; set; }

        public NameValueCollection Headers { get; }

        public System.Text.Encoding HeadersEncoding { get; set; }

        public bool IsBodyHtml { get; set; }

        public MailPriority Priority { get; set; }

        public SerializableMailAddress ReplyTo { get; set; }

        public SerializableMailAddressCollection ReplyToList { get; }

        public SerializableMailAddress Sender { get; set; }

        public string Subject { get; set; }

        public System.Text.Encoding SubjectEncoding { get; set; }

        public SerializableMailAddressCollection To { get; }

        public static implicit operator MailMessage(SerializableMailMessage message)
        {

            if (message == null)
                return null;
            var m = new MailMessage();
            foreach (var a in message.AlternateViews)
                m.AlternateViews.Add(a);
            foreach (var a in message.Attachments)
                m.Attachments.Add(a);
            foreach (var a in message.Bcc)
                m.Bcc.Add(a);
            m.Body = message.Body;
            m.BodyEncoding = message.BodyEncoding;
            //m.BodyTransferEncoding = message.BodyTransferEncoding;
            foreach (var a in message.CC)
                m.CC.Add(a);
            m.DeliveryNotificationOptions = message.DeliveryNotificationOptions;
            if (message.From != null)
            {
                m.From = message.From;
            }
            m.Headers.Add(message.Headers);
            m.HeadersEncoding = message.HeadersEncoding;
            m.IsBodyHtml = message.IsBodyHtml;
            m.Priority = message.Priority;
            if (message.ReplyTo != null)
            {
                m.ReplyTo = message.ReplyTo;
            }
            foreach (var a in message.ReplyToList)
                m.ReplyToList.Add(a);
            if (message.Sender != null)
            {
                m.Sender = message.Sender;
            }
            m.Subject = message.Subject;
            m.SubjectEncoding = message.SubjectEncoding;
            foreach (var a in message.To)
                m.To.Add(a);
            return m;
        }

        public static implicit operator SerializableMailMessage(MailMessage message) 
            => message == null? null : new SerializableMailMessage(message);
    }
}