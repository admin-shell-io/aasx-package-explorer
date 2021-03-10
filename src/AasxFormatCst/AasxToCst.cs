using AdminShellNS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxFormatCst
{
    public class AasxToCst
    {
        protected AdminShellPackageEnv _env;

        protected int _customIndex = 1;
        public string CustomNS = "UNSPEC";

        public List<CstClassDef.ClassDefinition> ClassDefs = new List<CstClassDef.ClassDefinition>();
        public List<CstPropertyDef.PropertyDefinition> PropertyDefs = new List<CstPropertyDef.PropertyDefinition>();

        protected Dictionary<AdminShell.ConceptDescription, CstPropertyDef.PropertyDefinition>
            _cdToProp = new Dictionary<AdminShellV20.ConceptDescription, CstPropertyDef.PropertyDefinition>();

        private CstId GenerateCustomId(string threePrefix)
        {
            var res = new CstId()
            {
                Namespace = "CustomNS",
                ID = String.Format("{0:}{1:000}", threePrefix, _customIndex++),
                Revision = "001"
            };
            return res;
        }

        private void RecurseOnSme(
            AdminShell.SubmodelElementWrapperCollection smwc,
            CstId presetId,
            string presetClassType)
        {
            // access
            if (smwc == null)
                return;

            // create a class def
            CstClassDef.ClassDefinition clsdef;
            if (presetId != null)
                clsdef = new CstClassDef.ClassDefinition(presetId);
            else
                clsdef = new CstClassDef.ClassDefinition(GenerateCustomId("CLS"));
            clsdef.ClassType = presetClassType;

            // values
            foreach (var smw in smwc)
            {
                var sme = smw?.submodelElement;
                if (sme == null)
                    continue;

                if (sme is AdminShell.SubmodelElementCollection smc)
                {

                }
                else
                {
                    // normal case .. Property or so

                    // check, if ConceptDescription exists ..
                    var cd = _env?.AasEnv.FindConceptDescription(sme.semanticId);

                    // create a new class attribute
                    var a1 = new CstClassDef.ClassAttribute()
                    {
                        Type = "Property"
                    };

                    // try to take over as much information from the pure SME as possible
                    var semid = sme.semanticId.GetAsExactlyOneKey()?.value;
                    if (semid != null)
                    {
                        if (semid.StartsWith("0173") || semid.StartsWith("0112"))
                        {
                            a1.Reference = semid;
                        }
                    }

                    // already existing as property def?
                    if (_cdToProp.ContainsKey())

                    clsdef.ClassAttributes.Add(a1);
                }
            }

            // finally add the class def
            ClassDefs.Add(clsdef);
        }

        public void ExportSingleSubmodel(
            AdminShellPackageEnv env, string path,
            AdminShell.Key smId,
            IEnumerable<AdminShell.Referable> cdReferables,
            CstId topClassId)
        {
            // access
            if (env?.AasEnv == null || path == null)
                return;
            _env = env;

            var sm = _env?.AasEnv.FindFirstSubmodelBySemanticId(smId);
            if (sm == null)
                return;

            // Step 1: copy all relevant CDs into the AAS
            if (cdReferables != null)
                foreach (var rf in cdReferables)
                    if (rf is AdminShell.ConceptDescription cd)
                        env?.AasEnv.ConceptDescriptions.AddIfNew(cd);

            // Step 2: start list of (later) lson entities
            // Note: already done by class init

            // Step 3: recursively look at SME
            RecurseOnSme(sm.submodelElements, topClassId, "Application Class");

            // Step 90: write class definitions
            var clsRoot = new CstClassDef.Root() { ClassDefinitions = ClassDefs };
            File.WriteAllText(path + "_classdefs.json", JsonConvert.SerializeObject(clsRoot, Formatting.Indented));
        }
    }
}
