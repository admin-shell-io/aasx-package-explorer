using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions for operation of Package Explorer
    /// </summary>
    public class PackageExplorer : AasxDefinitionBase
    {
        public static PackageExplorer Static = new PackageExplorer();

        public AdminShell.ConceptDescription
            CD_AasxLoadedNavigateTo;

        public PackageExplorer()
        {
            // info
            this.DomainInfo = "AASX Package Explorer";

            // Referable
            CD_AasxLoadedNavigateTo = CreateSparseConceptDescription("en", "IRI",
                "AasxLoadedNavigateTo",
                "http://admin-shell.io/aasx-package-explorer/main/AasxLoadedNavigateTo/1/0",
                "Specifies a ReferenceElement, to which the Package Explorer will refer directly " +
                "after loading. Can be situated in any Submodel, but on first hierarchy level.");

            // reflect
            AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
        }
    }
}
