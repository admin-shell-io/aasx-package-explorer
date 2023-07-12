/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// resharper disable all

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;
using AasxPackageLogic;
using BlazorUI;

namespace BlazorUI.Data
{
    public class SubmodelText
    {
        public string text { get; set; }
    }

    public class Item
    {
        public Aas.IReferable Referable;
        public string Text { get; set; }
        public IEnumerable<Item> Childs { get; set; }
        public object parent { get; set; }
        public string Type { get; set; }
        public object Tag { get; set; }
        public Aas.IReferable ParentContainer { get; set; }
        public Aas.ISubmodelElement Wrapper { get; set; }
        public int envIndex { get; set; }

        public static void updateVisibleTree(List<Item> viewItems, Item selectedNode, IList<Item> ExpandedNodes)
        {
        }
    }

    public class ListOfItems : List<Item>
    {
        public IEnumerable<Item> FindItems(IEnumerable<Item> childs, Func<Item, bool> predicate)
        {
            // access
            if (childs == null)
                yield break;

            // broad search
            foreach (var ch in childs)
                if (predicate == null || predicate.Invoke(ch))
                    yield return ch;

            foreach (var ch in childs)
                foreach (var x in FindItems(ch.Childs, predicate))
                    yield return x;
        }

        public Item FindSubmodel(string smid)
        {
            // access
            if (smid == null)
                return null;

            return FindItems(this, (it) =>
            {
                return it?.Referable is Aas.Submodel sm && sm?.Id == smid;
            }).FirstOrDefault();
        }

        public Item FindReferable(Aas.IReferable rf, string pluginTag)
        {
            // access
            if (rf == null)
                return null;

            // find item in general
            var res = FindItems(this, (it) =>
            {
                return it?.Referable == rf;
            }).FirstOrDefault();

            // special case
            if (res != null && rf is Aas.Submodel && pluginTag.HasContent()
                && res.Childs != null)
                foreach (var ch in res.Childs)
                    if (ch != null
                        && ch.Type == "Plugin"
                        && ch.Tag is Tuple<AdminShellPackageEnv, Aas.Submodel,
                            Plugins.PluginInstance, AasxIntegrationBase.AasxPluginResultVisualExtension> tag
                        && tag?.Item4?.Tag?.Trim().ToLower() == pluginTag.Trim().ToLower())
                        return ch;

            // give back
            return res;
        }

        public Item FindSubmodelPlugin(string smid, string pluginTag)
        {
            // access
            if (smid == null || pluginTag == null)
                return null;

            return FindItems(this, (it) =>
            {
                if (it?.parent == null || !(it.parent is Item parit))
                    return false;
                return parit.Referable is Aas.Submodel sm && sm?.Id == smid
                    && it.Type == "Plugin"
                    && it.Tag is Tuple<AdminShellPackageEnv, Aas.Submodel,
                            Plugins.PluginInstance, AasxIntegrationBase.AasxPluginResultVisualExtension> tag
                    && tag?.Item4?.Tag?.Trim().ToLower() == pluginTag.Trim().ToLower();
            }).FirstOrDefault();
        }

        public static void AddToExpandNodesFor(IList<Item> expanded, Item it)
        {
            if (it == null)
                return;

            if (expanded != null)
                expanded.Add(it);
            AddToExpandNodesFor(expanded, it.parent as Item);
        }
    }


    public class AASService
    {

        public AASService()
        {
            Program.NewDataAvailable += (s, a) =>
            {
                NewDataAvailable?.Invoke(s, a);
            };
        }
        public event EventHandler NewDataAvailable;

        public ListOfItems GetTree(blazorSessionService bi, Item selectedNode, IList<Item> ExpandedNodes)
        {
            Item.updateVisibleTree(bi.items, selectedNode, ExpandedNodes);
            return bi.items;
        }

        public void syncSubTree(Item item)
        {
            if (item.Tag is Aas.SubmodelElementCollection smec)
            {
                if (item.Childs.Count() != smec.Value.Count)
                {
                    createSMECItems(item, smec, item.envIndex);
                }
            }
        }

        public void buildTree(blazorSessionService bi)
        {
            // interested plug-ins
            var _pluginsToCheck = new List<Plugins.PluginInstance>();
            _pluginsToCheck.Clear();
            if (Plugins.LoadedPlugins != null)
                foreach (var lpi in Plugins.LoadedPlugins.Values)
                {
                    try
                    {
                        var x =
                            lpi.InvokeAction(
                                "get-check-visual-extension") as AasxIntegrationBase.AasxPluginResultBaseObject;
                        if (x != null && (bool)x.obj)
                            _pluginsToCheck.Add(lpi);
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
                }

            // now iterate for items
            bi.items = new ListOfItems();
            for (int i = 0; i < 1; i++)
            {
                if (bi.env != null)
                {
                    // NEW (2022-05-17): iterate correctly over AAS, used Submodels
                    foreach (var aas in bi.env.AasEnv.AssetAdministrationShells)
                    {
                        // access
                        if (aas?.Submodels == null)
                            continue;

                        //
                        // make a new AAS root
                        //

                        Item root = new Item();
                        root.envIndex = i;
                        root.Text = aas.IdShort;
                        root.Tag = aas;
                        List<Item> childs = new List<Item>();

                        // find Submodels
                        foreach (var smref in aas.Submodels)
                        {
                            // access
                            var sm = bi.env.AasEnv.FindSubmodel(smref);
                            if (sm?.IdShort == null)
                                continue;

                            // Submodel with parents
                            sm.SetAllParents();

                            // add Submodel
                            var smItem = new Item()
                            {
                                Referable = sm,
                                envIndex = i,
                                Text = sm.IdShort,
                                Tag = sm
                            };
                            childs.Add(smItem);
                            List<Item> smChilds = new List<Item>();

                            // add some plugins?
                            // check for visual extensions
                            if (_pluginsToCheck != null)
                                foreach (var lpi in _pluginsToCheck)
                                {
                                    try
                                    {
                                        var ext = lpi.InvokeAction(
                                            "call-check-visual-extension", sm)
                                            as AasxIntegrationBase.AasxPluginResultVisualExtension;
                                        if (ext != null)
                                        {
                                            var piItem = new Item()
                                            {
                                                Referable = sm,
                                                envIndex = i,
                                                Text = "PLUGIN",
                                                Tag = new Tuple<AdminShellPackageEnv, Aas.Submodel,
                                                    Plugins.PluginInstance, AasxPluginResultVisualExtension>
                                                        (bi.env, sm, lpi, ext),
                                                Type = "Plugin"
                                            };
                                            smChilds.Add(piItem);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                                    }
                                }

                            // add SMEs
                            if (sm.SubmodelElements != null)
                                foreach (var sme in sm.SubmodelElements)
                                {
                                    var smeItem = new Item()
                                    {
                                        Referable = sme,
                                        envIndex = i,
                                        Text = sme.IdShort,
                                        Tag = sme,
                                        ParentContainer = sm,
                                        Wrapper = sme
                                    };
                                    smChilds.Add(smeItem);
                                    if (sme is Aas.SubmodelElementCollection smec)
                                    {
                                        createSMECItems(smeItem, smec, i);
                                    }
                                    if (sme is Aas.Operation smop)
                                    {
                                        createOperationItems(smeItem, smop, i);
                                    }
                                    if (sme is Aas.Entity sment)
                                    {
                                        createEntityItems(smeItem, sment, i);
                                    }
                                }

                            smItem.Childs = smChilds;
                            foreach (var c in smChilds)
                                c.parent = smItem;
                        }

                        // post process for AAS root

                        root.Childs = childs;
                        foreach (var c in childs)
                            c.parent = root;
                        bi.items.Add(root);
                    }
                }
            }
        }

        void createSMECItems(Item smeRootItem, Aas.SubmodelElementCollection smec, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var sme in smec.Value)
            {
                if (sme != null)
                {
                    var smeItem = new Item();
                    smeItem.envIndex = i;
                    smeItem.Text = sme.IdShort;
                    smeItem.Tag = sme;
                    smeItem.ParentContainer = smec;
                    smeItem.Wrapper = sme;
                    smChilds.Add(smeItem);
                    if (sme is Aas.SubmodelElementCollection smesmc)
                    {
                        createSMECItems(smeItem, smesmc, i);
                    }
                    if (sme is Aas.Operation smeop)
                    {
                        createOperationItems(smeItem, smeop, i);
                    }
                    if (sme is Aas.Entity smeent)
                    {
                        createEntityItems(smeItem, smeent, i);
                    }
                }
            }
            smeRootItem.Childs = smChilds;
            foreach (var c in smChilds)
                c.parent = smeRootItem;
        }

        void createOperationItems(Item smeRootItem, Aas.Operation op, int i)
        {
            List<Item> smChilds = new List<Item>();
            foreach (var v in op.InputVariables)
            {
                var smeItem = new Item();
                smeItem.envIndex = i;
                smeItem.Text = v.Value.IdShort;
                smeItem.Type = "In";
                smeItem.Tag = v.Value;
                smChilds.Add(smeItem);
            }
            foreach (var v in op.OutputVariables)
            {
                var smeItem = new Item();
                smeItem.envIndex = i;
                smeItem.Text = v.Value.IdShort;
                smeItem.Type = "Out";
                smeItem.Tag = v.Value;
                smChilds.Add(smeItem);
            }
            foreach (var v in op.InoutputVariables)
            {
                var smeItem = new Item();
                smeItem.envIndex = i;
                smeItem.Text = v.Value.IdShort;
                smeItem.Type = "InOut";
                smeItem.Tag = v.Value;
                smChilds.Add(smeItem);
            }
            smeRootItem.Childs = smChilds;
            foreach (var c in smChilds)
                c.parent = smeRootItem;
        }

        void createEntityItems(Item smeRootItem, Aas.Entity e, int i)
        {
            List<Item> smChilds = new List<Item>();
            if (e.Statements != null)
                foreach (var s in e.Statements)
                {
                    if (s != null)
                    {
                        var smeItem = new Item();
                        smeItem.envIndex = i;
                        smeItem.Text = s.IdShort;
                        smeItem.Type = "In";
                        smeItem.Tag = s;
                        smChilds.Add(smeItem);
                    }
                }
            smeRootItem.Childs = smChilds;
            foreach (var c in smChilds)
                c.parent = smeRootItem;
        }

        public List<Aas.Submodel> GetSubmodels(blazorSessionService bi)
        {
            return bi.env.AasEnv.Submodels;
        }
    }
}
