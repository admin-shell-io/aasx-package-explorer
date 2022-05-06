﻿/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using Newtonsoft.Json;

// ReSharper disable AssignNullToNotNullAttribute .. a bit unclear, why issues here

namespace AasxIntegrationBase
{
    /// <summary>
    /// Base class for an options record. This is a piece of options information, which is
    /// associated with an id of a Submodel template.
    /// </summary>
    public class AasxPluginOptionsRecordBase
    {
    }

    /// <summary>
    /// Base class of plugin options, which may be also load from file.
    /// </summary>
    public class AasxPluginOptionsBase
    {
        /// <summary>
        /// This string marks the "file-format" version of the options.
        /// Typically, it is connected with major versions of the AAS meta model.
        /// </summary>
        public string Version = "AAS3.0";

        protected MultiValueDictionary<string, AasxPluginOptionsRecordBase> _recordLookup = null;

        public virtual void Merge(AasxPluginOptionsBase options)
        {
        }

        protected static T LoadOptionsFromJson<T>(
            string json, string fnInfo,
            JsonSerializerSettings settings = null,
            LogInstance log = null,
            UpgradeMapping[] upgrades = null)
            where T : AasxPluginOptionsBase
        {
            // access
            if (!json.HasContent())
                return null;

            T opts = null;

            // find an upgrade path?
            UpgradeMapping um = null;
            if (upgrades != null)
                foreach (var u in upgrades)
                    if (u?.Trigger.HasContent() == true
                        && u.OldRootType != null
                        && u.UpgradeLambda != null
                        && json.Contains(u.Trigger))
                    {
                        um = u;
                        break;
                    }

            // upgrade or straight forward
            if (um != null)
            {
                log?.Append(new StoredPrint(StoredPrint.Color.Blue,
                    $"Detected an old version of options: {um.Info}. Upgrading from {fnInfo} .."));

                if (um.Replacements != null)
                    foreach (var rkey in um.Replacements.Keys)
                        json = json.Replace(rkey, um.Replacements[rkey]);
                var oldOpts = Newtonsoft.Json.JsonConvert.DeserializeObject(json, um.OldRootType, settings);
                opts = um.UpgradeLambda.Invoke(oldOpts) as T;

                if (opts != null)
                    log?.Append(new StoredPrint(StoredPrint.Color.Blue,
                        $"Upgraded successfully! Consider saving options in new format."));
            }
            else
            {
                // assume current version
                log?.Info($"Try load options from {fnInfo} ..");
                opts = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, settings);
            }

            return opts;
        }

        public static T LoadDefaultOptionsFromAssemblyDir<T>(
            string pluginName, Assembly assy = null,
            JsonSerializerSettings settings = null,
            LogInstance log = null,
            UpgradeMapping[] upgrades = null) 
            where T : AasxPluginOptionsBase
        {
            // expand assy?
            if (assy == null)
                assy = Assembly.GetExecutingAssembly();
            if (pluginName == null || pluginName == "")
                return null;

            // build fn
            var optfn = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(assy.Location),
                        pluginName + ".options.json");

            if (File.Exists(optfn))
            {
                var json = File.ReadAllText(optfn);
                var opts = LoadOptionsFromJson<T>(json, optfn, settings, log, upgrades);
                return opts;
            }

            // no
            return null;
        }

        public class UpgradeMapping
        {
            public string Info = "";
            public string Trigger;
            public Type OldRootType;
            public Dictionary<string, string> Replacements;
            public Func<object, object> UpgradeLambda;
        }

        public void TryLoadAdditionalOptionsFromAssemblyDir<T>(
            string pluginName, Assembly assy = null,
            JsonSerializerSettings settings = null,
            LogInstance log = null,
            UpgradeMapping[] upgrades = null
            ) where T : AasxPluginOptionsBase
        {
            // expand assy?
            if (assy == null)
                assy = Assembly.GetExecutingAssembly();
            if (pluginName == null || pluginName == "")
                return;

            // build dir name
            var baseDir = System.IO.Path.GetDirectoryName(assy.Location);

            // search
            var files = Directory.GetFiles(baseDir, "*.add-options.json");

            foreach (var fn in files)
                try
                {
                    var json = File.ReadAllText(fn);
                    var opts = LoadOptionsFromJson<T>(json, fn, settings, log, upgrades);
                    if (opts != null)
                        this.Merge(opts);
                }
                catch (Exception ex)
                {
                    log?.Error(ex, $"loading additional options (${fn})");
                }
        }
    }

    //
    // Extension: options (records) with lookup of semantic ids
    //

    /// <summary>
    /// Base class for an options record. This is a piece of options information, which is
    /// associated with an id of a Submodel template.
    /// This base class is extended for lookup information.
    /// </summary>
    public class AasxPluginOptionsLookupRecordBase : AasxPluginOptionsRecordBase
    {
        /// <summary>
        /// This keyword is used by the plugin options to code allowed semantic ids for
        /// a Submodel sensitive plugin
        /// </summary>
        public List<AdminShell.Key> AllowSubmodelSemanticId = new List<AdminShell.Key>();
    }

    /// <summary>
    /// Base class of plugin options, which may be also load from file.
    /// This base class is extended for lookup information.
    /// </summary>
    public class AasxPluginLookupOptionsBase : AasxPluginOptionsBase
    {
        private string GenerateIndexKey(AdminShell.Key key)
        {
            if (key == null)
                return null;
            var k = new AdminShell.Key(key);
            var ndx = k?.ToString(format: 1);
            return ndx;
        }

        public void IndexRecord(AdminShell.Key key, AasxPluginOptionsRecordBase rec)
        {
            if (_recordLookup == null)
                _recordLookup = new MultiValueDictionary<string, AasxPluginOptionsRecordBase>();

            var ndx = GenerateIndexKey(key);
            if (!ndx.HasContent())
                return;
            _recordLookup.Add(ndx, rec);
        }

        public void IndexListOfRecords(IEnumerable<AasxPluginOptionsLookupRecordBase> records)
        {
            if (records == null)
                return;

            foreach (var rec in records)
                if (rec?.AllowSubmodelSemanticId != null)
                    foreach (var a2 in rec.AllowSubmodelSemanticId)
                        IndexRecord(a2, rec);
        }

        public bool ContainsIndexKey(AdminShell.Key key)
        {
            // access
            var ndx = GenerateIndexKey(key);
            if (_recordLookup == null || !ndx.HasContent())
                return false;

            return _recordLookup.ContainsKey(ndx);
        }

        public IEnumerable<T> LookupAllIndexKey<T>(AdminShell.Key key)
            where T : AasxPluginOptionsRecordBase
        {
            // access
            var ndx = GenerateIndexKey(key);
            if (_recordLookup == null || !ndx.HasContent())
                yield break;

            if (!_recordLookup.ContainsKey(ndx))
                yield break;

            foreach (var r in _recordLookup[ndx])
                if (r is T rr)
                    yield return rr;
        }
    }
}
