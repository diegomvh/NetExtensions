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

        public static SpreadsheetDocument CreateClone(this SpreadsheetDocument document, string path)
        {
            return SpreadsheetDocument.Create(path, document.DocumentType, document.AutoSave);
        }

        public static SpreadsheetDocument CreateClone(this SpreadsheetDocument document, Package package)
        {
            return SpreadsheetDocument.Create(package, document.DocumentType, document.AutoSave);
        }

        internal static OpenXmlPackage FromFlatOpcDocument(XDocument document)
        {
            throw new NotImplementedException();
        }

        public static SpreadsheetDocument CreateClone(this SpreadsheetDocument document, Stream stream)
        {
            return SpreadsheetDocument.Create(stream, document.DocumentType, document.AutoSave);
        }

        public static SpreadsheetDocument OpenClone(this SpreadsheetDocument document, Stream stream, bool isEditable, OpenSettings openSettings)
        {
            return SpreadsheetDocument.Open(stream, isEditable, openSettings);
        }

        internal static OpenXmlPackage FromFlatOpcString(string text)
        {
            throw new NotImplementedException();
        }
    }
}