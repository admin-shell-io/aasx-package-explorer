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

* CDs below SME?
* CD below MLP does not work
* NavigateTo: Find also CDs with GlobalReference
* crash: add entity
* web browser not working
* TreeViewItems / MLP / value displayed multi line
* remove unnecessary XAML files from WPF legacy

## Findings / Open questions in spec

* DataSpecification61360.Value is string (aas core) or ShortNameTypeIEC61360 (spec)?
* AssetInformation: UML and table with different ordering of attributes
* AssetInformation.assetType .. what is it?
* Entity.GlobalAssetId is Reference (aas core) or Identifier (spec)?

## Feature Requests to AAS core

* GetSelfDescription() per Element
* Attribute per IClass : Name, ShortName
* Attribute per Member : meta model name
* Constructors for SME taking over attributes from other SMEs (different subtypes!)
* Factory for SubmodelElements
* Constructors without mandatory init parameters ("Bevormundung war letztes Jahrhundert")
* have an attribute telling if there are children (Descend().OfType<ISubmodelElement> .., IEnumerateChildren) or not
* LevelType needs powers of 2 in order to have enum with multiple bits!!
* still xmlns="https://admin-shell.io/aas/3/0/RC02" -> changed to /3/0
* .NET NS also still RC02
* AssetKind/NotApplicable
* AssetInformation.assetType missing?
* mapping between Enum:KeyTpes and various other types, such as Enum:AasSubmodelElements

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

## PRs to be integrated

* from 10 Aug, some parts were already moved (Jui?), some parts not
  https://github.com/admin-shell-io/aasx-package-explorer/commit/2a5f5f6a1d11a56fa874490072b6bdea0ab08527

  => TODO check for every plugin, for Blazor !!

* Fix AnyUI usage:
  https://github.com/admin-shell-io/aasx-package-explorer/compare/master...MIHO/FixAnyUiInPlugins

  => TODO take over changes in DocuShelf..

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
* rework of AssetInformation
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