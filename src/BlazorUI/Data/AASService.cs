/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// resharper disable all

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdminShellNS;
using BlazorUI;
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
            Program.NewDataAvailable += (s, a) =>
            {
                NewDataAvailable?.Invoke(this, EventArgs.Empty);
            };
        }
        public event EventHandler NewDataAvailable;

        public static List<Item> items = null;
        public static List<Item> viewItems = null;

        public List<Item> GetTree(Item selectedNode, IList<Item> ExpandedNodes)
        {
            Item.updateVisibleTree(viewItems, selectedNode, ExpandedNodes);
            return viewItems;
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
        public void buildTree(blazorIntance bi)
        {

            items = new List<Item>();
            for (int i = 0; i < 1; i++)
            {
                Item root = new Item();
                root.envIndex = i;
                if (bi.env != null)
                {
                    root.Text = bi.env.AasEnv.AdministrationShells[0].idShort;
                    root.Tag = bi.env.AasEnv.AdministrationShells[0];
                    if (true)
                    {
                        List<Item> childs = new List<Item>();
                        foreach (var sm in bi.env.AasEnv.Submodels)
                        {
                            if (sm?.idShort != null)
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
                                        smeItem.ParentContainer = sm;
                                        smeItem.Wrapper = sme;
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
                if (sme?.submodelElement != null)
                {
                    var smeItem = new Item();
                    smeItem.envIndex = i;
                    smeItem.Text = sme.submodelElement.idShort;
                    smeItem.Tag = sme.submodelElement;
                    smeItem.ParentContainer = smec;
                    smeItem.Wrapper = sme;
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
                if (s?.submodelElement != null)
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

        public List<Submodel> GetSubmodels(blazorIntance bi)
        {
            return bi.env.AasEnv.Submodels;
        }
    }
}
