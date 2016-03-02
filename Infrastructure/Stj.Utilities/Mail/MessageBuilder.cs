using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace Stj.Utilities.Mail
{
  /// <summary>
  ///   A helper class for reading mail message data and building a MailMessage
  ///   instance out of it.
  /// </summary>
  internal static class MessageBuilder
  {
    /// <summary>
    ///   Creates a new empty instance of the MailMessage class from a string
    ///   containing a raw mail message header.
    /// </summary>
    /// <param name="text">
    ///   A string containing the mail header to create
    ///   the MailMessage instance from.
    /// </param>
    /// <returns>
    ///   A MailMessage instance with initialized Header fields but
    ///   no content
    /// </returns>
    internal static MailMessage FromHeader(string text)
    {
      var header = ParseMailHeader(text);
      var m = new MailMessage();
      foreach (string key in header)
      {
        var value = header.GetValues(key)[0];
        try
        {
          m.Headers.Add(key, value);
        }
        catch
        {
          // HeaderCollection throws an exception if adding an empty string as
          // value, which can happen, if reading a mail message with an empty
          // subject.
          // Also spammers often forge headers, so just fall through and ignore.
        }
      }
      var ma = Regex.Match(header["Subject"] ?? "", @"=\?([A-Za-z0-9\-_]+)");
      if (ma.Success)
      {
        // encoded-word subject. A subject must not contain any encoded newline
        // characters, so if we find any, we strip them off.
        m.SubjectEncoding = Encoding.GetEncoding(ma.Groups[1].Value);
        try
        {
          m.Subject = Encoding.DecodeWords(header["Subject"]).
            Replace(Environment.NewLine, "");
        }
        catch
        {
          // if, for any reason encoding fails, set the subject to the
          // original, unaltered string.
          m.Subject = header["Subject"];
        }
      }
      else
      {
        m.SubjectEncoding = System.Text.Encoding.ASCII;
        m.Subject = header["Subject"];
      }
      m.Priority = ParsePriority(header["Priority"]);
      SetAddressFields(m, header);
      return m;
    }

    /// <summary>
    ///   Creates a new instance of the MailMessage class from a string
    ///   containing raw RFC822/MIME mail message data.
    /// </summary>
    /// <param name="text">
    ///   A string containing the mail message data to
    ///   create the MailMessage instance from.
    /// </param>
    /// <returns>An initialized instance of the MailMessage class.</returns>
    /// <remarks>
    ///   This is used when fetching entire messages instead
    ///   of the partial-fetch mechanism because it saves redundant
    ///   round-trips to the server.
    /// </remarks>
    internal static MailMessage FromMIME822(string text)
    {
      var reader = new StringReader(text);
      var header = new StringBuilder();
      string line;
      while (!string.IsNullOrEmpty(line = reader.ReadLine()))
        header.AppendLine(line);
      var m = FromHeader(header.ToString());
      var parts = ParseMailBody(reader.ReadToEnd(), m.Headers);
      foreach (var p in parts)
        m.AddBodypart(BodypartFromMIME(p), p.body);
      return m;
    }

    /// <summary>
    ///   Parses the mail header of a mail message and returns it as a
    ///   NameValueCollection.
    /// </summary>
    /// <param name="header">The mail header to parse.</param>
    /// <returns>
    ///   A NameValueCollection containing the header fields as keys
    ///   with their respective values as values.
    /// </returns>
    internal static NameValueCollection ParseMailHeader(string header)
    {
      var reader = new StringReader(header);
      var coll = new NameValueCollection();
      string line, fieldname = null;
      while ((line = reader.ReadLine()) != null)
      {
        if (line == string.Empty)
          continue;
        /* Values may stretch over several lines */
        if (line[0] == ' ' || line[0] == '\t')
        {
          if (fieldname != null)
            coll[fieldname] = coll[fieldname] + line.TrimEnd();
          continue;
        }
        /* The mail header consists of field:value pairs */
        var delimiter = line.IndexOf(':');
        if (delimiter < 0)
          continue;
        fieldname = line.Substring(0, delimiter).Trim();
        var fieldvalue = line.Substring(delimiter + 1).Trim();
        coll.Add(fieldname, fieldvalue);
      }
      return coll;
    }

    /// <summary>
    ///   Parses a MIME header field which can contain multiple 'parameter = value'
    ///   pairs (such as Content-Type: text/html; charset=iso-8859-1).
    /// </summary>
    /// <param name="field">The header field to parse</param>
    /// <returns>
    ///   A NameValueCollection containing the parameter names as keys
    ///   with the respective parameter values as values.
    /// </returns>
    /// <remarks>
    ///   The value of the actual field disregarding the 'parameter = value'
    ///   pairs is stored in the collection under the key "value" (in the above example
    ///   of Content-Type, this would be "text/html").
    /// </remarks>
    private static NameValueCollection ParseMIMEField(string field)
    {
      var coll = new NameValueCollection();
      try
      {
        var matches = Regex.Matches(field, "([\\w\\-]+)=((['\"]).*?\\2|[^;\\n]*)");
        foreach (Match m in matches)
          coll.Add(m.Groups[1].Value, m.Groups[2].Value);
        var mvalue = Regex.Match(field, @"^\s*([\w\/]+)");
        coll.Add("value", mvalue.Success ? mvalue.Groups[1].Value : "");
      }
      catch
      {
        // We don't want this to blow up on the user with weird mails so
        // just return an empty collection.
        coll.Add("value", string.Empty);
      }
      return coll;
    }

    /// <summary>
    ///   Parses a mail header address-list field such as To, Cc and Bcc which
    ///   can contain multiple email addresses.
    /// </summary>
    /// <param name="list">The address-list field to parse</param>
    /// <returns>
    ///   An array of MailAddress objects representing the parsed
    ///   mail addresses.
    /// </returns>
    private static MailAddress[] ParseAddressList(string list)
    {
      var mails = new List<MailAddress>();
      var addr = list.Split(',');
      foreach (var a in addr)
      {
        var m = Regex.Match(a.Trim(),
          @"(.*)\s*<?([A-Z0-9._%-]+@[A-Z0-9.-]+\.[A-Z]{2,4})>?",
          RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
        if (m.Success)
        {
          // The above regex will erroneously match some illegal (very rare)
          // local-parts. RFC-compliant validation is not worth the effort
          // at all, so just wrap this in a try/catch block in case
          // MailAddress' ctor complains.
          try
          {
            mails.Add(new MailAddress(m.Groups[2].Value, m.Groups[1].Value));
          }
          catch
          {
          }
        }
      }
      return mails.ToArray();
    }

    /// <summary>
    ///   Parses a mail message identifier from a string.
    /// </summary>
    /// <param name="field">The field to parse the message id from</param>
    /// <exception cref="ArgumentException">
    ///   Thrown when the field
    ///   argument does not contain a valid message identifier.
    /// </exception>
    /// <returns>The parsed message id</returns>
    /// <remarks>
    ///   A message identifier (msg-id) is a globally unique
    ///   identifier for a message.
    /// </remarks>
    private static string ParseMessageId(string field)
    {
      /* a msg-id is enclosed in < > brackets */
      var m = Regex.Match(field, @"<(.+)>");
      if (m.Success)
        return m.Groups[1].Value;
      throw new ArgumentException("The field does not contain a valid message " +
                                  "identifier: " + field);
    }

    /// <summary>
    ///   Parses the priority of a mail message which can be specified
    ///   as part of the header information.
    /// </summary>
    /// <param name="priority">
    ///   The mail header priority value. The value
    ///   can be null in which case a "normal priority" is returned.
    /// </param>
    /// <returns>
    ///   A value from the MailPriority enumeration corresponding to
    ///   the specified mail priority. If the passed priority value is null
    ///   or invalid, a normal priority is assumed and MailPriority.Normal
    ///   is returned.
    /// </returns>
    private static MailPriority ParsePriority(string priority)
    {
      var Map =
        new Dictionary<string, MailPriority>(StringComparer.OrdinalIgnoreCase)
        {
          {"non-urgent", MailPriority.Low},
          {"normal", MailPriority.Normal},
          {"urgent", MailPriority.High}
        };
      try
      {
        return Map[priority];
      }
      catch
      {
        return MailPriority.Normal;
      }
    }

    /// <summary>
    ///   Sets the address fields (From, To, CC, etc.) of a MailMessage
    ///   object using the specified mail message header information.
    /// </summary>
    /// <param name="m">The MailMessage instance to operate on</param>
    /// <param name="header">A collection of mail and MIME headers</param>
    private static void SetAddressFields(MailMessage m, NameValueCollection header)
    {
      MailAddress[] addr;
      if (header["To"] != null)
      {
        addr = ParseAddressList(header["To"]);
        foreach (var a in addr)
          m.To.Add(a);
      }
      if (header["Cc"] != null)
      {
        addr = ParseAddressList(header["Cc"]);
        foreach (var a in addr)
          m.CC.Add(a);
      }
      if (header["Bcc"] != null)
      {
        addr = ParseAddressList(header["Bcc"]);
        foreach (var a in addr)
          m.Bcc.Add(a);
      }
      if (header["From"] != null)
      {
        addr = ParseAddressList(header["From"]);
        if (addr.Length > 0)
          m.From = addr[0];
      }
      if (header["Sender"] != null)
      {
        addr = ParseAddressList(header["Sender"]);
        if (addr.Length > 0)
          m.Sender = addr[0];
      }
      if (header["Reply-to"] != null)
      {
        addr = ParseAddressList(header["Reply-to"]);
        foreach (var a in addr)
          m.ReplyToList.Add(a);
      }
    }

    /// <summary>
    ///   Adds a body part to an existing MailMessage instance.
    /// </summary>
    /// <param name="message">Extension method for the MailMessage class.</param>
    /// <param name="part">The body part to add to the MailMessage instance.</param>
    /// <param name="content">The content of the body part.</param>
    internal static void AddBodypart(this MailMessage message, Bodypart part, string content)
    {
      var encoding = part.Parameters.ContainsKey("Charset")
        ? Encoding.GetEncoding(part.Parameters["Charset"])
        : System.Text.Encoding.ASCII;
      // decode content if it was encoded
      byte[] bytes;
      try
      {
        switch (part.Encoding)
        {
          case ContentTransferEncoding.QuotedPrintable:
            bytes = encoding.GetBytes(Encoding.QPDecode(content, encoding));
            break;
          case ContentTransferEncoding.Base64:
            bytes = Encoding.Base64Decode(content);
            break;
          default:
            bytes = System.Text.Encoding.ASCII.GetBytes(content);
            break;
        }
      }
      catch
      {
        // If it's not a valid Base64 or quoted-printable encoded string
        // just leave the data as is
        bytes = System.Text.Encoding.ASCII.GetBytes(content);
      }

      // If the MailMessage's Body fields haven't been initialized yet, put it there.
      // Some weird (i.e. spam) mails like to omit content-types so don't check for
      // that here and just assume it's text.
      if (message.Body == string.Empty)
      {
        message.Body = encoding.GetString(bytes);
        message.BodyEncoding = encoding;
        message.IsBodyHtml = part.Subtype.ToLower() == "html";
        return;
      }

      if (part.Disposition.Type == ContentDispositionType.Attachment)
        message.Attachments.Add(CreateAttachment(part, bytes));
      else
        message.AlternateViews.Add(CreateAlternateView(part, bytes));
    }

    /// <summary>
    ///   Creates an instance of the Attachment class used by the MailMessage class
    ///   to store mail message attachments.
    /// </summary>
    /// <param name="part">The MIME body part to create the attachment from.</param>
    /// <param name="bytes">
    ///   An array of bytes composing the content of the
    ///   attachment
    /// </param>
    /// <returns>An initialized instance of the Attachment class</returns>
    private static Attachment CreateAttachment(Bodypart part, byte[] bytes)
    {
      var stream = new MemoryStream(bytes);
      var name = part.Disposition.Filename ?? Path.GetRandomFileName();
      var attachment = new Attachment(stream, name);
      try
      {
        attachment.ContentId = ParseMessageId(part.Id);
      }
      catch
      {
      }
      try
      {
        attachment.ContentType = new System.Net.Mime.ContentType(
          part.Type.ToString().ToLower() + "/" + part.Subtype.ToLower());
      }
      catch
      {
        attachment.ContentType = new System.Net.Mime.ContentType();
      }
      return attachment;
    }

    /// <summary>
    ///   Creates an instance of the AlternateView class used by the MailMessage class
    ///   to store alternate views of the mail message's content.
    /// </summary>
    /// <param name="part">The MIME body part to create the alternate view from.</param>
    /// <param name="bytes">
    ///   An array of bytes composing the content of the
    ///   alternate view
    /// </param>
    /// <returns>An initialized instance of the AlternateView class</returns>
    private static AlternateView CreateAlternateView(Bodypart part, byte[] bytes)
    {
      var stream = new MemoryStream(bytes);
      System.Net.Mime.ContentType contentType;
      try
      {
        contentType = new System.Net.Mime.ContentType(
          part.Type.ToString().ToLower() + "/" + part.Subtype.ToLower());
      }
      catch
      {
        contentType = new System.Net.Mime.ContentType();
      }
      var view = new AlternateView(stream, contentType);
      try
      {
        view.ContentId = ParseMessageId(part.Id);
      }
      catch
      {
      }
      return view;
    }

    /// <summary>
    ///   Parses the body part of a MIME/RFC822 mail message.
    /// </summary>
    /// <param name="body">The body of the mail message.</param>
    /// <param name="header">
    ///   The header of the mail message whose body
    ///   will be parsed.
    /// </param>
    /// <returns>
    ///   An array of initialized MIMEPart instances representing
    ///   the body parts of the mail message.
    /// </returns>
    private static MIMEPart[] ParseMailBody(string body,
      NameValueCollection header)
    {
      var contentType = ParseMIMEField(header["Content-Type"]);
      if (contentType["Boundary"] != null)
      {
        return ParseMIMEParts(new StringReader(body), contentType["Boundary"]);
      }
      return new[]
      {
        new MIMEPart
        {
          body = body,
          header = new NameValueCollection
          {
            {"Content-Type", header["Content-Type"]},
            {"Content-Id", header["Content-Id"]},
            {"Content-Transfer-Encoding", header["Content-Transfer-Encoding"]},
            {"Content-Disposition", header["Content-Disposition"]}
          }
        }
      };
    }

    /// <summary>
    ///   Parses the body of a multipart MIME mail message.
    /// </summary>
    /// <param name="reader">
    ///   An instance of the StringReader class initialized
    ///   with a string containing the body of the mail message.
    /// </param>
    /// <param name="boundary">
    ///   The boundary value as is present as part of
    ///   the Content-Type header field in multipart mail messages.
    /// </param>
    /// <returns>
    ///   An array of initialized MIMEPart instances representing
    ///   the various parts of the MIME mail message.
    /// </returns>
    private static MIMEPart[] ParseMIMEParts(StringReader reader, string boundary)
    {
      var list = new List<MIMEPart>();
      string start = "--" + boundary, end = "--" + boundary + "--", line;
      // Skip everything up to the first boundary
      while ((line = reader.ReadLine()) != null)
      {
        if (line.StartsWith(start))
          break;
      }
      // Read MIME parts delimited by boundary strings
      while (line != null && line.StartsWith(start))
      {
        var p = new MIMEPart();
        // Read the part header
        var header = new StringBuilder();
        while (!string.IsNullOrEmpty(line = reader.ReadLine()))
          header.AppendLine(line);
        p.header = ParseMailHeader(header.ToString());
        // Account for nested multipart content
        var contentType = ParseMIMEField(p.header["Content-Type"]);
        if (contentType["Boundary"] != null)
          list.AddRange(ParseMIMEParts(reader, contentType["boundary"]));
        // Read the part body
        var body = new StringBuilder();
        while ((line = reader.ReadLine()) != null)
        {
          if (line.StartsWith(start))
            break;
          body.AppendLine(line);
        }
        p.body = body.ToString();
        // Add the MIME part to the list unless body is null which means the
        // body contained nested multipart content
        if (!string.IsNullOrEmpty(p.body))
          list.Add(p);
        // If the boundary is actually the end boundary, we're done
        if (line == null || line.StartsWith(end))
          break;
      }
      return list.ToArray();
    }

    /// <summary>
    ///   Glue method to create a bodypart from a MIMEPart instance.
    /// </summary>
    /// <param name="mimePart">
    ///   The MIMEPart instance to create the
    ///   bodypart instance from.
    /// </param>
    /// <returns>An initialized instance of the Bodypart class.</returns>
    private static Bodypart BodypartFromMIME(MIMEPart mimePart)
    {
      var contentType = ParseMIMEField(
        mimePart.header["Content-Type"]);
      var p = new Bodypart(null);
      var m = Regex.Match(contentType["value"], "(.+)/(.+)");
      if (m.Success)
      {
        p.Type = ContentTypeMap.fromString(m.Groups[1].Value);
        p.Subtype = m.Groups[2].Value;
      }
      p.Encoding = ContentTransferEncodingMap.fromString(
        mimePart.header["Content-Transfer-Encoding"]);
      p.Id = mimePart.header["Content-Id"];
      foreach (var k in contentType.AllKeys)
        p.Parameters.Add(k, contentType[k]);
      p.Size = mimePart.body.Length;
      if (mimePart.header["Content-Disposition"] != null)
      {
        var disposition = ParseMIMEField(
          mimePart.header["Content-Disposition"]);
        p.Disposition.Type = ContentDispositionTypeMap.fromString(
          disposition["value"]);
        p.Disposition.Filename = disposition["Filename"];
        foreach (var k in disposition.AllKeys)
          p.Disposition.Attributes.Add(k, disposition[k]);
      }
      return p;
    }
  }
}