/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;

using AasxPredefinedConcepts;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxToolkit
{
    public static class Generate
    {
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
            var aasenv1 = new AdminShell.AdministrationShellEnv();

            try
            {

                // ASSET
                var asset1 = new AdminShell.Asset("Asset_3s7plfdrs35");
                aasenv1.Assets.Add(asset1);
                asset1.SetIdentification("http://example.com/3s7plfdrs35", "3s7plfdrs35");
                asset1.AddDescription("en", "USB Stick");
                asset1.AddDescription("de", "USB Speichereinheit");

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
                var aas1 = AdminShell.AdministrationShell.CreateNew(
                    "AAS_3s7plfdrs35", "IRI", repo.CreateOneTimeId(), "1", "0");
                aas1.derivedFrom = new AdminShell.AssetAdministrationShellRef(
                    new AdminShell.Key("AssetAdministrationShell", 
                        "www.admin-shell.io/aas/sample-series-aas/1/1"));
                aasenv1.AdministrationShells.Add(aas1);
                aas1.assetRef = asset1.GetAssetReference();

                // Link things together
                Log.WriteLine(2, "Linking entities to AAS ..");
                aas1.submodelRefs.Add(subCad.GetReference() as AdminShell.SubmodelRef);
                aas1.submodelRefs.Add(subDocu.GetReference() as AdminShell.SubmodelRef);
                aas1.submodelRefs.Add(subDatasheet.GetReference() as AdminShell.SubmodelRef);
                aas1.submodelRefs.Add(subEng.GetReference() as AdminShell.SubmodelRef);
                aas1.submodelRefs.Add(subVars.GetReference() as AdminShell.SubmodelRef);
                aas1.submodelRefs.Add(subBOM.GetReference() as AdminShell.SubmodelRef);
                aas1.submodelRefs.Add(subBOM2.GetReference() as AdminShell.SubmodelRef);
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

        public static AdminShell.Submodel CreateSubmodelCad(
            InputFilePrefs prefs, IriIdentifierRepository repo, AdminShell.AdministrationShellEnv aasenv)
        {

            // CONCEPTS
            var cdGroup = AdminShell.ConceptDescription.CreateNew(
                "CadItem", "IRI", repo.CreateOrRetrieveIri("Example Submodel Cad Item Group"));
            aasenv.ConceptDescriptions.Add(cdGroup);
            cdGroup.SetIEC61360Spec(
                preferredNames: new[] { "de", "CAD Dateieinheit", "en", "CAD file item" },
                shortName: "CadItem",
                unit: "",
                definition: new[] {
                    "de", "Gruppe von Merkmalen, die Zugriff gibt auf eine Datei für ein CAD System.",
                    "en", "Collection of properties, which make a file for a CAD system accessible." }
            );

            var cdFile = AdminShell.ConceptDescription.CreateNew(
                "File", "IRI", repo.CreateOrRetrieveIri("Example Submodel Cad Item File Elem"));
            aasenv.ConceptDescriptions.Add(cdFile);
            cdFile.SetIEC61360Spec(
                preferredNames: new[] { "de", "Enthaltene CAD Datei", "en", "Embedded CAD file" },
                shortName: "File",
                unit: "",
                definition: new[] {
                    "de", "Verweis auf enthaltene CAD Datei.", "en", "Reference to embedded CAD file." }
            );

            var cdFormat = AdminShell.ConceptDescription.CreateNew(
                "FileFormat", AdminShell.Identifier.IRDI, "0173-1#02-ZAA120#007");
            aasenv.ConceptDescriptions.Add(cdFormat);
            cdFormat.SetIEC61360Spec(
                preferredNames: new[] { "de", "Filetype CAD", "en", "Filetype CAD" },
                shortName: "FileFormat",
                unit: "",
                definition: new[] {
                    "de", "Eindeutige Kennung Format der eingebetteten CAD Datei im ECLASS Standard.",
                    "en", "Unambigous ID of format of embedded CAD file in ECLASS standard." }
            );

            // SUB MODEL
            var sub1 = AdminShell.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "CAD";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(
                AdminShell.Key.CreateNew("Submodel", "http://example.com/id/type/submodel/cad/1/1"));

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
                var propGroup = AdminShell.SubmodelElementCollection.CreateNew(
                    $"CadItem{ndx:D2}", "PARAMETER",
                    AdminShell.Key.GetFromRef(cdGroup.GetCdReference()));
                sub1.Add(propGroup);

                // FILE
                var propFile = AdminShell.File.CreateNew(
                    "File", "PARAMETER", AdminShell.Key.GetFromRef(cdFile.GetCdReference()));
                propGroup.Add(propFile);
                propFile.mimeType = AdminShellPackageEnv.GuessMimeType(fr.fn);
                propFile.value = "" + fr.targetdir.Trim() + Path.GetFileName(fr.fn);

                // FILEFORMAT
                var propType = AdminShell.ReferenceElement.CreateNew(
                    "FileFormat", "PARAMETER", AdminShell.Key.GetFromRef(cdFormat.GetCdReference()));
                propGroup.Add(propType);
                propType.value = AdminShell.Reference.CreateNew(
                    AdminShell.Key.CreateNew(
                        AdminShell.Key.GlobalReference, "" + fr.args[0]));
            }

            return sub1;
        }

        public static AdminShell.Submodel CreateSubmodelDocumentationBasedOnVDI2770(
            InputFilePrefs prefs, IriIdentifierRepository repo, AdminShell.AdministrationShellEnv aasenv)
        {
            // use pre-definitions
            var preDefLib = new DefinitionsVDI2770();
            var preDefs = new DefinitionsVDI2770.SetOfDefsVDI2770(preDefLib);

            // add concept descriptions
            foreach (var rf in preDefs.GetAllReferables())
                if (rf is AdminShell.ConceptDescription)
                    aasenv.ConceptDescriptions.Add(rf as AdminShell.ConceptDescription);

            // SUB MODEL
            var sub1 = new AdminShell.Submodel(preDefs.SM_VDI2770_Documentation);
            sub1.SetIdentification(repo.CreateOneTimeId());
            aasenv.Submodels.Add(sub1);

            // execute LAMBDA on different data sources
            Action<int, string[], string, string, string> lambda = (idx, args, fn, url, targetdir) =>
            {
                // Document Item
                var cd = preDefs.CD_VDI2770_Document;
                using (var p0 = AdminShell.SubmodelElementCollection.CreateNew($"Document{idx:D2}",
                    "CONSTANT", AdminShell.Key.GetFromRef(cd.GetCdReference())))
                {
                    sub1.Add(p0);

                    // Document itself

                    // DOCUMENT ID
                    cd = preDefs.CD_VDI2770_DocumentId;
                    using (var p = AdminShell.Property.CreateNew(
                        cd.GetDefaultPreferredName(), "CONSTANT", AdminShell.Key.GetFromRef(cd.GetReference())))
                    {
                        p.valueType = "string";
                        p.value = "" + args.GetHashCode();
                        p0.Add(p);
                    }

                    // Is Primary
                    cd = preDefs.CD_VDI2770_IsPrimaryDocumentId;
                    using (var p = AdminShell.Property.CreateNew(
                        cd.GetDefaultPreferredName(), "CONSTANT", AdminShell.Key.GetFromRef(cd.GetReference())))
                    {
                        p.valueType = "boolean";
                        p.value = "true";
                        p0.Add(p);
                    }

                    // DOCUMENT CLASS ID
                    cd = preDefs.CD_VDI2770_DocumentClassId;
                    using (var p = AdminShell.Property.CreateNew(
                        cd.GetDefaultPreferredName(), "CONSTANT", AdminShell.Key.GetFromRef(cd.GetReference())))
                    {
                        p.valueType = "string";
                        p.value = "" + args[0];
                        p.valueId = AdminShell.Reference.CreateIrdiReference(args[2]);
                        p0.Add(p);
                    }

                    // DOCUMENT CLASS NAME
                    cd = preDefs.CD_VDI2770_DocumentClassName;
                    using (var p = AdminShell.Property.CreateNew(
                        cd.GetDefaultPreferredName(), "CONSTANT", AdminShell.Key.GetFromRef(cd.GetReference())))
                    {
                        p.valueType = "string";
                        p.value = "" + args[1];
                        p0.Add(p);
                    }

                    // CLASS SYS
                    cd = preDefs.CD_VDI2770_DocumentClassificationSystem;
                    using (var p = AdminShell.Property.CreateNew(
                        cd.GetDefaultPreferredName(), "CONSTANT", AdminShell.Key.GetFromRef(cd.GetReference())))
                    {
                        p0.Add(p);
                        p.valueType = "string";
                        p.value = "VDI2770:2018";
                    }

                    // Document version

                    cd = preDefs.CD_VDI2770_DocumentVersion;
                    using (var p1 = AdminShell.SubmodelElementCollection.CreateNew($"DocumentVersion01",
                                        "CONSTANT", AdminShell.Key.GetFromRef(cd.GetCdReference())))
                    {
                        p0.Add(p1);

                        // LANGUAGE
                        cd = preDefs.CD_VDI2770_Language;
                        var lngs = args[4].Split(',');
                        for (int i = 0; i < lngs.Length; i++)
                            using (var p = AdminShell.Property.CreateNew(
                                cd.GetDefaultPreferredName() + $"{i + 1:00}", "CONSTANT",
                                AdminShell.Key.GetFromRef(cd.GetReference())))
                            {
                                p1.Add(p);
                                p.valueType = "string";
                                p.value = "" + lngs[i];
                            }

                        // VERSION
                        cd = preDefs.CD_VDI2770_DocumentVersionId;
                        using (var p = AdminShell.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShell.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.valueType = "string";
                            p.value = "" + args[5];
                        }

                        // TITLE
                        cd = preDefs.CD_VDI2770_Title;
                        using (var p = AdminShell.MultiLanguageProperty.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShell.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.value.Add("en", "" + args[3]);
                            p.value.Add("de", "Deutsche Übersetzung von: " + args[3]);
                            p.value.Add("FR", "Traduction française de: " + args[3]);
                        }

                        // SUMMARY
                        cd = preDefs.CD_VDI2770_Summary;
                        using (var p = AdminShell.MultiLanguageProperty.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShell.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.value.Add("en", "Summary for: " + args[3]);
                            p.value.Add("de", "Zusammenfassung von: " + args[3]);
                            p.value.Add("FR", "Résumé de: " + args[3]);
                        }

                        // TITLE
                        cd = preDefs.CD_VDI2770_Keywords;
                        using (var p = AdminShell.MultiLanguageProperty.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShell.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.value.Add("en", "Keywords for: " + args[3]);
                            p.value.Add("de", "Stichwörter für: " + args[3]);
                            p.value.Add("FR", "Repèrs par: " + args[3]);
                        }

                        // SET DATE
                        cd = preDefs.CD_VDI2770_Date;
                        using (var p = AdminShell.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShell.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.valueType = "date";
                            p.value = "" + DateTime.Now.ToString("yyyy-MM-dd");
                        }

                        // STATUS
                        cd = preDefs.CD_VDI2770_StatusValue;
                        using (var p = AdminShell.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShell.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.valueType = "string";
                            p.value = "Released";
                        }

                        // ROLE
                        cd = preDefs.CD_VDI2770_Role;
                        using (var p = AdminShell.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShell.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.valueType = "string";
                            p.value = "Author";
                        }

                        // ORGANIZATION
                        cd = preDefs.CD_VDI2770_OrganizationName;
                        using (var p = AdminShell.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShell.Key.GetFromRef(cd.GetReference())))
                        {
                            p1.Add(p);
                            p.valueType = "string";
                            p.value = "Example company";
                        }

                        // ORGANIZATION OFFICIAL
                        cd = preDefs.CD_VDI2770_OrganizationOfficialName;
                        using (var p = AdminShell.Property.CreateNew(
                            cd.GetDefaultPreferredName(), "CONSTANT",
                            AdminShell.Key.GetFromRef(cd.GetReference())))
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
                            using (var p = AdminShell.File.CreateNew(
                                cd.GetDefaultPreferredName(), "CONSTANT",
                                AdminShell.Key.GetFromRef(cd.GetReference())))
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
                            using (var p = AdminShell.File.CreateNew(
                                cd.GetDefaultPreferredName(), "CONSTANT",
                                AdminShell.Key.GetFromRef(cd.GetReference())))
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

        public static AdminShell.Submodel CreateSubmodelDatasheet(
            InputFilePrefs prefs, IriIdentifierRepository repo, AdminShell.AdministrationShellEnv aasenv)
        {
            // eClass product group: 19-15-07-01 USB stick

            // SUB MODEL
            var sub1 = AdminShell.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "Datatsheet";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(
                AdminShell.Key.CreateNew("Submodel", "http://example.com/id/type/submodel/datasheet/1/1"));

            // CONCEPT: Manufacturer
            using (var cd = AdminShell.ConceptDescription.CreateNew(
                "Manufacturer", AdminShell.Identifier.IRDI, "0173-1#02-AAO677#001"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] { "de", "TBD", "en", "Manufacturer name" },
                    shortName: "Manufacturer",
                    definition: new[] { "de", "TBD",
                    "en",
                    "legally valid designation of the natural or judicial person which is directly " +
                        "responsible for the design, production, packaging and labeling of a product in respect " +
                        "to its being brought into circulation" }
                );

                var p = AdminShell.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShell.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.valueType = "string";
                p.value = "Example company Ltd.";
            }

            // CONCEPT: Width
            using (var cd = AdminShell.ConceptDescription.CreateNew(
                "Width", AdminShell.Identifier.IRDI, "0173-1#02-BAF016#005"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] { "de", "Breite", "en", "Width" },
                    shortName: "Width",
                    unit: "mm",
                    valueFormat: "REAL_MEASURE",
                    definition: new[] {
                        "de",
                        "bei eher rechtwinkeligen Körpern die orthogonal zu Höhe/Länge/Tiefe stehende Ausdehnung " +
                        "rechtwinklig zur längsten Symmetrieachse",
                        "en",
                        "for objects with orientation in preferred position during use the dimension " +
                        "perpendicular to height/ length/depth" }
                );

                var p = AdminShell.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShell.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.valueType = "double";
                p.value = "48";
            }

            // CONCEPT: Height
            using (var cd = AdminShell.ConceptDescription.CreateNew(
                "Height", AdminShell.Identifier.IRDI, "0173-1#02-BAA020#008"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] { "de", "Höhe", "en", "Height" },
                    shortName: "Height",
                    unit: "mm",
                    valueFormat: "REAL_MEASURE",
                    definition: new[] {
                        "de",
                        "bei eher rechtwinkeligen Körpern die orthogonal zu Länge/Breite/Tiefe stehende " +
                        "Ausdehnung - bei Gegenständen mit fester Orientierung oder in bevorzugter "+
                        "Gebrauchslage der parallel zur Schwerkraft gemessenen Abstand zwischen Ober- und Unterkante",
                        "en",
                        "for objects with orientation in preferred position during use the dimension " +
                        "perpendicular to diameter/length/width/depth" }
                );

                var p = AdminShell.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShell.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.valueType = "double";
                p.value = "56";
            }

            // CONCEPT: Depth
            using (var cd = AdminShell.ConceptDescription.CreateNew(
                "Depth", AdminShell.Identifier.IRDI, "0173-1#02-BAB577#007"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] { "de", "Tiefe", "en", "Depth" },
                    shortName: "Depth",
                    unit: "mm",
                    valueFormat: "REAL_MEASURE",
                    definition: new[] {
                        "de",
                        "bei Gegenständen mit fester Orientierung oder in bevorzugter Gebrauchslage wird die " +
                        "nach hinten, im Allgemeinen vom Betrachter weg verlaufende Ausdehnung als Tiefe bezeichnet",
                        "en",
                        "for objects with fixed orientation or in preferred utilization position, " +
                        "the rear , generally away from the observer expansion is described as depth" }
                );

                var p = AdminShell.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShell.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.valueType = "double";
                p.value = "11.9";
            }

            // CONCEPT: Weight
            using (var cd = AdminShell.ConceptDescription.CreateNew(
                "Weight", AdminShell.Identifier.IRDI, "0173-1#02-AAS627#001"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "de", "Gewicht der Artikeleinzelverpackung", "en", "Weight of the individual packaging" },
                    shortName: "Weight",
                    unit: "g",
                    valueFormat: "REAL_MEASURE",
                    definition: new[] { "de", "Masse der Einzelverpackung eines Artikels",
                    "en", "Mass of the individual packaging of an article" }
                );

                // as designed
                var p = AdminShell.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShell.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.AddQualifier("life cycle qual", "SPEC",
                    AdminShell.KeyList.CreateNew(
                        AdminShell.Key.GlobalReference, "0112/2///61360_4#AAF575"),
                    AdminShell.Reference.CreateNew(
                        AdminShell.Key.GlobalReference, 
                        "0112/2///61360_4#AAF579"));
                p.valueType = "double";
                p.value = "23.1";

                // as produced
                var p2 = AdminShell.Property.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShell.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p2);
                p2.AddQualifier("life cycle qual", "BUILT",
                    AdminShell.KeyList.CreateNew(
                        AdminShell.Key.GlobalReference, "0112/2///61360_4#AAF575"),
                    AdminShell.Reference.CreateNew(
                        AdminShell.Key.GlobalReference, 
                        "0112/2///61360_4#AAF573"));
                p2.valueType = "double";
                p2.value = "23.05";
            }

            // CONCEPT: Material
            using (var cd = AdminShell.ConceptDescription.CreateNew(
                "Material", AdminShell.Identifier.IRDI, "0173-1#02-BAB577#007"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] { "de", "Werkstoff", "en", "Material" },
                    shortName: "Material",
                    definition: new[] { "de", "TBD",
                    "en",
                    "Materialzusammensetzung, aus der ein einzelnes Bauteil hergestellt ist, als Ergebnis " +
                    "eines Herstellungsprozesses, in dem der/die Rohstoff(e) durch Extrusion, Verformung, " +
                    "Schweißen usw. in die endgültige Form gebracht werden" }
                );

                var p = AdminShell.ReferenceElement.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER", AdminShell.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.value = p.value = AdminShell.Reference.CreateNew(
                    AdminShell.Key.CreateNew(
                        AdminShell.Key.GlobalReference, "0173-1#07-AAA878#004")); // Polyamide (PA)
            }

            // Nice
            return sub1;
        }

        public static AdminShell.Submodel CreateSubmodelVariousSingleItems(
            IriIdentifierRepository repo, AdminShell.AdministrationShellEnv aasenv)
        {
            // SUB MODEL
            var sub1 = AdminShell.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "VariousItems";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(AdminShell.Key.CreateNew(
                type: "Submodel",
                value: "http://example.com/id/type/submodel/various/1/1"));

            AdminShell.SubmodelElement sme1, sme2;

            // CONCEPT: MultiLanguageProperty
            using (var cd = AdminShell.ConceptDescription.CreateNew(
                "DocuName",
                idType: AdminShell.Identifier.IRDI,                  // immer IRDI für ECLASS
                id: "0173-1#02-ZZZ991#001"))                             // die ID des Merkmales bei ECLASS
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "de", "Name Dokument in Landessprache",    // wechseln Sie die Sprache bei ECLASS
                        "en", "Name of document in national language" },   // um die Sprach-Texte aufzufinden
                    shortName: "DocuName",                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: "STRING",                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "legally valid designation of the natural or judicial person..." }
                );

                var p = AdminShell.MultiLanguageProperty.CreateNew(cd.GetDefaultPreferredName(), "PARAMETER",
                            AdminShell.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.value.Add("en", "An english value.");
                p.value.Add("de", "Ein deutscher Wert.");
                sme1 = p;
            }

            // CONCEPT: Range
            using (var cd = AdminShell.ConceptDescription.CreateNew(
                "VoltageRange",
                idType: AdminShell.Identifier.IRDI,                  // immer IRDI für ECLASS
                id: "0173-1#02-ZZZ992#001"))                             // die ID des Merkmales bei ECLASS
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "de", "Betriebsspannungsbereich",    // wechseln Sie die Sprache bei ECLASS
                        "en", "Range operational voltage" },   // um die Sprach-Texte aufzufinden
                    shortName: "VoltageRange",                                // kurzer, sprechender Name
                    unit: "V",                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: "REAL",                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely limited voltage range..." }
                );

                var p = AdminShell.Range.CreateNew(cd.GetDefaultPreferredName(), "PARAMETER",
                            AdminShell.Key.GetFromRef(cd.GetReference()));
                sub1.Add(p);
                p.min = "11.5";
                p.max = "13.8";
                sme2 = p;
            }

            // CONCEPT: AnnotatedRelationship
            using (var cd = AdminShell.ConceptDescription.CreateNew(
                "VerConn",
                idType: AdminShell.Identifier.IRDI,  // immer IRDI für ECLASS
                id: "0173-1#02-XXX992#001"))  // die ID des Merkmales bei ECLASS
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "de", "Verbindung",    // wechseln Sie die Sprache bei ECLASS 
                        "en", "Connection" },   // um die Sprach-Texte aufzufinden
                    shortName: "VerConn",                                // kurzer, sprechender Name
                    unit: "V",                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: "REAL",                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely defined electrical connection..." }
                );

                var ar = AdminShell.AnnotatedRelationshipElement.CreateNew(
                    cd.GetDefaultPreferredName(), "PARAMETER",
                    AdminShell.Key.GetFromRef(cd.GetReference()));
                sub1.Add(ar);
                ar.first = sme1.GetReference();
                ar.second = sme2.GetReference();

                ar.annotations = new AdminShell.DataElementWrapperCollection();
                ar.annotations.Add(sme1);
                ar.annotations.Add(sme2);
            }


            // Nice
            return sub1;
        }

        public static AdminShell.Submodel CreateSubmodelBOMforECAD(
            IriIdentifierRepository repo, AdminShell.AdministrationShellEnv aasenv)
        {
            // SUB MODEL
            var sub1 = AdminShell.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "BOM-ECAD";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(AdminShell.Key.CreateNew(
                type: "Submodel",
                value: "http://example.com/id/type/submodel/BOM/1/1"));

            // CONCEPT: electrical plan

            AdminShell.ConceptDescription cdRelEPlan, cdRelElCon, cdContact1, cdContact2;

            using (var cd = AdminShell.ConceptDescription.CreateNew(
                idType: AdminShell.Identifier.IRDI,        // immer IRDI für ECLASS
                id: "0173-1#02-ZZZ993#001",
                idShort: "E-CAD"))                             // die ID des Merkmales bei ECLASS
            {
                cdRelEPlan = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "en", "Electrical plan",    // wechseln Sie die Sprache bei ECLASS
                        "de", "Stromlaufplan" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.idShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely limited language constructs..." }
                );
            }

            using (var cd = AdminShell.ConceptDescription.CreateNew(
                idType: AdminShell.Identifier.IRDI,                         // immer IRDI für ECLASS
                id: "0173-1#02-ZZZ982#001",
                idShort: "single pole connection"))                             // die ID des Merkmales bei ECLASS
            {
                cdRelElCon = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "en", "single pole electrical connection",    // wechseln Sie die Sprache bei ECLASS
                        "de", "einpolig elektrische Verbindung" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.idShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely limited language constructs..." }
                );
            }

            using (var cd = AdminShell.ConceptDescription.CreateNew(
                idType: AdminShell.Identifier.IRDI,    // immer IRDI für ECLASS
                id: "0173-1#02-ZZZ994#001",
                idShort: "1"))                             // die ID des Merkmales bei ECLASS
            {
                cdContact1 = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "en", "Contact point 1",    // wechseln Sie die Sprache bei ECLASS
                        "de", "Kontaktpunkt 1" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.idShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely limited language constructs..." }
                );
            }

            using (var cd = AdminShell.ConceptDescription.CreateNew(
                idType: AdminShell.Identifier.IRDI,                // immer IRDI für ECLASS
                id: "0173-1#02-ZZZ995#001",
                idShort: "2"))                             // die ID des Merkmales bei ECLASS
            {
                cdContact2 = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "en", "Contact point 2",    // wechseln Sie die Sprache bei ECLASS
                        "de", "Kontaktpunkt 2" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.idShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely limited language constructs..." }
                );
            }

            // ENTITIES

            var ps001 = new AdminShell.Entity(
                AdminShell.Entity.EntityTypeEnum.CoManagedEntity, "PowerSource001");
            sub1.Add(ps001);
            var ps001_1 = AdminShell.Property.CreateNew(
                "1", "CONSTANT", cdContact1.GetCdReference()[0]);
            var ps001_2 = AdminShell.Property.CreateNew(
                "2", "CONSTANT", cdContact2.GetCdReference()[0]);
            ps001.Add(ps001_1);
            ps001.Add(ps001_2);

            var sw001 = new AdminShell.Entity(
                AdminShell.Entity.EntityTypeEnum.CoManagedEntity, "Switch001");
            sub1.Add(sw001);
            var sw001_1 = AdminShell.Property.CreateNew(
                "1", "CONSTANT", cdContact1.GetCdReference()[0]);
            var sw001_2 = AdminShell.Property.CreateNew(
                "2", "CONSTANT", cdContact2.GetCdReference()[0]);
            sw001.Add(sw001_1);
            sw001.Add(sw001_2);

            var la001 = new AdminShell.Entity(
                AdminShell.Entity.EntityTypeEnum.SelfManagedEntity, "Lamp001",
                new AdminShell.AssetRef(
                    AdminShell.Reference.CreateNew(
                        "Asset", "example.com/assets/23224234234232342343234")));
            sub1.Add(la001);
            var la001_1 = AdminShell.Property.CreateNew(
                "1", "CONSTANT", cdContact1.GetCdReference()[0]);
            var la001_2 = AdminShell.Property.CreateNew(
                "2", "CONSTANT", cdContact2.GetCdReference()[0]);
            la001.Add(la001_1);
            la001.Add(la001_2);

            // RELATIONS

            var smec1 = AdminShell.SubmodelElementCollection.CreateNew(
                "E-CAD", semanticIdKey: cdRelEPlan.GetCdReference()[0]);
            sub1.Add(smec1);

            smec1.Add(AdminShell.RelationshipElement.CreateNew(
                "w001", semanticIdKey: cdRelElCon.GetCdReference()[0],
                first: ps001_1.GetReference(), second: sw001_1.GetReference()));

            smec1.Add(AdminShell.RelationshipElement.CreateNew(
                "w002", semanticIdKey: cdRelElCon.GetCdReference()[0],
                first: sw001_2.GetReference(), second: la001_1.GetReference()));

            smec1.Add(AdminShell.RelationshipElement.CreateNew(
                "w003", semanticIdKey: cdRelElCon.GetCdReference()[0],
                first: la001_2.GetReference(), second: ps001_2.GetReference()));

            // Nice
            return sub1;
        }

        public static AdminShell.Submodel CreateSubmodelBOMforAssetStructure(
            IriIdentifierRepository repo, AdminShell.AdministrationShellEnv aasenv)
        {
            // SUB MODEL
            var sub1 = AdminShell.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "BOM-ASSETS";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(AdminShell.Key.CreateNew(
                type: "Submodel",
                value: "http://example.com/id/type/submodel/BOM/1/1"));

            // CONCEPT: Generic asset decomposition

            AdminShell.ConceptDescription cdIsPartOf;

            using (var cd = AdminShell.ConceptDescription.CreateNew(
                idType: AdminShell.Identifier.IRDI,                         // immer IRDI für ECLASS
                id: "0173-1#02-ZZZ998#002",
                idShort: "isPartOf"))                             // die ID des Merkmales bei ECLASS
            {
                cdIsPartOf = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "en", "Is part of",    // wechseln Sie die Sprache bei ECLASS
                        "de", "Teil von" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.idShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely limited language constructs..." }
                );
            }

            // ENTITIES

            var axisGroup = new AdminShell.Entity(
                AdminShell.Entity.EntityTypeEnum.CoManagedEntity, "AxisGroup001");
            sub1.Add(axisGroup);

            var motor = new AdminShell.Entity(
                AdminShell.Entity.EntityTypeEnum.CoManagedEntity, "Motor002");
            sub1.Add(motor);

            var encoder = new AdminShell.Entity(
                AdminShell.Entity.EntityTypeEnum.CoManagedEntity, "Encoder003");
            sub1.Add(encoder);

            var gearbox = new AdminShell.Entity(
                AdminShell.Entity.EntityTypeEnum.CoManagedEntity, "Gearbox003");
            sub1.Add(gearbox);

            var amp = new AdminShell.Entity(
                AdminShell.Entity.EntityTypeEnum.CoManagedEntity, "ServoAmplifier004");
            sub1.Add(amp);

            // RELATIONS

            sub1.Add(
                AdminShell.RelationshipElement.CreateNew(
                    "rel001", semanticIdKey: cdIsPartOf.GetCdReference()[0],
                first: axisGroup.GetReference(), second: motor.GetReference()));

            sub1.Add(
                AdminShell.RelationshipElement.CreateNew(
                    "rel002", semanticIdKey: cdIsPartOf.GetCdReference()[0],
                first: axisGroup.GetReference(), second: encoder.GetReference()));

            sub1.Add(
                AdminShell.RelationshipElement.CreateNew(
                    "rel003", semanticIdKey: cdIsPartOf.GetCdReference()[0],
                first: axisGroup.GetReference(), second: gearbox.GetReference()));

            sub1.Add(
                AdminShell.RelationshipElement.CreateNew(
                    "rel004", semanticIdKey: cdIsPartOf.GetCdReference()[0],
                first: axisGroup.GetReference(), second: amp.GetReference()));


            // Nice
            return sub1;
        }

        public static AdminShell.Submodel CreateSubmodelEnergyMode(
            IriIdentifierRepository repo, AdminShell.AdministrationShellEnv aasenv)
        {
            // SUB MODEL
            var sub1 = AdminShell.Submodel.CreateNew("IRI", repo.CreateOneTimeId());
            sub1.idShort = "EnergyMode";
            aasenv.Submodels.Add(sub1);
            sub1.semanticId.Keys.Add(AdminShell.Key.CreateNew(
                type: "Submodel",
                value: "http://example.com/id/type/submodel/energymode/1/1"));

            // CONCEPT: SetMode
            var theOp = new AdminShell.Operation();
            using (var cd = AdminShell.ConceptDescription.CreateNew(
                "SetMode",
                idType: AdminShell.Identifier.IRDI,
                id: "0173-1#02-AAS999#001"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "de", "Setze Energiespare-Modus",
                        "en", "Set energy saving mode" },
                    shortName: "SetMode",
                    definition: new[] { "de", "Setze Energiemodus 1..4",
                    "en", "Set energy saving mode 1..4" }
                );

                theOp.idShort = "setmode";
                sub1.Add(theOp);
            }

            // CONCEPT: Mode
            using (var cd = AdminShell.ConceptDescription.CreateNew(
                "mode",
                idType: AdminShell.Identifier.IRDI,
                id: "0173-1#02-AAX777#002"))
            {
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "de", "Energiesparemodus-Vorgabe",
                        "en", "Preset of energy saving mode" },
                    shortName: "mode",
                    valueFormat: "INT",
                    definition: new[] { "de", "Vorgabe für den Energiesparmodus für optimalen Betrieb",
                    "en", "Preset in optimal case for the energy saving mode" }
                );

                var p = AdminShell.Property.CreateNew(cd.GetDefaultPreferredName(), "PARAMETER",
                            AdminShell.Key.GetFromRef(cd.GetReference()));

                var ovp = new AdminShell.OperationVariable(p);

                theOp.inputVariable.Add(ovp);
                p.valueType = "int";
            }

            // Nice
            return sub1;
        }

        private static void CreateStochasticViewOnSubmodelsRecurse(
            AdminShell.View vw, AdminShell.Submodel submodel, AdminShell.SubmodelElement sme)
        {
            if (vw == null || sme == null)
                return;

            var isSmc = (sme is AdminShell.SubmodelElementCollection);

            // spare out some of the leafs of the tree ..
            if (!isSmc)
                if (Math.Abs(sme.idShort.GetHashCode() % 100) > 50)
                    return;

            // ok, create
            var ce = new AdminShell.ContainedElementRef();
            sme.CollectReferencesByParent(ce.Keys);
            vw.AddContainedElement(ce.Keys);
            // recurse
            if (isSmc)
                foreach (var sme2wrap in (sme as AdminShell.SubmodelElementCollection).value)
                    CreateStochasticViewOnSubmodelsRecurse(vw, submodel, sme2wrap.submodelElement);
        }

        public static AdminShell.View CreateStochasticViewOnSubmodels(AdminShell.Submodel[] sms, string idShort)
        {
            // create
            var vw = new AdminShell.View();
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

    }
}
