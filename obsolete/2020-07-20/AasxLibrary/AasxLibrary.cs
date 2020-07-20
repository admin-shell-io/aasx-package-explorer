/* Copyright(c) 2020 Schneider Electric Automation GmbH <marco.mendes@se.com>, author: Marco Mendes */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

// ReSharper disable All .. as this is code from others!

namespace De.Zvei.Aasx
{
    /// <summary>
    /// Used in the package logical model to express how many times a relationship can be used
    /// </summary>
    public enum CardinalityType { Zero, One, ZeroOrOne, ZeroOrMore, OneOrMore };

    /// <summary>
    /// Basic structure of a relationship
    /// </summary>
    public class RelationshipType
    {
        public string TypeUri;
        public CardinalityType Cardinality;
        // It should be clear that this points to a relationship type and not a specific instance in the model!
        // Should be null if there is no source (root relationship to the package)
        public RelationshipType SourceType;
        // The default Uri for a target part (it can be changed, if necessary). It may also be a path for
        // OnetoMany cardinality or a composite format string´(e.g. with {0}´, {1}, ...) if parts of the string
        // are to be replaced. If null, it means that is typically generated automatically
        // by the OPC API (e.g. signature parts)
        public string DefaultTargetUri;
        // MIME type of the target. If null, it means that is typically generated automatically
        // by the OPC API (e.g. signature parts) or given by the extension
        // (e.g. the type of serialization that was used)
        public string DefaultMimeType;
    }

    /// <summary>
    /// Logical model (type) for the AASX package (defines the rules on which relationships are to be used and
    /// how often).
    /// The instance of this logical model is then defined by the specific parts and relationships
    /// added to the package, based on the "rules" defined by this model.
    /// </summary>
    public static class LogicalModel
    {
        // Root folder where the AAS are stored
        public static readonly string AasRootRelativeDirectory = "aasx" + Path.DirectorySeparatorChar;
        public static readonly string AasFileNameSignature = ".aas.";
        public static readonly string AasHeaderFileNameSignature = ".header.";
        public static readonly string AasBodyFileNameSignature = ".body.";
        public static readonly string AasSubmodelFileNameSignature = ".submodel.";
        public static readonly string AasViewsFileNameSignature = ".views.";
        public static readonly string AasPddicFileNameSignature = ".pddic."; //< pddic = PropertyDescriptionDictionary

        public static readonly RelationshipType Thumbnail = new RelationshipType()
        {
            TypeUri = @"http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail",
            Cardinality = CardinalityType.ZeroOrOne,
            SourceType = null,
            DefaultTargetUri = @"thumbnail.jpeg",
            DefaultMimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg
        };
        public static readonly RelationshipType CoreProperties = new RelationshipType()
        {
            TypeUri = @"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties",
            Cardinality = CardinalityType.ZeroOrOne,
            SourceType = null,
            DefaultTargetUri = @"core-properties.xml",
            DefaultMimeType = System.Net.Mime.MediaTypeNames.Text.Xml
        };
        public static readonly RelationshipType DigitalSignatureOrigin = new RelationshipType()
        {
            TypeUri = @"http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/origin",
            Cardinality = CardinalityType.ZeroOrOne,
            SourceType = null,
            DefaultTargetUri = null,
            DefaultMimeType = null
        };
        public static readonly RelationshipType DigitalSignatureSignature = new RelationshipType()
        {
            TypeUri = @"http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/signature",
            Cardinality = CardinalityType.OneOrMore,
            SourceType = DigitalSignatureOrigin,
            DefaultTargetUri = null,
            DefaultMimeType = null
        };
        public static readonly RelationshipType DigitalSignatureCertificate = new RelationshipType()
        {
            TypeUri = @"http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/certificate",
            Cardinality = CardinalityType.ZeroOrOne,
            SourceType = DigitalSignatureSignature,
            DefaultTargetUri = null,
            DefaultMimeType = null
        };
        public static readonly RelationshipType AasxOrigin = new RelationshipType()
        {
            TypeUri = @"http://www.zvei.de/aasx/relationships/aasx-origin",
            Cardinality = CardinalityType.One,
            SourceType = null,
            DefaultTargetUri = AasRootRelativeDirectory + @"aasx-origin",
            DefaultMimeType = System.Net.Mime.MediaTypeNames.Application.Octet
        };
        public static readonly RelationshipType AasStructure = new RelationshipType()
        {
            TypeUri = @"http://www.zvei.de/aasx/relationships/aas-structure",
            Cardinality = CardinalityType.OneOrMore,
            SourceType = AasxOrigin,
            DefaultTargetUri = AasRootRelativeDirectory + @"{0}\{0}" + AasFileNameSignature + "{1}",
            DefaultMimeType = null
        };
        public static readonly RelationshipType AasHeader = new RelationshipType()
        {
            TypeUri = @"http://www.zvei.de/aasx/relationships/aas-structure-split",
            Cardinality = CardinalityType.ZeroOrOne,
            SourceType = AasStructure,
            DefaultTargetUri = AasRootRelativeDirectory + @"{0}\{0}" + AasHeaderFileNameSignature + "{1}",
            DefaultMimeType = null
        };
        public static readonly RelationshipType AasBody = new RelationshipType()
        {
            TypeUri = @"http://www.zvei.de/aasx/relationships/aas-structure-split",
            Cardinality = CardinalityType.ZeroOrOne,
            SourceType = AasStructure,
            DefaultTargetUri = AasRootRelativeDirectory + @"{0}\{0}" + AasBodyFileNameSignature + "{1}",
            DefaultMimeType = null
        };
        public static readonly RelationshipType AasViews = new RelationshipType()
        {
            TypeUri = @"http://www.zvei.de/aasx/relationships/aas-structure-split",
            Cardinality = CardinalityType.ZeroOrOne,
            SourceType = AasStructure,
            DefaultTargetUri = AasRootRelativeDirectory + @"{0}\{0}" + AasViewsFileNameSignature + "{1}",
            DefaultMimeType = null
        };
        public static readonly RelationshipType AasPddic = new RelationshipType()
        {
            TypeUri = @"http://www.zvei.de/aasx/relationships/aas-structure-split",
            Cardinality = CardinalityType.ZeroOrOne,
            SourceType = AasStructure,
            DefaultTargetUri = AasRootRelativeDirectory + @"{0}\{0}" + AasPddicFileNameSignature + "{1}",
            DefaultMimeType = null
        };
        public static readonly RelationshipType AasSubmodel = new RelationshipType()
        {
            TypeUri = @"http://www.zvei.de/aasx/relationships/aas-structure-split",
            Cardinality = CardinalityType.ZeroOrMore,
            SourceType = AasStructure,
            DefaultTargetUri = AasRootRelativeDirectory + @"{0}\{1}\{1}" + AasSubmodelFileNameSignature + "{2}",
            DefaultMimeType = null
        };
        public static readonly RelationshipType AasExtraFile = new RelationshipType()
        {
            TypeUri = @"http://www.zvei.de/aasx/relationships/aas-extra-file",
            Cardinality = CardinalityType.ZeroOrMore,
            SourceType = AasStructure,
            DefaultTargetUri = AasRootRelativeDirectory + @"{0}\{1}\{2}",
            DefaultMimeType = System.Net.Mime.MediaTypeNames.Application.Octet
        };
    }

    /// <summary>
    /// Core Properties of the package (not the properties of an AAS), based on the
    /// ISO/IEC 29500:2008 and ECMA-376 Part 2 standards.
    /// Needed to create a separate class, because System.IO.Packaging.PackageProperties is abstract.
    /// </summary>
    public class PackageCoreProperties : System.IO.Packaging.PackageProperties
    {
        public override string Title { get; set; }
        public override string Subject { get; set; }
        public override string Creator { get; set; }
        public override string Keywords { get; set; }
        public override string Description { get; set; }
        public override string LastModifiedBy { get; set; }
        public override string Revision { get; set; }
        public override DateTime? LastPrinted { get; set; }
        public override DateTime? Created { get; set; }
        public override DateTime? Modified { get; set; }
        public override string Category { get; set; }
        public override string Identifier { get; set; }
        public override string ContentType { get; set; }
        public override string Language { get; set; }
        public override string Version { get; set; }
        public override string ContentStatus { get; set; }
    }

    /// <summary>
    /// Helper methods to create, manage, etc. the package.
    /// </summary>
    /// <remarks>
    /// No new class for the AASX package was defined that inherits from System.IO.Packaging.Package
    /// to represent the AASX package.
    /// This was not done, because it is required to override some methods of abstract class "Package" and
    /// would consume much more efforts to do so.
    /// The best solution was to use the Package object returned by Package.Open() and define a set of
    /// helper methods to handle that package.
    /// </remarks>
    public static class PackageHelper
    {

        /// <summary>
        /// Creates a new package
        /// </summary>
        /// <param name="aasxSourceFilesPath">Must have a trailing directory separator</param>
        /// <param name="aasxDestinationPath"></param>
        /// <remarks>Uses MessageBox for user input/output</remarks>
        public static bool Create(string aasxSourceFilesPath, string aasxDestinationPath)
        {
            // TODO Warning on files that aren't included as parts

            if (!Directory.Exists(aasxSourceFilesPath))
            {
                MessageBox.Show(
                    "Invalid folder \"" + aasxSourceFilesPath + "\"", "Create AASX", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            if (!aasxSourceFilesPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                MessageBox.Show(
                    "No trailing directory separator in \"" + aasxSourceFilesPath + "\"", "Create AASX",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Create new package, add relationships and parts. Will overwrite existing file!
            using (System.IO.Packaging.Package package = System.IO.Packaging.Package.Open(
                aasxDestinationPath, FileMode.Create))
            {
                // Create thumbnail relationship and target part (only if source file exists)
                CreateRelationshipAndTargetPart(
                    package, null, aasxSourceFilesPath + LogicalModel.Thumbnail.DefaultTargetUri,
                    LogicalModel.Thumbnail.DefaultTargetUri, LogicalModel.Thumbnail.DefaultMimeType,
                    LogicalModel.Thumbnail.TypeUri);

                // Create core-properties relationship and target part (only if source file exists)
                CreateRelationshipAndTargetPart(
                    package, null, aasxSourceFilesPath + LogicalModel.CoreProperties.DefaultTargetUri,
                    LogicalModel.CoreProperties.DefaultTargetUri, LogicalModel.CoreProperties.DefaultMimeType,
                    LogicalModel.CoreProperties.TypeUri);

                // Create aasx-origin relationship and target part
                System.IO.Packaging.PackagePart aasxOriginPart;
                if (!CreateRelationshipAndTargetPart(
                    package, null, aasxSourceFilesPath + LogicalModel.AasxOrigin.DefaultTargetUri,
                    LogicalModel.AasxOrigin.DefaultTargetUri, LogicalModel.AasxOrigin.DefaultMimeType,
                    LogicalModel.AasxOrigin.TypeUri, out aasxOriginPart))
                {
                    MessageBox.Show(
                        "No aasx-origin file in folder \"" + aasxSourceFilesPath +
                        "\"\nPlease create an empty aasx-origin file.", "Create AASX",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var aasDirectories = Directory.GetDirectories(
                    aasxSourceFilesPath + LogicalModel.AasRootRelativeDirectory);

                // Get all AAS directories and traverse them
                foreach (var aasDirectory in aasDirectories)
                {
                    var aasFriendlyName = new DirectoryInfo(aasDirectory).Name;
                    var aasFiles = Directory.GetFiles(aasDirectory, "*" + LogicalModel.AasFileNameSignature + "*");

                    var aasParts = new List<PackagePart>();

                    // Note that there might be more than one aas file for each serialization type
                    foreach (var file in aasFiles)
                    {
                        PackagePart aasPart = null;

                        // File must start with aasFriendlyName
                        if (Path.GetFileName(file).StartsWith(aasFriendlyName + LogicalModel.AasFileNameSignature))
                        {
                            // Create aas-structure relationship and target part
                            CreateRelationshipAndTargetPart(
                                package, aasxOriginPart, file,
                                String.Format(LogicalModel.AasStructure.DefaultTargetUri, aasFriendlyName,
                                    Path.GetExtension(file).Substring(1)), GetMimeType(file),
                                    LogicalModel.AasStructure.TypeUri, out aasPart);
                            aasParts.Add(aasPart);

                            // Check if there is a splitted header file
                            string headerFile =
                                aasxSourceFilesPath +
                                String.Format(
                                    LogicalModel.AasHeader.DefaultTargetUri, aasFriendlyName,
                                    Path.GetExtension(file).Substring(1));
                            if (File.Exists(headerFile))
                            {
                                // Create aas header relationship and target part
                                CreateRelationshipAndTargetPart(
                                    package, aasPart, headerFile,
                                    String.Format(
                                        LogicalModel.AasHeader.DefaultTargetUri, aasFriendlyName,
                                        Path.GetExtension(file).Substring(1)),
                                    GetMimeType(file), LogicalModel.AasHeader.TypeUri);
                            }

                            // Check if there is a splitted body file
                            string bodyFile =
                                aasxSourceFilesPath +
                                String.Format(
                                    LogicalModel.AasBody.DefaultTargetUri, aasFriendlyName,
                                    Path.GetExtension(file).Substring(1));
                            if (File.Exists(bodyFile))
                            {
                                // Create aas-structure-split body relationship and target part
                                CreateRelationshipAndTargetPart(
                                    package, aasPart, bodyFile,
                                    String.Format(
                                        LogicalModel.AasBody.DefaultTargetUri, aasFriendlyName,
                                        Path.GetExtension(file).Substring(1)),
                                    GetMimeType(file), LogicalModel.AasBody.TypeUri);
                            }

                            // Check if there is a splitted views file
                            string viewsFile =
                                aasxSourceFilesPath +
                                String.Format(
                                    LogicalModel.AasViews.DefaultTargetUri, aasFriendlyName,
                                    Path.GetExtension(file).Substring(1));
                            if (File.Exists(viewsFile))
                            {
                                // Create aas-structure-split views relationship and target part
                                CreateRelationshipAndTargetPart(
                                    package, aasPart, viewsFile,
                                    String.Format(
                                        LogicalModel.AasViews.DefaultTargetUri, aasFriendlyName,
                                        Path.GetExtension(file).Substring(1)),
                                    GetMimeType(file), LogicalModel.AasViews.TypeUri);
                            }

                            // Check if there is a splitted pddic file
                            string pddicFile =
                                aasxSourceFilesPath +
                                String.Format(
                                    LogicalModel.AasPddic.DefaultTargetUri, aasFriendlyName,
                                    Path.GetExtension(file).Substring(1));
                            if (File.Exists(pddicFile))
                            {
                                // Create aas-structure-split pddic relationship and target part
                                CreateRelationshipAndTargetPart(
                                    package, aasPart, pddicFile,
                                    String.Format(
                                        LogicalModel.AasPddic.DefaultTargetUri, aasFriendlyName,
                                        Path.GetExtension(file).Substring(1)),
                                    GetMimeType(file), LogicalModel.AasPddic.TypeUri);
                            }

                        }
                    }

                    var submodelDirectories = Directory.GetDirectories(aasDirectory);

                    // Traverse each submodel directory
                    foreach (var submodelDirectory in submodelDirectories)
                    {
                        var submodelFriendlyName = new DirectoryInfo(submodelDirectory).Name;

                        // Get submodel files
                        var submodelFiles = Directory.GetFiles(
                            submodelDirectory, "*" + LogicalModel.AasSubmodelFileNameSignature + "*");

                        // Get extra files
                        var extraFiles = Directory.GetFiles(submodelDirectory, "*");
                        // Will remove the ".submodel." files and leave only the extra files
                        extraFiles = extraFiles.Except(
                            submodelFiles).ToArray();
                        // Check if there is a corresponding submodel part for each aas-structure part
                        // (depending on the serialization type)
                        foreach (var aasPart2 in aasParts)
                        {
                            var submodelFile = submodelFiles.FirstOrDefault(
                                x => Path.GetExtension(x) == Path.GetExtension(aasPart2.Uri.OriginalString));

                            if (string.IsNullOrEmpty(submodelFile))
                            {
                                foreach (var extraFile in extraFiles)
                                {
                                    // Create aas-extra-file relationship (source is the ass-structure) and
                                    // aas-extra-file part (if not already existing)
                                    CreateRelationshipAndTargetPart(
                                        package, aasPart2, extraFile,
                                        String.Format(
                                            LogicalModel.AasExtraFile.DefaultTargetUri, aasFriendlyName,
                                            submodelFriendlyName, Path.GetFileName(extraFile)),
                                        GetMimeType(extraFile), LogicalModel.AasExtraFile.TypeUri);
                                }
                            }
                            else
                            {
                                // File must start with submodelFriendlyName
                                if (Path.GetFileName(submodelFile)
                                    .StartsWith(submodelFriendlyName + LogicalModel.AasSubmodelFileNameSignature))
                                {
                                    PackagePart submodelPart;
                                    // Create aas-structure-split submodel relationship and part,
                                    // create aas-extra-file relationships with aas-structure as source and
                                    // aas-extra-file part (if not already existing)
                                    CreateRelationshipAndTargetPart(
                                        package, aasPart2, submodelFile,
                                        String.Format(
                                            LogicalModel.AasSubmodel.DefaultTargetUri, aasFriendlyName,
                                            submodelFriendlyName, Path.GetExtension(submodelFile).Substring(1)),
                                        GetMimeType(submodelFile), LogicalModel.AasSubmodel.TypeUri, out submodelPart);

                                    foreach (var extraFile in extraFiles)
                                    {
                                        CreateRelationshipAndTargetPart(
                                            package, aasPart2, extraFile,
                                            String.Format(
                                                LogicalModel.AasExtraFile.DefaultTargetUri, aasFriendlyName,
                                                submodelFriendlyName, Path.GetFileName(extraFile)),
                                            GetMimeType(extraFile), LogicalModel.AasExtraFile.TypeUri);
                                    }
                                }
                            }
                        }
                    }
                }
            } //< End using - Will create package if there was no error

            return true;
        }

        public static void SignCustom(List<string> policies)
        {
            // TODO
        }

        /// <summary>
        /// Will sign all parts and relationships in the package (any modification will invalidate the signature)
        /// Will prompt the user to select a certificate to sign with.
        /// New files can be added to the package, but they will not be signed,
        /// therefore easy to detect during verification.
        /// TODO The signature file and [Content_Types].xml can be tampered? Is this an issue?
        /// </summary>
        /// <param name="packagePath"></param>
        /// <param name="storeName"></param>
        public static void SignAll(string packagePath, string storeName = "My")
        {
            using (Package package = Package.Open(packagePath, FileMode.Open))
            {
                // Create the DigitalSignature Manager
                PackageDigitalSignatureManager dsm = new PackageDigitalSignatureManager(package);
                dsm.CertificateOption = CertificateEmbeddingOption.InSignaturePart;

                // Create a list of all the part URIs in the package to sign
                // (GetParts() also includes PackageRelationship parts).
                System.Collections.Generic.List<Uri> toSign = new System.Collections.Generic.List<Uri>();
                foreach (PackagePart packagePart in package.GetParts())
                {
                    // Add all package parts to the list for signing.
                    toSign.Add(packagePart.Uri);
                }

                // Create list of selectors for the list of relationships
                List<PackageRelationshipSelector> relationshipSelectors = new List<PackageRelationshipSelector>();

                // Create one selector for each package-level relationship, based on id
                foreach (PackageRelationship relationship in package.GetRelationships())
                {
                    relationshipSelectors.Add(
                        new PackageRelationshipSelector(
                            relationship.SourceUri, PackageRelationshipSelectorType.Id, relationship.Id));
                }

                // For parts-level relationships ...
                foreach (PackagePart packagePart in package.GetParts())
                {
                    try
                    {
                        if (packagePart.GetRelationships() != null)
                        {
                            foreach (PackageRelationship relationship in packagePart.GetRelationships())
                            {
                                relationshipSelectors.Add(
                                    new PackageRelationshipSelector(
                                        relationship.SourceUri, PackageRelationshipSelectorType.Id, relationship.Id));
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                // Also sign the SignatureOrigin part.
                toSign.Add(dsm.SignatureOrigin);

                // Add the URI for SignatureOrigin PackageRelationship part.
                // The SignatureOrigin relationship is created when Sign() is called.
                // Signing the SignatureOrigin relationship disables counter-signatures.
                toSign.Add(PackUriHelper.GetRelationshipPartUri(dsm.SignatureOrigin));

                // Sign all relationships entry of signature-origin inside the root .rels file
                relationshipSelectors.Add(
                    new PackageRelationshipSelector(
                        new Uri("/", UriKind.Relative), PackageRelationshipSelectorType.Type,
                        "http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/origin"));

                // Sign() will prompt the user to select a Certificate to sign with.
                try
                {
                    var dlg = new OpenFileDialog();
                    try
                    {
                        dlg.InitialDirectory = System.IO.Path.GetDirectoryName("\\");
                    }
                    catch { }
                    dlg.Filter = ".pfx files (*.pfx)|*.pfx";
                    var res = dlg.ShowDialog();
                    X509Certificate2 x509 = new X509Certificate2(dlg.FileName, "i40");
                    X509Certificate2Collection scollection = new X509Certificate2Collection(x509);
                    dsm.Sign(toSign, scollection[0], relationshipSelectors);
                }

                // If there are no certificates or the SmartCard manager is
                // not running, catch the exception and show an error message.
                catch (CryptographicException ex)
                {
                    MessageBox.Show(
                        "Cannot Sign\n" + ex.Message, "Error signing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Checks the signatures in the package
        /// </summary>
        /// <param name="packagePath"></param>
        /// <param name="certificatesStatus">Status of the certificate (dictionary with the subject and
        /// verification status of the certificates) </param>
        /// <returns></returns>
        public static VerifyResult VerifySignatures(
            string packagePath, out Dictionary<string, X509ChainStatusFlags> certificatesStatus)
        {
            VerifyResult vResult;
            certificatesStatus = new Dictionary<string, X509ChainStatusFlags>();
            using (Package package = Package.Open(packagePath, FileMode.Open, FileAccess.Read))
            {
                PackageDigitalSignatureManager dsm = new PackageDigitalSignatureManager(package);

                // Verify the collection of certificates in the package
                foreach (PackageDigitalSignature signature in dsm.Signatures)
                {
                    certificatesStatus.Add(
                        signature.Signer.Subject, PackageDigitalSignatureManager.VerifyCertificate(signature.Signer));
                }

                // For this example, if all certificates are valid, verify all signatures in the package.
                vResult = dsm.VerifySignatures(false);
            }
            return vResult;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="packagePath"></param>
        public static bool Validate(string packagePath)
        {
            // docx procedure:
            // Verify if OPC package
            // AASX logical model
            // Optional: physical model best practice
            // No free roaming files
            // checked ceriticates
            // Signatures must be verifiesd
            // Which policies are implemented?


            try
            {
                using (Package package = Package.Open(packagePath, FileMode.Open, FileAccess.Read))
                {
                    // If openend, I think that the package is according to the OPC standard
                    // TODO Is package according to the Logical model of the Admin Shell?
                    // => use AdminShell logical model to compare
                }

                // Certificates and Signature status
                Dictionary<string, X509ChainStatusFlags> certificatesStatus;
                var verifyResult = VerifySignatures(packagePath, out certificatesStatus);

                string certRes = "";
                foreach (var res in certificatesStatus)
                {
                    certRes += res.Key + ": " + res.Value.ToString() + "\n";
                }

                MessageBox.Show(
                    null, "Certificate status: \n" + certRes, "Certificates",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (verifyResult == VerifyResult.Success)
                {
                    MessageBox.Show(
                        null, "Package signatures verified", "Signatures",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        null, "Error verifying signatures: " + verifyResult.ToString(),
                        "Signatures", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }


                // If there are no signatures - OK, but must be mentioned in the result

                // TODO is package sealed? => no other signatures can be added?
                // All files are signed (except those that could not be signed). New files (unsigned) were added

                // TODO The information from the analysis
                //  -> return as an object (list of enums with the issues/warings???)
            }
            catch (Exception e)
            {
                MessageBox.Show(null, e.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return true;
        }

        /// <summary>
        /// Creates a package-level or part-level relationship and its target part
        /// (if the target part wasn't already created before). If the sourceTargetFilePath does not exist,
        /// then both relationship and part will not be created.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="sourcePart">Set to null for a package-level relationship</param>
        /// <param name="sourceTargetFilePath"></param>
        /// <param name="targetUri"></param>
        /// <param name="targetMimeType"></param>
        /// <param name="relationshipTypeUri"></param>
        /// <param name="targetPart">The target part that was created</param>
        /// <returns>True if relationship and part (if not already created before) was created,
        /// False if source file does not exist and thus relationship and part wasn't created.</returns>
        private static bool CreateRelationshipAndTargetPart(
            System.IO.Packaging.Package package, System.IO.Packaging.PackagePart sourcePart,
            string sourceTargetFilePath, string targetUri, string targetMimeType, string relationshipTypeUri,
            out System.IO.Packaging.PackagePart targetPart)
        {
            // TODO add console output for added parts and relationships

            targetPart = null;

            if (!File.Exists(sourceTargetFilePath))
            {
                Console.WriteLine(
                    "Warning: The following source file does not exist: " + sourceTargetFilePath +
                    ". Part and relationship will not be created.");
                return false;
            }
            try
            {
                targetPart = package.CreatePart(
                    PackUriHelper.CreatePartUri(
                        new Uri(targetUri, UriKind.Relative)), targetMimeType, CompressionOption.Maximum);
                using (FileStream fileStream = new FileStream(sourceTargetFilePath, FileMode.Open, FileAccess.Read))
                {
                    fileStream.CopyTo(targetPart.GetStream());
                }
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine(
                    "Warning: The following part URI already exists and will not be created again: " +
                    targetUri + ". Relationship will still be created.");
            }
            if (sourcePart == null)
            {
                package.CreateRelationship(
                    PackUriHelper.CreatePartUri(
                        new Uri(targetUri, UriKind.Relative)), TargetMode.Internal, relationshipTypeUri);
            }
            else
            {
                sourcePart.CreateRelationship(
                    PackUriHelper.CreatePartUri(
                        new Uri(targetUri, UriKind.Relative)), TargetMode.Internal, relationshipTypeUri);
            }
            return true;
        }

        private static bool CreateRelationshipAndTargetPart(
            System.IO.Packaging.Package package, System.IO.Packaging.PackagePart sourcePart,
            string sourceTargetFilePath, string targetUri, string targetMimeType, string relationshipTypeUri)
        {
            System.IO.Packaging.PackagePart targetPart;
            return CreateRelationshipAndTargetPart(
                package, sourcePart, sourceTargetFilePath, targetUri, targetMimeType, relationshipTypeUri,
                out targetPart);
        }

        private static string GetMimeType(string file)
        {
            if (!Path.HasExtension(file))
            {
                return System.Net.Mime.MediaTypeNames.Application.Octet;
            }
            var ext = Path.GetExtension(file);
            if (String.Compare(ext, ".xml", true) == 0)
            {
                return System.Net.Mime.MediaTypeNames.Text.Xml;
            }
            else if (String.Compare(ext, ".json", true) == 0)
            {
                return "application/json";
            }
            else if (String.Compare(ext, ".jpeg", true) == 0 || String.Compare(ext, ".jpg", true) == 0)
            {
                return System.Net.Mime.MediaTypeNames.Image.Jpeg;
            }
            return System.Net.Mime.MediaTypeNames.Application.Octet;
        }

        /// <summary>
        /// We need this because package.PackageProperties cannot be assigned (read-only)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        private static void CopyPropertyValues(object source, object destination)
        {
            var destProperties = destination.GetType().GetProperties();

            foreach (var sourceProperty in source.GetType().GetProperties())
            {
                foreach (var destProperty in destProperties)
                {
                    if (destProperty.Name == sourceProperty.Name &&
                destProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                    {
                        destProperty.SetValue(destination, sourceProperty.GetValue(
                            source, new object[] { }), new object[] { });

                        break;
                    }
                }
            }
        }
    }
}
