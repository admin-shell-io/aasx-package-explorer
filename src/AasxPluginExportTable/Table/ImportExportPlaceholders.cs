/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;

// ReSharper disable PossiblyMistakenUseOfParamsMethod .. issue, even if according to samples of Word API

// Disable dead code detection due to comments such as `//-9- {Referable}.kind`
// dead-csharp off

namespace AasxPluginExportTable.Table
{
    public static class ImportExportPlaceholders
    {
        public static string GetHelp()
        {
            return AdminShellUtil.CleanHereStringWithNewlines(
                @"All placeholders delimited by %{..}%, {} = set arithmetics, [] = optional
                {Referable}.{idShort, category, description[@en..], elementName, elementShort, elementShort2, elementAbbreviation, kind, parent, details}, {Referable|Identifiable} = {SM, SME, CD}, depth, indent}
                Identifiable.id, administration.{ version, revision}}, {Qualifiable}.qualifiers, {Qualifiable}.multiplicity
                {Aas.Reference}, {Aas.Reference}[0..n], {Aas.Reference}[0..n].{type, local, idType, value}, {Aas.Reference} = {semanticId, isCaseOf, unitId}
                SME.value, Property.{value, valueType, valueId}, MultiLanguageProperty.{value, vlaueId}, Range.{valueType, min, max}, Blob.{mimeType, value}, File.{mimeType, value}, ReferenceElement.value, 
                RelationshipElement.{first, second}, SubmodelElementCollection.{value = #elements, ordered, allowDuplicates}, Entity.{entityType, asset}
                CD.{preferredName[@en..], shortName[@en..], unit, unitId, sourceOfDefinition, symbol, dataType, definition[@en..], valueFormat}
                Special: %*% = match any, %stop% = stop if non-empty, %seq={ascii}% = split sequence by char {ascii}, %opt% = optional match
                Commands for header cells include: %{fg,bg,table-bg}={color}% with {color} = {#a030a0, Red, blue, ..}, %halign={left, center, right}%, %valign={top, center, bottom}%,
                %font={bold, italic, underline}, %frame={0,1,2,3}% (only whole table), %colspan={2,3,..}%");
        }
    }
}
