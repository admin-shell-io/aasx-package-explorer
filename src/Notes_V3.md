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

## Decisions

* After phone call with Birgit, when importing a V20, set all Keys with ConceptDescription to GlobalReference assuming it is always a external concept
* for "PackCntChangeEventData.ThisElem" and such: use AAS env as containing object for CDs, as
  List<CD> does not fulfill IClass => this will get tricky in the future for event handling 
  - Rethink?
  - VisualAasxElements.UpdateByEvent() was using this -> changed
  - see: ThisElemLocation = PackCntChangeEventLocation.ListOfConceptDescriptions

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
  - rework was required; as pointer/class was changed to simple enum (which is fantastic)
* worked over ModelKind
* "Print asset code sheet" does not work! => solved, new version of ZXing required renderer!
* DispEdit shows key: "Editing of entities" / "ISubmodelElement:"
  => changed to "SubmodelElement:" (use wording of DotAAS)
* Delete of CD does not refresh tree => solved, see decision for PackCntChangeEventLocation.ListOfConceptDescriptions
* Multi-select of CD leads to exception => solved, see decision for PackCntChangeEventLocation.ListOfConceptDescriptions
* Create AAS from scratch fails! => solved by checking Count of entities
* AssetInfo / defaultThumbnail/ Create empty File element does not work!
  => solved, have DisplayOrEditEntityFileResource()
* Asset below AAS? => done