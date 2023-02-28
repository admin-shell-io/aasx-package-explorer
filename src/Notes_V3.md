# Notes for migrating to V3.0
This files holds notes for migrating Package Explorer sources to meta mode V3.0

## Observations

* AdminShellPackageSupplementaryFile is not IReferable anymore!!
  => Track this, however that was never the optimal solution

## Questions

* In AAS core, can different enum types for keys have identical (numerical) enum values?
  (see expensive ExtendKey.MapFrom)

* Which data spec is valid for V3.0? Jui had .. /3/0, the DotAAS seems to use .. /2/0 (which I took)
  return "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/2/0";
  Answer Birgit: Nope, shall be /3/0 or /3 !!

* AssetKind.NotApplicable is curently not in the AAS core, but in the DotAAS

* Jui and Michael to discuss with Birgit if the HasDataSpecification dialogues
  are acceptable or need to be changed

## Todo

* redesign logo file
* web browser not working
* ImageMap does not display anything without background image
* Plugin options?
* for AnnotatedRel: check SelectAdequateEnum() for allowed elems
* change file repo format with unsynchronized AAS/ AssetIds, its stupid
* BOM / Option / Styles ??

* more work on plotting (energy..)
* new ICON set?
* Package Env / AasEnv -> no "Add" button 
  -> fixed
  -> fixed 2nd time (wrong i == .. values)
  => TODO: Add CD does not focus the newly added CD (though search for business object does work)

* solve MANY issues marked with #if TODO

* V3.0 will have AssetInformation.TemplateId in order to ientify Submodels

* IndexPos of OperationVariables is constant == 0

* PackageExplorer seems NOT to invoke() "dispose-anyui-visual-extension" -> memory leak!!

## Fixed TODOs

* DocuShelf / Double click in WPF does not work -> took over changes from FixAnyUiPlugins, working
* remove unnecessary XAML files from WPF legacy -> looks ok
* CDs below SME? -> done
* NavigateTo: Find also CDs with GlobalReference -> should work
* TreeViewItems / MLP / value displayed multi line -> done
* crash: add entity -> already works
* AREL NOT SHOWN!! -> works
* integrate AML -> done
* .AddAction() is used in Widgets & AasxMenu -> renamed to "AddActionPanel()" -> done
* Add "Value" to CD in case of value list application -> done
* "New file" could not be saved!!!!! BUG !!!!!! 
  �No open AASX file to be saved�. -> fixed
* New package -> save -> crash AssetInformation == null in xmlization
  -> kind of fixed (made a big warning, create AssetInfo by default, but no hard fix realized)
* wrong default logo is loaded (PI40) -> fixed
* CD below MLP does not work -> works
* Bug reports from Birgit, including IndexPos for SMLs
* for SML, disabled accelerated move up/ down

## Regexes

* (\.CD_\w+)\.GetReference\(\) .. $1

## Findings / Open questions in spec

* DataSpecification61360.Value is string (aas core) or ShortNameTypeIEC61360 (spec)?
* AssetInformation: UML and table with different ordering of attributes
* AssetInformation.assetType .. what is it?
* Entity.GlobalAssetId is Reference (aas core) or Identifier (spec)?

* Key.type does not exist anymore. So all business cases, where a RelationShip links
  against an Asset instead of a (installation-specific) AAS are not possible anymore.
  THIS IS VERY DISAPPOINTING!!!
  => Remedy: Do an asset-id-match with every GlobalReference .. We're doing modeling and
     DO NOT CARE ABOUT OUTSIDE WORLD!!

## Feature Requests to AAS core

* GetSelfDescription() per Element
* Attribute per IClass : Name, ShortName
* Attribute per Member : meta model name
* Constructors for SME taking over attributes from other SMEs (different subtypes!)
* Factory for SubmodelElements (AasSubmodelElementsFrom(), CreateSubmodelElementFromEnum())
* Constructors without mandatory init parameters ("Bevormundung war letztes Jahrhundert")
* have an attribute telling if there are children (Descend().OfType<ISubmodelElement> .., IEnumerateChildren) or not
* LevelType needs powers of 2 in order to have enum with multiple bits!!
* still xmlns="https://admin-shell.io/aas/3/0/RC02" -> changed to /3/0
* .NET NS also still RC02
* AssetKind/NotApplicable
* AssetInformation.assetType missing?
* mapping between Enum:KeyTpes and various other types, such as Enum:AasSubmodelElements
* DataTypeDefXsd.Integer and DataTypeDefXsd.Int??

* places which deserve a factory/ mapping approach:
  GetKeyType()

## Decisions

* After phone call with Birgit, when importing a V20, set all Keys with ConceptDescription 
  to **GlobalReference** assuming it is always a external concept
* for "PackCntChangeEventData.ThisElem" and such: use AAS env as containing object for CDs, as
  List<CD> does not fulfill IClass => this will get tricky in the future for event handling 
  - Rethink?
  - VisualAasxElements.UpdateByEvent() was using this -> changed
  - see: ThisElemLocation = PackCntChangeEventLocation.ListOfConceptDescriptions

## PRs integrated

* PR550 (backup, make unique)

* PR606 / MIHO/TestDynamicMenues

* PR547 / Export markdown for SM templates

* Fix json export and adapt schema exporter #605
  - (JSON Schema epxort; still in V20 fashion)
  - Tests still unloaded
  - AasxSchema & AasxSchema.Tests

* PR506 / Fix to incorrect language codes in langString menu
  - XAML changes not applicable any more

## PRs to be integrated

* from 10 Aug, some parts were already moved (Jui?), some parts not
  https://github.com/admin-shell-io/aasx-package-explorer/commit/2a5f5f6a1d11a56fa874490072b6bdea0ab08527

  => TODO check for every plugin, for Blazor !!

* Fix AnyUI usage:
  https://github.com/admin-shell-io/aasx-package-explorer/compare/master...MIHO/FixAnyUiInPlugins

  => TODO take over changes in DocuShelf..

* Fix resharper problem #607
  https://github.com/admin-shell-io/aasx-package-explorer/pull/607/files
  => TODO when Blazor is ready

* Fix json export and adapt schema exporter #605
  https://github.com/admin-shell-io/aasx-package-explorer/pull/605
  Fixed JSON export not taken over (new serialization). TO BE CHECKED
  => Ask Andreas/ Jui
  - JSON schema export taken over

## Projects, currently unloaded

These projects are not migrated or integrated, yet.

* AasxCsharpLibrary.Tests
* AasxDictionaryImport.Tests
* AasxFileServerRestLibrary (already migrated) -> introduce Aas. NS
* AasxOpenidClient
* AasxRestServerLibrary <- to be replaced by AASX Server sources
* AasxSchemaExport.Tests
* AasxUaNetServer
* AasxIntegrationEmptrySample
* AasxRestConsoleServer
* AasxToolkit (already migrated) -> introduce Aas. NS
* AasxToolkit.Tests
* AasxUaNetConsoleServer
* AasxPackageExplorer.GuiTests
* AasxPackageExplorer.Tests
* BlazorUI

## Notes, w.r.t. to procedures

* Convert to new SDK style: https://www.partech.nl/nl/publicaties/2020/11/converting-c-sharp-projects-to-the-new-sdk-format

## Done

* added Reference/type to dialogues. Added guessing.
* added referredSemanticId to dialogues.
* added supplementalSemanticIds to dialogues
* worked over HasExtension dialogues
* worked over HasSemantics dialogues
* worked over HasDataSpecification dialogues
  (still "old" embeddedSpecification handling)
* worked over isCaseOf dialogue
* checked AssetKind
  - see question: AssetKind.NotApplicable is curently not in the AAS core
  - rework was required; as pointer/class was changed to simple 
  enum (which is fantastic)
* worked over ModelKind
* "Print asset code sheet" does not work! => solved, new version 
  of ZXing required renderer!
* DispEdit shows key: "Editing of entities" / "ISubmodelElement:"
  => changed to "SubmodelElement:" (use wording of DotAAS)
* Delete of CD does not refresh tree => solved, see decision 
  for PackCntChangeEventLocation.ListOfConceptDescriptions
* Multi-select of CD leads to exception => solved, see decision 
  for PackCntChangeEventLocation.ListOfConceptDescriptions
* Create AAS from scratch fails! => solved by checking Count of entities
* AssetInfo / defaultThumbnail/ Create empty File element does not work!
  => solved, have DisplayOrEditEntityFileResource()
* Asset below AAS? => done
* Refactoring of SMEs re-implemented completely new
* make unique implemented
* new handling of EmbeddedDataSpecification
* physical units implemented
* valuelist for iec61360
* first serializations converted
* copy/paste via clipboard
* rework of Qualifiers (References, ValueType, Kind, Value with multi-line)
* rework of AasCore.Aas3_0_RC02.AssetInformation
* remove Referable.Checksum
* rework of AAS
* check of Submodel
* check of SME
* rework of Entity (sigh! single specific asset id pair ..)
* rework of File (FileResources): re-order of attributes
* rework of Operation, OperationVariable, OV may contain SME hierarchies
* in most display strings, renamed ISubmodelElement to SubmodelElement
* little tuning on Range
* little tuning on ReferenceElement, show referredSemanticId (what a monster!)
* little tuning on RelationshipElem, "Jump" works again (bug in Env.FindReferable())
* quite intense rework of AasxPluginGenericForms
* rework of AasxPluginDocumentShelf
* touching up AasxPluginKnownSubmodels
* touching up AasxPluginAdvancedTextEditor
  had to: <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies> in .cdproj
  in order to copy ICSharpCode.AvalonEdit.dll to debug folder
  this includes ALL *.dll to bindary folder, which IS NOT NICE :-(
* touching up AasxPluginTechnicalData
  => add always: MatchMode.Relaxed (stupid GlobalReference vor CDs)
  => very general handling of finding CondeptDescription by GlobalReference
* touching up AasxPluginBomStructure
  - implemented formatting of Edges and arrow-head styles
* rework of AasxPluginPlotting
  - change Qualifiers to Extensions
  - migrating Qualifiers
* touching up AasxPluginImageMap
  - changed to Extension
  - error when no background image
* initial touch up of AasxPluginMtpViewer
  - seems to work, no testing
* integrated #PR606 / MIHO/TestDynamicMenues
* WebBrowser still sucking :-/
* rework AasxBammRdfImExport
* pointless attempt to get WebBrowser running
  -> still not finding .ddls / binary files
  -> also quick&dirty approach not working
* rework AasxPluginExportTable
* integrated PR547
* LARGE REFACTORING to "using Aas = AasCore.Aas3_0_RC02" namespace
* AasxFormatCst
* AasxMqtt, AasxMqttClient
* dictionary import works
* Search/ Replace functioning, without need for attributes in AasCore!!
* Festo Plugins working
* "Jump" function changed, as Asset is no longer IReferable nor Key
  and therefore could only be searched by "GlobalReference"
* try using AasxPluginBase for all plugins,
  including AasxPluginBomStructure, AasxPluginExportTable, AasxPluginMtpViewer, 
  AasxPluginPlotting, AasxPluginWebBrowser, 2 x Festo plugin
* polished tree views for CDs (wishes from Birgit)
* in BOM plugin, unify arguments handing
* BlazorUI compiles & runs