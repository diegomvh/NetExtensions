using System;
using System.Collections.Specialized;

namespace Stj.Utilities.Mail
{
    [Serializable]
    public class SerializableContentType
    {
        private SerializableContentType(System.Net.Mime.ContentType contentType)
        {
            Boundary = contentType.Boundary;
            CharSet = contentType.CharSet;
            MediaType = contentType.MediaType;
            Name = contentType.Name;
            Parameters = new StringDictionary();
            foreach (string k in contentType.Parameters.Keys)
            {
                if (contentType.Parameters.ContainsKey(k) == false)
                    Parameters.Add(k, contentType.Parameters[k]);
            }
        }

        public string Boundary { get; set; }

        public string CharSet { get; set; }

        public string MediaType { get; set; }

        public string Name { get; set; }

        public StringDictionary Parameters { get; }

        public static implicit operator System.Net.Mime.ContentType(SerializableContentType contentType)
        {
            if (contentType == null)
                return null;
            var ct = new System.Net.Mime.ContentType()
            {
                Boundary = contentType.Boundary,
                CharSet = contentType.CharSet,
                MediaType = contentType.MediaType,
                Name = contentType.Name
            };

            foreach (string k in contentType.Parameters.Keys)
                ct.Parameters.Add(k, contentType.Parameters[k]);
            return ct;
        }

        public static implicit operator SerializableContentType(System.Net.Mime.ContentType contentType)
            => contentType == null ? null : new SerializableContentType(contentType);
    }
}