/*
Copyright(c) 2020 Schneider Electric Automation GmbH <marco.mendes@se.com>
Author: Marco Mendes

This file has been shortened to include only the features needed by AASX Package Explorer.
The original file is available at: obsolete/2020-07-20/AasxLibrary/AasxLibrary.cs

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AnyUi;

// ReSharper disable All .. as this is code from others!

namespace AasxSignature
{
    /// <summary>
    /// Signs and validates the packages.
    /// </summary>
    public static class PackageHelper
    {
        // TODO (Andreas Orzelski, 2020-08-01): The signature file and [Content_Types].xml can be tampered? 
        // Is this an issue?

        /// <summary>
        /// Will sign all parts and relationships in the package (any modification will invalidate the signature)
        /// Will prompt the user to select a certificate to sign with.
        /// New files can be added to the package, but they will not be signed,
        /// therefore easy to detect during verification.        
        /// </summary>
        public async static Task<bool> SignAll(
            string packagePath,
            string certFn,
            string storeName = "My",
            AnyUiMinimalInvokeMessageDelegate invokeMessage = null)
        {
            // access
            if (!File.Exists(packagePath) || !File.Exists(certFn))
            {
                await invokeMessage?.Invoke(true, "SignAll: invalid package path or certificate filename.");
                return false;
            }

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
                        foreach (PackageRelationship relationship in packagePart.GetRelationships())
                        {
                            relationshipSelectors.Add(
                                new PackageRelationshipSelector(
                                    relationship.SourceUri, PackageRelationshipSelectorType.Id, relationship.Id));
                        }
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
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
                    // TODO (MIHO, 2022-12-16): check if this code is required 
                    // might been converted due to AasxScript refactoring
#if UNCLEAR
                    //var dlg = new OpenFileDialog();
                    //try
                    //{
                    //    dlg.InitialDirectory = System.IO.Path.GetDirectoryName("\\");
                    //}
                    //catch (Exception ex)
                    //{
                    //    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    //}
                    //dlg.Filter = ".pfx files (*.pfx)|*.pfx";
                    //dlg.ShowDialog();
#endif
                    X509Certificate2 x509 = new X509Certificate2(certFn, "i40");
                    X509Certificate2Collection scollection = new X509Certificate2Collection(x509);
                    dsm.Sign(toSign, scollection[0], relationshipSelectors);
                }

                // If there are no certificates or the SmartCard manager is
                // not running, catch the exception and show an error message.
                catch (CryptographicException ex)
                {
                    await invokeMessage?.Invoke(true, "Sign all: cannot sign \n" + ex.Message);
                    return false;
                }
            }

            // ok?!
            return true;
        }

        /// <summary>
        /// Checks the signatures in the package
        /// </summary>
        /// <param name="packagePath">Path to the existing package</param>
        /// <param name="certificatesStatus">Status of the certificate (dictionary with the subject and
        /// verification status of the certificates)</param>
        /// <returns></returns>
        private static VerifyResult VerifySignatures(
            string packagePath,
            out Dictionary<string, X509ChainStatusFlags> certificatesStatus)
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
        /// <param name="packagePath">Path to the existing package</param>
        /// <param name="invokeMessage">Action to display/ log messages</param>
        public static bool Validate(string packagePath,
            AnyUiMinimalInvokeMessageDelegate invokeMessage = null)
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
                using (Package.Open(packagePath, FileMode.Open, FileAccess.Read))
                {
                    // If openend, I think that the package is according to the OPC standard
                    // TODO (Andreas Orzelski, 2020-08-01): Is package according to the Logical model of the AAS?
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

                invokeMessage?.Invoke(false,
                    "Validate: Certificate status: \n" + certRes);

                if (verifyResult == VerifyResult.Success)
                {
                    invokeMessage?.Invoke(false,
                        "Validate: Package signatures verified");
                    return true;
                }
                else
                {
                    invokeMessage?.Invoke(true,
                        "Validate: Error verifying signatures: " + verifyResult.ToString());
                }

                // If there are no signatures - OK, but must be mentioned in the result

                // TODO (Andreas Orzelski, 2020-08-01): is package sealed? => no other signatures can be added?
                // All files are signed (except those that could not be signed). New files (unsigned) were added

                /*
                 TODO (Andreas Orzelski, 2020-08-01): The information from the analysis
                  -> return as an object (list of enums with the issues/warings???)
                */
            }
            catch (Exception e)
            {
                invokeMessage?.Invoke(true,
                    "Validate: failed because of: " + e.Message);
            }

            return true;
        }
    }
}
