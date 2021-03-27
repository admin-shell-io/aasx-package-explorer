using AdminShellNS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;

namespace AasxFormatCst
{
    public class AasxToCst
    {
        protected AdminShellPackageEnv _env;

        protected int _customIndex = 1;
        public string CustomNS = "UNSPEC";

        public ListOfUnique<CstClassDef.ClassDefinition> ClassDefs = new ListOfUnique<CstClassDef.ClassDefinition>();
        public ListOfUnique<CstPropertyDef.PropertyDefinition> PropertyDefs = new ListOfUnique<CstPropertyDef.PropertyDefinition>();
        public List<CstPropertyRecord.PropertyRecord> PropertyRecs = new List<CstPropertyRecord.PropertyRecord>();

        protected Dictionary<AdminShell.ConceptDescription, CstPropertyDef.PropertyDefinition>
            _cdToProp = new Dictionary<AdminShellV20.ConceptDescription, CstPropertyDef.PropertyDefinition>();

        protected CstIdStore _knownIdStore = new CstIdStore();

        public AasxToCst(string jsonKnownIds = null)
        {
            _knownIdStore.AddFromFile(jsonKnownIds);
        }

        private CstIdObjectBase GenerateCustomId(string threePrefix)
        {
            var res = new CstIdObjectBase()
            {
                Namespace = "CustomNS",
                ID = String.Format("{0:}{1:000}", threePrefix, _customIndex++),
                Revision = "001"
            };
            return res;
        }

        private void RecurseOnSme(
            AdminShell.SubmodelElementWrapperCollection smwc,
            CstIdObjectBase presetId,
            string presetClassType,
            CstPropertyRecord.ListOfProperty propRecs)
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

            // add the class def (to have the following classes below)
            ClassDefs.AddIfUnique(clsdef);

            // values
            for (int smcMode=0; smcMode < 2; smcMode++)
                foreach (var smw in smwc)
                {
                    // trivial
                    var sme = smw?.submodelElement;
                    if (sme == null)
                        continue;

                    // is SMC? .. ugly contraption to have first all Properties, then all SMCs
                    var smc = sme as AdminShell.SubmodelElementCollection;
                    if ((smcMode == 0 && smc != null)
                        || (smcMode == 1 && smc == null))
                        continue;

                    // check, if ConceptDescription exists ..
                    var cd = _env?.AasEnv.FindConceptDescription(sme.semanticId);

                    // try to take over as much information from the pure SME as possible
                    var semid = sme.semanticId.GetAsExactlyOneKey()?.value;
                    string refStr = null;
                    string refName = null;
                    CstIdDictionaryItem refItem = null;
                    if (semid != null)
                    {
                        // standardized ID?
                        if (semid.StartsWith("0173") || semid.StartsWith("0112"))
                        {
                            refStr = semid;
                        }

                        // already known, fixed id?
                        var it = _knownIdStore?.FindStringSemId(semid);
                        if (it != null)
                        {
                            if (it.cstRef != null)
                                refStr = it.cstRef;
                            if (it.cstId != null)
                            {
                                refStr = it.cstId.ToRef();
                                refItem = it;
                            }
                            if (it.preferredName != null)
                                refName = it.preferredName;
                        }
                    }

                    // prepare a prop def & data type
                    CstPropertyDef.PropertyDefinition tmpPd = null;
                    string tmpDt = null;
                    if (refStr != null)
                    {
                        // make it
                        var bo = CstIdObjectBase.Parse(refStr);
                        tmpPd = new CstPropertyDef.PropertyDefinition(bo);

                        // more info
                        tmpPd.Name = "" + sme.idShort;
                        if (sme.description != null)
                            tmpPd.Remark = sme.description.GetDefaultStr("en");

                        if (sme is AdminShell.Property prop)
                            tmpDt = prop.valueType;

                        // more info
                        if (cd != null)
                        {
                            var ds61360 = cd.IEC61360Content;
                            if (ds61360 != null)
                            {
                                if (ds61360.definition != null)
                                    tmpPd.Definition = ds61360.definition.GetDefaultStr("en");

                                var dst = ds61360.dataType?.Trim().ToUpper();
                                if (ds61360 == null && dst != null)
                                {
                                    tmpDt = dst;
                                }
                            }
                        }

                        // default
                        if (!tmpDt.HasContent())
                            tmpDt = "STRING";
                    }

                    if (smc != null)
                    {
                        // SMC

                        // make a reference property def
                        // the property itself needs to have an ALTERED ID, to be not ídentical with
                        if (tmpPd != null)
                        {
                            // class id
                            tmpPd.ID = "BLPRP_" + tmpPd.ID;

                            // will be a reference to the intended class
                            tmpPd.DataType = new CstPropertyDef.DataType() {
                                Type = "Reference",
                                BlockReference = "" + refStr
                            };

                            // rest of attributes
                            tmpPd.ObjectType = "02";
                            tmpPd.Status = "Released";

                            // add
                            PropertyDefs.AddIfUnique(tmpPd);
                        }

                        // create a new class attribute (with reference to ALTERED ID!)
                        var attr = new CstClassDef.ClassAttribute()
                        {
                            Type = "Property",
                            Reference = tmpPd?.ToRef() ?? "NULL"
                        };
                        clsdef.ClassAttributes.AddIfUnique(attr);

                        // start new list of property values, embedded in a PropertyRecord
                        var rec = new CstPropertyRecord.PropertyRecord();
                        var lop = new CstPropertyRecord.ListOfProperty();
                        rec.ClassDefinition = refStr;
                        rec.Properties = lop;
                        var pr = new CstPropertyRecord.Property()
                        {
                            // ID = refStr, // to be the block id
                            ID = tmpPd.ToRef(),
                            Name = "" + smc.idShort,
                            ValueProps = rec
                        };
                        if (propRecs != null)
                            propRecs.Add(pr);

                        // recursion, but as Block
                        // TODO: extend Parse() to parse also ECLASS, IEC CDD
                        var blockId = CstIdObjectBase.Parse(refStr);
                        blockId.Name = refName;
                        RecurseOnSme(smc.value, blockId, "Block", lop);
                    }
                    else
                    {
                        // normal case .. Property or so

                        // create a new class attribute
                        var attr = new CstClassDef.ClassAttribute()
                        {
                            Type = "Property",
                            Reference = refStr
                        };
                        clsdef.ClassAttributes.AddIfUnique(attr);

                        // make a "normal" property definition
                        if (tmpPd != null)
                        {
                            // use data type?
                            if (tmpDt != null)
                                tmpPd.DataType = new CstPropertyDef.DataType() { Type = AasxCstUtils.ToPascalCase(tmpDt) };

                            tmpPd.ObjectType = "02";
                            tmpPd.Status = "Released";
                            PropertyDefs.AddIfUnique(tmpPd);
                        }

                        // make a prop rec
                        var pr = new CstPropertyRecord.Property()
                        {
                            ValueStr = sme.ValueAsText(),
                            ID = refStr,
                            Name = "" + sme.idShort
                        };
                        if (propRecs != null)
                            propRecs.Add(pr);
                    }
                }            
        }

        public void ExportSingleSubmodel(
            AdminShellPackageEnv env, string path,
            AdminShell.Key smId,
            IEnumerable<AdminShell.Referable> cdReferables,
            CstIdObjectBase topClassId)
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

            // Step 2: make up a list of used semantic references and write to default file
            var tmpIdStore = new CstIdStore();
            tmpIdStore.CreateEmptyItemsFromSMEs(sm.submodelElements, omitIecEclass: true);
            tmpIdStore.WriteToFile(path + "_default_prop_refs.json");

            // Step 3: start list of (later) lson entities
            // Note: already done by class init

            var lop = new CstPropertyRecord.ListOfProperty();
            PropertyRecs.Add(new CstPropertyRecord.PropertyRecord()
            {
                ID = "0815",
                ClassDefinition = topClassId.ToRef(),
                ObjectType = "PR",
                UnitSystem = 1,
                Properties = lop,
                ClassifiedObject = new CstPropertyRecord.ClassifiedObject()
                {
                    ObjectType = "Item",
                    ClassifyRevision = true,
                    ID = new List<CstPropertyRecord.ID>(new[] { new CstPropertyRecord.ID() { 
                        PropertyName = "item_id",
                        PropertyValue = "000337"
                    }}),
                    Properties = new CstPropertyRecord.ListOfProperty(new[] { new CstPropertyRecord.Property() {
                        PropertyName = "object_name",
                        PropertyValue = "" + topClassId.Name
                    }})
                }
            });

            // Step 4: recursively look at SME
            RecurseOnSme(sm.submodelElements, topClassId, "Application Class", lop);

            // Step 90: write class definitions
            var clsRoot = new CstClassDef.Root() { ClassDefinitions = ClassDefs };
            clsRoot.WriteToFile(path + "_classdefs.json");           

            // Step 91: write property definitions
            var prpRoot = new CstPropertyDef.Root() { PropertyDefinitions = PropertyDefs };
            prpRoot.WriteToFile(path + "_propdefs.json");

            // Step 92: write property definitions
            var recRoot = new CstPropertyRecord.Root() { PropertyRecords = PropertyRecs };
            recRoot.WriteToFile(path + "_proprecs.json");
        }
    }
}
