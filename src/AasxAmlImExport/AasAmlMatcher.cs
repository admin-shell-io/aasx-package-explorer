using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using Aml.Engine.CAEX;

namespace AasxAmlImExport
{
    /// <summary>
    /// Maintains a bidirectinal dictionary between AAS Referables and AML / CAEX Objects
    /// </summary>
    public class AasAmlMatcher
    {
        private Dictionary<AdminShell.Referable, CAEXObject> aasToAml =
            new Dictionary<AdminShell.Referable, CAEXObject>();

        private Dictionary<CAEXObject, AdminShell.Referable> amlToAas =
            new Dictionary<CAEXObject, AdminShell.Referable>();

        public void AddMatch(AdminShell.Referable aasReferable, CAEXObject amlObject)
        {
            aasToAml.Add(aasReferable, amlObject);
            amlToAas.Add(amlObject, aasReferable);
        }

        public ICollection<AdminShell.Referable> GetAllAasReferables()
        {
            return aasToAml.Keys;
        }

        public CAEXObject GetAmlObject(AdminShell.Referable aasReferable)
        {
            if (aasToAml.ContainsKey(aasReferable))
                return aasToAml[aasReferable];
            return null;
        }

        public AdminShell.Referable GetAasObject(CAEXObject amlObject)
        {
            if (amlToAas.ContainsKey(amlObject))
                return amlToAas[amlObject];
            return null;
        }

        public bool ContainsAasObject(AdminShell.Referable aasReferable)
        {
            return aasToAml.ContainsKey(aasReferable);
        }

        public bool ContainsAmlObject(CAEXObject amlObject)
        {
            return amlToAas.ContainsKey(amlObject);
        }
    }
}
