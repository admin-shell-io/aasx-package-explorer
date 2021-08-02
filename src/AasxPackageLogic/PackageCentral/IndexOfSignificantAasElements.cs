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
        public AdminShell.Reference Reference;
    }

    public class IndexOfSignificantAasElements
    {
        protected MultiValueDictionary<SignificantAasElement, SignificantAasElemRecord> _records
            = new MultiValueDictionary<SignificantAasElement, SignificantAasElemRecord>();

        public void Add(
            SignificantAasElement kind,
            AdminShell.ListOfSubmodelElement parents,
            AdminShell.SubmodelElement sme)
        {
            ;
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
                    if (sme is AdminShell.BasicEvent be
                        && true == sme.semanticId?.Matches(
                            AasxPredefinedConcepts.AasEvents.Static.CD_UpdateValueOutwards,
                            AdminShellV20.Key.MatchMode.Relaxed))
                        Add(SignificantAasElement.EventUpdateValueOutwards, parents, sme);
                });
        }
    }
}
