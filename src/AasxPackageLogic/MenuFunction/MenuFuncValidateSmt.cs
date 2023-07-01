/*
Copyright (c) 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


using AasCore.Aas3_0;
using AasxIntegrationBase;
using AasxPackageLogic;
using AdminShellNS;
using Aml.Engine.CAEX;
using AngleSharp.Text;
using AnyUi;
using Extensions;
using Jose;
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.Webpki.JsonCanonicalizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static AasxPredefinedConcepts.ConceptModel.ConceptModelZveiTechnicalData;
using System.Windows.Forms;
using VDS.RDF.Ontology;
using VDS.RDF.Query.Paths;
using static System.Net.Mime.MediaTypeNames;
using Aas = AasCore.Aas3_0;
using VDS.RDF.Query.Algebra;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace AasxPackageExplorer
{
    /// <summary>
    /// Tools to valdiate a Submodel template (incl. UI menu function)
    /// </summary>
    public class MenuFuncValidateSmt : LogicValidationMenuFuncBase
    {
        /// <summary>
        /// Used AAS ENV for performing the validation.
        /// </summary>
        public Aas.Environment Env = null;

        //
        // some statistics
        //

        protected IntValueDictionary<IConceptDescription> DictCdFound = new IntValueDictionary<IConceptDescription>();

        protected int NumCdFound = 0;
        protected int NumCdMissed = 0;

        //
        // RepoKind
        //

        /// <summary>
        /// Can detect different kinds of concept repositories.
        /// </summary>
        public enum RepoKind { Unknown, AdminShellIo, Url, Eclass, Iec, OtherRai }

        public string[] RepoKindExplain = { "Unknown", "https://admin-shell.io", "Other URL", 
            "ECLASS", "IEC", "Other RAI" }; 

        public RepoKind DetectRepoKind(string id)
        {
            if (id == null)
                return RepoKind.Unknown;
            id = id.Trim();
            if (id.StartsWith("0173"))
                return RepoKind.Eclass;
            if (id.StartsWith("0112"))
                return RepoKind.Iec;
            if (Regex.Match(id, @"(\d{2,4})[^0-9a-zA-Z]").Success)
                return RepoKind.Url;
            if (id.ToLower().StartsWith("https://admin-shell.io"))
                return RepoKind.AdminShellIo;
            if (Regex.Match(id, @"^(\w+)://").Success)
                return RepoKind.Url;
            return RepoKind.Unknown;
        }

        protected int[] RepoKindNumsAny;
        protected int[] RepoKindNumsNonStruct;

        private void RepoKindDoStat(int[] dict, string message)
        {
            Recs.AddComment("Statistics of concept repository use for: " + message);
            foreach (var v in Enum.GetValues<RepoKind>())
                Recs.AddComment(String.Format("{0,-4} of {1}", dict[(int)v], RepoKindExplain[(int)v]));
        }

        /// <summary>
        /// Counts how many catgories (enum-values) of the <c>dict</c> feature non-zero values.
        /// Skips the <c>Unknown</c>.
        /// </summary>
        private int RepoKindNumberOfUsedCategories(int[] dict)
        {
            int res = 0;
            foreach (var v in Enum.GetValues<RepoKind>())
            {
                if (v == RepoKind.Unknown)
                    continue;
                if (dict[(int)v] > 0)
                    res++;
            }
            return res;
        }

        //
        // semanticId histogram
        //

        protected IntValueDictionary<string> SemIdHistogram = new IntValueDictionary<string>();

        private void SemIdDoStat(IntValueDictionary<string> dict, string category, string name)
        {
            Recs.AddComment("Statistics of semanticId use in: " + category + " " + name);
            foreach (var v in dict.Keys)
                // Recs.AddComment(String.Format("SEM01 \t{0,-4}\tof\t{1}\tin\t{2}", dict[v], v, category));
                Recs.AddTabbedCells("SEM01", "" + dict[v], "of", v, "in", category, name);
        }

        //
        // Validation
        //

        public bool CheckForRegex (string head, string pattern, string input, 
            RegexOptions options = RegexOptions.None,
            Func<Match, bool> matchLambda = null)
        {
            if (head == null || pattern == null || input == null)
                return false;

            Recs.AddComment($"{head} \"{pattern}\" ..");

            var m = Regex.Match(input, pattern, options);
            var res = m.Success;
            if (res && matchLambda != null)
                res = matchLambda.Invoke(m);
            return res;
        }

        public bool CheckForIdShort(string idShort)
        {
            if (idShort?.HasContent() != true)
                return false;
            var m = Regex.Match(idShort, @"[a-zA-Z][a-zA-Z0-9_]*");
            return m.Success;
        }

        private void CheckAnyElement(IReferable rf)
        {
            // check semantics
            if (rf is IHasSemantics sem)
            {
                if (sem.SemanticId == null || sem.SemanticId.IsValid() != true)
                    Recs.AddElemDetail("AX08", "Fail", true, 
                        "Element does not have a valid semanticId.", rf.GetReference());
                else
                {
                    // kind of semanticId
                    var rk = DetectRepoKind(sem.SemanticId.Keys[0].Value);
                    RepoKindNumsAny[(int)rk]++;
                    if (rk == RepoKind.Unknown)
                        Recs.AddElemDetail("AX09", "Fail", true, 
                            "Non-structural elements features unknown kind of semanticId!", rf.GetReference());

                    // find a CD?
                    var cd = Env.FindConceptDescriptionByReference(sem.SemanticId);
                    if (cd == null)
                    {
                        NumCdMissed++;
                        Recs.AddElemDetail("AX12", "Fail", true,
                            "No ConceptDescription found for this !", rf.GetReference());
                    }
                    else
                    {
                        NumCdFound++;
                        DictCdFound.IncKey(cd);
                    }

                    // histogram
                    var sk = sem.SemanticId.ToStringExtended(2);
                    if (sk.Length > 0)
                        SemIdHistogram.IncKey(sk);
                }
            }

            // check SME
            if (rf is ISubmodelElement sme)
            {
                // count Qualifiers
                var cntSmtCard = sme.FindAllQualifierType("SMT/Cardinality").Count();
                var cntJustCard = sme.FindAllQualifierType("Cardinality").Count();
                var cntMulti = sme.FindAllQualifierType("Cardinality").Count();

                if (cntSmtCard < 1)
                {
                    Recs.AddElemDetail("AX11", "Fail", true, "Element does not have a valid Qualifier named \"SMT/Cardinality\".", rf.GetReference());
                    if (cntJustCard > 0 || cntMulti > 0)
                        Recs.AddComment("Element has in fact a deprecated Qualifier (just Cardinality or Multiplicity) given.");
                }
            }
        }

        private void CheckStructuralElement(IReferable rf, string ver, string rev)
        {
            if (rf is IHasSemantics sem && sem.SemanticId.IsValid() == true)
            {
                // cheat here: add a trailing '/' to each kind of value
                var fsem = sem.SemanticId.Keys[0].Value.Trim() + "/";

                // convert any special char to '/'
                var slashed = new StringBuilder(fsem);
                for (int i = 0; i < fsem.Length; i++)
                    if (!(fsem[i].IsDigit() || fsem[i].IsLetter()))
                        slashed[i] = '/';
                var fsem2 = slashed.ToString();

                // possible match to the trailing '/'
                if ((ver != null && !(fsem2.Contains("/" + ver + "/")))
                    || (rev != null && !(fsem2.Contains("/" + rev + "/"))))
                {
                    Recs.AddElemDetail("AX07", "Fail", true, 
                        "Structural element does not have version/revision in semanticId. " +
                        "SemanticId=" + fsem + ".", rf.GetReference());
                }
            }
        }

        private void CheckNonStructuralElement(IReferable rf)
        {
            if (rf is IHasSemantics sem && sem.SemanticId?.IsValid() == true)
            {
                var rk = DetectRepoKind(sem.SemanticId.Keys[0].Value);
                RepoKindNumsNonStruct[(int)rk]++;
            }
        }

        public void PerformValidation(
            Aas.Environment givenEnv = null, AdminShellPackageEnv package = null, string fn = null)
        {
            // access
            Env = givenEnv;
            if (package != null)
                Env = package.AasEnv;
            if (Env == null)
                return;

            Recs.AddComment("Assessment of Submodel template");
            Recs.AddComment("Report performed: " + DateTime.Now.ToShortDateString());

            // version, revision
            string ver = null, rev = null;

            // filename
            if (fn == null)
                fn = package.Filename;
            if (fn != null)
            {
                Recs.AddComment("Filename of the package: " + fn);

                if (!CheckForRegex("Checking for pattern: ",
                    @"IDTA (\d){5}-(\d+)-(\d+)_Template_.+", System.IO.Path.GetFileNameWithoutExtension(fn),
                    matchLambda: (m) =>
                    {
                        ver = m.Groups[2].ToString();
                        rev = m.Groups[3].ToString();
                        return true;
                    }))
                    Recs.AddStatement("AX01", "Fail", true, "SMT Filename does not match defined pattern!");
                else
                    Recs.AddStatement("AX01", "Success", false, "SMT Filename does match defined pattern.");
            }
            else
            {
                Recs.AddStatement("AX01", "Fail", true, "Filename could not be validated.");
            }

            // AAS?
            if (Env.AssetAdministrationShells.Count < 1)
            {
                Recs.AddStatement("AX02", "Fail", true, "No AAS found in the SMT AASX package.");
                Recs.AddComment("No AAS available. Further validation stopped.");
                return;
            }

            if (Env.AssetAdministrationShells.Count > 1)
            {
                Recs.AddStatement("AX02", "Fail", true, "More than one AAS found in the SMT AASX package.");
            }

            // over all AAS
            int aasIdx = 0;
            foreach (var aas in Env.AssetAdministrationShells)
            {
                // start
                Recs.AddComment($"Approaching AAS[{aasIdx++}] and idShort={aas.IdShort}");

                //idShort
                if (!CheckForIdShort(aas.IdShort))
                    Recs.AddStatement("AX03", "Fail", true, "AAS does not have a valid idShort.");

                // any Submodels
                if (aas.Submodels == null || aas.Submodels.Count < 1)
                {
                    Recs.AddStatement("AX04", "Fail", true, "AAS does not have a valid Submodel.");
                    continue;
                }

                // go over all Submodels
                int smIdx = 0;
                foreach (var smr in aas.Submodels)
                {
                    // access SM
                    var sm = Env.FindSubmodel(smr);
                    if (sm == null)
                    {
                        Recs.AddStatement("AX04", "Fail", true, "AAS does have an invalid Submodel Reference.");
                        continue;
                    }
                    sm.SetAllParents();

                    // debug
                    Recs.AddComment($"Approaching SM[{smIdx++}] and idShort={sm.IdShort}");

                    // clear some stats
                    RepoKindNumsAny = new int[Enum.GetNames(typeof(RepoKind)).Length];
                    RepoKindNumsNonStruct = new int[Enum.GetNames(typeof(RepoKind)).Length];

                    // idShort
                    if (!CheckForIdShort(sm.IdShort))
                        Recs.AddStatement("AX05", "Fail", true, "Submodel does not have a valid idShort.");
                    else
                        Recs.AddStatement("DAT01", "DATA", false, "" + sm.IdShort);

                    // kind = Template
                    if (sm.Kind == null || sm.Kind != ModellingKind.Template)
                        Recs.AddStatement("AX04", "Fail", true, "Submodel does not have kind = Template.");

                    // semanticId
                    if (sm.SemanticId?.Keys == null || sm.SemanticId.Count() < 1)
                        Recs.AddStatement("AX16", "Fail", true, "Submodel does not have valid semanticId.");
                    else
                    {
                        var smid = sm.SemanticId.Keys[0].Value;
                        Recs.AddStatement("DAT03", "DATA", false, "" + smid);
                    }

                    // description
                    var desc = sm.Description?.GetDefaultString();
                    if (desc != null)
                        Recs.AddStatement("DAT04", "DATA", false, "" + desc);

                    // administrative info
                    if (sm.Administration == null)
                        Recs.AddStatement("AX06", "Fail", true, 
                            "Submodel does not have a valid administrative information.");
                    else
                    {
                        // check
                        if (ver != sm.Administration.Version || rev != sm.Administration.Revision)
                            Recs.AddStatement("AX06", "Fail", true, 
                                "Submodel version/ revision does not match filename.");

                        // if in doubt, take over ver/rev
                        if (ver == null || rev == null)
                        {
                            ver = sm.Administration.Version;
                            rev = sm.Administration.Revision;
                        }
                        Recs.AddComment($"From now, take version=\"{ver}\" and revision=\"{rev}\" ..");
                        Recs.AddStatement("DAT02", "DATA", false, $"{ver}/{rev}");
                    }

                    // Submodel is a (structural) element
                    CheckAnyElement(sm);
                    CheckStructuralElement(sm, ver, rev);

                    // over all SMEs
                    foreach(var sme in sm.FindDeep<ISubmodelElement>())
                    {
                        CheckAnyElement(sme);

                        if (sme is ISubmodelElementCollection
                            || sme is ISubmodelElementList)
                        {
                            CheckStructuralElement(sme, ver, rev);
                        }
                        else
                        {
                            CheckNonStructuralElement(sme);
                        }
                    }

                    //
                    // post SM stats
                    //

                    RepoKindDoStat(RepoKindNumsAny, "All elements");
                    RepoKindDoStat(RepoKindNumsNonStruct, "Non-structural elements");

                    SemIdDoStat(SemIdHistogram, "SM", sm.SemanticId?.ToStringExtended(2));
                }
            }

            //
            // globals stats (w.r.t. to CDs)
            //

            Recs.AddComment("Usage of ConceptDescriptions:");

            var cdsUsedAtAll = DictCdFound.Keys.Count();
            Recs.AddComment($"{cdsUsedAtAll} ConceptDescriptions used in the SMTs of this AAS");
            Recs.AddComment($"{NumCdMissed} ConceptDescriptions which were missed in SMTs of this AAS");

            var unUsedCds = 0;
            if (Env.ConceptDescriptions != null)
                foreach (var cd in Env.ConceptDescriptions)
                    if (!DictCdFound.ContainsKey(cd))
                        unUsedCds++;

            Recs.AddComment($"{unUsedCds} ConceptDescriptions in AAS-ENV but not used in the SMTs of this AAS");
            
            if (unUsedCds > 0)
                Recs.AddStatement("AX13", "Fail", true, "Within the AASX package, ConceptDescriptions found, " +
                    "which were not referred to by SMT elements.");

            var cdsWithoutDataSpec = 0;
            if (Env.ConceptDescriptions != null)
                foreach (var cd in Env.ConceptDescriptions)
                    if (cd.EmbeddedDataSpecifications == null
                        || cd.EmbeddedDataSpecifications.Count < 1
                        || cd.EmbeddedDataSpecifications[0].DataSpecificationContent == null)
                    {
                        cdsWithoutDataSpec++;
                        Recs.AddElemDetail("AX15", "Fail", true, "ConceptDescriptions found, which do not have " +
                            "DataSpecification content.", cd.GetReference());
                    }

            Recs.AddComment($"{cdsWithoutDataSpec} ConceptDescriptions without DataSpecificationContent found.");

            Recs.AddComment("");
            Recs.AddComment("Summary of assessments over all SMTs in this AASX package");
            Recs.AddComment("=========================================================");

            Recs.AddAnyFailStatement("AX01", "Does the filename conform to the SMT?");
            Recs.AddAnyFailStatement("AX02", "Is a single AAS contained in the SMT AASX?");
            Recs.AddAnyFailStatement("AX03", "For the AAS in the SMT AASX, is a valid idShort given?");
            Recs.AddAnyFailStatement("AX04", "For the Submodels in the SMT AASX, is Submodel kind = Template ?");
            Recs.AddAnyFailStatement("AX05", "For the Submodels in the SMT AASX, is a valid idShort given?");
            Recs.AddAnyFailStatement("AX06", "For the Submodels in the SMT AASX, is administrative information " +
                "with specified version / revision given?");
            Recs.AddAnyFailStatement("AX07", "For the structural elements of the SMT, is the semanticId specific " +
                "to the respective version / revision given?");
            Recs.AddAnyFailStatement("AX08", "For all listed elements, are the semanticIds defined?");
            Recs.AddAnyFailStatement("AX09", "For all non-structure elements, are the semanticIds given from " +
                "always the same repository?");
            Recs.AddAnyFailStatement("AX10", "For all listed elements, is clear, which idShorts shall be given " +
                "by an SM instance?");
            Recs.AddAnyFailStatement("AX11", "For all listed elements, is multiplicity / cardinality defined?");
            Recs.AddAnyFailStatement("AX12", "For all elements, is a respective ConceptDescription (CD) given within " +
                "the AASX?");
            Recs.AddAnyFailStatement("AX13", "Within the AASX, are any CDs contained, which are not used by " +
                "the SMT?");
            Recs.AddAnyFailStatement("AX14", "Are all identifiers, semanticIds in correct format " +
                "(no spaces, …)?");
            Recs.AddAnyFailStatement("AX15", "For all ConceptDescription, is an DataSpecification with " +
                "content defined?");
        }
    }
}
