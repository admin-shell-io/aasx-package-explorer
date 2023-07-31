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

## Todo (w.r.t. recently V3.0)

* SMC / Add Prop / does not focus new Prop -- open, frustrated :-(
* CDs / Add CD / does not focus new CD!! (but has a unique id...) -- same issue :-(
* serialization of plugin options does not work with Stringify(): "Type" is 20 instead of "GlobalReference"
  -- refactored, but still not with AAS core
* Ctrl-V does not work in modal dialogues .. (Blazor only?)
  -> not sop easy to provoke .. test again ..
* Copy "single/ recursive" for Submodels is not great

* Entity is realized like a Collection, a List or a Submodel , however the attribute 
  is called “statement” in contrast to “value”. This is a little bit confusing. 
  Should we add at least some note “add statement as” .. ???????

* Bug, Extensions erlaubt 0..* refersTo. „Add Reference“ tut nichts. Man kann nur maximal eine 
  Reference pro Extension hinzufügen

* ExportUML / cancel -> Crash?

* Refactor does not preserve Extensions (e.g. Blob to File)

* lag for "Add known" is, when the UC dialogue creates a new pool object ..
  var uc = new SelectFromReferablesPoolFlyout(AasxPredefinedConcepts.DefinitionsPool.Static);
  public static DefinitionsPool Static { get { return thePool; } }

* plugin known submodels does not render correctly for Blazor

 move up/down of SM does not work always

* "Adds known" / for SM (DPP v20) there is Id not SemanticId!

* ExportPreDef double exports CDs???

* multiple SMC in SML marked -> no "delete!

* AAS event compressor for just update value events does obviously not work ?!!

* change "Ctrl-C, Ctrl-V" to more exotic shortcuts (often interfering with WPF/ Browser
  default behaviour) -- done

* Submodel "move up" (fast re-ordering) show other order than real (slow) ordering!

* switch from Aas.Environmet to Aas.IEnvironment

## Done (w.r.t. recently V3.0)

* AAS / data spec / "Add known" ?? by preset list?? -- done
* AAS / gobalAssetId: remove "create data element", as being pointless -- no, is "string?"
* AAS / globalAssetId: "Generate", "Enter" missing -- done
* Suppl. file / source file to add/ drop box: wrong color -- done
* SM (SMC) / "Copy recursively" will make ID unique, even if not required -- done
* right hand panel / "Title" is in cyan (wrong color) -- done (already, by AnyUI colors)
* SM / remove "Turn to kind Tempatle/Inst" -- done
* SM / add "Remove extensions" -- done
* for all: new "Administrative information" -> creator, template-id -- done
* SME Refactor does not work? (example: SMC -> event element) -- done
* SME / Qualifiers / "Add preset": error while loading presets (Ifx serialize) -- done
* SME / Qualifiers / value / multi line / title string wrong -- done
* new CD / IEC61360 / "EN?" language makes no sense (either "", "en")
  casing!!! -- done, changed to be configurable via "DefaultLang"
* when auto-load, "Save as .." will not point the the intended directory -- fixed (?)
* AssetInformation could be NULL -> trouble for serialization -- introduced "SilentFix30",
  already warnings existed -- somehow done
* convert V2.0/Cardinality to SMT/Cardinality -- done
* SMT assessment: check if embedded data spec (e.g. IEC61360) is present -- done
* "Qualifiers (all) / Migrate to extension"" can be activated w/o edit mode -- done
* When adding a predefined qualifier it would be great to also set the (new attribute) 
  qualifierKind automatically. -- Done, was just a matter of configuring qualifier-presets.json
* Copy/paste does not work for submodels (neither Buffer, nor copy recursively)
  -- buffer works, copy recursively is fishy
* could not add Statements to Entities -- works
* Bugfix: New attribute „typeAsset“ of type “Identifier” is missing -- done
* Etwas „seltsam“, dass man zwar auf „All Submodels“ klicken kann, aber 
  rechts kein „Add Submodel“ erhält -- done
* Man könnte Text ergänzen, dass category „deprecated“ ist
  -- There is a already hint, if the category is used. Elsewise, its a waste of screenspace
* Major ist der bug, dass ich keine Elemente hinzufügen kann für SM, Collection usw., 
  nur wenn ich von existierenden SMT mit einer enthaltenen Entity ausgegangen bin, die ich 
  dann ändern kann (solange ich keine weitere Collection etc. hinzufügen 
  möchte…wirklich nur ändern) -- done
* Level type funktioniert nicht (mehr): war früher enum und ist jetzt ein Struct.
  -- Yes, data structure of AAS core was completely changed for this entity. Reworked. Done.
* Property/valueId is still KeyList instead of Reference -- done
* specificAssetId: name/value not key/value!! Wording "IdentifierKeyValue" not 
  longer existing. -- Done
* Entity / add specificAssetId .. does not work -- Done
* specificAssetId: name/value not key/value!! Wording "IdentifierKeyValue" not 
  longer existing. -- Done
* when Find/ Replace: before invoke "Replace all", "Start" needs to be invoked! -- Done
* "add known" .. alphabetic sorting of domains and individual names of semanticIds.
  Done for WPF and Blazor (WPF keeps also last selected).
* Export table / cells / allow return -- done (re-structed TextBox / MultiLine)

* Bug from Matthias Freund:  mir ist gerade ein Bug in „AasxCsharpLibrary/Extensions/ExtendedReference.cs“ aufgefallen. In der Methode
public static bool Matches(this IReference reference, IReference otherReference, MatchMode matchMode = MatchMode.Strict)
werden  die Keys miteinander verglichen. Wenn die Anzahl der Keys der ‚otherReference‘ allerdings größer als die Anzahl der Keys der ‚reference‘ sind, wird trotzdem ein passender Match zurückgegeben, solange nur die ersten Keys zusammen passen.

Hier müsste in Zeile 167 statt nur
if (reference.Keys == null || reference.Keys.Count == 0 || otherReference?.Keys == null || otherReference.Keys.Count == 0)
auch noch auf die Länge der Keys verglichen werden:
if (reference.Keys == null || reference.Keys.Count == 0 || otherReference?.Keys == null || otherReference.Keys.Count == 0 || reference.Keys.Count != otherReference.Keys.Count)

-- done

## Notes (influencing todos)

* : valueType with "xs:..." headers
* introduced "SilentFix30" for fixing flaws in the serialization
  THIS IS REALLY ANNOYING!!!
  - AAS core crashes without AssetInformation
  - AAS core crashes without EmbeddedDataSpecification.DataSpecificationContent
  - AAS core crashes without EmbeddedDataSpecification.DataSpecificationContent.PreferredName
  - AAS core crashes when ReferenceElement.value = null is serialized
  - AAS core crashes when RelationshipElement.first/second = null is serialized


## Todo (old RC02)

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
  „No open AASX file to be saved“. -> fixed
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

* Difficult to handle mixed (de-) serializations. CLARIFY WITH AAS CORE teal.
  Currently, AdaptiveAasIClassConverter is being used.

## Discussion topics for handling AAS

* how to handle "derivedFrom"; how does "cloning" information work
* how two handle 2++ Submodels with different versions?
  - same idShort?


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

* AAs core / ISubmodelElement shall have an optimized DescendOnceSme()
  (idea: DescendOnce() may yield first all SMEs and then all other IClass .. then it could done by breaking)

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

These projects are not migrated or integrated, yet:

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

# Notes w.r.t. to scripting

```
.\AasxPackageExplorer.exe -read-json options-debug.MIHO.json -aasx-to-load "C:\HOMI\Develop\Aasx\repo\IDTA 02003-1-2_Template_TechnicalData.aasx" -log-file out.log -cmd 'Tool(\"AssessSmt\", \"Target\", \"test.xlsx\"); Tool(\"Exit\");'
```


