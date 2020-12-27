using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdminShellNS;
using static BlazorUI.Pages.Index;
using static AdminShellNS.AdminShellV20;

namespace BlazorUI.Data
{
    public class SubmodelText
    {
        public string text { get; set; }
    }

    public class AASService
    {

        public AASService()
        {
            // buildTree();
            // NewDataAvailable?.Invoke(this, EventArgs.Empty);

            Program.NewDataAvailable += (s, a) =>
            {
                buildTree();
                NewDataAvailable?.Invoke(this, EventArgs.Empty);
            };
        }
        public event EventHandler NewDataAvailable;

        public static List<Item> items = null;
        public static List<Item> viewItems = null;

        public List<Item> GetTree()
        {
            // buildTree();
            updateVisibleTree();
            return viewItems;
        }

        public void updateVisibleTree()
        {

        }

        public void syncSubTree(Item item)
        {
            if (item.Tag is SubmodelElementCollection)
            {
                var smec = item.Tag as SubmodelElementCollection;
                if (item.Childs.Count() != smec.value.Count)
                {
                    createSMECItems(item, smec, item.envIndex);
                }
            }
        }
        public void buildTree()
        {

            items = new List<Item>();
            for (int i = 0; i < 1; i++)
            {
                Item root = new Item();
                root.envIndex = i;
                if (Program.env != null)
                {
                    root.Text = Program.env.AasEnv.AdministrationShells[0].idShort;
                    root.Tag = Program.env.AasEnv.AdministrationShells[0];
                    if (true)
                    {
                        List<Item> childs = new List<Item>();
                        foreach (var sm in Program.env.AasEnv.Submodels)
                        {
                            if (sm != null && sm.idShort != null)
                            {
                                var smItem = new Item();
                                smItem.envIndex = i;
                                smItem.Text = sm.idShort;
                                smItem.Tag = sm;
                                childs.Add(smItem);
                                List<Item> smChilds = new List<Item>();
                                if (sm.submodelElements != null)
                                foreach (var sme in sm.submodelElements)
                                {
                                    var smeItem = new Item();
                                    smeItem.envIndex = i;
                                    smeItem.Text = sme.submodelElement.idShort;
                                    smeItem.Tag = sme.submodelElement;
                                    smChilds.Add(smeItem);
                                    if (sme.submodelElement is SubmodelElementCollection)
                                    {
                                        var smec = sme.submodelElement as SubmodelElementCollection;
                                        createSMECItems(smeItem, smec, i);
                                    }
                                    if (sme.submodelElement is Operation)
                                    {
                                        var o = sme.submodelElement as Operation;
                                        createOperationItems(smeItem, o, i);
                                    }
                                    if (sme.submodelElement is Entity)
                                    {
                                        var e = sme.submodelElement as Entity;
                                        createEntityItems(smeItem, e, i);
                                    }
                                }
                                smItem.Childs = smChilds;
                                foreach (var c in smChilds)
                                    c.parent = smItem;
                            }
                        }
                        root.Childs = childs;
                        foreach (var c in childs)
                            c.parent = root;
                        items.Add(root);
                    }
                }
            }
            viewItems = items;
        }

        void createSMECItems(Item smeRootItem, SubmodelElementCollection smec, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var sme in smec.value)
            {
                if (sme != null && sme.submodelElement != null)
                {
                    var smeItem = new Item();
                    smeItem.envIndex = i;
                    smeItem.Text = sme.submodelElement.idShort;
                    smeItem.Tag = sme.submodelElement;
                    smChilds.Add(smeItem);
                    if (sme.submodelElement is SubmodelElementCollection)
                    {
                        var smecNext = sme.submodelElement as SubmodelElementCollection;
                        createSMECItems(smeItem, smecNext, i);
                    }
                    if (sme.submodelElement is Operation)
                    {
                        var o = sme.submodelElement as Operation;
                        createOperationItems(smeItem, o, i);
                    }
                    if (sme.submodelElement is Entity)
                    {
                        var e = sme.submodelElement as Entity;
                        createEntityItems(smeItem, e, i);
                    }
                }
            }
            smeRootItem.Childs = smChilds;
            foreach (var c in smChilds)
                c.parent = smeRootItem;
        }

        void createOperationItems(Item smeRootItem, Operation op, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var v in op.inputVariable)
            {
                var smeItem = new Item();
                smeItem.envIndex = i;
                smeItem.Text = v.value.submodelElement.idShort;
                smeItem.Type = "In";
                smeItem.Tag = v.value.submodelElement;
                smChilds.Add(smeItem);
            }
            foreach (var v in op.outputVariable)
            {
                var smeItem = new Item();
                smeItem.envIndex = i;
                smeItem.Text = v.value.submodelElement.idShort;
                smeItem.Type = "Out";
                smeItem.Tag = v.value.submodelElement;
                smChilds.Add(smeItem);
            }
            foreach (var v in op.inoutputVariable)
            {
                var smeItem = new Item();
                smeItem.envIndex = i;
                smeItem.Text = v.value.submodelElement.idShort;
                smeItem.Type = "InOut";
                smeItem.Tag = v.value.submodelElement;
                smChilds.Add(smeItem);
            }
            smeRootItem.Childs = smChilds;
            foreach (var c in smChilds)
                c.parent = smeRootItem;
        }

        void createEntityItems(Item smeRootItem, Entity e, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var s in e.statements)
            {
                if (s != null && s.submodelElement != null)
                {
                    var smeItem = new Item();
                    smeItem.envIndex = i;
                    smeItem.Text = s.submodelElement.idShort;
                    smeItem.Type = "In";
                    smeItem.Tag = s.submodelElement;
                    smChilds.Add(smeItem);
                }
            }
            smeRootItem.Childs = smChilds;
            foreach (var c in smChilds)
                c.parent = smeRootItem;
        }

        public List<Submodel> GetSubmodels()
        {
            return Program.env.AasEnv.Submodels;
        }
    }
}
