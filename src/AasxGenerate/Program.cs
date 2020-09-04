using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using AasxAmlImExport;
using AasxIntegrationBase.AasForms;
using AasxPredefinedConcepts;
using AdminShellNS;
using Newtonsoft.Json;

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
*/

/*
Many parts of the program became obsolete over time (such as a method <c>CreateSubmodelDocumentation</c>).
The obsoleted code was moved to obsolete/2020-07-20/AasxGenerate/Program.cs for archiving purposes and removed
from this file for readability.
 */

namespace AasxGenerate
{
    public static class Program
    {

        public static AdminShellV20.Submodel CreateSubmodelCad(
            InputFilePrefs prefs, IriIdentifierRepository repo, AdminShellV20.AdministrationShellEnv aasenv)
        {

            // CONCEPTS
            var cdGroup = AdminShellV20.ConceptDescription.CreateNew(
                "CadItem", "IRI", repo.CreateOrRetrieveIri("Example Submodel Cad Item Group"));
            aasenv.ConceptDescriptions.Add(cdGroup);
            cdGroup.SetIEC61360Spec(
                preferredNames: new[] { "DE", "CAD Dateieinheit", "EN", "CAD file item" },
                shortName: "CadItem",
                unit: "",
                definition: new[] {
                    "DE", "Gruppe von Merkmalen, die Zugriff gibt auf eine Datei für ein CAD System.",
                    "EN", "Collection of properties, which make a file for a CAD system accessible." }
            );

            var cdFile = AdminShellV20.ConceptDescription.CreateNew(
                "File", "IRI", repo.CreateOrRetrieveIri("Example Submodel Cad Item File Elem"));
            aasenv.ConceptDescriptions.Add(cdFile);
            cdFile.SetIEC61360Spec(
                preferredNames: new[] { "DE", "Enthaltene CAD Datei", "EN", "Embedded CAD file" },
                shortName: "File",
                unit: "",
                definition: new[] {
                    "DE", "Verweis auf enthaltene CAD Datei.", "EN", "Reference to embedded CAD file." }
            );

            var cdFormat = AdminShellV20.ConceptDescription.CreateNew(
                "FileFormat", AdminShellV20.Identification.IRDI, "0173-1#02-ZAA120#007");
            aasenv.ConceptDescriptions.Add(cdFormat);
            cdFormat.SetIEC61360Spec(
                preferredNames: new[] { "DE", "Filetype CAD", "EN", "Filetype CAD" },
                shortName: "FileFormat",
                unit: "",
                definition: new[] {
                    "DE", "Eindeutige Kennung Format der eingebetteten CAD Datei im eCl@ss Standard.",
                    "EN", "Unambigous ID of format of embedded CAD file in eCl@ss standard." }
            );

            // SUB MODEL
            var sub1 = AdminShellV20.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "CAD";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(
                AdminShellV20.Key.CreateNew(
                    "Submodel", false, "IRI", "http://example.com/id/type/submodel/cad/1/1"));

            // for each cad file in prefs
            int ndx = 0;
            foreach (var fr in prefs.filerecs)
            {

                if (fr.submodel != "cad")
                    continue;
                if (fr.args == null || fr.args.Count != 1)
                    continue;

                ndx++;

                // GROUP
                var propGroup = AdminShellV20.SubmodelElementCollection.CreateNew(
                    $"CadItem{ndx:D2}", "PARAMETER",
                    AdminShellV20.Key.GetFromRef(cdGroup.GetReference()));
                sub1.Add(propGroup);

                // FILE
                var propFile = AdminShellV20.File.CreateNew(
                    "File", "PARAMETER", AdminShellV20.Key.GetFromRef(cdFile.GetReference()));
                propGroup.Add(propFile);
                propFile.mimeType = AdminShellPackageEnv.GuessMimeType(fr.fn);
                propFile.value = "" + fr.targetdir.Trim() + Path.GetFileName(fr.fn);

                // FILEFORMAT
                var propType = AdminShellV20.ReferenceElement.CreateNew(
                    "FileFormat", "PARAMETER", AdminShellV20.Key.GetFromRef(cdFormat.GetReference()));
                propGroup.Add(propType);
                propType.value = AdminShellV20.Reference.CreateNew(
                    AdminShellV20.Key.CreateNew(
                        "GlobalReference", false,
                        AdminShellV20.Identification.IRDI, "" + fr.args[0]));
            }

            return sub1;
        }

        public static AdminShellV20.Submodel CreateSubmodelDocumentationBasedOnVDI2770(
            InputFilePrefs prefs, IriIdentifierRepository repo, AdminShellV20.AdministrationShellEnv aasenv)
        {
            // use pre-definitions
            var preDefLib = new DefinitionsVDI2770();
            var preDefs = new DefinitionsVDI2770.SetOfDefsVDI2770(preDefLib);

            // add concept descriptions
            foreach (var rf in preDefs.GetAllReferables())
                if (rf is AdminShellV20.ConceptDescription)
                    aasenv.ConceptDescriptions.Add(rf as AdminShellV20.ConceptDescription);

            // SUB MODEL
            var sub1 = new AdminShellV20.Submodel(preDefs.SM_VDI2770_Documentation);
            sub1.SetIdentification("IRI", repo.CreateOneTimeId());
            aasenv.Submodels.Add(sub1);

            // execute LAMBDA on different data sources
            Action<int, string[], string, string, string> lambda = (idx, args, fn, url, targetdir) =>
            {
                // Document Item
                var cd = preDefs.CD_VDI2770_Document;
                using (var p0 = AdminShellV20.SubmodelElementCollection.CreateNew($"Document{idx:D2}",
                    "CONSTANT", AdminShellV20.Key.GetFromRef(cd.GetReference())))
                {
                    sub1.Add(p0);

                    // Document itself

                    // DOCUMENT ID
                    cd = preDefs.CD_VDI2770_DocumentId;
                    using (var p = AdminShellV20.Property.CreateNew(
                        cd.GetDefaultPreferredName(), "CONSTANT", AdminShellV20.Key.GetFromRef(cd.GetReference())))
                    {
                        p.valueType = "string";
                        p.value = "" + args.GetHashCode();
                        p0.Add(p);
                    }

                    // Is Primary
                    cd = preDefs.CD_VDI2770_IsPrimaryDocumentId;
                    using (var p = AdminShellV20.Property.CreateNew(
                        cd.GetDefaultPreferredName(), "CONSTANT", AdminShellV20.Key.GetFromRef(cd.GetReference())))
                    {
                        p.valueType = "boolean";
                        p.value = "true";
                        p0.Add(p);
                    }

                    // DOCUMENT CLASS ID
                    cd = preDefs.CD_VDI2770_DocumentClassId;
                    using (var p = AdminShellV20.Property.CreateNew(
                        cd.GetDefaultPreferredName(), "CONSTANT", AdminShellV20.Key.GetFromRef(cd.GetReference())))
                    {
                        p.valueType = "string";
                        p.value = "" + args[0];
                        p.valueId = AdminShellV20.Reference.CreateIrdiReference(args[2]);
                        p0.Add(p);
                    }

                    // DOCUMENT CLASS NAME
                    cd = preDefs.CD_VDI2770_DocumentClassName;
                    using (var p = AdminShellV20.Property.CreateNew(
                        cd.GetDefaultPreferredName(), "CONSTANT", AdminShellV20.Key.GetFromRef(cd.GetReference())))
                    {
                        p.valueType = "string";
                        p.value = "" + args[1];
                        p0.Add(p);
                    }

                    // CLASS SYS
                    cd = preDefs.CD_VDI2770_DocumentClassificationSystem;
                    using (var p = AdminShellV20.Property.CreateNew(
                        cd.GetDefaultPreferredName(), "CONSTANT", AdminShellV20.Key.GetFromRef(cd.GetReference())))
                    {
                        p0.Add(p);
                        p.valueType = "string";
                        p.value = "VDI2770:2018";
                    }

                    // Document version

                    cd = preDefs.CD_VDI2770_DocumentVersion;
                    using (var p1 = AdminShellV20.SubmodelElementCollection.CreateNew($"DocumentVersion01",
                                        "CONSTANT", AdminShellV20.Key.GetFromRef(cd.GetReference())))
                    {
                        p0.Add(p1);

                        // LANGUAGE
                        cd = preDefs.CD_VDI2770_Language;
                        var lngs = args[4].Split(',');
                        for (int i = 0; i < lngs.Length; i++)
                            using (var p = AdminShellV20.Property.CreateNew(
                                cd.GetDefaultPreferredName() + $"{i + 1:00}", "CONSTANT",
                                AdminShellV20.Key.GetFromRef(cd.GetReference())))
                            {
                                p1.Add(p);
                                p.valueType = "string";
                                p.value = "" + lngs[i];
                            }

                        // VERSION
                        cd = preDefs.CD_VDI2770_DocumentVersionId;
                        using (var p = AdminShellV20.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShellV20.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.valueType = "string";
                            p.value = "" + args[5];
                        }

                        // TITLE
                        cd = preDefs.CD_VDI2770_Title;
                        using (var p = AdminShellV20.MultiLanguageProperty.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShellV20.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.value.Add("EN", "" + args[3]);
                            p.value.Add("DE", "Deutsche Übersetzung von: " + args[3]);
                            p.value.Add("FR", "Traduction française de: " + args[3]);
                        }

                        // SUMMARY
                        cd = preDefs.CD_VDI2770_Summary;
                        using (var p = AdminShellV20.MultiLanguageProperty.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShellV20.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.value.Add("EN", "Summary for: " + args[3]);
                            p.value.Add("DE", "Zusammenfassung von: " + args[3]);
                            p.value.Add("FR", "Résumé de: " + args[3]);
                        }

                        // TITLE
                        cd = preDefs.CD_VDI2770_Keywords;
                        using (var p = AdminShellV20.MultiLanguageProperty.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShellV20.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.value.Add("EN", "Keywords for: " + args[3]);
                            p.value.Add("DE", "Stichwörter für: " + args[3]);
                            p.value.Add("FR", "Repèrs par: " + args[3]);
                        }

                        // SET DATE
                        cd = preDefs.CD_VDI2770_Date;
                        using (var p = AdminShellV20.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShellV20.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.valueType = "date";
                            p.value = "" + DateTime.Now.ToString("yyyy-MM-dd");
                        }

                        // STATUS
                        cd = preDefs.CD_VDI2770_StatusValue;
                        using (var p = AdminShellV20.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShellV20.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.valueType = "string";
                            p.value = "Released";
                        }

                        // ROLE
                        cd = preDefs.CD_VDI2770_Role;
                        using (var p = AdminShellV20.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShellV20.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.valueType = "string";
                            p.value = "Author";
                        }

                        // ORGANIZATION
                        cd = preDefs.CD_VDI2770_OrganizationName;
                        using (var p = AdminShellV20.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShellV20.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.valueType = "string";
                            p.value = "Example company";
                        }

                        // ORGANIZATION OFFICIAL
                        cd = preDefs.CD_VDI2770_OrganizationOfficialName;
                        using (var p = AdminShellV20.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShellV20.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.valueType = "string";
                            p.value = "Example company Ltd.";
                        }

                        // DIGITAL FILE
                        if (fn != null && targetdir != null)
                        {
                            // physical file
                            cd = preDefs.CD_VDI2770_DigitalFile;
                            using (var p = AdminShellV20.File.CreateNew(
                                cd.GetDefaultPreferredName(), "CONSTANT",
                                AdminShellV20.Key.GetFromRef(cd.GetReference())))
                            {
                                p1.Add(p);
                                p.mimeType = AdminShellPackageEnv.GuessMimeType(fn);
                                p.value = "" + targetdir.Trim() + Path.GetFileName(fn);
                            }
                        }
                        if (url != null)
                        {
                            // URL
                            cd = preDefs.CD_VDI2770_DigitalFile;
                            using (var p = AdminShellV20.File.CreateNew(
                                cd.GetDefaultPreferredName(), "CONSTANT",
                                AdminShellV20.Key.GetFromRef(cd.GetReference())))
                            {
                                p1.Add(p);
                                p.mimeType = AdminShellPackageEnv.GuessMimeType(url);
                                p.value = "" + url.Trim();
                            }

                        }
                    }
                }
            };

            // for each file one group
            int ndx = 0;
            foreach (var fr in prefs.filerecs)
            {
                if (fr.submodel != "docu")
                    continue;
                if (fr.args == null || fr.args.Count != 6)
                    continue;

                ndx++;
                // (idx, args, fn, url, targetdir)
                lambda(ndx, fr.args.ToArray(), fr.fn, null, fr.targetdir);
            }

            // for each url one group
            foreach (var web in prefs.webrecs)
            {
                if (web.submodel != "docu")
                    continue;
                if (web.args == null || web.args.Count != 6)
                    continue;

                ndx++;
                // (idx, args, fn, url, targetdir)
                lambda(ndx, web.args.ToArray(), null, web.url, null);
            }

            return sub1;
        }

        public static AdminShellV20.Submodel CreateSubmodelDatasheet(
            InputFilePrefs prefs, IriIdentifierRepository repo, AdminShellV20.AdministrationShellEnv aasenv)
        {
            // eClass product group: 19-15-07-01 USB stick

            // SUB MODEL
            var sub1 = AdminShellV20.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "Datatsheet";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(
                AdminShellV20.Key.CreateNew(
                    "Submodel", false, "IRI",
                    "http://example.com/id/type/submodel/datasheet/1/1"));

            // CONCEPT: Manufacturer
            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                "Manufacturer", AdminShellV20.Identification.IRDI, "0173-1#02-AAO677#001"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] { "DE", "TBD", "EN", "Manufacturer name" },
                    shortName: "Manufacturer",
                    definition: new[] { "DE", "TBD",
                    "EN",
                    "legally valid designation of the natural or judicial person which is directly " +
                        "responsible for the design, production, packaging and labeling of a product in respect " +
                        "to its being brought into circulation" }
                );

                var p = AdminShellV20.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShellV20.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.valueType = "string";
                p.value = "Example company Ltd.";
            }

            // CONCEPT: Width
            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                "Width", AdminShellV20.Identification.IRDI, "0173-1#02-BAF016#005"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] { "DE", "Breite", "EN", "Width" },
                    shortName: "Width",
                    unit: "mm",
                    valueFormat: "REAL_MEASURE",
                    definition: new[] {
                        "DE",
                        "bei eher rechtwinkeligen Körpern die orthogonal zu Höhe/Länge/Tiefe stehende Ausdehnung " +
                        "rechtwinklig zur längsten Symmetrieachse",
                        "EN",
                        "for objects with orientation in preferred position during use the dimension " +
                        "perpendicular to height/ length/depth" }
                );

                var p = AdminShellV20.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShellV20.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.valueType = "double";
                p.value = "48";
            }

            // CONCEPT: Height
            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                "Height", AdminShellV20.Identification.IRDI, "0173-1#02-BAA020#008"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] { "DE", "Höhe", "EN", "Height" },
                    shortName: "Height",
                    unit: "mm",
                    valueFormat: "REAL_MEASURE",
                    definition: new[] {
                        "DE",
                        "bei eher rechtwinkeligen Körpern die orthogonal zu Länge/Breite/Tiefe stehende " +
                        "Ausdehnung - bei Gegenständen mit fester Orientierung oder in bevorzugter "+
                        "Gebrauchslage der parallel zur Schwerkraft gemessenen Abstand zwischen Ober- und Unterkante",
                        "EN",
                        "for objects with orientation in preferred position during use the dimension " +
                        "perpendicular to diameter/length/width/depth" }
                );

                var p = AdminShellV20.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShellV20.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.valueType = "double";
                p.value = "56";
            }

            // CONCEPT: Depth
            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                "Depth", AdminShellV20.Identification.IRDI, "0173-1#02-BAB577#007"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] { "DE", "Tiefe", "EN", "Depth" },
                    shortName: "Depth",
                    unit: "mm",
                    valueFormat: "REAL_MEASURE",
                    definition: new[] {
                        "DE",
                        "bei Gegenständen mit fester Orientierung oder in bevorzugter Gebrauchslage wird die " +
                        "nach hinten, im Allgemeinen vom Betrachter weg verlaufende Ausdehnung als Tiefe bezeichnet",
                        "EN",
                        "for objects with fixed orientation or in preferred utilization position, " +
                        "the rear , generally away from the observer expansion is described as depth" }
                );

                var p = AdminShellV20.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShellV20.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.valueType = "double";
                p.value = "11.9";
            }

            // CONCEPT: Weight
            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                "Weight", AdminShellV20.Identification.IRDI, "0173-1#02-AAS627#001"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "DE", "Gewicht der Artikeleinzelverpackung", "EN", "Weight of the individual packaging" },
                    shortName: "Weight",
                    unit: "g",
                    valueFormat: "REAL_MEASURE",
                    definition: new[] { "DE", "Masse der Einzelverpackung eines Artikels",
                    "EN", "Mass of the individual packaging of an article" }
                );

                // as designed
                var p = AdminShellV20.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShellV20.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.AddQualifier("life cycle qual", "SPEC",
                    AdminShellV20.KeyList.CreateNew(
                        "GlobalReference", false, AdminShellV20.Identification.IRDI,
                        "0112/2///61360_4#AAF575"),
                    AdminShellV20.Reference.CreateNew(
                        "GlobalReference", false, AdminShellV20.Identification.IRDI,
                        "0112/2///61360_4#AAF579"));
                p.valueType = "double";
                p.value = "23.1";

                // as produced
                var p2 = AdminShellV20.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShellV20.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p2);
                p2.AddQualifier("life cycle qual", "BUILT",
                    AdminShellV20.KeyList.CreateNew(
                        "GlobalReference", false, AdminShellV20.Identification.IRDI,
                        "0112/2///61360_4#AAF575"),
                    AdminShellV20.Reference.CreateNew(
                        "GlobalReference", false, AdminShellV20.Identification.IRDI,
                        "0112/2///61360_4#AAF573"));
                p2.valueType = "double";
                p2.value = "23.05";
            }

            // CONCEPT: Material
            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                "Material", AdminShellV20.Identification.IRDI, "0173-1#02-BAB577#007"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] { "DE", "Werkstoff", "EN", "Material" },
                    shortName: "Material",
                    definition: new[] { "DE", "TBD",
                    "EN",
                    "Materialzusammensetzung, aus der ein einzelnes Bauteil hergestellt ist, als Ergebnis " +
                    "eines Herstellungsprozesses, in dem der/die Rohstoff(e) durch Extrusion, Verformung, " +
                    "Schweißen usw. in die endgültige Form gebracht werden" }
                );

                var p = AdminShellV20.ReferenceElement.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShellV20.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.value = p.value = AdminShellV20.Reference.CreateNew(
                    AdminShellV20.Key.CreateNew(
                        "GlobalReference", false, AdminShellV20.Identification.IRDI,
                        "0173-1#07-AAA878#004")); // Polyamide (PA)
            }

            // Nice
            return sub1;
        }

        public static AdminShellV20.Submodel CreateSubmodelVariousSingleItems(
            IriIdentifierRepository repo, AdminShellV20.AdministrationShellEnv aasenv)
        {
            // SUB MODEL
            var sub1 = AdminShellV20.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "VariousItems";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(AdminShellV20.Key.CreateNew(
                type: "Submodel",
                local: false,
                idType: "IRI",
                value: "http://example.com/id/type/submodel/various/1/1"));

            AdminShellV20.SubmodelElement sme1, sme2;

            // CONCEPT: MultiLanguageProperty
            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                "DocuName",
                idType: AdminShellV20.Identification.IRDI,                  // immer IRDI für eCl@ss
                id: "0173-1#02-ZZZ991#001"))                             // die ID des Merkmales bei eCl@ss
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "DE", "Name Dokument in Landessprache",    // wechseln Sie die Sprache bei eCl@ss
                        "EN", "Name of document in national language" },   // um die Sprach-Texte aufzufinden
                    shortName: "DocuName",                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: "STRING",                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "DE", "TBD",
                    "EN", "legally valid designation of the natural or judicial person..." }
                );

                var p = AdminShellV20.MultiLanguageProperty.CreateNew(cd.GetDefaultPreferredName(), "PARAMETER",
                            AdminShellV20.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.value.Add("EN", "An english value.");
                p.value.Add("DE", "Ein deutscher Wert.");
                sme1 = p;
            }

            // CONCEPT: Range
            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                "VoltageRange",
                idType: AdminShellV20.Identification.IRDI,                  // immer IRDI für eCl@ss
                id: "0173-1#02-ZZZ992#001"))                             // die ID des Merkmales bei eCl@ss
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "DE", "Betriebsspannungsbereich",    // wechseln Sie die Sprache bei eCl@ss
                        "EN", "Range operational voltage" },   // um die Sprach-Texte aufzufinden
                    shortName: "VoltageRange",                                // kurzer, sprechender Name
                    unit: "V",                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: "REAL",                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "DE", "TBD",
                    "EN", "very precisely limited voltage range..." }
                );

                var p = AdminShellV20.Range.CreateNew(cd.GetDefaultPreferredName(), "PARAMETER",
                            AdminShellV20.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.min = "11.5";
                p.max = "13.8";
                sme2 = p;
            }

            // CONCEPT: AnnotatedRelationship
            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                "VerConn",
                idType: AdminShellV20.Identification.IRDI,  // immer IRDI für eCl@ss
                id: "0173-1#02-XXX992#001"))  // die ID des Merkmales bei eCl@ss
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "DE", "Verbindung",    // wechseln Sie die Sprache bei eCl@ss 
                        "EN", "Connection" },   // um die Sprach-Texte aufzufinden
                    shortName: "VerConn",                                // kurzer, sprechender Name
                    unit: "V",                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: "REAL",                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "DE", "TBD",
                    "EN", "very precisely defined electrical connection..." }
                );

                var ar = AdminShellV20.AnnotatedRelationshipElement.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER",
                    AdminShellV20.Key.GetFromRef(cd.GetReference()));
                sub1.Add(ar);
                ar.first = sme1.GetReference();
                ar.second = sme2.GetReference();

                ar.annotations = new AdminShellV20.DataElementWrapperCollection();
                ar.annotations.Add(sme1);
                ar.annotations.Add(sme2);
            }


            // Nice
            return sub1;
        }

        public static AdminShellV20.Submodel CreateSubmodelBOMforECAD(
            IriIdentifierRepository repo, AdminShellV20.AdministrationShellEnv aasenv)
        {
            // SUB MODEL
            var sub1 = AdminShellV20.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "BOM-ECAD";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(AdminShellV20.Key.CreateNew(
                type: "Submodel",
                local: false,
                idType: "IRI",
                value: "http://example.com/id/type/submodel/BOM/1/1"));

            // CONCEPT: electrical plan

            AdminShellV20.ConceptDescription cdRelEPlan, cdRelElCon, cdContact1, cdContact2;

            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                idType: AdminShellV20.Identification.IRDI,        // immer IRDI für eCl@ss
                id: "0173-1#02-ZZZ993#001",
                idShort: "E-CAD"))                             // die ID des Merkmales bei eCl@ss
            {
                cdRelEPlan = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "EN", "Electrical plan",    // wechseln Sie die Sprache bei eCl@ss
                        "DE", "Stromlaufplan" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.idShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "DE", "TBD",
                    "EN", "very precisely limited language constructs..." }
                );
            }

            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                idType: AdminShellV20.Identification.IRDI,                         // immer IRDI für eCl@ss
                id: "0173-1#02-ZZZ982#001",
                idShort: "single pole connection"))                             // die ID des Merkmales bei eCl@ss
            {
                cdRelElCon = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "EN", "single pole electrical connection",    // wechseln Sie die Sprache bei eCl@ss
                        "DE", "einpolig elektrische Verbindung" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.idShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "DE", "TBD",
                    "EN", "very precisely limited language constructs..." }
                );
            }

            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                idType: AdminShellV20.Identification.IRDI,    // immer IRDI für eCl@ss
                id: "0173-1#02-ZZZ994#001",
                idShort: "1"))                             // die ID des Merkmales bei eCl@ss
            {
                cdContact1 = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "EN", "Contact point 1",    // wechseln Sie die Sprache bei eCl@ss
                        "DE", "Kontaktpunkt 1" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.idShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "DE", "TBD",
                    "EN", "very precisely limited language constructs..." }
                );
            }

            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                idType: AdminShellV20.Identification.IRDI,                // immer IRDI für eCl@ss
                id: "0173-1#02-ZZZ995#001",
                idShort: "2"))                             // die ID des Merkmales bei eCl@ss
            {
                cdContact2 = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "EN", "Contact point 2",    // wechseln Sie die Sprache bei eCl@ss
                        "DE", "Kontaktpunkt 2" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.idShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "DE", "TBD",
                    "EN", "very precisely limited language constructs..." }
                );
            }

            // ENTITIES

            var ps001 = new AdminShellV20.Entity(
                AdminShellV20.Entity.EntityTypeEnum.CoManagedEntity, "PowerSource001");
            sub1.Add(ps001);
            var ps001_1 = AdminShellV20.Property.CreateNew(
                "1", "CONSTANT", cdContact1.GetReference()[0]);
            var ps001_2 = AdminShellV20.Property.CreateNew(
                "2", "CONSTANT", cdContact2.GetReference()[0]);
            ps001.Add(ps001_1);
            ps001.Add(ps001_2);

            var sw001 = new AdminShellV20.Entity(
                AdminShellV20.Entity.EntityTypeEnum.CoManagedEntity, "Switch001");
            sub1.Add(sw001);
            var sw001_1 = AdminShellV20.Property.CreateNew(
                "1", "CONSTANT", cdContact1.GetReference()[0]);
            var sw001_2 = AdminShellV20.Property.CreateNew(
                "2", "CONSTANT", cdContact2.GetReference()[0]);
            sw001.Add(sw001_1);
            sw001.Add(sw001_2);

            var la001 = new AdminShellV20.Entity(
                AdminShellV20.Entity.EntityTypeEnum.SelfManagedEntity, "Lamp001",
                new AdminShellV20.AssetRef(
                    AdminShellV20.Reference.CreateNew(
                        "Asset", false, "IRI", "example.com/assets/23224234234232342343234")));
            sub1.Add(la001);
            var la001_1 = AdminShellV20.Property.CreateNew(
                "1", "CONSTANT", cdContact1.GetReference()[0]);
            var la001_2 = AdminShellV20.Property.CreateNew(
                "2", "CONSTANT", cdContact2.GetReference()[0]);
            la001.Add(la001_1);
            la001.Add(la001_2);

            // RELATIONS

            var smec1 = AdminShellV20.SubmodelElementCollection.CreateNew(
                "E-CAD", semanticIdKey: cdRelEPlan.GetReference()[0]);
            sub1.Add(smec1);

            smec1.Add(AdminShellV20.RelationshipElement.CreateNew(
                "w001", semanticIdKey: cdRelElCon.GetReference()[0],
                first: ps001_1.GetReference(), second: sw001_1.GetReference()));

            smec1.Add(AdminShellV20.RelationshipElement.CreateNew(
                "w002", semanticIdKey: cdRelElCon.GetReference()[0],
                first: sw001_2.GetReference(), second: la001_1.GetReference()));

            smec1.Add(AdminShellV20.RelationshipElement.CreateNew(
                "w003", semanticIdKey: cdRelElCon.GetReference()[0],
                first: la001_2.GetReference(), second: ps001_2.GetReference()));

            // Nice
            return sub1;
        }

        public static AdminShellV20.Submodel CreateSubmodelBOMforAssetStructure(
            IriIdentifierRepository repo, AdminShellV20.AdministrationShellEnv aasenv)
        {
            // SUB MODEL
            var sub1 = AdminShellV20.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "BOM-ASSETS";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(AdminShellV20.Key.CreateNew(
                type: "Submodel",
                local: false,
                idType: "IRI",
                value: "http://example.com/id/type/submodel/BOM/1/1"));

            // CONCEPT: Generic asset decomposition

            AdminShellV20.ConceptDescription cdIsPartOf;

            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                idType: AdminShellV20.Identification.IRDI,                         // immer IRDI für eCl@ss
                id: "0173-1#02-ZZZ998#002",
                idShort: "isPartOf"))                             // die ID des Merkmales bei eCl@ss
            {
                cdIsPartOf = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "EN", "Is part of",    // wechseln Sie die Sprache bei eCl@ss
                        "DE", "Teil von" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.idShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "DE", "TBD",
                    "EN", "very precisely limited language constructs..." }
                );
            }

            // ENTITIES

            var axisGroup = new AdminShellV20.Entity(
                AdminShellV20.Entity.EntityTypeEnum.CoManagedEntity, "AxisGroup001");
            sub1.Add(axisGroup);

            var motor = new AdminShellV20.Entity(
                AdminShellV20.Entity.EntityTypeEnum.CoManagedEntity, "Motor002");
            sub1.Add(motor);

            var encoder = new AdminShellV20.Entity(
                AdminShellV20.Entity.EntityTypeEnum.CoManagedEntity, "Encoder003");
            sub1.Add(encoder);

            var gearbox = new AdminShellV20.Entity(
                AdminShellV20.Entity.EntityTypeEnum.CoManagedEntity, "Gearbox003");
            sub1.Add(gearbox);

            var amp = new AdminShellV20.Entity(
                AdminShellV20.Entity.EntityTypeEnum.CoManagedEntity, "ServoAmplifier004");
            sub1.Add(amp);

            // RELATIONS

            sub1.Add(
                AdminShellV20.RelationshipElement.CreateNew(
                    "rel001", semanticIdKey: cdIsPartOf.GetReference()[0],
                first: axisGroup.GetReference(), second: motor.GetReference()));

            sub1.Add(
                AdminShellV20.RelationshipElement.CreateNew(
                    "rel002", semanticIdKey: cdIsPartOf.GetReference()[0],
                first: axisGroup.GetReference(), second: encoder.GetReference()));

            sub1.Add(
                AdminShellV20.RelationshipElement.CreateNew(
                    "rel003", semanticIdKey: cdIsPartOf.GetReference()[0],
                first: axisGroup.GetReference(), second: gearbox.GetReference()));

            sub1.Add(
                AdminShellV20.RelationshipElement.CreateNew(
                    "rel004", semanticIdKey: cdIsPartOf.GetReference()[0],
                first: axisGroup.GetReference(), second: amp.GetReference()));


            // Nice
            return sub1;
        }

        public static AdminShellV20.Submodel CreateSubmodelEnergyMode(
            IriIdentifierRepository repo, AdminShellV20.AdministrationShellEnv aasenv)
        {
            // SUB MODEL
            var sub1 = AdminShellV20.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "EnergyMode";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(AdminShellV20.Key.CreateNew(
                type: "Submodel",
                local: false,
                idType: "IRI",
                value: "http://example.com/id/type/submodel/energymode/1/1"));

            // CONCEPT: SetMode
            var theOp = new AdminShellV20.Operation();
            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                "SetMode",
                idType: AdminShellV20.Identification.IRDI,
                id: "0173-1#02-AAS999#001"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "DE", "Setze Energiespare-Modus",
                        "EN", "Set energy saving mode" },
                    shortName: "SetMode",
                    definition: new[] { "DE", "Setze Energiemodus 1..4",
                    "EN", "Set energy saving mode 1..4" }
                );

                theOp.idShort = "setmode";
                sub1.Add(theOp);
            }

            // CONCEPT: Mode
            using (var cd = AdminShellV20.ConceptDescription.CreateNew(
                "mode",
                idType: AdminShellV20.Identification.IRDI,
                id: "0173-1#02-AAX777#002"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "DE", "Energiesparemodus-Vorgabe",
                        "EN", "Preset of energy saving mode" },
                    shortName: "mode",
                    valueFormat: "INT",
                    definition: new[] { "DE", "Vorgabe für den Energiesparmodus für optimalen Betrieb",
                    "EN", "Preset in optimal case for the energy saving mode" }
                );

                var p = AdminShellV20.Property.CreateNew(cd.GetDefaultPreferredName(), "PARAMETER",
                            AdminShellV20.Key.GetFromRef(cd.GetReference()));

                var ovp = new AdminShellV20.OperationVariable(p);

                theOp.inputVariable.Add(ovp);
                p.valueType = "int";
            }

            // Nice
            return sub1;
        }


        private static void CreateStochasticViewOnSubmodelsRecurse(
            AdminShellV20.View vw, AdminShellV20.Submodel submodel, AdminShellV20.SubmodelElement sme)
        {
            if (vw == null || sme == null)
                return;

            var isSmc = (sme is AdminShellV20.SubmodelElementCollection);

            // spare out some of the leafs of the tree ..
            if (!isSmc)
                if (Math.Abs(sme.idShort.GetHashCode() % 100) > 50)
                    return;

            // ok, create
            var ce = new AdminShellV20.ContainedElementRef();
            sme.CollectReferencesByParent(ce.Keys);
            vw.AddContainedElement(ce.Keys);
            // recurse
            if (isSmc)
                foreach (var sme2wrap in (sme as AdminShellV20.SubmodelElementCollection).value)
                    CreateStochasticViewOnSubmodelsRecurse(vw, submodel, sme2wrap.submodelElement);
        }

        public static AdminShellV20.View CreateStochasticViewOnSubmodels(AdminShellV20.Submodel[] sms, string idShort)
        {
            // create
            var vw = new AdminShellV20.View();
            vw.idShort = idShort;

            // over all submodel elements
            if (sms != null)
                foreach (var sm in sms)
                {
                    // parent-ize submodel
                    sm.SetAllParents();

                    // loop in
                    if (sm.submodelElements != null)
                        foreach (var sme in sm.submodelElements)
                            CreateStochasticViewOnSubmodelsRecurse(vw, sm, sme.submodelElement);
                }
            // done
            return vw;
        }

        // ReSharper disable ClassNeverInstantiated.Global
        // ReSharper disable CollectionNeverUpdated.Global
        public class InputFilePrefs
        {

            public class FileRec
            {
                public string fn = "";
                public string submodel = "";
                public string targetdir = "";
                public List<string> args = new List<string>();
            }

            public List<FileRec> filerecs = new List<FileRec>();

            public class WebRec
            {
                public string url = "";
                public string submodel = "";
                public List<string> args = new List<string>();
            }

            public List<WebRec> webrecs = new List<WebRec>();

            public FileRec FindFileRecFn(string thefn)
            {
                foreach (var pr in filerecs)
                    if (thefn.ToLower().Trim() == pr.fn.ToLower().Trim())
                        return pr;
                return null;
            }
        }

        // ReSharper enable ClassNeverInstantiated.Global
        // ReSharper enable CollectionNeverUpdated.Global

        public static void Test4()
        {

            // MAKE or LOAD prefs
            InputFilePrefs prefs = new InputFilePrefs();
            var preffn = "prefs.json";
            try
            {
                if (File.Exists(preffn))
                {
                    Log.WriteLine(2, "Opening {0} for reading preferences ..", preffn);
                    var init = File.ReadAllText(preffn);
                    Log.WriteLine(2, "Parsing preferences ..");
                    prefs = JsonConvert.DeserializeObject<InputFilePrefs>(init);
                }
                else
                {
                    Log.WriteLine(2, "Using built-in preferences ..");
                    var init = @"
{ 'filerecs' : [
    { 'fn' : 'data\\thumb-usb.jpeg',                        'submodel' : 'thumb',   'targetdir' : '/',                      'args' : [ ] },
    { 'fn' : 'data\\USB_Hexagon.stp',                       'submodel' : 'cad',     'targetdir' : '/aasx/cad/',             'args' : [ '0173-1#02-ZBQ121#003' ] },
    { 'fn' : 'data\\USB_Hexagon.igs',                       'submodel' : 'cad',     'targetdir' : '/aasx/cad/',             'args' : [ '0173-1#02-ZBQ128#008' ] },
    { 'fn' : 'data\\FES_100500.edz',                        'submodel' : 'cad',     'targetdir' : '/aasx/cad/',             'args' : [ '0173-1#02-ZBQ133#002' ] },
    { 'fn' : 'data\\USB_Hexagon_offen.jpeg',                'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-02', 'Drawings, plans',           '0173-1#02-ZWY722#001', 'Product rendering open',               '',     'V1.2'      ] },
    { 'fn' : 'data\\USB_Hexagon_geschlossen.jpeg',          'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-02', 'Drawings, plans',           '0173-1#02-ZWX723#001', 'Product rendering closed',             '',     'V1.2c'     ] },
    { 'fn' : 'data\\docu_cecc_presales_DE.PDF',             'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-01', 'Technical specification',   '0173-1#02-ZWX723#001', 'Steuerungen CECC',                     'de-DE',   'V2.1.3' ] },
    { 'fn' : 'data\\docu_cecc_presales_EN.PDF',             'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-01', 'Technical specification',   '0173-1#02-ZWX723#001', 'Controls CECC',                        'en-US',   'V2.1.4' ] },
    { 'fn' : 'data\\USB_storage_medium_datasheet_EN.pdf',   'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-01', 'Technical specification',   '0173-1#02-ZWX724#001', 'Data sheet CECC-LK',                   'en-US',   'V1.0'   ] },
    { 'fn' : 'data\\docu_cecc_install_DE.PDF',              'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'Kurzbeschreibung Steuerung CECC-LK',   'de-DE',   'V3.2a'  ] },
    { 'fn' : 'data\\docu_cecc_install_EN.PDF',              'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'Brief description control CECC-LK',    'en-US',   'V3.6b'  ] },
    { 'fn' : 'data\\docu_cecc_fullmanual_DE.PDF',           'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '03-02', 'Operation',                 '0173-1#02-ZWX727#001', 'Beschreibung Steuerung CECC-LK',       'de-DE',   '1403a'  ] },
    { 'fn' : 'data\\docu_cecc_fullmanual_EN.PDF',           'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '03-02', 'Operation',                 '0173-1#02-ZWX727#001', 'Description Steuerung CECC-LK',        'en-US',   '1403a'  ] },
],  'webrecs' : [
    { 'url' : 'https://www.festo.com/net/de_de/SupportPortal/Downloads/385954/407353/CECC_2013-05a_8031104e2.pdf',
                                                            'submodel' : 'docu',                                            'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'Controlador CECC',                     'es',   '2013-05a'  ] },
    { 'url' : 'https://www.festo.com/net/SupportPortal/Files/407352/CECC_2013-05a_8031105x2.pdf',
                                                            'submodel' : 'docu',                                            'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'Controllore CECC',                     'it',   '2013-05a'  ] },
] }";
                    Log.WriteLine(3, "Dump of built-in preferences: {0}", init);
                    Log.WriteLine(2, "Parsing preferences ..");
                    prefs = JsonConvert.DeserializeObject<InputFilePrefs>(init);
                }
            }
            catch (Exception ex)
            {
                Console.Out.Write("While parsing preferences: " + ex.Message);
                Environment.Exit(-1);
            }

            // REPOSITORY
            var repo = new IriIdentifierRepository();
            try
            {
                if (!repo.Load("iri-repository.xml"))
                    repo.InitRepository("iri-repository.xml");
            }
            catch (Exception ex)
            {
                Console.Out.Write("While accessing IRI repository: " + ex.Message);
                Environment.Exit(-1);
            }

            // AAS ENV
            var aasenv1 = new AdminShellV20.AdministrationShellEnv();

            try
            {

                // ASSET
                var asset1 = new AdminShellV20.Asset();
                aasenv1.Assets.Add(asset1);
                asset1.SetIdentification("IRI", "http://exaple.com/3s7plfdrs35", "3s7plfdrs35");
                asset1.AddDescription("EN", "USB Stick");
                asset1.AddDescription("DE", "USB Speichereinheit");

                // CAD
                Log.WriteLine(2, "Creating submodel CAD ..");
                var subCad = CreateSubmodelCad(prefs, repo, aasenv1);

                // DOCU
                Log.WriteLine(2, "Creating submodel DOCU ..");
                var subDocu = CreateSubmodelDocumentationBasedOnVDI2770(prefs, repo, aasenv1);

                // DATASHEET
                Log.WriteLine(2, "Creating submodel DATASHEET ..");
                var subDatasheet = CreateSubmodelDatasheet(prefs, repo, aasenv1);

                // ENERGY
                Log.WriteLine(2, "Creating submodel ENERGY ..");
                var subEng = CreateSubmodelEnergyMode(repo, aasenv1);

                // VIEW1
                var view1 = CreateStochasticViewOnSubmodels(
                    new[] { subCad, subDocu, subDatasheet }, "View1");

                // ADMIN SHELL
                Log.WriteLine(2, "Create AAS ..");
                var aas1 = AdminShellV20.AdministrationShell.CreateNew(
                    "AAS_3s7plfdrs35", "IRI", repo.CreateOneTimeId(), "1", "0");
                aas1.derivedFrom = new AdminShellV20.AssetAdministrationShellRef(
                    new AdminShellV20.Key(
                        "AssetAdministrationShell", false, "IRI",
                        "www.admin-shell.io/aas/sample-series-aas/1/1"));
                aasenv1.AdministrationShells.Add(aas1);
                aas1.assetRef = asset1.GetReference();

                // Link things together
                Log.WriteLine(2, "Linking entities to AAS ..");
                aas1.submodelRefs.Add(subCad.GetReference() as AdminShellV20.SubmodelRef);
                aas1.submodelRefs.Add(subDocu.GetReference() as AdminShellV20.SubmodelRef);
                aas1.submodelRefs.Add(subDatasheet.GetReference() as AdminShellV20.SubmodelRef);
                aas1.submodelRefs.Add(subEng.GetReference() as AdminShellV20.SubmodelRef);
                aas1.AddView(view1);
            }
            catch (Exception ex)
            {
                Console.Out.Write("While building AAS: {0} at {1}", ex.Message, ex.StackTrace);
                Environment.Exit(-1);
            }

            if (true)
            {
                try
                {
                    //
                    // Test serialize
                    // this generates a "sample.xml" is addition to the package below .. for direct usag, e.g.
                    //
                    Log.WriteLine(2, "Test serialize sample.xml ..");
                    using (var s = new StreamWriter("sample.xml"))
                    {
                        var serializer = new XmlSerializer(aasenv1.GetType());
                        var nss = AdminShellPackageEnv.GetXmlDefaultNamespaces();
                        serializer.Serialize(s, aasenv1, nss);
                    }
                }
                catch (Exception ex)
                {
                    Console.Out.Write("While test serializing XML: {0} at {1}", ex.Message, ex.StackTrace);
                    Environment.Exit(-1);
                }
            }

            if (true)
                try
                {
                    //
                    // Test load
                    // (via package function)
                    //
                    Log.WriteLine(2, "Test de-serialize sample.xml ..");
                    var package2 = new AdminShellPackageEnv("sample.xml");
                    package2.Close();
                }
                catch (Exception ex)
                {
                    Console.Out.Write("While test serializing XML: {0} at {1}", ex.Message, ex.StackTrace);
                    Environment.Exit(-1);
                }

            //
            // Try JSON
            //

            // hardcore!
            if (true)
            {
                Log.WriteLine(2, "Test serialize sample.json ..");
                var sw = new StreamWriter("sample.json");
                sw.AutoFlush = true;
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, aasenv1);
                }

                // try to de-serialize
                Log.WriteLine(2, "Test de-serialize sample.json ..");
                using (StreamReader file = File.OpenText("sample.json"))
                {
                    JsonSerializer serializer2 = new JsonSerializer();
                    serializer2.Converters.Add(new AdminShellConverters.JsonAasxConverter());
                    serializer2.Deserialize(file, typeof(AdminShellV20.AdministrationShellEnv));
                }
            }

            // via utilities
#if FALSE
            {
                var package = new AdminShellPackageEnv(aasenv1);
                package.SaveAs("sample.json", writeFreshly: true);
                package.Close();

                var package2 = new AdminShellPackageEnv("sample.json");
                package2.Close();
            }
#endif

            //
            // Make PACKAGE
            //

            if (true)
            {
                try
                {

                    // use the library function
                    var opcfn = "sample-admin-shell.aasx";
                    Log.WriteLine(2, "Creating package {0} ..", opcfn);
                    var package = new AdminShellPackageEnv(aasenv1);

                    // supplementary files
                    Log.WriteLine(2, "Adding supplementary files ..");
                    foreach (var fr in prefs.filerecs)
                    {
                        Log.WriteLine(2, "  + {0}", fr.fn);
                        package.AddSupplementaryFileToStore(
                            fr.fn, fr.targetdir, Path.GetFileName(fr.fn),
                            fr.submodel == "thumb");
                    }

                    // save
                    Log.WriteLine(2, "Saving ..");
                    package.SaveAs(opcfn, writeFreshly: true);

#if FALSE
                    {
                        // Write AML
                        AasxAmlImExport.AmlExport.ExportTo(package, "test.aml", tryUseCompactProperties: true);
                    }
#endif

                    // finalize
                    package.Close();
                }
                catch (Exception ex)
                {
                    Console.Out.Write("While building OPC package: {0} at {1}", ex.Message, ex.StackTrace);
                    Environment.Exit(-1);
                }
            }
        }

        public static AdminShellPackageEnv GeneratePackage(string preffn = "*")
        {

            // MAKE or LOAD prefs
            InputFilePrefs prefs = new InputFilePrefs();
            try
            {
                if (preffn != null && preffn != "*" && File.Exists(preffn))
                {
                    Log.WriteLine(2, "Opening {0} for reading preferences ..", preffn);
                    var init = File.ReadAllText(preffn);
                    Log.WriteLine(2, "Parsing preferences ..");
                    prefs = JsonConvert.DeserializeObject<InputFilePrefs>(init);
                }
                else
                if (preffn == "usb")
                {
                    Log.WriteLine(2, "Using USB stick built-in preferences ..");
                    var init = @"
{ 'filerecs' : [
    { 'fn' : 'data\\thumb-usb.jpeg',                        'submodel' : 'thumb',   'targetdir' : '/',                      'args' : [ ] },
    { 'fn' : 'data\\USB_Hexagon.stp',                       'submodel' : 'cad',     'targetdir' : '/aasx/cad/',             'args' : [ '0173-1#02-ZBQ121#003' ] },
    { 'fn' : 'data\\USB_Hexagon.igs',                       'submodel' : 'cad',     'targetdir' : '/aasx/cad/',             'args' : [ '0173-1#02-ZBQ128#008' ] },
    { 'fn' : 'data\\FES_100500.edz',                        'submodel' : 'cad',     'targetdir' : '/aasx/cad/',             'args' : [ '0173-1#02-ZBQ133#002' ] },
    { 'fn' : 'data\\USB_Hexagon_offen.jpeg',                'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-02', 'Drawings, plans',           '0173-1#02-ZWY722#001', 'Product rendering open',               '',     'V1.2'      ] },
    { 'fn' : 'data\\USB_Hexagon_geschlossen.jpeg',          'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-02', 'Drawings, plans',           '0173-1#02-ZWX723#001', 'Product rendering closed',             '',     'V1.2c'     ] },
    { 'fn' : 'data\\docu_cecc_presales_DE.PDF',             'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-01', 'Technical specification',   '0173-1#02-ZWX723#001', 'Steuerungen CECC',                     'de',   'V2.1.3' ] },
    { 'fn' : 'data\\docu_cecc_presales_EN.PDF',             'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-01', 'Technical specification',   '0173-1#02-ZWX723#001', 'Controls CECC',                        'en',   'V2.1.4' ] },
    { 'fn' : 'data\\USB_storage_medium_datasheet_EN.pdf',   'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-01', 'Technical specification',   '0173-1#02-ZWX724#001', 'Data sheet CECC-LK',                   'en',   'V1.0'   ] },
    { 'fn' : 'data\\docu_cecc_install_DE.PDF',              'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'Kurzbeschreibung Steuerung CECC-LK',   'de',   'V3.2a'  ] },
    { 'fn' : 'data\\docu_cecc_install_EN.PDF',              'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'Brief description control CECC-LK',    'en',   'V3.6b'  ] },
    { 'fn' : 'data\\docu_cecc_fullmanual_DE.PDF',           'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '03-02', 'Operation',                 '0173-1#02-ZWX727#001', 'Beschreibung Steuerung CECC-LK',       'de',   '1403a'  ] },
    { 'fn' : 'data\\docu_cecc_fullmanual_EN.PDF',           'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '03-02', 'Operation',                 '0173-1#02-ZWX727#001', 'Description Steuerung CECC-LK',        'en',   '1403a'  ] },
],  'webrecs' : [
    { 'url' : 'https://www.festo.com/net/de_de/SupportPortal/Downloads/385954/407353/CECC_2013-05a_8031104e2.pdf',
                                                            'submodel' : 'docu',                                            'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'Controlador CECC',                     'es', '2013-05a'  ] },
    { 'url' : 'https://www.festo.com/net/SupportPortal/Files/407352/CECC_2013-05a_8031105x2.pdf',
                                                            'submodel' : 'docu',                                            'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'Controllore CECC',                     'it', '2013-05a'  ] },
] }";

                    Log.WriteLine(3, "Dump of built-in preferences for USB: {0}", init);
                    Log.WriteLine(2, "Parsing preferences ..");
                    prefs = JsonConvert.DeserializeObject<InputFilePrefs>(init);
                }
                else
                {
                    Log.WriteLine(2, "Using built-in default preferences ..");
                    var init = @"
{ 'filerecs' : [
    { 'fn' : 'data\\MotorI40.JPG',                          'submodel' : 'thumb',   'targetdir' : '/',                      'args' : [ ] },
    { 'fn' : 'data\\USB_Hexagon.stp',                       'submodel' : 'cad',     'targetdir' : '/aasx/cad/',             'args' : [ '0173-1#02-ZBQ121#003' ] },
    { 'fn' : 'data\\USB_Hexagon.igs',                       'submodel' : 'cad',     'targetdir' : '/aasx/cad/',             'args' : [ '0173-1#02-ZBQ128#008' ] },
    { 'fn' : 'data\\praxis-projekte.jpg',                   'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-02', 'Drawings, plans',           '0173-1#02-ZWY722#001', 'Overview picture',                     '',     'V1.2'      ] },
    { 'fn' : 'data\\vws-im-detail-praesentation_DE.pdf',    'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-01', 'Technical specification',   '0173-1#02-ZWX723#001', 'VWSiD Präsentation',                   'de',   'V2.1.3' ] },
    { 'fn' : 'data\\vws-in-detail-presentation_EN.pdf',     'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-01', 'Technical specification',   '0173-1#02-ZWX723#001', 'AAS in details Presentation',          'en',   'V2.1.4' ] },
    { 'fn' : 'data\\Starter_VWSiD20_EN.pdf',                'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '02-01', 'Technical specification',   '0173-1#02-ZWX724#001', 'Starter kit AAS',                      'en',   'V1.0'   ] },
    { 'fn' : 'data\\sicherer-bezug-von-cae-daten_DE.pdf',   'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'Sicherer Bezug von CAE Daten',         'de',   'V3.2a'  ] },
    { 'fn' : 'data\\Secure-Retrieval-of-CAE-Data_EN.pdf',   'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'Secure retrieval of CAE data',         'en',   'V3.6b'  ] },
    { 'fn' : 'data\\verwaltungsschale-praxis-flyer_DE.pdf', 'submodel' : 'docu',    'targetdir' : '/aasx/documentation/',   'args' : [ '03-02', 'Operation',                 '0173-1#02-ZWX727#001', 'VWS/ AAS in praxis',                   'de,en',   '1403a'  ] },
],  'webrecs' : [
    { 'url' : 'https://www.plattform-i40.de/PI40/Redaktion/EN/Downloads/Publikation/Details-of-the-Asset-Administration-Shell-Part1.pdf?__blob=publicationFile&v=5',
                                                            'submodel' : 'docu',                                            'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'AAS in details V2.0 specification',    'en', '2013-05a'  ] },
    { 'url' : 'https://www.plattform-i40.de/PI40/Redaktion/EN/Downloads/Publikation/wg3-trilaterale-coop.pdf?__blob=publicationFile&v=4',
                                                            'submodel' : 'docu',                                            'args' : [ '03-04', 'Maintenance, Inspection',   '0173-1#02-ZWX725#001', 'Paris Declaration',                    'fr', '2013-05a'  ] },
] }";

                    Log.WriteLine(3, "Dump of built-in preferences: {0}", init);
                    Log.WriteLine(2, "Parsing preferences ..");
                    prefs = JsonConvert.DeserializeObject<InputFilePrefs>(init);
                }
            }
            catch (Exception ex)
            {
                Console.Out.Write("While parsing preferences: " + ex.Message);
                Environment.Exit(-1);
            }

            // REPOSITORY
            var repo = new IriIdentifierRepository();
            try
            {
                if (!repo.Load("iri-repository.xml"))
                    repo.InitRepository("iri-repository.xml");
            }
            catch (Exception ex)
            {
                Console.Out.Write("While accessing IRI repository: " + ex.Message);
                Environment.Exit(-1);
            }

            // AAS ENV
            var aasenv1 = new AdminShellV20.AdministrationShellEnv();

            try
            {

                // ASSET
                var asset1 = new AdminShellV20.Asset("Asset_3s7plfdrs35");
                aasenv1.Assets.Add(asset1);
                asset1.SetIdentification("IRI", "http://example.com/3s7plfdrs35", "3s7plfdrs35");
                asset1.AddDescription("EN", "USB Stick");
                asset1.AddDescription("DE", "USB Speichereinheit");

                // CAD
                Log.WriteLine(2, "Creating submodel CAD ..");
                var subCad = CreateSubmodelCad(prefs, repo, aasenv1);

                // Test Hashing
                Log.WriteLine(2, "Hash for submodel CAD = " + subCad.ComputeHashcode());
                subCad.category += "!";
                Log.WriteLine(2, "Hash for submodel CAD = " + subCad.ComputeHashcode());

                // DOCU
                Log.WriteLine(2, "Creating submodel DOCU ..");
                var subDocu = CreateSubmodelDocumentationBasedOnVDI2770(prefs, repo, aasenv1);

                // DATASHEET
                Log.WriteLine(2, "Creating submodel DATASHEET ..");
                var subDatasheet = CreateSubmodelDatasheet(prefs, repo, aasenv1);

                // ENERGY
                Log.WriteLine(2, "Creating submodel ENERGY ..");
                var subEng = CreateSubmodelEnergyMode(repo, aasenv1);

                // VARIOUS
                Log.WriteLine(2, "Creating submodel VARIOUS ITEMS ..");
                var subVars = CreateSubmodelVariousSingleItems(repo, aasenv1);

                // BOM
                Log.WriteLine(2, "Creating submodel BOM for ECAD..");
                var subBOM = CreateSubmodelBOMforECAD(repo, aasenv1);

                Log.WriteLine(2, "Creating submodel BOM for Asset Structure..");
                var subBOM2 = CreateSubmodelBOMforAssetStructure(repo, aasenv1);

                // VIEW1
                var view1 = CreateStochasticViewOnSubmodels(
                    new[] { subCad, subDocu, subDatasheet, subVars }, "View1");

                // ADMIN SHELL
                Log.WriteLine(2, "Create AAS ..");
                var aas1 = AdminShellV20.AdministrationShell.CreateNew(
                    "AAS_3s7plfdrs35", "IRI", repo.CreateOneTimeId(), "1", "0");
                aas1.derivedFrom = new AdminShellV20.AssetAdministrationShellRef(
                    new AdminShellV20.Key("AssetAdministrationShell", false, "IRI",
                        "www.admin-shell.io/aas/sample-series-aas/1/1"));
                aasenv1.AdministrationShells.Add(aas1);
                aas1.assetRef = asset1.GetReference();

                // Link things together
                Log.WriteLine(2, "Linking entities to AAS ..");
                aas1.submodelRefs.Add(subCad.GetReference() as AdminShellV20.SubmodelRef);
                aas1.submodelRefs.Add(subDocu.GetReference() as AdminShellV20.SubmodelRef);
                aas1.submodelRefs.Add(subDatasheet.GetReference() as AdminShellV20.SubmodelRef);
                aas1.submodelRefs.Add(subEng.GetReference() as AdminShellV20.SubmodelRef);
                aas1.submodelRefs.Add(subVars.GetReference() as AdminShellV20.SubmodelRef);
                aas1.submodelRefs.Add(subBOM.GetReference() as AdminShellV20.SubmodelRef);
                aas1.submodelRefs.Add(subBOM2.GetReference() as AdminShellV20.SubmodelRef);
                aas1.AddView(view1);
            }
            catch (Exception ex)
            {
                Console.Out.Write("While building AAS: {0} at {1}", ex.Message, ex.StackTrace);
                Environment.Exit(-1);
            }

            //
            // Make PACKAGE
            //

            AdminShellPackageEnv package = null;
            try
            {

                Log.WriteLine(2, "Creating package in RAM ..");
                package = new AdminShellPackageEnv(aasenv1);

                // supplementary files
                Log.WriteLine(2, "Adding supplementary files ..");
                foreach (var fr in prefs.filerecs)
                {
                    Log.WriteLine(2, "  + {0}", fr.fn);
                    package.AddSupplementaryFileToStore(
                        fr.fn, fr.targetdir, Path.GetFileName(fr.fn),
                        fr.submodel == "thumb");
                }

            }
            catch (Exception ex)
            {
                Console.Out.Write("While building OPC package: {0} at {1}", ex.Message, ex.StackTrace);
                Environment.Exit(-1);
            }

            // final
            Log.WriteLine(2, "Returning with package ..");
            return package;

        }

        public static void TestSerialize(AdminShellPackageEnv package)
        {

            var aasenv1 = package.AasEnv;

            if (true)
            {
                try
                {
                    //
                    // Test serialize
                    // this generates a "sample.xml" is addition to the package below .. for direct usag, e.g.
                    //
                    Log.WriteLine(2, "Test serialize sample.xml ..");
                    using (var s = new StreamWriter("sample.xml"))
                    {
                        var serializer = new XmlSerializer(aasenv1.GetType());
                        var nss = AdminShellPackageEnv.GetXmlDefaultNamespaces();
                        serializer.Serialize(s, aasenv1, nss);
                    }
                }
                catch (Exception ex)
                {
                    Console.Out.Write("While test serializing XML: {0} at {1}", ex.Message, ex.StackTrace);
                    Environment.Exit(-1);
                }
            }

            if (true)
                try
                {
                    //
                    // Test load
                    // (via package function)
                    //
                    Log.WriteLine(2, "Test de-serialize sample.xml ..");
                    var package2 = new AdminShellPackageEnv("sample.xml");
                    package2.Close();
                }
                catch (Exception ex)
                {
                    Console.Out.Write("While test serializing XML: {0} at {1}", ex.Message, ex.StackTrace);
                    Environment.Exit(-1);
                }

            //
            // Try JSON
            //

            // hardcore!
            if (true)
            {
                Log.WriteLine(2, "Test serialize sample.json ..");
                var sw = new StreamWriter("sample.json");
                sw.AutoFlush = true;
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, aasenv1);
                }

                // try to de-serialize
                Log.WriteLine(2, "Test de-serialize sample.json ..");
                using (StreamReader file = File.OpenText("sample.json"))
                {
                    JsonSerializer serializer2 = new JsonSerializer();
                    serializer2.Converters.Add(new AdminShellConverters.JsonAasxConverter());
                    serializer2.Deserialize(file, typeof(AdminShellV20.AdministrationShellEnv));
                }
            }

            // via utilities
#if FALSE
            {
                var package2 = new AdminShellPackageEnv(aasenv1);
                package2.SaveAs("sample.json", writeFreshly: true);
                package2.Close();

                var package3 = new AdminShellPackageEnv("sample.json");
                package3.Close();
            }
#endif
        }

        static void Validate(string fn)
        {
            // see: AasxCsharpLibrary.Tests/TestLoadSave.cs

            // Load the schema

            var xmlSchemaSet = new System.Xml.Schema.XmlSchemaSet();
            xmlSchemaSet.XmlResolver = new System.Xml.XmlUrlResolver();

            string schemaPath = ".\\schemaV201\\AAS.xsd";
            xmlSchemaSet.Add(null, schemaPath);

            var schemaMessages = new List<string>();
            xmlSchemaSet.ValidationEventHandler +=
                (object sender, System.Xml.Schema.ValidationEventArgs e) => { schemaMessages.Add(e.Message); };
            xmlSchemaSet.Compile();

            if (schemaMessages.Count > 0)
            {
                var parts = new List<string> { $"Failed to compile the schema: {schemaPath}" };
                parts.AddRange(schemaMessages);

                throw new InvalidOperationException(string.Join(Environment.NewLine, parts));
            }

            // Load-Save-Validate
            var settings = new System.Xml.XmlReaderSettings();
            settings.ValidationType = System.Xml.ValidationType.Schema;
            settings.Schemas = xmlSchemaSet;

            var messages = new List<string>();
            settings.ValidationEventHandler +=
                (object sender, System.Xml.Schema.ValidationEventArgs e) =>
                {
                    messages.Add("" + e.Exception.LineNumber + "," + e.Exception.LinePosition + ":" + e.Message);
                };

            using (var reader = System.Xml.XmlReader.Create(fn, settings))
            {
                while (reader.Read())
                {
                    // Invoke callbacks
                };

                if (messages.Count > 0)
                {
                    var parts = new List<string>
                    {
                        $"Failed to validate XML file {fn}:"
                    };
                    parts.AddRange(messages);

                    throw new InvalidOperationException(string.Join(Environment.NewLine, parts));
                }
            }

        }

        static void Main(string[] args)
        {
            Console.Out.WriteLine(
                "AAS and OPC Writer v0.5. (c) 2019 Michael Hoffmeister, Festo AG & Co. KG. See LICENSE.TXT.");

            if (args.Length < 1)
            {
                // Default
                Console.Out.WriteLine(@"Help: AasxGenerate <cmd> [args] <cmd> [args] ..
      gen [fn]             = generates package within RAM. Filename points to init json-file. Internal fn = *
      load [fn]            = loads filename to RAM. Extensions: .xml, .json, .aasx, .aml
      save [fn]            = saves RAM to filename. Extensions: .xml, .json, .aasx, .aml
      export-template [fn] = saves RAM to template file
      Executing default behaviour..
");

                // test run
                try
                {
                    var tstpackage = GeneratePackage();
                    TestSerialize(tstpackage);
                    Console.Out.WriteLine("Writing sample-admin-shell.aasx ..");
                    tstpackage.SaveAs("sample-admin-shell.aasx");
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine("While testing: {0} at {1}", ex.Message, ex.StackTrace);
                    Environment.Exit(-1);
                }
            }

            AdminShellPackageEnv package = null;
            var ai = 0;
            while (ai < args.Length)
            {
                // with 1 argument
                if (ai < args.Length - 1)
                {
                    if (args[ai].Trim().ToLower() == "gen")
                        try
                        {
                            // execute
                            var fn = args[ai + 1].Trim();
                            package = GeneratePackage(fn);

                            // next command
                            Console.Out.WriteLine("Package generated.");
                            ai += 2;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Console.Out.WriteLine("While generating package: {0} at {1}", ex.Message, ex.StackTrace);
                            Environment.Exit(-1);
                        }


                    if (args[ai].Trim().ToLower() == "load")
                        try
                        {
                            // execute
                            var fn = args[ai + 1].Trim();
                            Console.Out.WriteLine("Loading package {0} ..", fn);
                            if (fn.EndsWith(".aml"))
                            {
                                package = new AdminShellPackageEnv();
                                AmlImport.ImportInto(package, fn);
                            }
                            else
                            {
                                package = new AdminShellPackageEnv(fn);
                            }

                            // next command
                            Console.Out.WriteLine("Package {0} loaded.", fn);
                            ai += 2;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Console.Out.WriteLine(
                                "While loading package {0}: {1} at {2}", args[ai + 1], ex.Message, ex.StackTrace);
                            Environment.Exit(-1);
                        }

                    if (args[ai].Trim().ToLower() == "save")
                        try
                        {
                            // check
                            if (package == null)
                            {
                                Console.Out.WriteLine("Package is null!");
                                Environment.Exit(-1);
                            }

                            // execute
                            var fn = args[ai + 1].Trim();
                            Console.Out.WriteLine("Writing package {0} ..", fn);
                            if (fn.EndsWith(".aml"))
                            {
                                AmlExport.ExportTo(package, fn, tryUseCompactProperties: false);
                            }
                            else
                            {
                                package.SaveAs(fn, writeFreshly: true);
                                package.Close();
                            }

                            // next command
                            Console.Out.WriteLine("Package {0} written.", fn);
                            ai += 2;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Console.Out.WriteLine(
                                "While loading package {0}: {1} at {2}", args[ai + 1], ex.Message, ex.StackTrace);
                            Environment.Exit(-1);
                        }

                    if (args[ai].Trim().ToLower() == "export-template")
                        try
                        {
                            // check
                            if (package == null)
                            {
                                Console.Out.WriteLine("Package is null!");
                                Environment.Exit(-1);
                            }

                            // execute
                            var fn = args[ai + 1].Trim();
                            Console.Out.WriteLine("Exporting to file {0} ..", fn);
                            AasFormUtils.ExportAsTemplate(package, fn);

                            // next command
                            Console.Out.WriteLine("Package {0} written.", fn);
                            ai += 2;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Console.Out.WriteLine(
                                "While loading package {0}: {1} at {2}", args[ai + 1], ex.Message, ex.StackTrace);
                            Environment.Exit(-1);
                        }

                    if (args[ai].Trim().ToLower() == "validate")
                        try
                        {
                            // execute
                            var fn = args[ai + 1].Trim();
                            var recs = new AasValidationRecordList();

                            if (fn.ToLower().EndsWith(".xml"))
                            {
                                Console.Out.WriteLine("Validating file {0} against XSD in schemaV201\\. ..", fn);
                                // Validate(fn);

                                var stream = File.Open(fn, FileMode.Open, FileAccess.Read);
                                AasSchemaValidation.ValidateXML(recs, stream);
                            }
                            else
                            {
                                Console.Out.WriteLine("Do not know to handle {0}. Aborting.", fn);
                            }

                            // list recs
                            Console.Out.WriteLine($"Found {recs.Count} issues:");
                            foreach (var r in recs)
                                Console.Out.WriteLine(r.ToString());

                            // next command
                            ai += 2;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Console.Out.WriteLine(
                                "While validating {0}: {1} at {2}", args[ai + 1], ex.Message, ex.StackTrace);
                            Environment.Exit(-1);
                        }
                }

                // without arguments
                if (true)
                {
                    if (args[ai].Trim().ToLower() == "check" || args[ai].Trim().ToLower() == "check+fix")
                    {
                        // check
                        if (package == null)
                        {
                            Console.Out.WriteLine("Package is null!");
                            Environment.Exit(-1);
                        }

                        // validate
                        var recs = package?.AasEnv?.ValidateAll();
                        if (recs == null)
                        {
                            Console.Out.WriteLine("Validation returned null .. error .. Aborting!");
                        }
                        else
                        {
                            Console.Out.WriteLine($"Found {recs.Count} records:");
                            foreach (var rec in recs)
                                Console.Out.WriteLine(rec.ToString());

                            // fix?
                            if (args[ai].Trim().ToLower() == "check+fix")
                            {
                                Console.Out.WriteLine($"Fixing all records..");
                                var i = package.AasEnv.AutoFix(recs);
                                Console.Out.WriteLine($".. gave result {i}.");
                            }
                        }

                        // next command
                        Console.Out.WriteLine("Checked.");
                        ai += 1;
                        continue;
                    }

                    if (args[ai].Trim().ToLower() == "test")
                        try
                        {
                            // check
                            if (package == null)
                            {
                                Console.Out.WriteLine("Package is null!");
                                Environment.Exit(-1);
                            }

                            // execute
                            var prop = AdminShellV20.Property.CreateNew("test", "cat01");
                            prop.semanticId = new AdminShellV20.SemanticId(
                                AdminShellV20.Reference.CreateNew(
                                    "GlobalReference", false, "IRI",
                                    "www.admin-shell.io/nonsense"));

                            var fil = AdminShellV20.File.CreateNew("test", "cat01");
                            fil.semanticId = new AdminShellV20.SemanticId(
                                AdminShellV20.Reference.CreateNew(
                                    "GlobalReference", false, "IRI",
                                    "www.admin-shell.io/nonsense"));
                            fil.parent = fil;

                            var so = new AdminShellUtil.SearchOptions();
                            so.allowedAssemblies = new[] { typeof(AdminShell).Assembly };
                            var sr = new AdminShellUtil.SearchResults();

                            AdminShellUtil.EnumerateSearchable(
                                sr, /* fil */ package.AasEnv, "", 0, so);

                            // test debug
                            foreach (var fr in sr.foundResults)
                                Console.Out.WriteLine(
                                    "{0}|{1} = {2}", fr.qualifiedNameHead, fr.metaModelName, fr.foundText);
                            Console.ReadLine();

                            // next command
                            Console.Out.WriteLine("Tested.");
                            ai += 1;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Console.Out.WriteLine("While testing: {0} at {1}", ex.Message, ex.StackTrace);
                            Environment.Exit(-1);
                        }
                }
            }
        }
    }
}
