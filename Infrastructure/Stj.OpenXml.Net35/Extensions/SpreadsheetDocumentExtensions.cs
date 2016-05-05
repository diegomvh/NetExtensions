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
    public static class SpreadsheetDocumentExtensions
    {
        public static XDocument ToFlatOpcDocument(this SpreadsheetDocument document)
        {
            return document.ToFlatOpcDocument(new XProcessingInstruction("mso-application", "progid=\"PowerPoint.Show\""));
        }

        public static string ToFlatOpcString(this SpreadsheetDocument document)
        {
            return document.ToFlatOpcDocument().ToString();
        }

        public static SpreadsheetDocument Clone(this SpreadsheetDocument document)
        {
            return document.Clone(new MemoryStream(), true, new OpenSettings());
        }

        public static SpreadsheetDocument Clone(this SpreadsheetDocument document, Stream stream)
        {
            return document.Clone(stream, document.FileOpenAccess == FileAccess.ReadWrite, new OpenSettings());
        }

        public static SpreadsheetDocument Clone(this SpreadsheetDocument document, Stream stream, bool isEditable)
        {
            return document.Clone(stream, isEditable);
        }

        public static SpreadsheetDocument Clone(this SpreadsheetDocument document, Stream stream, bool isEditable, OpenSettings openSettings)
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

        public static SpreadsheetDocument CreateClone(this SpreadsheetDocument document, string path)
        {
            return SpreadsheetDocument.Create(path, document.DocumentType, document.AutoSave);
        }

        public static SpreadsheetDocument CreateClone(this SpreadsheetDocument document, Package package)
        {
            return SpreadsheetDocument.Create(package, document.DocumentType, document.AutoSave);
        }

        internal static SpreadsheetDocument FromFlatOpcDocument(XDocument document)
        {
            return SpreadsheetDocumentExtensions.FromFlatOpcDocument(document, new MemoryStream(), true);
        }

        public static SpreadsheetDocument FromFlatOpcDocument(XDocument document, Stream stream, bool isEditable)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            if (stream == null)
                throw new ArgumentNullException("stream");

            return SpreadsheetDocument.Open(OpenXmlPackageExtensions.FromFlatOpcDocumentCore(document, stream), isEditable);
        }

        public static SpreadsheetDocument FromFlatOpcDocument(XDocument document, string path, bool isEditable)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            if (path == null)
                throw new ArgumentNullException("path");

            return SpreadsheetDocument.Open(OpenXmlPackageExtensions.FromFlatOpcDocumentCore(document, path), isEditable);
        }

        public static SpreadsheetDocument FromFlatOpcDocument(XDocument document, Package package)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            if (package == null)
                throw new ArgumentNullException("package");

            return SpreadsheetDocument.Open(OpenXmlPackageExtensions.FromFlatOpcDocumentCore(document, package));
        }

        public static SpreadsheetDocument CreateClone(this SpreadsheetDocument document, Stream stream)
        {
            return SpreadsheetDocument.Create(stream, document.DocumentType, document.AutoSave);
        }

        public static SpreadsheetDocument OpenClone(this SpreadsheetDocument document, Stream stream, bool isEditable, OpenSettings openSettings)
        {
            return SpreadsheetDocument.Open(stream, isEditable, openSettings);
        }

        internal static SpreadsheetDocument FromFlatOpcString(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            return SpreadsheetDocumentExtensions.FromFlatOpcDocument(XDocument.Parse(text), new MemoryStream(), true);
        }

        internal static SpreadsheetDocument FromFlatOpcString(string text, Stream stream, bool isEditable)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (stream == null)
                throw new ArgumentNullException("stream");

            return SpreadsheetDocumentExtensions.FromFlatOpcDocument(XDocument.Parse(text), stream, isEditable);
        }

        public static SpreadsheetDocument FromFlatOpcString(string text, string path, bool isEditable)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (path == null)
                throw new ArgumentNullException("path");

            return SpreadsheetDocumentExtensions.FromFlatOpcDocument(XDocument.Parse(text), path, isEditable);
        }

        public static SpreadsheetDocument FromFlatOpcString(string text, Package package)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (package == null)
                throw new ArgumentNullException("package");

            return SpreadsheetDocumentExtensions.FromFlatOpcDocument(XDocument.Parse(text), package);
        }

        public static SpreadsheetDocument CreateFromTemplate(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            // Check extensions as the template must have a valid Word Open XML extension.
            string extension = Path.GetExtension(path);
            if (extension != ".xlsx" && extension != ".xlsm" && extension != ".xltx" && extension != ".xltm")
                throw new ArgumentException("Illegal template file: " + path, "path");

            using (SpreadsheetDocument template = SpreadsheetDocument.Open(path, false))
            {
                // We've opened the template in read-only mode to let multiple processes or
                // threads open it without running into problems.
                SpreadsheetDocument document = (SpreadsheetDocument)template.Clone();

                // If the template is a document rather than a template, we are done.
                if (extension == ".xlsx" || extension == ".xlsm")
                    return document;

                // Otherwise, we'll have to do some more work.
                // Firstly, we'll change the document type from Template to Document.
                document.ChangeDocumentType(SpreadsheetDocumentType.Workbook);

                // We are done, so save and return.
                // TODO: Check whether it would be safe to return without saving.
                document.Save();
                return document;
            }
        }
    }
}