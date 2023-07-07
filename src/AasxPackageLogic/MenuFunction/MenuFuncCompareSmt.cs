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
    /// Tools to comapare a Submodel template to another (incl. UI menu function)
    /// </summary>
    public class MenuFuncCompareSmt : LogicValidationMenuFuncBase
    {
        //
        // the two subjects, which are compared, are named: First, Second
        //

        /// <summary>
        /// Old, original, first subject to be compared
        /// </summary>
        public Aas.Environment FirstEnv = null;

        /// <summary>
        /// New, modified, second subject to be compared
        /// </summary>
        public Aas.Environment SecondEnv = null;

        public string IndexName(int i) => (i == 0) ? "First" : "Second";
        public Aas.Environment IndexEnv(int i) => (i == 0) ? FirstEnv : SecondEnv;

        /// <summary>
        /// First SMT
        /// </summary>
        public Aas.ISubmodel First = null;

        /// <summary>
        /// Second SMT
        /// </summary>
        public Aas.ISubmodel Second = null;

        public Aas.ISubmodel IndexSm(int i) => (i == 0) ? First : Second;

        //
        // some statistics
        //

        //
        // investigate diffs
        //

        protected void CheckDiffsQualifable(
            Aas.IQualifiable qlf1, Aas.IQualifiable qlf2)
        {
            // access
            if (qlf1 == null || qlf2 == null)
                return;

            if (qlf1.Qualifiers == null && qlf2.Qualifiers == null)
                // nothing to do
                return;

            Action lambdaHead = () =>
                Recs.AddElemDetail("Q01", "DIFF", true, "Difference(s) on element of second: ",
                    (qlf2 as Aas.IReferable)?.GetReference());

            if (qlf1.Qualifiers == null)
            {
                lambdaHead();
                Recs.AddStatement("Q02", "DIFF", true, "First qualifers is null while second is not!");
                return;
            }
            if (qlf2.Qualifiers == null)
            {
                lambdaHead();
                Recs.AddStatement("Q03", "DIFF", true, "Second qualifers is null while first is not!");
                return;
            }

            Func<IEnumerable<Aas.IQualifier>, string> lambda = (qs) =>
                "[" + string.Join(", ", qs.Select((q) => "" + q.Type + "=" + q.Value).OrderBy((x) => x)) + "]";

            var qns1 = lambda(qlf1.Qualifiers);
            var qns2 = lambda(qlf2.Qualifiers);

            if (qns1 != qns2)
            {
                lambdaHead();
                Recs.AddDifference("Q04", "DIFF", true, "Qualifers", qns1, qns2);
            }
        }

        protected void CheckDiffsExtensions(
            Aas.IHasExtensions ext1, Aas.IHasExtensions ext2)
        {
            // access
            if (ext1 == null || ext2 == null)
                return;

            if (ext1.Extensions == null && ext2.Extensions == null)
                // nothing to do
                return;

            Action lambdaHead = () =>
                Recs.AddElemDetail("X01", "DIFF", true, "Difference(s) on element of second: ",
                    (ext2 as Aas.IReferable)?.GetReference());

            if (ext1.Extensions == null)
            {
                lambdaHead();
                Recs.AddStatement("X02", "DIFF", true, "First Extentions is null while second is not!");
                return;
            }
            if (ext2.Extensions == null)
            {
                lambdaHead();
                Recs.AddStatement("X03", "DIFF", true, "Second Extentions is null while first is not!");
                return;
            }

            Func<IEnumerable<Aas.IExtension>, string> lambda = (qs) =>
                "[" + string.Join(", ", qs.Select((q) => "" + q.Name + "=" + q.Value).OrderBy((x) => x)) + "]";

            var qns1 = lambda(ext1.Extensions);
            var qns2 = lambda(ext2.Extensions);

            if (qns1 != qns2)
            {
                lambdaHead();
                Recs.AddDifference("X04", "DIFF", true, "Extensions", qns1, qns2);
            }
        }

        protected void CheckDiffsBetweenReferables(
            Aas.IReferable firstRf, Aas.IReferable secondRf,
            bool checkIdShort = false, bool checkSemanticId = false)
        {
            // access
            if (firstRf == null || secondRf == null)
                return;

            var rl = new LogicValidationRecordList();

            // idShort
            if (checkIdShort && firstRf.IdShort != secondRf.IdShort)
                rl.AddDifference("E02", "DIFF", true, "idShort", firstRf.IdShort, secondRf.IdShort);

            // semId
            if (checkSemanticId && firstRf is Aas.IHasSemantics hs1 && secondRf is Aas.IHasSemantics hs2)
            {
                var si1 = hs1.SemanticId?.ToStringExtended(1);
                var si2 = hs2.SemanticId?.ToStringExtended(1);
                if (si1 != si2)
                    rl.AddDifference("E03", "DIFF", true, "semanticId", si1, si2);
            }

            // displayName
            var dn1 = firstRf.DisplayName?.ToStringExtended(1);
            var dn2 = firstRf.DisplayName?.ToStringExtended(1);
            if (dn1 != dn2)
                rl.AddDifference("E04", "DIFF", true, "displayName", dn1, dn2);

            // description
            var desc1 = firstRf.Description?.ToStringExtended(1);
            var desc2 = secondRf.Description?.ToStringExtended(1);
            if (desc1 != desc2)
                rl.AddDifference("E05", "DIFF", true, "description", desc1, desc2);

            // Identifiables
            if (firstRf is Aas.IIdentifiable idf1 && secondRf is Aas.IIdentifiable idf2)
            {
                // id
                var id1 = "" + idf1.Id;
                var id2 = "" + idf2.Id;
                if (id1 != id2)
                    rl.AddDifference("E06", "DIFF", true, "Identifiable/id", id1, id2);

                // administrative information
                if (idf1.Administration is Aas.IAdministrativeInformation ai1
                    && idf2.Administration is Aas.IAdministrativeInformation ai2)
                {
                    var ais1 = ai1.ToStringExtended(1);
                    var ais2 = ai2.ToStringExtended(1);
                    if (ais1 != ais2)
                        rl.AddDifference("E07", "DIFF", true, "administration", ais1, ais2);
                }

            }

            // Qualifiers
            if (firstRf is Aas.IQualifiable qlf1 && secondRf is Aas.IQualifiable qlf2)
                CheckDiffsQualifable(qlf1, qlf2);

            // Extensions
            if (firstRf is Aas.IHasExtensions ext1 && secondRf is Aas.IHasExtensions ext2)
                CheckDiffsExtensions(ext1, ext2);

            // for the sake of simplicity, do this not polymorphic ..
            if (firstRf is Aas.IProperty prop1 && secondRf is Aas.IProperty prop2)
            {
                // valueType
                var vt1 = "" + Stringification.ToString(prop1.ValueType);
                var vt2 = "" + Stringification.ToString(prop2.ValueType);
                if (vt1 != vt2)
                    rl.AddDifference("E08", "DIFF", true, "valueType", vt1, vt2);

                // value
                var vl1 = "" + prop1.Value;
                var vl2 = "" + prop2.Value;
                if (vl1 != vl2)
                    rl.AddDifference("E09", "DIFF", true, "value",
                        AdminShellUtil.ShortenWithEllipses(vl1, 60),
                        AdminShellUtil.ShortenWithEllipses(vl2, 60));
            }

            if (firstRf is Aas.IMultiLanguageProperty mlp1 && secondRf is Aas.IMultiLanguageProperty mlp2)
            {
                var ml1 = "" + mlp1.Value?.ToStringExtended(1);
                var ml2 = "" + mlp2.Value?.ToStringExtended(1);
                if (ml1 != ml2)
                    rl.AddDifference("E10", "DIFF", true, "valueType", ml1, ml2);
            }

            // Final
            if (rl.Count > 0)
            {
                Recs.AddElemDetail("E01", "DIFF", true, "Difference(s) on element of second: ", secondRf.GetReference());
                Recs.AddRange(rl);
            }
        }

        /// <summary>
        /// Check, how much equal the two referables are.
        /// Returns: 3 = idShort&semId are equal, 2 = semId is equal, 
        /// 1 = shortId is equal, 0 else.
        /// </summary>
        /// <returns> </returns>
        protected int CheckReferablesEqualityLevel(
            Aas.IReferable firstRf, Aas.IReferable secondRf)
        {
            // need to have semanticIds
            if (!(firstRf is Aas.IHasSemantics hs1 && secondRf is Aas.IHasSemantics hs2))
                return 0;

            // TODO: need to be the same SME kind??

            // check semanticId
            var si1 = hs1.SemanticId?.ToStringExtended(1);
            var si2 = hs2.SemanticId?.ToStringExtended(1);
            var equalSemInd = si1 == si2;

            // for the idShort, allow a bit sloppyness
            var ids1 = firstRf.IdShort.Trim();
            var ids2 = secondRf.IdShort.Trim();
            var equalIdShort = ids1.Equals(ids2, StringComparison.InvariantCultureIgnoreCase);

            // okay, assess
            if (equalSemInd && equalIdShort)
                return 3;
            if (equalSemInd)
                return 2;
            if (equalIdShort)
                return 1;
            return 0;
        }

        protected void ListSme(Aas.ISubmodelElement root, string recId, string recOutcome)
        {
            root?.RecurseOnReferables(
                null, includeThis: true,
                lambda: (o, parents, rf) =>
                {
                    if (!(rf is Aas.ISubmodelElement sme))
                        return false;
                    var indent = "";
                    if (parents != null && parents.Count > 0)
                    {
                        for (int i = 0; i < parents.Count - 1; i++)
                            indent += "  ";
                        indent += "+-";
                    }
                    Recs.AddStatement(recId, recOutcome, true,
                        $"{indent}[{sme.GetSelfDescription()?.ElementAbbreviation}] {sme.IdShort} / " +
                        (sme as IHasSemantics).SemanticId?.ToStringExtended(1));
                    return true;
                });
        }

        protected void RecurseOnReferables(
            Aas.IReferable firstRf, Aas.IReferable secondRf,
            bool checkIdShort = false, bool checkSemanticId = false)
        {
            // access
            if (firstRf == null || secondRf == null)
                return;

            // differences of SM itself
            CheckDiffsBetweenReferables(firstRf, secondRf, checkIdShort: checkIdShort, checkSemanticId: checkSemanticId);

            // create 2 lists of children
            var firstChilds = firstRf.EnumerateChildren().ToList();
            var secondChilds = secondRf.EnumerateChildren().ToList();

            if (firstChilds.Count < 1 && secondChilds.Count < 1)
                // good case
                return;

            Action lambdaHead = () =>
                Recs.AddElemDetail("R10", "DIFF", true, "Difference(s) on element of second: ",
                secondRf.GetReference());

            if (firstChilds.Count < 1)
            {
                lambdaHead();
                Recs.AddStatement("R11", "DIFF", true, "First list of children is 0 while second is not!");
                return;
            }

            if (secondChilds.Count < 1)
            {
                lambdaHead();
                Recs.AddStatement("R12", "DIFF", true, "Second list of children is 0 while first is not!");
                return;
            }

            // the list of 2nd childs in the frame of reference
            foreach (var ch2 in secondChilds)
            {
                // try find in first a child of much equality as possible
                Aas.IReferable foundRf1 = null;
                var foundEqL = 0;
                foreach (var ch1 in firstChilds)
                {
                    var eql = CheckReferablesEqualityLevel(ch1, ch2);
                    if (eql >= 1 && eql > foundEqL)
                    {
                        foundRf1 = ch1;
                        foundEqL = eql;
                    }
                }

                // ok, which level of equality?
                if (foundEqL >= 1)
                {
                    // matching sibling found, remove from pending list
                    firstChilds.Remove(foundRf1 as ISubmodelElement);

                    // recurse into found element
                    RecurseOnReferables(foundRf1, ch2, checkIdShort: true, checkSemanticId: true);
                }
                else
                {
                    // report an addition
                    Recs.AddElemDetail("R20", "ADD", true,
                        "Second: Added element found:", ch2.GetReference());
                    ListSme(ch2, "R21", "ADD");
                }
            }

            // all remaing of first were NOT found and are therefore deleted
            foreach (var del1 in firstChilds)
            {
                // report an deletetion
                Recs.AddElemDetail("R30", "DELETE", true,
                    "Second: Deleted element found:", del1.GetReference());
                ListSme(del1, "R31", "DELETE");
            }
        }

        //
        // RepoKind
        //

        public void PerformCompare(
            Aas.Environment firstEnv, Aas.Environment secondEnv,
            string firstFn, string secondFn)
        {
            // access
            if (firstEnv == null || secondEnv == null)
            {
                Log.Singleton.Error("Compare SMT: First or second AAS environment not given. Aborting.");
                return;
            }
            FirstEnv = firstEnv;
            SecondEnv = secondEnv;

            // head
            Recs.AddComment("Comparison of Submodel templates");
            Recs.AddComment("Report performed: " + DateTime.Now.ToShortDateString());
            Recs.AddComment("First  AASX (aux) : " + firstFn);
            Recs.AddComment("Second AASX (main): " + secondFn);

            // SMT content
            for (int i = 0; i < 2; i++)
            {
                if (IndexEnv(i).AssetAdministrationShells == null
                    || IndexEnv(i).AssetAdministrationShells.Count < 1
                    || IndexEnv(i).AssetAdministrationShells[0].Submodels == null
                    || IndexEnv(i).AssetAdministrationShells[0].Submodels.Count < 1
                    || IndexEnv(i).Submodels == null
                    || IndexEnv(i).Submodels.Count < 1)
                {
                    Log.Singleton.Error($"Compare SMT: {IndexName(i)} does not have sufficient information. " +
                        "Aborting!");
                }
            }
            First = FirstEnv.FindSubmodel(FirstEnv.AssetAdministrationShells[0].Submodels[0]);
            Second = SecondEnv.FindSubmodel(SecondEnv.AssetAdministrationShells[0].Submodels[0]);
            if (First == null || Second == null)
            {
                Log.Singleton.Error($"Compare SMT: either First or Second SMT cannot be found properly. " +
                    "Aborting!");
            }
            Recs.AddComment("First  SMT idShort: " + First?.IdShort);
            Recs.AddComment("Second SMT idShort: " + Second?.IdShort);
            First.SetAllParents();
            Second.SetAllParents();

            // templates
            for (int i = 0; i < 2; i++)
                if (IndexSm(i).Kind != ModellingKind.Template)
                    Recs.AddStatement("GEN01", "WARN", true, $"{IndexName(i)} is not kind = Template!");

            // recursion on SM itself
            RecurseOnReferables(First, Second, checkIdShort: true, checkSemanticId: true);
        }
    }
}
