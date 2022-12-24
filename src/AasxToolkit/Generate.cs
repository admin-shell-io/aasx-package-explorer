/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using AasCore.Aas3_0_RC02;
using AasxPredefinedConcepts;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;
using NJsonSchema.Validation;
using Environment = AasCore.Aas3_0_RC02.Environment;

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
                if (preffn != null && preffn != "*" && System.IO.File.Exists(preffn))
                {
                    Log.WriteLine(2, "Opening {0} for reading preferences ..", preffn);
                    var init = System.IO.File.ReadAllText(preffn);
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
    { 'url' : 'https://www.plattform-i40.de/PI40/Redaktion/EN/Downloads/Publikation/Details-of-the-AssetInformation-Administration-Shell-Part1.pdf?__blob=publicationFile&v=5',
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
                System.Environment.Exit(-1);
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
                System.Environment.Exit(-1);
            }

            // AAS ENV
            var aasenv1 = new Environment();

            try
            {

                // ASSET
                //var asset1 = new AssetInformation("Asset_3s7plfdrs35");
                var asset1 = new AssetInformation(AssetKind.Instance);
                //aasenv1.Assets.Add(asset1);
                //TODO:jtikekar 
                //asset1.SetIdentification("IRI", "http://example.com/3s7plfdrs35", "3s7plfdrs35");
                //No Description in AssetInformation
                //asset1.AddDescription("en", "USB Stick");
                //asset1.AddDescription("de", "USB Speichereinheit");

                // CAD
                Log.WriteLine(2, "Creating submodel CAD ..");
                var subCad = CreateSubmodelCad(prefs, repo, aasenv1);

                // Test Hashing
                //TODO:jtikekar Uncomment ComputeHashcode
                //Log.WriteLine(2, "Hash for submodel CAD = " + subCad.ComputeHashcode());
                //subCad.Category += "!";
                //Log.WriteLine(2, "Hash for submodel CAD = " + subCad.ComputeHashcode());

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

                Log.WriteLine(2, "Creating submodel BOM for AssetInformation Structure..");
                var subBOM2 = CreateSubmodelBOMforAssetStructure(repo, aasenv1);

                // VIEW1
                //Viw Not supported in V3
                //var view1 = CreateStochasticViewOnSubmodels(
                //    new[] { subCad, subDocu, subDatasheet, subVars }, "View1");

                // ADMIN SHELL
                Log.WriteLine(2, "Create AAS ..");
                var aas1 = new AssetAdministrationShell(repo.CreateOneTimeId(), new AssetInformation(AssetKind.Instance), idShort: "AAS_3s7plfdrs35", administration: new AdministrativeInformation(version: "1", revision: "0"));

                aas1.DerivedFrom = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.AssetAdministrationShell, "www.admin-shell.io/aas/sample-series-aas/1/1") });

                aasenv1.AssetAdministrationShells.Add(aas1);
                aas1.AssetInformation = asset1;

                // Link things together
                Log.WriteLine(2, "Linking entities to AAS ..");
                if(aas1.Submodels == null)
                {
                    aas1.Submodels = new List<Reference>();
                }
                aas1.Submodels.Add(subCad.GetReference());
                aas1.Submodels.Add(subDocu.GetReference());
                aas1.Submodels.Add(subDatasheet.GetReference());
                aas1.Submodels.Add(subEng.GetReference());
                aas1.Submodels.Add(subVars.GetReference());
                aas1.Submodels.Add(subBOM.GetReference());
                aas1.Submodels.Add(subBOM2.GetReference());
            }
            catch (Exception ex)
            {
                Console.Out.Write("While building AAS: {0} at {1}", ex.Message, ex.StackTrace);
                System.Environment.Exit(-1);
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
                System.Environment.Exit(-1);
            }

            // final
            Log.WriteLine(2, "Returning with package ..");
            return package;
        }

        public static Submodel CreateSubmodelCad(
            InputFilePrefs prefs, IriIdentifierRepository repo, Environment aasenv)
        {

            // CONCEPTS
            var cdGroup = new ConceptDescription(repo.CreateOrRetrieveIri("Example Submodel Cad Item Group"), idShort: "CadItem");
            aasenv.ConceptDescriptions.Add(cdGroup);
            cdGroup.SetIEC61360Spec(
                preferredNames: new[] { "de", "CAD Dateieinheit", "en", "CAD file item" },
                shortName: "CadItem",
                unit: "",
                definition: new[] {
                    "de", "Gruppe von Merkmalen, die Zugriff gibt auf eine Datei für ein CAD System.",
                    "en", "Collection of properties, which make a file for a CAD system accessible." }
            );

            var cdFile = new ConceptDescription(repo.CreateOrRetrieveIri("Example Submodel Cad Item File Elem"), idShort:"File");
            aasenv.ConceptDescriptions.Add(cdFile);
            cdFile.SetIEC61360Spec(
                preferredNames: new[] { "de", "Enthaltene CAD Datei", "en", "Embedded CAD file" },
                shortName: "File",
                unit: "",
                definition: new[] {
                    "de", "Verweis auf enthaltene CAD Datei.", "en", "Reference to embedded CAD file." }
            );

            var cdFormat = new ConceptDescription("0173-1#02-ZAA120#007", idShort:"FileFormat");
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
            var sub1 = new Submodel(repo.CreateOneTimeId());
            sub1.IdShort = "CAD";
            aasenv.Submodels.Add(sub1);
            sub1.SemanticId = new Reference(ReferenceTypes.ModelReference, new List<Key>() { new Key(KeyTypes.Submodel, "http://example.com/id/type/submodel/cad/1/1") });

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
                var propGroup = new SubmodelElementCollection(category: "PARAMETER", idShort: $"CadItem{ndx:D2}", semanticId: cdGroup.GetCdReference());
                sub1.Add(propGroup);

                // FILE
                var propFile = new AasCore.Aas3_0_RC02.File("", idShort: "File", category: "PARAMETER", semanticId: cdFile.GetCdReference());
                propGroup.Add(propFile);
                propFile.ContentType = AdminShellPackageEnv.GuessMimeType(fr.fn);
                propFile.Value = "" + fr.targetdir.Trim() + Path.GetFileName(fr.fn);

                // FILEFORMAT
                var propType = new ReferenceElement(idShort: "FileFormat", category: "PARAMETER", semanticId: cdFormat.GetCdReference());
                propGroup.Add(propType);
                propType.Value = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, "" + fr.args[0]) });
            }

            return sub1;
        }

        public static Submodel CreateSubmodelDocumentationBasedOnVDI2770(
            InputFilePrefs prefs, IriIdentifierRepository repo, Environment aasenv)
        {
            // use pre-definitions
            var preDefLib = new DefinitionsVDI2770();
            var preDefs = new DefinitionsVDI2770.SetOfDefsVDI2770(preDefLib);

            // add concept descriptions
            foreach (var rf in preDefs.GetAllReferables())
                if (rf is ConceptDescription)
                    aasenv.ConceptDescriptions.Add(rf as ConceptDescription);

            // SUB MODEL
            var sub1 = new Submodel(
                preDefs.SM_VDI2770_Documentation.Id, preDefs.SM_VDI2770_Documentation.Extensions, 
                preDefs.SM_VDI2770_Documentation.Category, preDefs.SM_VDI2770_Documentation.IdShort, 
                preDefs.SM_VDI2770_Documentation.DisplayName, preDefs.SM_VDI2770_Documentation.Description, 
                preDefs.SM_VDI2770_Documentation.Checksum, preDefs.SM_VDI2770_Documentation.Administration, 
                preDefs.SM_VDI2770_Documentation.Kind, preDefs.SM_VDI2770_Documentation.SemanticId, 
                preDefs.SM_VDI2770_Documentation.SupplementalSemanticIds, 
                preDefs.SM_VDI2770_Documentation.Qualifiers, 
                preDefs.SM_VDI2770_Documentation.EmbeddedDataSpecifications, 
                preDefs.SM_VDI2770_Documentation.SubmodelElements);
            sub1.Id = repo.CreateOneTimeId();
            aasenv.Submodels.Add(sub1);

            // execute LAMBDA on different data sources
            Action<int, string[], string, string, string> lambda = (idx, args, fn, url, targetdir) =>
            {
                // Document Item
                var cd = preDefs.CD_VDI2770_Document;
                var p0 = new SubmodelElementCollection(idShort: $"Document{idx:D2}", category: "CONSTANT", semanticId: cd.GetCdReference());
                {
                    sub1.Add(p0);

                    // Document itself

                    // DOCUMENT ID
                    cd = preDefs.CD_VDI2770_DocumentId;
                    var p = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                    {
                        p.Value = "" + args.GetHashCode();
                        p0.Add(p);
                    }

                    // Is Primary
                    cd = preDefs.CD_VDI2770_IsPrimaryDocumentId;
                    p = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                    {
                        p.ValueType = DataTypeDefXsd.Boolean;
                        p.Value = "true";
                        p0.Add(p);
                    }

                    // DOCUMENT CLASS ID
                    cd = preDefs.CD_VDI2770_DocumentClassId;
                    p = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                    {
                        p.Value = "" + args[0];
                        p.ValueId = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, args[2])});
                        p0.Add(p);
                    }

                    // DOCUMENT CLASS NAME
                    cd = preDefs.CD_VDI2770_DocumentClassName;
                    p = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                    {
                        p.Value = "" + args[1];
                        p0.Add(p);
                    }

                    // CLASS SYS
                    cd = preDefs.CD_VDI2770_DocumentClassificationSystem;
                    p = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                    {
                        p0.Add(p);
                        p.Value = "VDI2770:2018";
                    }

                    // Document version

                    cd = preDefs.CD_VDI2770_DocumentVersion;
                    var p1 = new SubmodelElementCollection(idShort: "DocumentVersion01", category:"CONSTANT", semanticId: cd.GetCdReference());
                    {
                        p0.Add(p1);

                        // LANGUAGE
                        cd = preDefs.CD_VDI2770_Language;
                        var lngs = args[4].Split(',');
                        for (int i = 0; i < lngs.Length; i++)
                        {
                            var prop2 = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                            {
                                p1.Add(prop2);
                                prop2.Value = "" + lngs[i];
                            }
                        }
                            

                        // VERSION
                        cd = preDefs.CD_VDI2770_DocumentVersionId;
                        //using (var prop = Property.CreateNew(
                        //    cd.GetDefaultPreferredName(), "CONSTANT",
                        //    Key.GetFromRef(cd.GetReference())))
                        var prop = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                        {
                            p1.Add(prop);
                            prop.Value = "" + args[5];
                        }

                        // TITLE
                        cd = preDefs.CD_VDI2770_Title;
                        //using (var mlp = MultiLanguageProperty.CreateNew(
                        //    cd.GetDefaultPreferredName(), "CONSTANT",
                        //    Key.GetFromRef(cd.GetReference())))
                        var mlp = new MultiLanguageProperty(idShort: cd.GetDefaultPreferredName(), category:"CONSTANT", semanticId: cd.GetReference());
                        {
                            p1.Add(mlp);
                            if(mlp.Value == null)
                            {
                                mlp.Value = new List<LangString>();
                            }
                            mlp.Value.Add(new LangString("en", args[3]));
                            mlp.Value.Add(new LangString("de", "Deutsche Übersetzung von: " + args[3]));
                            mlp.Value.Add(new LangString("FR", "Traduction française de: " + args[3]));
                        }

                        // SUMMARY
                        cd = preDefs.CD_VDI2770_Summary;
                        //using (var p = MultiLanguageProperty.CreateNew(
                        //    cd.GetDefaultPreferredName(), "CONSTANT",
                        //    Key.GetFromRef(cd.GetReference())))
                        mlp = new MultiLanguageProperty(idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                        {
                            p1.Add(p);
                            mlp.Value.Add(new LangString("en", "Summary for: " + args[3]));
                            mlp.Value.Add(new LangString("de", "Zusammenfassung von: " + args[3]));
                            mlp.Value.Add(new LangString("FR", "Résumé de: " + args[3]));
                        }

                        // TITLE
                        cd = preDefs.CD_VDI2770_Keywords;
                        //using (var mlp = MultiLanguageProperty.CreateNew(
                        //    cd.GetDefaultPreferredName(), "CONSTANT",
                        //    Key.GetFromRef(cd.GetReference())))
                        mlp = new MultiLanguageProperty(idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                        {
                            p1.Add(mlp);
                            mlp.Value.Add(new LangString("en", "Keywords for: " + args[3]));
                            mlp.Value.Add(new LangString("de", "Stichwörter für: " + args[3]));
                            mlp.Value.Add(new LangString("FR", "Repèrs par: " + args[3]));
                        }

                        // SET DATE
                        cd = preDefs.CD_VDI2770_Date;
                        //using (var prop = Property.CreateNew(
                        //    cd.GetDefaultPreferredName(), "CONSTANT",
                        //    Key.GetFromRef(cd.GetReference())))
                        prop = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                        {
                            p1.Add(prop);
                            prop.ValueType = DataTypeDefXsd.Date;
                            prop.Value = "" + DateTime.Now.ToString("yyyy-MM-dd");
                        }

                        // STATUS
                        cd = preDefs.CD_VDI2770_StatusValue;
                        //using (var prop = Property.CreateNew(
                        //    cd.GetDefaultPreferredName(), "CONSTANT",
                        //    Key.GetFromRef(cd.GetReference())))
                        prop = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                        {
                            p1.Add(prop);
                            prop.Value = "Released";
                        }

                        // ROLE
                        cd = preDefs.CD_VDI2770_Role;
                        //using (var prop = Property.CreateNew(
                        //    cd.GetDefaultPreferredName(), "CONSTANT",
                        //    Key.GetFromRef(cd.GetReference())))
                        prop = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                        {
                            p1.Add(prop);
                            prop.Value = "Author";
                        }

                        // ORGANIZATION
                        cd = preDefs.CD_VDI2770_OrganizationName;
                        //using (var prop = Property.CreateNew(
                        //    cd.GetDefaultPreferredName(), "CONSTANT",
                        //    Key.GetFromRef(cd.GetReference())))
                        prop = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                        {
                            p1.Add(prop);
                            prop.Value = "Example company";
                        }

                        // ORGANIZATION OFFICIAL
                        cd = preDefs.CD_VDI2770_OrganizationOfficialName;
                        //using (var prop = Property.CreateNew(
                        //    cd.GetDefaultPreferredName(), "CONSTANT",
                        //    Key.GetFromRef(cd.GetReference())))
                        prop = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                        {
                            p1.Add(prop);
                            prop.Value = "Example company Ltd.";
                        }

                        // DIGITAL FILE
                        if (fn != null && targetdir != null)
                        {
                            // physical file
                            cd = preDefs.CD_VDI2770_DigitalFile;
                            //using (var file = File.CreateNew(
                            //    cd.GetDefaultPreferredName(), "CONSTANT",
                            //    Key.GetFromRef(cd.GetReference())))
                            var file = new AasCore.Aas3_0_RC02.File("", idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                            {
                                p1.Add(file);
                                file.ContentType = AdminShellPackageEnv.GuessMimeType(fn);
                                file.Value = "" + targetdir.Trim() + Path.GetFileName(fn);
                            }
                        }
                        if (url != null)
                        {
                            // URL
                            cd = preDefs.CD_VDI2770_DigitalFile;
                            //using (var p = File.CreateNew(
                            //    cd.GetDefaultPreferredName(), "CONSTANT",
                            //    Key.GetFromRef(cd.GetReference())))
                            var file = new AasCore.Aas3_0_RC02.File("", idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                            {
                                p1.Add(file);
                                file.ContentType = AdminShellPackageEnv.GuessMimeType(url);
                                file.Value = "" + url.Trim();
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

        public static Submodel CreateSubmodelDatasheet(
            InputFilePrefs prefs, IriIdentifierRepository repo, Environment aasenv)
        {
            // eClass product group: 19-15-07-01 USB stick

            // SUB MODEL
            var sub1 = new Submodel(repo.CreateOneTimeId());
            sub1.IdShort = "Datatsheet";
            aasenv.Submodels.Add(sub1);
            sub1.SemanticId = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.Submodel, "http://example.com/id/type/submodel/datasheet/1/1") });

            // CONCEPT: Manufacturer
            var cd = new ConceptDescription("0173-1#02-AAO677#001", idShort: "Manufacturer");
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

                var p = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());

                sub1.Add(p);
                p.Value = "Example company Ltd.";
            }

            // CONCEPT: Width
            //using (var cd = ConceptDescription.CreateNew(
            //    "Width", Identification.IRDI, "0173-1#02-BAF016#005"))
            cd = new ConceptDescription("0173-1#02-BAF016#005", idShort: "Width");
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

                var p = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "CONSTANT", semanticId: cd.GetReference());
                sub1.Add(p);
                p.ValueType = DataTypeDefXsd.Double;
                p.Value = "48";
            }

            // CONCEPT: Height
            //using (var cd = ConceptDescription.CreateNew(
            //    "Height", Identification.IRDI, "0173-1#02-BAA020#008"))
            cd = new ConceptDescription("0173-1#02-BAA020#008", idShort: "Height");
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

                //var p = Property.CreateNew(
                //    cd.GetDefaultPreferredName(), "PARAMETER", Key.GetFromRef(cd.GetReference()));
                var p = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "PARAMETER", semanticId: cd.GetReference());
                sub1.Add(p);
                p.ValueType = DataTypeDefXsd.Double;
                p.Value = "56";
            }

            // CONCEPT: Depth
            //using (var cd = ConceptDescription.CreateNew(
            //    "Depth", Identification.IRDI, "0173-1#02-BAB577#007"))
            cd = new ConceptDescription("0173-1#02-BAB577#007", idShort: "Depth");
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

                //var p = Property.CreateNew(
                //    cd.GetDefaultPreferredName(), "PARAMETER", Key.GetFromRef(cd.GetReference()));
                var p = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "PARAMETER", semanticId: cd.GetReference());
                sub1.Add(p);
                p.ValueType = DataTypeDefXsd.Double;
                p.Value = "11.9";
            }

            // CONCEPT: Weight
            //using (var cd = ConceptDescription.CreateNew(
            //    "Weight", Identification.IRDI, "0173-1#02-AAS627#001"))
                cd = new ConceptDescription("0173-1#02-AAS627#001", idShort:"Weight");
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
                //var p = Property.CreateNew(
                //    cd.GetDefaultPreferredName(), "PARAMETER", Key.GetFromRef(cd.GetReference()));
                var p = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "PARAMETER", semanticId: cd.GetReference());
                sub1.Add(p);
                p.Qualifiers = new List<Qualifier>() { new Qualifier("life cycle qual", DataTypeDefXsd.String, value:"SPEC", semanticId:new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, "0112/2///61360_4#AAF575") }), valueId:new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, "0112/2///61360_4#AAF579") })) };
                //p.AddQualifier("life cycle qual", "SPEC",
                //    KeyList.CreateNew(
                //        "GlobalReference", false, Identification.IRDI,
                //        "0112/2///61360_4#AAF575"),
                //    Reference.CreateNew(
                //        "GlobalReference", false, Identification.IRDI,
                //        "0112/2///61360_4#AAF579"));
                p.ValueType = DataTypeDefXsd.Double;
                p.Value = "23.1";

                // as produced
                //var p2 = Property.CreateNew(
                //    cd.GetDefaultPreferredName(), "PARAMETER", Key.GetFromRef(cd.GetReference()));
                var p2 = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "PARAMETER", semanticId: cd.GetReference());
                sub1.Add(p2);
                p.Qualifiers = new List<Qualifier>() { new Qualifier("life cycle qual", DataTypeDefXsd.String, value: "BUILT", semanticId: new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, "0112/2///61360_4#AAF575") }), valueId: new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, "0112/2///61360_4#AAF573") })) };
                p2.ValueType = DataTypeDefXsd.Double;
                p2.Value = "23.05";
            }

            // CONCEPT: Material
            cd = new ConceptDescription("0173-1#02-BAB577#007", idShort: "Material");
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

                //var p = ReferenceElement.CreateNew(
                //    cd.GetDefaultPreferredName(), "PARAMETER", Key.GetFromRef(cd.GetReference()));
                var p = new ReferenceElement(idShort:cd.GetDefaultPreferredName(),category: "PARAMETER", semanticId: cd.GetReference());
                sub1.Add(p);
                //p.Value = p.Value = Reference.CreateNew(
                //    Key.CreateNew(
                //        "GlobalReference", false, Identification.IRDI,
                //        "0173-1#07-AAA878#004")); // Polyamide (PA)
                p.Value = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, "0173-1#07-AAA878#004") });
            }

            // Nice
            return sub1;
        }

        public static Submodel CreateSubmodelVariousSingleItems(
            IriIdentifierRepository repo, Environment aasenv)
        {
            // SUB MODEL
            var sub1 = new Submodel(repo.CreateOneTimeId());
            sub1.IdShort = "VariousItems";
            aasenv.Submodels.Add(sub1);
            sub1.SemanticId = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.Submodel, "http://example.com/id/type/submodel/various/1/1") });

            ISubmodelElement sme1, sme2;

            // CONCEPT: MultiLanguageProperty
            var cd = new ConceptDescription("0173-1#02-ZZZ991#001", idShort: "DocuName");
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

                //var p = MultiLanguageProperty.CreateNew(cd.GetDefaultPreferredName(), "PARAMETER",
                //            Key.GetFromRef(cd.GetReference()));
                var p = new MultiLanguageProperty(idShort: cd.GetDefaultPreferredName(), category: "PARAMETER", semanticId: cd.GetReference());
                sub1.Add(p);
                if(p.Value == null)
                {
                    p.Value = new List<LangString>();
                }
                p.Value.Add(new LangString("en", "An english value."));
                p.Value.Add(new LangString("de", "Ein deutscher Wert."));
                sme1 = p;
            }

            // CONCEPT: Range
            //using (var cd = ConceptDescription.CreateNew(
            //    "VoltageRange",
            //    idType: Identification.IRDI,                  // immer IRDI für ECLASS
            //    id: "0173-1#02-ZZZ992#001"))                             // die ID des Merkmales bei ECLASS
            cd = new ConceptDescription("0173-1#02-ZZZ992#001", idShort: "VoltageRange");
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

                //var p = Range.CreateNew(cd.GetDefaultPreferredName(), "PARAMETER",
                //            Key.GetFromRef(cd.GetReference()));
                var p = new AasCore.Aas3_0_RC02.Range(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "PARAMETER", semanticId: cd.GetReference());
                sub1.Add(p);
                p.Min = "11.5";
                p.Max = "13.8";
                sme2 = p;
            }

            // CONCEPT: AnnotatedRelationship
            //using (var cd = ConceptDescription.CreateNew(
            //    "VerConn",
            //    idType: Identification.IRDI,  // immer IRDI für ECLASS
            //    id: "0173-1#02-XXX992#001"))  // die ID des Merkmales bei ECLASS
                cd = new ConceptDescription("0173-1#02-XXX992#001", idShort: "VerConn");
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

                //var ar = AnnotatedRelationshipElement.CreateNew(
                //    cd.GetDefaultPreferredName(), "PARAMETER",
                //    Key.GetFromRef(cd.GetReference()));
                var ar = new AnnotatedRelationshipElement(sme1.GetModelReference(), sme2.GetModelReference(), idShort:cd.GetDefaultPreferredName(), category:"PARAMETER", semanticId:cd.GetReference());
                sub1.Add(ar);

                ar.Annotations = new List<IDataElement>();
                ar.Annotations.Add((IDataElement)sme1);
                ar.Annotations.Add((IDataElement)sme2);
            }


            // Nice
            return sub1;
        }

        public static Submodel CreateSubmodelBOMforECAD(
            IriIdentifierRepository repo, Environment aasenv)
        {
            // SUB MODEL
            var sub1 = new Submodel(repo.CreateOneTimeId());
            sub1.IdShort = "BOM-ECAD";
            aasenv.Submodels.Add(sub1);
            sub1.SemanticId = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.Submodel, "http://example.com/id/type/submodel/BOM/1/1") });

            // CONCEPT: electrical plan

            ConceptDescription cdRelEPlan, cdRelElCon, cdContact1, cdContact2;

            //using (var cd = ConceptDescription.CreateNew(
            //    idType: Identification.IRDI,        // immer IRDI für ECLASS
            //    id: "0173-1#02-ZZZ993#001",
            //    idShort: "E-CAD"))                             // die ID des Merkmales bei ECLASS
            var cd = new ConceptDescription("0173-1#02-ZZZ993#001", idShort: "E-CAD");
            {
                cdRelEPlan = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "en", "Electrical plan",    // wechseln Sie die Sprache bei ECLASS
                        "de", "Stromlaufplan" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.IdShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely limited language constructs..." }
                );
            }

            //using (var cd = ConceptDescription.CreateNew(
            //    idType: Identification.IRDI,                         // immer IRDI für ECLASS
            //    id: "0173-1#02-ZZZ982#001",
            //    idShort: "single pole connection"))                             // die ID des Merkmales bei ECLASS
            cd = new ConceptDescription("0173-1#02-ZZZ982#001", idShort: "single pole connection");
            {
                cdRelElCon = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "en", "single pole electrical connection",    // wechseln Sie die Sprache bei ECLASS
                        "de", "einpolig elektrische Verbindung" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.IdShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely limited language constructs..." }
                );
            }

            //using (var cd = ConceptDescription.CreateNew(
            //    idType: Identification.IRDI,    // immer IRDI für ECLASS
            //    id: "0173-1#02-ZZZ994#001",
            //    idShort: "1"))                             // die ID des Merkmales bei ECLASS
            cd = new ConceptDescription("0173-1#02-ZZZ994#001", idShort: "1");
            {
                cdContact1 = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "en", "Contact point 1",    // wechseln Sie die Sprache bei ECLASS
                        "de", "Kontaktpunkt 1" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.IdShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely limited language constructs..." }
                );
            }

            //using (var cd = ConceptDescription.CreateNew(
            //    idType: Identification.IRDI,                // immer IRDI für ECLASS
            //    id: "0173-1#02-ZZZ995#001",
            //    idShort: "2"))                             // die ID des Merkmales bei ECLASS
            cd = new ConceptDescription("0173-1#02-ZZZ995#001", idShort: "2");
            {
                cdContact2 = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "en", "Contact point 2",    // wechseln Sie die Sprache bei ECLASS
                        "de", "Kontaktpunkt 2" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.IdShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely limited language constructs..." }
                );
            }

            // ENTITIES

            var ps001 = new Entity(
                EntityType.CoManagedEntity, idShort:"PowerSource001");
            sub1.Add(ps001);
            var ps001_1 = new Property(DataTypeDefXsd.String, idShort: "1", category: "CONSTANT", semanticId: cdContact1.GetCdReference());
            var ps001_2 = new Property(DataTypeDefXsd.String, idShort: "2", category: "CONSTANT", semanticId: cdContact2.GetCdReference());
            if(ps001.Statements == null)
            {
                ps001.Statements = new List<ISubmodelElement>();
            }
            ps001.Statements.Add(ps001_1);
            ps001.Statements.Add(ps001_2);

            var sw001 = new Entity(
                EntityType.CoManagedEntity, idShort:"Switch001");
            sub1.Add(sw001);
            var sw001_1 = new Property(DataTypeDefXsd.String, idShort: "1", category: "CONSTANT", semanticId: cdContact1.GetCdReference());
            var sw001_2 = new Property(DataTypeDefXsd.String, idShort: "2", category: "CONSTANT", semanticId: cdContact2.GetCdReference());
            if (sw001.Statements == null)
            {
                sw001.Statements = new List<ISubmodelElement>();
            }
            sw001.Statements.Add(ps001_1);
            sw001.Statements.Add(ps001_2);

            //TODO: jtikekar keyType should be AssetInformation
            var la001 = new Entity(
                EntityType.SelfManagedEntity, idShort:"Lamp001", globalAssetId: new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.Identifiable, "example.com/assets/23224234234232342343234") }));
            sub1.Add(la001);
            var la001_1 = new Property(DataTypeDefXsd.String, idShort: "1", category: "CONSTANT", semanticId: cdContact1.GetCdReference());
            var la001_2 = new Property(DataTypeDefXsd.String, idShort: "2", category: "CONSTANT", semanticId: cdContact2.GetCdReference());
            if (la001.Statements == null)
            {
                la001.Statements = new List<ISubmodelElement>();
            }
            la001.Statements.Add(ps001_1);
            la001.Statements.Add(ps001_2);

            // RELATIONS

            var smec1 = new SubmodelElementCollection(idShort: "E-CAD", semanticId: cdRelEPlan.GetCdReference());
            smec1.Value = new List<ISubmodelElement>();
            sub1.Add(smec1);

            smec1.Value.Add(new RelationshipElement(ps001_1.GetModelReference(), sw001_1.GetModelReference(), idShort: "w001", semanticId: cdRelElCon.GetCdReference()));
            smec1.Value.Add(new RelationshipElement(sw001_2.GetModelReference(), la001_1.GetModelReference(), idShort: "w002", semanticId: cdRelElCon.GetCdReference()));
            smec1.Value.Add(new RelationshipElement(la001_2.GetModelReference(), ps001_2.GetModelReference(), idShort: "w003", semanticId: cdRelElCon.GetCdReference()));

            // Nice
            return sub1;
        }

        public static Submodel CreateSubmodelBOMforAssetStructure(
            IriIdentifierRepository repo, Environment aasenv)
        {
            // SUB MODEL
            var sub1 = new Submodel(repo.CreateOneTimeId());
            sub1.IdShort = "BOM-ASSETS";
            aasenv.Submodels.Add(sub1);
            sub1.SemanticId = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.Submodel, "http://example.com/id/type/submodel/BOM/1/1") });

            // CONCEPT: Generic asset decomposition

            ConceptDescription cdIsPartOf;

            var cd = new ConceptDescription("0173-1#02-ZZZ998#002", idShort: "isPartOf");
            {
                cdIsPartOf = cd;
                aasenv.ConceptDescriptions.Add(cd);
                cd.SetIEC61360Spec(
                    preferredNames: new[] {
                        "en", "Is part of",    // wechseln Sie die Sprache bei ECLASS
                        "de", "Teil von" },   // um die Sprach-Texte aufzufinden
                    shortName: cd.IdShort,                                // kurzer, sprechender Name
                    unit: null,                                          // Gewicht als SI Einheit ohne Klammern
                    valueFormat: null,                        // REAL oder INT_MEASURE oder STRING
                    definition: new[] { "de", "TBD",
                    "en", "very precisely limited language constructs..." }
                );
            }

            // ENTITIES

            var axisGroup = new Entity(
                EntityType.CoManagedEntity, idShort:"AxisGroup001");
            sub1.Add(axisGroup);

            var motor = new Entity(
                EntityType.CoManagedEntity, idShort:"Motor002");
            sub1.Add(motor);

            var encoder = new Entity(
                EntityType.CoManagedEntity, idShort:"Encoder003");
            sub1.Add(encoder);

            var gearbox = new Entity(
                EntityType.CoManagedEntity, idShort:"Gearbox003");
            sub1.Add(gearbox);

            var amp = new Entity(
                EntityType.CoManagedEntity, idShort:"ServoAmplifier004");
            sub1.Add(amp);

            // RELATIONS

            if(sub1.SubmodelElements == null)
            {
                sub1.SubmodelElements = new List<ISubmodelElement>();
            }

            sub1.SubmodelElements.Add(new RelationshipElement(axisGroup.GetModelReference(), motor.GetModelReference(), idShort: "rel001", semanticId: cdIsPartOf.GetCdReference()));
            sub1.SubmodelElements.Add(new RelationshipElement(axisGroup.GetModelReference(), encoder.GetModelReference(), idShort: "rel002", semanticId: cdIsPartOf.GetCdReference()));
            sub1.SubmodelElements.Add(new RelationshipElement(axisGroup.GetModelReference(), gearbox.GetModelReference(), idShort: "rel003", semanticId: cdIsPartOf.GetCdReference()));
            sub1.SubmodelElements.Add(new RelationshipElement(axisGroup.GetModelReference(), amp.GetModelReference(), idShort: "rel004", semanticId: cdIsPartOf.GetCdReference()));

            // Nice
            return sub1;
        }

        public static Submodel CreateSubmodelEnergyMode(
            IriIdentifierRepository repo, Environment aasenv)
        {
            // SUB MODEL
            var sub1 = new Submodel(repo.CreateOneTimeId());
            sub1.IdShort = "EnergyMode";
            aasenv.Submodels.Add(sub1);
            sub1.SemanticId = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.Submodel, "http://example.com/id/type/submodel/energymode/1/1") });

            // CONCEPT: SetMode
            var theOp = new Operation();
            var cd = new ConceptDescription("0173-1#02-AAS999#001", idShort: "SetMode");
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

                theOp.IdShort = "setmode";
                sub1.Add(theOp);
            }

            // CONCEPT: Mode
            cd = new ConceptDescription("0173-1#02-AAX777#002", idShort: "mode");
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

                var p = new Property(DataTypeDefXsd.String, idShort: cd.GetDefaultPreferredName(), category: "PARAMETER", semanticId: cd.GetReference());

                var ovp = new OperationVariable(p);
                if(theOp.InputVariables == null)
                {
                    theOp.InputVariables = new List<OperationVariable>();
                }

                theOp.InputVariables.Add(ovp);
                p.ValueType = DataTypeDefXsd.Int;
            }

            // Nice
            return sub1;
        }

        //View no more supported in AAS V3
        //private static void CreateStochasticViewOnSubmodelsRecurse(
        //    View vw, Submodel submodel, ISubmodelElement sme)
        //{
        //    if (vw == null || sme == null)
        //        return;

        //    var isSmc = (sme is SubmodelElementCollection);

        //    // spare out some of the leafs of the tree ..
        //    if (!isSmc)
        //        if (Math.Abs(sme.IdShort.GetHashCode() % 100) > 50)
        //            return;

        //    // ok, create
        //    var ce = new ContainedElementRef();
        //    sme.CollectReferencesByParent(ce.Keys);
        //    vw.AddContainedElement(ce.Keys);
        //    // recurse
        //    if (isSmc)
        //        foreach (var sme2wrap in (sme as SubmodelElementCollection).Value)
        //            CreateStochasticViewOnSubmodelsRecurse(vw, submodel, sme2wrap.submodelElement);
        //}

        //View no more supported in AAS V3
        //public static View CreateStochasticViewOnSubmodels(Submodel[] sms, string idShort)
        //{
        //    // create
        //    var vw = new View();
        //    vw.IdShort = idShort;

        //    // over all submodel elements
        //    if (sms != null)
        //        foreach (var sm in sms)
        //        {
        //            // parent-ize submodel
        //            sm.SetAllParents();

        //            // loop in
        //            if (sm.SubmodelElements != null)
        //                foreach (var sme in sm.SubmodelElements)
        //                    CreateStochasticViewOnSubmodelsRecurse(vw, sm, sme);
        //        }
        //    // done
        //    return vw;
        //}

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
