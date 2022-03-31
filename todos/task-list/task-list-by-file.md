## AasxAmlImExport\AmlExport.cs

[Line 871, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxAmlImExport/AmlExport.cs#L871
), 
Michael Hoffmeister,
2020-08-01

    If further data specifications exist (in future), add here

## AasxAmlImExport\AmlImport.cs

[Line 169, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxAmlImExport/AmlImport.cs#L169
), 
MIHO,
2020-08-01

    The check for role class or requirements is still questionable
    but seems to be correct (see below)
    
    Question MIHO: I dont understand the determinism behind that!
    WIEGAND: me, neither ;-)
    Wiegand:  ich hab mir von Prof.Drath nochmal erklären lassen, wie SupportedRoleClass und
    RoleRequirement verwendet werden:
    In CAEX2.15(aktuelle AML Version und unsere AAS Mapping Version):
      1.Eine SystemUnitClass hat eine oder mehrere SupportedRoleClasses, die ihre „mögliche Rolle
        beschreiben(Drucker / Fax / kopierer)
      2.Wird die SystemUnitClass als InternalElement instanziiert entscheidet man sich für eine
        Hauptrolle, die dann zum RoleRequirement wird und evtl. Nebenklassen die dann
        SupportedRoleClasses sind(ist ein Workaround weil CAEX2.15 in der Norm nur
        ein RoleReuqirement erlaubt)
    InCAEX3.0(nächste AMl Version):
      1.Wie bei CAEX2.15
      2.Wird die SystemUnitClass als Internal Elementinstanziiert werden die verwendeten Rollen
        jeweils als RoleRequirement zugewiesen (in CAEX3 sind mehrere RoleReuqirements nun erlaubt)

[Line 1436, column 45](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxAmlImExport/AmlImport.cs#L1436
), 
Michael Hoffmeister,
2020-08-01

    fill out 
    eds.hasDataSpecification by using outer attributes

## AasxCsharpLibrary.Tests\TestLoadSaveChain.cs

[Line 42, column 5](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary.Tests/TestLoadSaveChain.cs#L42
), 
mristin,
2020-10-05

    The class is unused since all its tests were disabled temporarily and
    will be fixed in the near future.
    
    Once the tests are enabled, please remove this Resharper directive.

[Line 92, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary.Tests/TestLoadSaveChain.cs#L92
), 
mristin,
2020-10-05

    This test has been temporary disabled so that we can merge in the branch
    MIHO/EnhanceDocumentShelf. The test should be fixed in a future pull request and we will then re-enable it
    again.
    
    Please do not forget to remove the Resharper directive at the top of this class.
    
    [TestCase(".xml")]
    
    dead-csharp ignore this comment

[Line 132, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary.Tests/TestLoadSaveChain.cs#L132
), 
mristin,
2020-10-05

    This test has been temporary disabled so that we can merge in the branch
    MIHO/EnhanceDocumentShelf. The test should be fixed in a future pull request and we will then re-enable it
    again.
    
    Please do not forget to remove the Resharper directive at the top of this class.
    
    [Test]
    
    dead-csharp ignore this comment

[Line 155, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary.Tests/TestLoadSaveChain.cs#L155
), 
mristin,
2020-09-17

    Remove autofix once XSD and Aasx library in sync
    
    Package has been loaded, now we need to do an automatic check & fix.
    
    This is necessary as Aasx library is still not conform with the XSD AASX schema and breaks
    certain constraints (*e.g.*, the cardinality of langString = 1..*).

## AasxCsharpLibrary\AasxCompatibilityModels\V10\AdminShellV10.cs

[Line 1843, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L1843
), 
Michael Hoffmeister,
1970-01-01

    in V1.0, shall be a list of embeddedDataSpecification

[Line 2561, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L2561
), 
Michael Hoffmeister,
1970-01-01

    Qualifiers not working!

[Line 2921, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L2921
), 
Michael Hoffmeister,
1970-01-01

    Operation

[Line 3900, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L3900
), 
Michael Hoffmeister,
1970-01-01

    use aasenv serialzers here!

[Line 3925, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L3925
), 
Michael Hoffmeister,
1970-01-01

    use aasenv serialzers here!

[Line 4032, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L4032
), 
Michael Hoffmeister,
1970-01-01

    use aasenv serialzers here!

[Line 4061, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L4061
), 
Michael Hoffmeister,
1970-01-01

    use aasenv serialzers here!

[Line 4088, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L4088
), 
Michael Hoffmeister,
1970-01-01

    use aasenv serialzers here!

## AasxCsharpLibrary\AdminShell.cs

[Line 1411, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L1411
), 
MIHO,
2020-08-30

    this does not prevent the corner case, that we could have
    * multiple dataSpecificationIEC61360 in this list, which would be an error

[Line 3468, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L3468
), 
MIHO,
2020-08-27

    According to spec, cardinality is [1..1][1..n]

[Line 3472, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L3472
), 
MIHO,
2020-08-27

    According to spec, cardinality is [0..1][1..n]

[Line 3503, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L3503
), 
MIHO,
2020-08-27

    According to spec, cardinality is [0..1][1..n]

[Line 3782, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L3782
), 
MIHO,
2020-08-30

    align wording of the member ("embeddedDataSpecification") with the 
    * wording of the other entities ("hasDataSpecification")

[Line 4539, column 33](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L4539
), 
MIHO,
2020-08-26

    not very elegant, yet. Avoid temporary collection

[Line 5298, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L5298
), 
Michael Hoffmeister,
2020-08-01

    check, if Json has Qualifiers or not

[Line 5766, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L5766
), 
MIHO,
2021-07-08

    obvious error .. info should receive semanticId .. but would change

[Line 5860, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L5860
), 
MIHO,
2021-08-12

    consider using:
    Activator.CreateInstance(pl.GetType(), new object[] { pl })

[Line 6225, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L6225
), 
MIHO,
2020-07-31

    would be nice to use IEnumerateChildren for this ..

[Line 6361, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L6361
), 
MIHO,
2021-10-18

    there are overlaps of this new function with
    * this old function: FindFirstAnySemanticId(Key[] semId ..
    * clarify/ refactor

## AasxCsharpLibrary\AdminShellPackageEnv.cs

[Line 290, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellPackageEnv.cs#L290
), 
Michael Hoffmeister,
2020-08-01

    use a unified function to create a serializer

[Line 476, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellPackageEnv.cs#L476
), 
Michael Hoffmeister,
2020-08-01

    use a unified function to create a serializer

[Line 519, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellPackageEnv.cs#L519
), 
Michael Hoffmeister,
2020-08-01

    use a unified function to create a serializer

[Line 554, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellPackageEnv.cs#L554
), 
Michael Hoffmeister,
2020-08-01

    use a unified function to create a serializer

[Line 611, column 37](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellPackageEnv.cs#L611
), 
MIHO,
2021-01-02

    check again.
    * Revisiting this code after a while, and after
    * the code has undergo some changes by MR, the following copy command needed
    * to be amended with a if to protect against self-copy.

## AasxCsharpLibrary\AdminShellUtil.cs

[Line 212, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellUtil.cs#L212
), 
MIHO,
2020-11-12

    replace with Regex for multi language. Ideally have Exception messages
    always as English.

## AasxDictionaryImport.Tests\Cdd\TestImport.cs

[Line 83, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport.Tests/Cdd/TestImport.cs#L83
), 
Robin,
2020-09-03

    please check

[Line 99, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport.Tests/Cdd/TestImport.cs#L99
), 
Robin,
2020-09-03

    please check

## AasxDictionaryImport.Tests\Cdd\TestModel.cs

[Line 555, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport.Tests/Cdd/TestModel.cs#L555
), 
krahlro-sick,
2020-07-31

    make sure that there are no duplicates

## AasxDictionaryImport\Eclass\Model.cs

[Line 395, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport/Eclass/Model.cs#L395
), 
krahlro-sick,
2021-02-03

    HTML-decode SI code

[Line 820, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport/Eclass/Model.cs#L820
), 
krahlro-sick,
2021-02-23

    This logic is copied from EclassUtils.GenerateConceptDescription -- does

## AasxDictionaryImport\Iec61360Utils.cs

[Line 126, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport/Iec61360Utils.cs#L126
), 
Robin,
2020-09-03

    MIHO is not sure, if the data spec reference is correct; please check

[Line 142, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport/Iec61360Utils.cs#L142
), 
Robin,
2020-09-03

    MIHO is not sure, if the data spec reference is correct; please check

[Line 158, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport/Iec61360Utils.cs#L158
), 
Robin,
2020-09-03

    check this code

## AasxFormatCst\AasxToCst.cs

[Line 227, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxFormatCst/AasxToCst.cs#L227
), 
MIHO,
2021-05-28

    extend Parse() to parse also ECLASS, IEC CDD

## AasxIntegrationBase\AasxPluginOptionSerialization.cs

[Line 127, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxIntegrationBase/AasxPluginOptionSerialization.cs#L127
), 
MIHO,
2021-06-06

    move code to AasForms source file

## AasxIntegrationBaseWpf\EmptyFlyout.xaml.cs

[Line 35, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxIntegrationBaseWpf/EmptyFlyout.xaml.cs#L35
), 
MIHO,
2020-12-21

    make DiaData non-Nullable

## AasxMqttClient\MqttClient.cs

[Line 124, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxMqttClient/MqttClient.cs#L124
), 
MIHO,
2021-06-30

    check use of Url()

## AasxPackageExplorer.Tests\TestOptionsAndPlugins.cs

[Line 174, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageExplorer.Tests/TestOptionsAndPlugins.cs#L174
), 
mristin,
2020-11-13

    @MIHO please check -- Options should be null, not empty?

[Line 298, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageExplorer.Tests/TestOptionsAndPlugins.cs#L298
), 
Marko Ristin,
2021-07-09

    not clear, how this test could pass. As of today,

[Line 302, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageExplorer.Tests/TestOptionsAndPlugins.cs#L302
), 
Marko Ristin,
2021-07-09

    could not fix

## AasxPackageExplorer\Flyout\MqttPublisherFlyout.xaml.cs

[Line 36, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageExplorer/Flyout/MqttPublisherFlyout.xaml.cs#L36
), 
MIHO,
2020-12-21

    make DiaData non-Nullable

## AasxPackageExplorer\MainWindow.xaml.cs

[Line 333, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageExplorer/MainWindow.xaml.cs#L333
), 
MIHO,
2020-12-31

    check for ANYUI MIHO

[Line 362, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageExplorer/MainWindow.xaml.cs#L362
), 
MIHO,
2021-12-27

    consider extending for better testing or
    * script running

[Line 1454, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageExplorer/MainWindow.xaml.cs#L1454
), 
MIHO,
2021-10-28

    Check, if a better solution exists 
    * to instrument event updates in a way that they're automatically
    * visualized

[Line 1559, column 49](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageExplorer/MainWindow.xaml.cs#L1559
), 
MIHO,
2021-10-09

    prepare path to be relative

[Line 1570, column 49](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageExplorer/MainWindow.xaml.cs#L1570
), 
MIHO,
2021-10-09

    prepare path to be relative

[Line 2264, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageExplorer/MainWindow.xaml.cs#L2264
), 
MIHO,
2021-06-08

    find the root cause instead of doing a quick-fix

## AasxPackageLogic\DispEditHelperBasics.cs

[Line 1203, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperBasics.cs#L1203
), 
MIHO,
2020-12-21

    function & if-clause is obsolete

[Line 1366, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperBasics.cs#L1366
), 
Michael Hoffmeister,
2020-08-01

    possibly [Jump] button??

[Line 1552, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperBasics.cs#L1552
), 
MIHO,
2021-02-16

    this mechanism is ugly and only intended to be temporary!
    It shall be replaced (after intergrating AnyUI) by a better repo handling

[Line 1573, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperBasics.cs#L1573
), 
Michael Hoffmeister,
2020-08-01

    Needs to be revisited

[Line 2540, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperBasics.cs#L2540
), 
MIHO,
2021-08-17

    check if more SME types to serialize

## AasxPackageLogic\DispEditHelperCopyPaste.cs

[Line 340, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperCopyPaste.cs#L340
), 
MIHO,
2021-06-22

    think of converting Referable to IAasElement

[Line 572, column 33](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperCopyPaste.cs#L572
), 
Michael Hoffmeister,
2020-08-01

    Operation complete?

[Line 611, column 33](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperCopyPaste.cs#L611
), 
Michael Hoffmeister,
2020-08-01

    Operation complete?

[Line 630, column 33](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperCopyPaste.cs#L630
), 
MIHO,
2021-08-18

    createAtIndex missing here

## AasxPackageLogic\DispEditHelperEntities.cs

[Line 1504, column 37](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperEntities.cs#L1504
), 
MIHO,
2021-08-17

    create events for CDs are not emitted!

[Line 1885, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperEntities.cs#L1885
), 
MIHO,
2020-09-01

    extend the lines below to cover also data spec. for units

[Line 3689, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperEntities.cs#L3689
), 
MIHO,
2021-02-16

    this mechanism is ugly and only intended to be temporary!
    It shall be replaced (after intergrating AnyUI) by a better repo handling

## AasxPackageLogic\DispEditHelperMultiElement.cs

[Line 433, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/DispEditHelperMultiElement.cs#L433
), 
MIHO,
2021-07-08

    check for completeness

## AasxPackageLogic\ModifyRepo.cs

[Line 24, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/ModifyRepo.cs#L24
), 
MIHO,
2021-06-28

    eliminate ModifyRepo. Right now, it is used as boolean flag while editing

## AasxPackageLogic\PackageCentral\PackageCentral.cs

[Line 257, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageCentral.cs#L257
), 
MIHO,
2021-01-07

    rename to plural

## AasxPackageLogic\PackageCentral\PackageConnectorHttpRest.cs

[Line 247, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageConnectorHttpRest.cs#L247
), 
all,
2021-01-30

    check periodically for supported element types

[Line 305, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageConnectorHttpRest.cs#L305
), 
MIHO,
2021-01-03

    check to handle more SMEs for AasEventMsgUpdateValue

[Line 306, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageConnectorHttpRest.cs#L306
), 
MIHO,
2021-01-04

    ValueIds still missing ..

[Line 650, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageConnectorHttpRest.cs#L650
), 
MIHO,
2021-11-07

    refactor use of SetParentsForSME to be generic

[Line 751, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageConnectorHttpRest.cs#L751
), 
MIHO,
2021-05-21

    make sure, this is required by the specification!

[Line 787, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageConnectorHttpRest.cs#L787
), 
MIHO,
2021-10-09

    Modify missing!!

[Line 835, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageConnectorHttpRest.cs#L835
), 
MIHO,
2021-05-28

    to be implemented

[Line 844, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageConnectorHttpRest.cs#L844
), 
MIHO,
2021-05-28

    to be implemented

[Line 853, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageConnectorHttpRest.cs#L853
), 
MIHO,
2021-05-28

    to be implemented

## AasxPackageLogic\PackageCentral\PackageContainerBase.cs

[Line 362, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageContainerBase.cs#L362
), 
MIHO,
2021-08-17

    check if to refactor/ move to another location 
    * and to extend to Submodels ..

[Line 375, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageContainerBase.cs#L375
), 
MIHO,
2021-08-17

    add more type specific conversions?

[Line 451, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageContainerBase.cs#L451
), 
MIHO,
2021-01-03

    check to handle more SMEs for AasEventMsgUpdateValue

## AasxPackageLogic\PackageCentral\PackageContainerBuffered.cs

[Line 65, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageContainerBuffered.cs#L65
), 
MIHO,
2020-12-25

    think of creating a temp file which resemebles the source file

## AasxPackageLogic\PackageCentral\PackageContainerFactory.cs

[Line 160, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageContainerFactory.cs#L160
), 
MIHO,
2021-02-01

    check, if demo option is still required

## AasxPackageLogic\PackageCentral\PackageContainerListBase.cs

[Line 324, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageContainerListBase.cs#L324
), 
MIHO,
2020-08-05

    refacture this with DispEditHelper.cs

## AasxPackageLogic\PackageCentral\PackageContainerListHttpRestRepository.cs

[Line 106, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageContainerListHttpRestRepository.cs#L106
), 
MIHO,
2021-01-08

    check, how to make absolute

## AasxPackageLogic\PackageCentral\PackageContainerLocalFile.cs

[Line 143, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageContainerLocalFile.cs#L143
), 
MIHO,
2020-12-15

    consider removing "indirectLoadSave" from AdminShellPackageEnv

## AasxPackageLogic\PackageCentral\PackageContainerRepoItem.cs

[Line 122, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/PackageCentral/PackageContainerRepoItem.cs#L122
), 
MIHO,
2021-01-08

    add SubmodelIds

## AasxPackageLogic\VisualAasxElements.cs

[Line 202, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/VisualAasxElements.cs#L202
), 
MIHO,
2020-07-31

    check if commented out because of non-working multi-select?

[Line 2373, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageLogic/VisualAasxElements.cs#L2373
), 
MIHO,
2021-06-11

    Submodel needs to be set in the long run

## AasxPluginExportTable\Uml\BaseWriter.cs

[Line 62, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPluginExportTable/Uml/BaseWriter.cs#L62
), 
MIHO,
2021-12-24

    check if to refactor multiplicity handling as utility

## AasxPluginPlotting\PlotItem.cs

[Line 295, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPluginPlotting/PlotItem.cs#L295
), 
MIHO,
2021-01-04

    consider at least to include MLP, as well

## AasxPluginPlotting\PlottingViewControl.xaml.cs

[Line 1985, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPluginPlotting/PlottingViewControl.xaml.cs#L1985
), 
MIHO,
2021-11-09

    AasxPlugPlotting does not allow all options

## AasxPluginUaNetClient\UASampleClient.cs

[Line 1, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPluginUaNetClient/UASampleClient.cs#L1
), 
MIHO,
2020-08-06

    lookup SOURCE!

## AasxPluginUaNetServer\Plugin.cs

[Line 45, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPluginUaNetServer/Plugin.cs#L45
), 
MIHO,
2021-11-17

    damned, weird dependency reasons between
    * .netstandard2.0 and .net472 seem NOT TO ALLOW referring to AasxIntegrationBase.
    * Fix

## AasxPluginWebBrowser\Plugin.cs

[Line 144, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPluginWebBrowser/Plugin.cs#L144
), 
MIHO,
2020-08-02

    when dragging the divider between elements tree and browser window,

## AasxSignature\AasxSignature.cs

[Line 30, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxSignature/AasxSignature.cs#L30
), 
Andreas Orzelski,
2020-08-01

    The signature file and [Content_Types].xml can be tampered?

[Line 180, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxSignature/AasxSignature.cs#L180
), 
Andreas Orzelski,
2020-08-01

    Is package according to the Logical model of the AAS?

[Line 214, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxSignature/AasxSignature.cs#L214
), 
Andreas Orzelski,
2020-08-01

    is package sealed? => no other signatures can be added?

[Line 217, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxSignature/AasxSignature.cs#L217
), 
Andreas Orzelski,
2020-08-01

    The information from the analysis
    -> return as an object (list of enums with the issues/warings???)

## AasxToolkit.Tests\TestProgram.cs

[Line 263, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxToolkit.Tests/TestProgram.cs#L263
), 
mristin,
2020-10-30

    add json once the validation is in place.
     Michael Hoffmeister had it almost done today.
     
    Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "TestResources\\AasxToolkit.Tests\\sample.json")
        
        dead-csharp ignore this comment

## AasxUaNetConsoleServer\Program.cs

[Line 10, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetConsoleServer/Program.cs#L10
), 
MIHO,
2020-08-03

    check SOURCE

## AasxUaNetServer\AasxServer\AasEntityBuilder.cs

[Line 280, column 33](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasEntityBuilder.cs#L280
), 
MIHO,
2020-08-06

    check, which namespace shall be used

## AasxUaNetServer\AasxServer\AasUaEntities.cs

[Line 20, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L20
), 
MIHO,
2020-08-29

    The UA mapping needs to be overworked in order to comply the joint aligment with I4AAS

[Line 21, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L21
), 
MIHO,
2020-08-29

    The UA mapping needs to be checked for the "new" HasDataSpecification strcuture of V2.0.1

[Line 695, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L695
), 
MIHO,
2020-08-06

    check (again) if reference to CDs is done are shall be done

[Line 986, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L986
), 
MIHO,
2020-08-06

    not sure if to add these

[Line 1087, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1087
), 
MIHO,
2020-08-06

    use the collection element of UA?

[Line 1423, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1423
), 
MIHO,
2020-08-06

    decide to from where the name comes

[Line 1426, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1426
), 
MIHO,
2020-08-06

    description: get "en" version which is appropriate?

[Line 1429, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1429
), 
MIHO,
2020-08-06

    parse UA data type out .. OK?

[Line 1438, column 33](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1438
), 
MIHO,
2020-08-06

    description: get "en" version is appropriate?

[Line 1447, column 37](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1447
), 
MIHO,
2020-08-06

    this any better?

[Line 1451, column 37](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1451
), 
MIHO,
2020-08-06

    description: get "en" version is appropriate?

[Line 1795, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1795
), 
MIHO,
2020-08-06

    check, if to make super classes for UriDictionaryEntryType?

## AasxUaNetServer\Base\SampleNodeManager.cs

[Line 666, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/Base/SampleNodeManager.cs#L666
), 
MIHO,
2020-08-06

    check, if this is valid use of the SDK. MIHO added this

## AasxUaNetServer\SampleServer.cs

[Line 173, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/SampleServer.cs#L173
), 
MIHO,
2020-08-04

    To be checked by Andreas. All applications have software certificates

## AasxUANodesetImExport\UANodeSet.cs

[Line 24, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUANodesetImExport/UANodeSet.cs#L24
), 
Michael Hoffmeister,
2020-08-01

    Fraunhofer IOSB: Check ReSharper to be OK

## AasxUANodesetImExport\UANodeSetExport.cs

[Line 30, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUANodesetImExport/UANodeSetExport.cs#L30
), 
Michael Hoffmeister,
1970-01-01

    License

[Line 31, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUANodesetImExport/UANodeSetExport.cs#L31
), 
Michael Hoffmeister,
1970-01-01

    Fraunhofer IOSB: Check ReSharper

## AasxUANodesetImExport\UANodeSetImport.cs

[Line 27, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUANodesetImExport/UANodeSetImport.cs#L27
), 
Michael Hoffmeister,
2020-08-01

    Fraunhofer IOSB: Check ReSharper settings to be OK

## AasxWpfControlLibrary\AnyUiWpf.cs

[Line 986, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/AnyUiWpf.cs#L986
), 
MIHO,
2020-12-21

    can be realized without tedious central dispatch?

## AasxWpfControlLibrary\DiplayVisualAasxElements.xaml.cs

[Line 275, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/DiplayVisualAasxElements.xaml.cs#L275
), 
MIHO,
2021-11-09

    check, if clearing selected items on refresh is required

[Line 496, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/DiplayVisualAasxElements.xaml.cs#L496
), 
MIHO,
2021-01-04

    check to replace all occurences of RefreshFromMainData() by
    * making the tree-items ObservableCollection and INotifyPropertyChanged

[Line 812, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/DiplayVisualAasxElements.xaml.cs#L812
), 
MIHO,
2020-07-21

    was because of multi-select

## AasxWpfControlLibrary\DispEditAasxEntity.xaml.cs

[Line 158, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/DispEditAasxEntity.xaml.cs#L158
), 
MIHO,
2020-12-24

    check if required

## AasxWpfControlLibrary\Flyouts\ChangeElementAttributesFlyout.xaml.cs

[Line 35, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/Flyouts/ChangeElementAttributesFlyout.xaml.cs#L35
), 
MIHO,
2020-12-21

    make DiaData non-Nullable

## AasxWpfControlLibrary\Flyouts\ProgressBarFlyout.xaml.cs

[Line 35, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/Flyouts/ProgressBarFlyout.xaml.cs#L35
), 
MIHO,
2020-12-21

    make DiaData non-Nullable

## AasxWpfControlLibrary\Flyouts\SelectAasEntityFlout.xaml.cs

[Line 38, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/Flyouts/SelectAasEntityFlout.xaml.cs#L38
), 
MIHO,
2020-12-21

    make DiaData non-Nullable

## AasxWpfControlLibrary\Flyouts\SelectFromListFlyout.xaml.cs

[Line 40, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/Flyouts/SelectFromListFlyout.xaml.cs#L40
), 
MIHO,
2020-12-21

    make DiaData non-Nullable

## AasxWpfControlLibrary\Flyouts\SelectFromReferablesPoolFlyout.xaml.cs

[Line 42, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/Flyouts/SelectFromReferablesPoolFlyout.xaml.cs#L42
), 
MIHO,
2020-12-21

    make DiaData non-Nullable

## AasxWpfControlLibrary\Flyouts\SelectQualifierPresetFlyout.xaml.cs

[Line 41, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/Flyouts/SelectQualifierPresetFlyout.xaml.cs#L41
), 
MIHO,
2020-12-21

    make DiaData non-Nullable

## AasxWpfControlLibrary\Flyouts\TextBoxFlyout.xaml.cs

[Line 35, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/Flyouts/TextBoxFlyout.xaml.cs#L35
), 
MIHO,
2020-12-21

    make DiaData non-Nullable

## AasxWpfControlLibrary\Flyouts\TextEditorFlyout.xaml.cs

[Line 37, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/Flyouts/TextEditorFlyout.xaml.cs#L37
), 
MIHO,
2020-12-21

    make DiaData non-Nullable

## AasxWpfControlLibrary\PackageCentral\PackageContainerListOfListControl.xaml.cs

[Line 123, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageContainerListOfListControl.xaml.cs#L123
), 
MIHO,
2021-01-09

    check to use moveup/down of the PackageContainerListBase

[Line 134, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageContainerListOfListControl.xaml.cs#L134
), 
MIHO,
2021-01-09

    check to use moveup/down of the PackageContainerListBase

## AnyUi\AnyUiContextBase.cs

[Line 139, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AnyUi/AnyUiContextBase.cs#L139
), 
MIHO,
2020-12-24

    check if to move/ refactor these functions

## WpfMtpControl\MtpAmlHelper.cs

[Line 51, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpControl/MtpAmlHelper.cs#L51
), 
MIHO,
2020-08-03

    see equivalent function in AmlImport.cs; may be re-use

[Line 219, column 41](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpControl/MtpAmlHelper.cs#L219
), 
MIHO,
2020-08-06

    spec/example files seem not to be in a final state

## WpfMtpControl\MtpVisuOpcUaClient.cs

[Line 242, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpControl/MtpVisuOpcUaClient.cs#L242
), 
MIHO,
2020-08-06

    remove this, if not required anymore

## WpfMtpControl\UiElementHelper.cs

[Line 426, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpControl/UiElementHelper.cs#L426
), 
MICHA,
2020-10-04

    check if font is set correctly ..

[Line 427, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpControl/UiElementHelper.cs#L427
), 
MICHA,
2020-10-04

    seems, that for Textblock the alignement DOES NOT WORK!

## WpfMtpVisuViewer\MainWindow.xaml.cs

[Line 76, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpVisuViewer/MainWindow.xaml.cs#L76
), 
MIHO,
2020-09-18

    remove this test code


