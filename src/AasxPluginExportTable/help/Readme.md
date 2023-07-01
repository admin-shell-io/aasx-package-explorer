# Export and import tables via Plugin

> Plugin: AasxPluginExportTable

## Export

### General

The plugin was crated to create a meaningful and efficient way to universally export simple tables from 
`Submodel` data, specifically the hierarchical structures of `SubmodelElememnts`. The basic approach is to
iterate through the `Submodel` and underlying `SubmodelElememnts`, providing possibly attached 
`ConceptDescriptions` as well. Therefore, a table will contain a section `table top` which is exported
once per table and a section `table body`, which is exported per table entry.

### Export formats

The table paradigm could be exported to four file formats:

* Tab separated (simple ascii text file, separated by `Tab` characters)
* LaTex (still waiting for an volunteering person!)
* Word (the office applcation, `.docx` format)
* Excel (the office applcation, `.xlsx` format)

### Section grid specification

The `table top` and `table body` sections are each specified by a grid of cells, consisting of a number
of rows and a number of columns. Both sections can have different number of rows but share the number of
columns.

For examples, the usual tables in the Submodel template specifications have 7 x 4 cells for the top and
1 x 4 cells for the body. But, an formatting of 2 x 4 cells for each `SubmodelElememnt` would be 
possible, as well.

For the actual specification dialogue, grids are provided, which are by one larger than the original
dimensions. In this extra row/ column (the first one), a general format specification could be done,
which would then be the same for the all cells in the respective row or column! So, for a possible 
2 x 4 export table, the specifcation grid looks as follows:

| Table format | Col 0 format | Col 1 format | Col 2 format | Colo 3 format |
| ------------ | ------------ | ------------ | ------------ | ------------- |
| Row 0 format | Cell 0, 0    | Cell 0, 1    | Cell 0, 2    | Cell 0,3      |
| Row 1 format | Cell 1, 0    | Cell 1, 1    | Cell 1, 2    | Cell 1,3      |

### Placeholders

For each iteraion of `Submodel` and underlying `SubmodelElememnt`, and additionally the possibly attached 
`ConceptDescription` as well, the attributes are provided by differen placeholders, all of which are
delimited by percentages, e.g. `%CD.idShort%`.

### Format placeholders

Some format placeholders require a parameter, such as the background color specifcation e.g. `bg=Red`.

### Set arithmetics

Because many attributes of the AAS itself are structured in an identical way, a primitive form of
set arithmetics is used to express possible combinations:

For example a notion of: `{Referable|Identifiable}.{attribute}[0..n].{attribute}` 

could allow to specify a placeholder: `SME.semanticId[2].value`

for which as Referable a `SME` would be used, for the first attribute a `semanticId` is given, by
`2` the *third* key of the semanticId is used and here, by `value`, the value of the key is meant.

> Note: this complex set arithmetics works for most of the exports, but not for the *import*!

### Set of placeholders

The following placeholder could be used (to be constantly updated):

```
All placeholders delimited by %{..}%, {} = set arithmetics, [] = optional
{Referable}.{idShort, category, description[@en..], elementName, elementShort, elementShort2, 
elementAbbreviation, kind, parent, details}, {Referable|Identifiable} = {SM, SME, CD}, depth, indent}

{Identifiable}.{identification[.{idType, id}], administration.{ version, revision}}, 
{Qualifiable}.qualifiers, {Qualifiable}.multiplicity

{Reference}, {Reference}[0..n], {Reference}[0..n].{type, local, idType, value}, 
{Reference} = {semanticId, isCaseOf, unitId}

SME.value, Property.{value, valueType, valueId}, MultiLanguageProperty.{value, vlaueId}, 
Range.{valueType, min, max}, Blob.{mimeType, value}, File.{mimeType, value}, 
ReferenceElement.value, RelationshipElement.{first, second}, 
SubmodelElementCollection.{value = #elements, ordered, allowDuplicates}, Entity.{entityType, asset}

CD.{preferredName[@en..], shortName[@en..], unit, unitId, sourceOfDefinition, symbol, dataType, 
definition[@en..], valueFormat}

Special: %*% = match any, %stop% = stop if non-empty, %seq={ascii}% = split sequence by char {ascii}, 
%opt% = optional match

Commands for header cells include: %fg={color}%, %bg={color}% with {color} = {#a030a0, Red, blue, ..}, 
%halign={left, center, right}%, %valign={top, center, bottom}%,
%font={bold, italic, underline}, %frame={0,1,2,3}% (only whole table)
```

### Plugin's user dialogue

After activating the menu command, the plugin always the user dialogue. In this quite messy screen, the
export (and import) format can be specified.

As specifying all of this, `presets` of the current specification can be loaded and saved as JSON files.
This JSON files could be also used as template to define a set of presets, which is automatically loaded
on program startup and selected by the respective combo box.

> Note: a load, resize or selection of combo box will overwrite the current specification 
> without further ado!

Some further options:

`Act in hierarchy`: By default, all SubmodelElements of all hierarchy levels will be exported into one
table; the `%depth%` placeholder will render the numerical hierarchy depth, the `%indent%` placeholder
will arespective sequence of `~` characters to visually care for an indent. If set, this option will
split the elements in multiple table, each for one changing hierarchy level. After a table, a number
of `gap` empty sections/ paragraphs are inserted in the generated document to delimit the tables.

`Replace failed matches`: if an placholder could not be filled with content, it is left as normal
ascii text within the cell. This holds also true for mispelled placeholders. If the option is set,
these placeholders will be replaced by the indicated string, including the possibility of a *blank*
or *empty* string. This cares for a clean export, a possible manual post-process string replacement 
with an text editor or to put multiple placeholders into one cell, implicitely concatening only the
resulting contents.

Finally, `Start ... ` will query a filename with adequate file extension. A overwrite warning may
occur.

## Import

### General

In recent versions of the AASX Package Explorer, the above mechanism can be used for an import, as 
well. The vision is a true roundtrip workflow with iterated engineering between AASX and Word an
Excel, but this vision is still ongoing.

The idea is to use an identical grid specification to match *incoming* table cells. The different
placeholder partially provide an adaptive behaviour, to cope with different formats of provided
text input, hence not an over-specification is necessary. However, not all placeholders are 
currently supported for input. This is an ongoing activity.

If `Act in hierarchy` is set: `gap` number of rows is used to figure out, when one table has 
ended and another table is to be started.

> Note: always introduce multiple empty paragrpahs/ rows to allow the algorithm to detect a
> further table.

### Special import placeholder

For controlling import matching, some special placeholders are provided:

```
Special: %*% = match any, %stop% = stop if non-empty, %seq={ascii}% = split sequence by char {ascii}, 
%opt% = optional match
```

`%opt%` as very first placeholder in the cell will declare matching optional, that is, the 
matching process will not stop unsuccessfull for the row/ table, even if not matching 
text input is found.

`%*%` will match any sequence of characters in the cell. This can be used to ignore some 
pre-formatted content or make the mapping more adaptable.

`%stop%` will stop matching, when already a successful matching (to a previous placeholder) took
place. By this, a kind of either-or beheviour could be realized.

`%seq={ascii}%` will allow to take a text input to a cell as a multiple of distinct parts
which allows multiple placeholders to match sequentially. The `{ascii}` argument allows to
specify a delimiting character to split the text input into parts. To specify a character,
each of `%seq=<NL>%`, `%seq=\n%`, `%seq=10%` would end in matching the newline charachter.

## Preset configuration

### General

Preset configuration is done by modifying the file `AasxPluginExportTable.options.json`, which could
be found in the respective plugin folder.

The basic format of the JSON is as follows:

```
{
  <overall option>,
  ..
  <overall option>,

  "Preset": [
    { .. <content of JSON export of a configuration> }, 
    { .. <content of JSON export of a configuration> }, 
    ..
    { .. <content of JSON export of a configuration> }, 
  ]
}
```

### Overall options

`TemplateIdConceptDescription` gives an template for the id, if no id for an CD is given by the 
import.