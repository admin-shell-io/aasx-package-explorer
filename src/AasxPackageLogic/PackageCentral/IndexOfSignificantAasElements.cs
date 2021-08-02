using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public AdminShell.Reference Reference;

        /// <summary>
        /// This object reference will be filled out upon retrieval!
        /// </summary>
        public AdminShell.IAasElement LiveObject;
    }

    public class IndexOfSignificantAasElements
    {
        protected MultiValueDictionary<SignificantAasElement, SignificantAasElemRecord> _records
            = new MultiValueDictionary<SignificantAasElement, SignificantAasElemRecord>();

        public IndexOfSignificantAasElements() { }

        public IndexOfSignificantAasElements(AdminShell.AdministrationShellEnv env)
        {
            Index(env);
        }

        public void Add(
            SignificantAasElement kind,
            AdminShell.Submodel sm,
            AdminShell.ListOfSubmodelElement parents,
            AdminShell.SubmodelElement sme)
        {
            var r = new SignificantAasElemRecord()
            {
                Kind = kind,
                Reference = sm?.GetReference()
                    + parents?.GetReference()
                    + sme?.GetReference()
            };
            _records.Add(kind, r);
        }

        public void Index(AdminShell.AdministrationShellEnv env)
        {
            // trivial
            if (env == null)
                return;
            _records = new MultiValueDictionary<SignificantAasElement, SignificantAasElemRecord>();

            // find all Submodels in use, but no one twice
            var visited = new Dictionary<AdminShell.Submodel, bool>();
            foreach (var sm in env.FindAllSubmodelGroupedByAAS())
                if (!visited.ContainsKey(sm))
                    visited.Add(sm, true);

            // now, call them in order to find elements
            foreach (var sm in visited.Keys)
                sm.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                {
                    if (sme is AdminShell.BasicEvent be)
                    {
                        if (true == sme.semanticId?.Matches(
                            AasxPredefinedConcepts.AasEvents.Static.CD_UpdateValueOutwards,
                            AdminShellV20.Key.MatchMode.Relaxed))
                        Add(SignificantAasElement.EventUpdateValueOutwards, sm, parents, sme);

                        if (true == sme.semanticId?.Matches(
                            AasxPredefinedConcepts.AasEvents.Static.CD_StructureChangeOutwards,
                            AdminShellV20.Key.MatchMode.Relaxed))
                            Add(SignificantAasElement.EventStructureChangeOutwards, sm, parents, sme);
                    }
                });
        }

        public IEnumerable<SignificantAasElemRecord> Retrieve(
            AdminShell.AdministrationShellEnv env, 
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
