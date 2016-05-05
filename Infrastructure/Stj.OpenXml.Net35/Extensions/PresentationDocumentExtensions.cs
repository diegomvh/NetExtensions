/*
 * WordprocessingDocumentExtensions.cs - Extensions for WordprocessingDocument
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

using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using System.IO;
using System.IO.Packaging;
using System;
using DocumentFormat.OpenXml;

namespace Stj.OpenXml.Extensions
{
    [SuppressMessage("ReSharper", "PossiblyMistakenUseOfParamsMethod")]
    public static class PresentationDocumentExtensions
    {
        public static XDocument ToFlatOpcDocument(this PresentationDocument document)
        {
            return document.ToFlatOpcDocument(new XProcessingInstruction("mso-application", "progid=\"PowerPoint.Show\""));
        }

        public static string ToFlatOpcString(this PresentationDocument document)
        {
            return document.ToFlatOpcDocument().ToString();
        }

        public static PresentationDocument Clone(this PresentationDocument document)
        {
            return document.Clone(new MemoryStream(), true, new OpenSettings());
        }

        public static PresentationDocument Clone(this PresentationDocument document, Stream stream)
        {
            return document.Clone(stream, document.FileOpenAccess == FileAccess.ReadWrite, new OpenSettings());
        }

        public static PresentationDocument Clone(this PresentationDocument document, Stream stream, bool isEditable)
        {
            return document.Clone(stream, isEditable);
        }

        public static PresentationDocument Clone(this PresentationDocument document, Stream stream, bool isEditable, OpenSettings openSettings)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (openSettings == null)
                openSettings = new OpenSettings();

            document.Save();
            using (OpenXmlPackage clone = document.CreateClone(stream))
            {
                foreach (var part in document.Parts)
                    clone.AddPart(part.OpenXmlPart, part.RelationshipId);
            }
            return document.OpenClone(stream, isEditable, openSettings);
        }

        public static PresentationDocument CreateClone(this PresentationDocument document, string path)
        {
            return PresentationDocument.Create(path, document.DocumentType, document.AutoSave);
        }

        public static PresentationDocument CreateClone(this PresentationDocument document, Package package)
        {
            return PresentationDocument.Create(package, document.DocumentType, document.AutoSave);
        }

        public static PresentationDocument CreateClone(this PresentationDocument document, Stream stream)
        {
            return PresentationDocument.Create(stream, document.DocumentType, document.AutoSave);
        }

        internal static PresentationDocument FromFlatOpcDocument(XDocument document)
        {
            return PresentationDocumentExtensions.FromFlatOpcDocument(document, new MemoryStream(), true);
        }


        public static PresentationDocument FromFlatOpcDocument(XDocument document, Stream stream, bool isEditable)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            if (stream == null)
                throw new ArgumentNullException("stream");

            return PresentationDocument.Open(OpenXmlPackageExtensions.FromFlatOpcDocumentCore(document, stream), isEditable);
        }

        public static PresentationDocument FromFlatOpcDocument(XDocument document, string path, bool isEditable)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            if (path == null)
                throw new ArgumentNullException("path");

            return PresentationDocument.Open(OpenXmlPackageExtensions.FromFlatOpcDocumentCore(document, path), isEditable);
        }

        public static PresentationDocument FromFlatOpcDocument(XDocument document, Package package)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            if (package == null)
                throw new ArgumentNullException("package");

            return PresentationDocument.Open(OpenXmlPackageExtensions.FromFlatOpcDocumentCore(document, package));
        }

        public static PresentationDocument OpenClone(this PresentationDocument document, Stream stream, bool isEditable, OpenSettings openSettings)
        {
            return PresentationDocument.Open(stream, isEditable, openSettings);
        }

        internal static PresentationDocument FromFlatOpcString(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            return PresentationDocumentExtensions.FromFlatOpcDocument(XDocument.Parse(text), new MemoryStream(), true);
        }

        internal static PresentationDocument FromFlatOpcString(string text, Stream stream, bool isEditable)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (stream == null)
                throw new ArgumentNullException("stream");

            return PresentationDocumentExtensions.FromFlatOpcDocument(XDocument.Parse(text), stream, isEditable);
        }

        public static PresentationDocument FromFlatOpcString(string text, string path, bool isEditable)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (path == null)
                throw new ArgumentNullException("path");

            return PresentationDocumentExtensions.FromFlatOpcDocument(XDocument.Parse(text), path, isEditable);
        }

        public static PresentationDocument FromFlatOpcString(string text, Package package)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (package == null)
                throw new ArgumentNullException("package");

            return PresentationDocumentExtensions.FromFlatOpcDocument(XDocument.Parse(text), package);
        }

        public static PresentationDocument CreateFromTemplate(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            // Check extensions as the template must have a valid Word Open XML extension.
            string extension = Path.GetExtension(path);
            if (extension != ".pptx" && extension != ".pptm" && extension != ".potx" && extension != ".potm")
                throw new ArgumentException("Illegal template file: " + path, "path");

            using (PresentationDocument template = PresentationDocument.Open(path, false))
            {
                // We've opened the template in read-only mode to let multiple processes or
                // threads open it without running into problems.
                PresentationDocument document = (PresentationDocument)template.Clone();

                // If the template is a document rather than a template, we are done.
                if (extension == ".xlsx" || extension == ".xlsm")
                    return document;

                // Otherwise, we'll have to do some more work.
                document.ChangeDocumentType(PresentationDocumentType.Presentation);

                // We are done, so save and return.
                // TODO: Check whether it would be safe to return without saving.
                document.Save();
                return document;
            }
        }
    }
}