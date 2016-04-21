/*
 * OpenXmlPackageExtensions.cs - Extensions for OpenXmlPackage
 * 
 * Copyright 2014 Thomas Barnekow
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * Developer: Thomas Barnekow
 * Email: thomas<at/>barnekow<dot/>info
 * 
 * Version: 1.0.01
 */

using System;
using System.Collections;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Packaging;
using System.Xml.Linq;
using System.IO.Packaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Stj.OpenXml.Extensions
{
    /// <summary>
    /// Extensions for <see cref="OpenXmlPackage" />.
    /// </summary>
    public static class OpenXmlPackageExtensions
    {
        private static readonly XNamespace pkg = "http://schemas.microsoft.com/office/2006/xmlPackage";

        /// <summary>
        /// Gets all parts contained in the <see cref="OpenXmlPackage" /> in a
        /// breadth-first fashion, i.e., the direct and indirect relationship
        /// targets of the package (where the <see cref="OpenXmlPartContainer.Parts" />
        /// property only returns the direct relationship targets).
        /// </summary>
        public static IEnumerable<OpenXmlPart> GetAllParts(this OpenXmlPackage package)
        {
            return new OpenXmlParts(package);
        }

        public static void Save(this OpenXmlPackage package)
        {
            package.Package.Flush();
        }

        public static OpenXmlPackage Clone(this OpenXmlPackage package)
        {
            return package.Clone(new MemoryStream(), true, new OpenSettings());
        }

        public static OpenXmlPackage Clone(this OpenXmlPackage package, Stream stream)
        {
            return package.Clone(stream, package.FileOpenAccess == FileAccess.ReadWrite, new OpenSettings());
        }

        public static OpenXmlPackage Clone(this OpenXmlPackage package, Stream stream, bool isEditable)
        {
            return package.Clone(stream, isEditable);
        }

        public static OpenXmlPackage Clone(this OpenXmlPackage package, Stream stream, bool isEditable, OpenSettings openSettings)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (openSettings == null)
                openSettings = new OpenSettings();

            package.Package.Flush();
            using (OpenXmlPackage clone = package.CreateClone(stream))
            {
                foreach (var part in package.Parts)
                    clone.AddPart(part.OpenXmlPart, part.RelationshipId);
            }
            return package.OpenClone(stream, isEditable, openSettings);
        }

        public static OpenXmlPackage CreateClone(this OpenXmlPackage package, string path) { return package; }
        public static OpenXmlPackage CreateClone(this OpenXmlPackage package, Package _package) { return package; }
        public static OpenXmlPackage CreateClone(this OpenXmlPackage package, Stream stream) { return package; }

        public static OpenXmlPackage OpenClone(this OpenXmlPackage package, Stream stream, bool isEditable, OpenSettings openSettings) { return package; }

        public static XDocument ToFlatOpcDocument(this OpenXmlPackage package, XProcessingInstruction instruction)
        {
            package.Package.Flush();

            // Create an XML document with a standalone declaration, processing
            // instruction (if not null), and a package root element with a
            // namespace declaration and one child element for each part.
            return new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                instruction,
                new XElement(
                    pkg + "package",
                    new XAttribute(XNamespace.Xmlns + "pkg", pkg.ToString()),
                    package.Package.GetParts().Select(part => GetContentsAsXml(part))));

        }

        public static XDocument ToFlatOpcDocument(this OpenXmlPackage package)
        {
            return package.ToFlatOpcDocument(new XProcessingInstruction("mso-application", ""));
        }

        public static string ToFlatOpcString(this OpenXmlPackage package) {
            return package.ToFlatOpcDocument().ToString();
        }

        private static XElement GetContentsAsXml(PackagePart part)
        {
            if (part.ContentType.EndsWith("xml"))
            {
                using (Stream stream = part.GetStream())
                using (StreamReader streamReader = new StreamReader(stream))
                using (XmlReader xmlReader = XmlReader.Create(streamReader))
                    return new XElement(pkg + "part",
                        new XAttribute(pkg + "name", part.Uri),
                        new XAttribute(pkg + "contentType", part.ContentType),
                        new XElement(pkg + "xmlData", XElement.Load(xmlReader)));
            }
            else
            {
                using (Stream stream = part.GetStream())
                using (BinaryReader binaryReader = new BinaryReader(stream))
                {
                    int len = (int)binaryReader.BaseStream.Length;
                    byte[] byteArray = binaryReader.ReadBytes(len);

                    // The following expression creates the base64String, then chunks
                    // it to lines of 76 characters long.
                    string base64String = System.Convert.ToBase64String(byteArray)
                        .Select((c, i) => new { Character = c, Chunk = i / 76 })
                        .GroupBy(c => c.Chunk)
                        .Aggregate(
                            new StringBuilder(),
                            (s, i) =>
                                s.Append(
                                    i.Aggregate(
                                        new StringBuilder(),
                                        (seed, it) => seed.Append(it.Character),
                                        sb => sb.ToString())).Append(Environment.NewLine),
                            s => s.ToString());

                    return new XElement(pkg + "part",
                        new XAttribute(pkg + "name", part.Uri),
                        new XAttribute(pkg + "contentType", part.ContentType),
                        new XAttribute(pkg + "compression", "store"),
                        new XElement(pkg + "binaryData", base64String));
                }
            }
        }

        /// <summary>
        /// Replaces the document's contents with the contents of the given replacement's contents.
        /// </summary>
        /// <param name="document">The destination document</param>
        /// <param name="replacement">The source document</param>
        /// <returns>The original document with replaced contents</returns>
        public static OpenXmlPackage ReplaceWith(this OpenXmlPackage document,
            OpenXmlPackage replacement)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            if (replacement == null)
                throw new ArgumentNullException("replacement");

            // Delete all parts (i.e., the direct relationship targets and their
            // children).
            document.DeleteParts(document.GetPartsOfType<OpenXmlPart>());

            // Add the replacement's parts to the document.
            foreach (var part in replacement.Parts)
                document.AddPart(part.OpenXmlPart, part.RelationshipId);

            // Save and return.
            document.Package.Flush();
            return document;
        }
    }

    /// <summary>
    /// Enumeration of all parts contained in an <see cref="OpenXmlPackage" />
    /// rather than just the direct relationship targets.
    /// </summary>
    public class OpenXmlParts : IEnumerable<OpenXmlPart>
    {
        private readonly OpenXmlPackage _package;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the OpenXmlPackagePartIterator class using the supplied OpenXmlPackage class.
        /// </summary>
        /// <param name="package">The OpenXmlPackage to use to enumerate parts</param>
        public OpenXmlParts(OpenXmlPackage package)
        {
            _package = package;
        }

        #endregion

        #region IEnumerable<OpenXmlPart> Members

        /// <summary>
        /// Gets an enumerator for parts in the whole package.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<OpenXmlPart> GetEnumerator()
        {
            var parts = new List<OpenXmlPart>();
            var queue = new Queue<OpenXmlPart>();

            // Enqueue all direct relationship targets.
            foreach (var target in _package.Parts)
            {
                queue.Enqueue(target.OpenXmlPart);
            }

            while (queue.Count > 0)
            {
                // Add next part from queue to the set of parts to be returned.
                var part = queue.Dequeue();
                parts.Add(part);

                // Enqueue all direct relationship targets of current part that
                // are not already enqueued or in the set of parts to be returned.
                foreach (var indirectTarget in part.Parts)
                {
                    if (!queue.Contains(indirectTarget.OpenXmlPart) &&
                        !parts.Contains(indirectTarget.OpenXmlPart))
                    {
                        queue.Enqueue(indirectTarget.OpenXmlPart);
                    }
                }
            }

            // Done.
            return parts.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Gets an enumerator for parts in the whole package.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
