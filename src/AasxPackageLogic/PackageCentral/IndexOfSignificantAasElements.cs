/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// This enum holds a defined set of elements, which might be indexed by an 
    /// <c>IndexOfSignificantAasElements</c>
    /// </summary>
    public enum SignificantAasElement
    {
        Unknown,
        EventStructureChangeOutwards,
        EventUpdateValueOutwards,
        QualifiedAnimation
    }

    /// <summary>
    /// This is the datat structure which is kept for each indexed significant AAS element.
    /// The idea is record is to be found fast and to index an AAS element in a way, that
    /// is can be found by an AAS reference rather than an object reference, as the AAS
    /// might have been changed (completely) between times of re-indexing it.
    /// </summary>
    public class SignificantAasElemRecord
    {
        public SignificantAasElement Kind;

        /// <summary>
        /// This reference is (kind of long-lasting) stored in the <c>IndexOfSignificantAasElements</c>
        /// </summary>
        public Reference Reference;

        /// <summary>
        /// This object reference will be filled out upon retrieval!
        /// </summary>
        public IClass LiveObject;
    }

    public class IndexOfSignificantAasElements
    {
        protected MultiValueDictionary<SignificantAasElement, SignificantAasElemRecord> _records
            = new MultiValueDictionary<SignificantAasElement, SignificantAasElemRecord>();

        public IndexOfSignificantAasElements() { }

        public IndexOfSignificantAasElements(AasCore.Aas3_0_RC02.Environment env)
        {
            Index(env);
        }

        public void Add(
            SignificantAasElement kind,
            Submodel sm,
            List<IReferable> parents,
            ISubmodelElement sme)
        {
            var r = new SignificantAasElemRecord()
            {
                Kind = kind,
                //Reference = sm?.GetReference()
                //    + parents?.GetReference()
                //    + sme?.GetReference(includeParents: false)
                Reference = sm?.GetReference().Add(parents?.GetReference()).Add(sme?.GetModelReference())
            };
            _records.Add(kind, r);
        }

        public void Index(AasCore.Aas3_0_RC02.Environment env)
        {
            // trivial
            if (env == null)
                return;
            _records = new MultiValueDictionary<SignificantAasElement, SignificantAasElemRecord>();

            // find all Submodels in use, but no one twice
            var visited = new Dictionary<Submodel, bool>();
            foreach (var sm in env.FindAllSubmodelGroupedByAAS())
                if (!visited.ContainsKey(sm))
                    visited.Add(sm, true);

            // now, call them in order to find elements
            foreach (var sm in visited.Keys)
                sm.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                {
                    if (sme is BasicEventElement)
                    {
                        if (true == sme.SemanticId?.Matches(
                            AasxPredefinedConcepts.AasEvents.Static.CD_UpdateValueOutwards.GetReference(),
                            MatchMode.Relaxed))
                            Add(SignificantAasElement.EventUpdateValueOutwards, sm, parents, sme);

                        if (true == sme.SemanticId?.Matches(
                            AasxPredefinedConcepts.AasEvents.Static.CD_StructureChangeOutwards.GetReference(),
                            MatchMode.Relaxed))
                            Add(SignificantAasElement.EventStructureChangeOutwards, sm, parents, sme);
                    }

                    if (null != sme.FindQualifierOfType("Animate.Args"))
                        Add(SignificantAasElement.QualifiedAnimation, sm, parents, sme);

                    // recurse
                    return true;
                });
        }

        public IEnumerable<SignificantAasElemRecord> Retrieve(
            AasCore.Aas3_0_RC02.Environment env,
            SignificantAasElement kind)
        {
            if (env == null || true != _records.ContainsKey(kind))
                yield break;
            foreach (var r in _records[kind])
            {
                // look up
                var lo = env.FindReferableByReference(r.Reference);
                if (lo != null)
                {
                    r.LiveObject = lo;
                    yield return r;
                }
            }
        }
    }
}
