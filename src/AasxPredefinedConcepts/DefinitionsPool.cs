/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using Extensions;
using System.Collections.Generic;
using Aas = AasCore.Aas3_0;

#nullable enable

namespace AasxPredefinedConcepts
{
    public class DefinitionsPoolEntityBase
    {
        public string Domain = "";

        public virtual string DisplayDomain { get { return Domain; } }
        public virtual string DisplayType { get { return ""; } }
        public virtual string DisplayName { get { return ""; } }
        public virtual string DisplayId { get { return ""; } }

        public DefinitionsPoolEntityBase() { }

        public DefinitionsPoolEntityBase(string Domain = "")
        {
            this.Domain = Domain;
        }
    }

    public class DefinitionsPoolReferableEntity : DefinitionsPoolEntityBase
    {
        public Aas.IReferable? Ref = null;
        public string Name = "";

        public override string DisplayType
        {
            get
            {
                return (Ref != null) ? "" + Ref.GetSelfDescription()?.ElementAbbreviation : "";
            }
        }

        public override string DisplayName { get { return (Ref != null) ? Ref.IdShort : ""; } }

        public override string DisplayId
        {
            get
            {
                if (Ref is Aas.IIdentifiable id)
                    return "" + id.Id?.ToString();
                return "";
            }
        }

        public DefinitionsPoolReferableEntity() { }

        public DefinitionsPoolReferableEntity(Aas.IReferable? Ref, string Domain = "", string Name = "")
        {
            this.Domain = Domain;
            this.Name = Name;
            this.Ref = Ref;
        }
    }

    public class DefinitionsPool
    {
        // 
        // Singleton
        //

        private static DefinitionsPool thePool = new DefinitionsPool();
        public static DefinitionsPool Static { get { return thePool; } }

        static DefinitionsPool()
        {
            thePool.IndexDefinitions(new DefinitionsExperimental.InteropRelations());

            thePool.IndexDefinitions(AasxPredefinedConcepts.ImageMap.Static);
            thePool.IndexDefinitions(AasxPredefinedConcepts.AsciiDoc.Static);
            thePool.IndexDefinitions(AasxPredefinedConcepts.Plotting.Static);

            thePool.IndexDefinitions(new AasxPredefinedConcepts.DefinitionsMTP.ModuleTypePackage());

            thePool.IndexDefinitions(AasxPredefinedConcepts.PackageExplorer.Static);

            thePool.IndexReferables("Manufacturer Documentation (VDI2770) v1.0",
                new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770()).GetAllReferables());

            thePool.IndexDefinitions(AasxPredefinedConcepts.VDI2770v11.Static);

            thePool.IndexReferables("ZVEI Digital Nameplate v1.0",
                new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate.SetOfNameplate(
                    new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate()).GetAllReferables());

            thePool.IndexDefinitions(AasxPredefinedConcepts.ZveiNameplateV10.Static);

            thePool.IndexReferables("ZVEI Digital Identification v1.0",
                new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate.SetOfIdentification(
                    new AasxPredefinedConcepts.DefinitionsZveiDigitalTypeplate()).GetAllReferables());

            thePool.IndexReferables("ZVEI TechnicalData v1.0",
                new DefinitionsZveiTechnicalData.SetOfDefs(new DefinitionsZveiTechnicalData()).GetAllReferables());

            thePool.IndexDefinitions(AasxPredefinedConcepts.ZveiTechnicalDataV11.Static);

            thePool.IndexDefinitions(AasxPredefinedConcepts.AasEvents.Static);

            thePool.IndexDefinitions(AasxPredefinedConcepts.ZveiTimeSeriesDataV10.Static);
        }

        //
        // Member
        //

        private Dictionary<string, List<DefinitionsPoolEntityBase>> pool =
            new Dictionary<string, List<DefinitionsPoolEntityBase>>();

        //
        // Basic Methods
        //

        public void Clear()
        {
            this.pool.Clear();
        }

        public void Add(DefinitionsPoolEntityBase ent)
        {
            // access
            if (ent?.Domain == null)
                return;

            // new hash value
            if (!this.pool.ContainsKey(ent.Domain))
                this.pool.Add(ent.Domain, new List<DefinitionsPoolEntityBase>());

            // ok, shall be present
            this.pool[ent.Domain].Add(ent);
        }

        public IEnumerable<string> GetDomains()
        {
            foreach (var k in this.pool.Keys)
                yield return k;
        }

        public IEnumerable<DefinitionsPoolEntityBase> GetEntitiesForDomain(string domain)
        {
            // check
            if (!this.pool.ContainsKey(domain))
                yield break;
            foreach (var ent in this.pool[domain])
                yield return ent;
        }

        //
        // Methods
        //

        public void IndexReferables(string Domain, IEnumerable<Aas.IReferable> indexRef)
        {
            foreach (var ir in indexRef)
            {
                if (ir == null)
                    continue;
                this.Add(new DefinitionsPoolReferableEntity(ir, Domain, ir.IdShort));
            }
        }

        public void IndexDefinitions(AasxDefinitionBase bs)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse  -- this seems like a false positive.
            if (bs == null || !bs.DomainInfo.HasContent())
                return;
            IndexReferables(bs.DomainInfo, bs.GetAllReferables());
        }
    }
}
