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
    public class DefinitionsPackageExplorer : AasxDefinitionBase
    {
        public static DefinitionsPackageExplorer Static = new DefinitionsPackageExplorer();

        public AdminShell.ConceptDescription
            CD_AasxLoadedNavigateTo;

        public DefinitionsPackageExplorer()
        {
            CD_AasxLoadedNavigateTo = CreateSparseConceptDescription("en", "IRI",
                "AasxLoadedNavigateTo",
                "http://admin-shell.io/aasx-package-explorer/main/AasxLoadedNavigateTo/1/0",
                "Specifies a ReferenceElement, to which the Package Explorer will refer directly " +
                "after loading. Can be situated in any Submodel, but on first hierarchy level.");
        }
    }
}
