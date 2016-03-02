using System;
using System.IO;
using System.Net.Mail;
using System.Runtime.Serialization.Formatters.Binary;

namespace Stj.Utilities.Mail
{
    public static class MailExtension
    {
        public static void Save(this MailMessage m, Stream stream)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(m.ToMIME822());
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void Store(this MailMessage message, Stream stream)
        {
            var formater = new BinaryFormatter();
            formater.Serialize(stream, (SerializableMailMessage)message);
        }

        public static void Store(this MailMessage m, string name)
        {
            using (var s = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.Read))
                m.Store(s);
        }

        public static void Save(this MailMessage m, string name)
        {
          using (var s = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.Read))
            m.Save(s);
        }

        public static MailMessage Load(Stream stream)
        {
          using (var r = new StreamReader(stream))
          {
            return MessageBuilder.FromMIME822(r.ReadToEnd());
          }
        }

        public static MailMessage Load(string name)
        {
          using (var s = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.Read))
            return Load(s);
        }

        public static MailMessage Restore(Stream stream)
        {
            using (var r = new StreamReader(stream))
                return (SerializableMailMessage)(new BinaryFormatter()).Deserialize(stream);
        }

        public static MailMessage Restore(string name)
        {
            using (var s = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Restore(s);
        }

        public static void SaveAs(this Attachment attachment, string name)
        {
          int count;
          var buffer = new byte[4096];
          var stream = attachment.ContentStream;
          try
          {
            using (var fs = new FileStream(name, FileMode.Create))
            {
              while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                fs.Write(buffer, 0, count);
            }
          }
          catch (Exception e)
          {
            throw new IOException(e.Message, e);
          }
          finally
          {
            if (stream.CanSeek)
              stream.Seek(0, SeekOrigin.Begin);
          }
        }
    }
}