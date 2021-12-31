/*
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
        protected MultiValueDictionary<string, AasxPluginOptionsRecordBase> _recordLookup = null;

        public virtual void Merge(AasxPluginOptionsBase options)
        {
        }

        public static T LoadDefaultOptionsFromAssemblyDir<T>(
            string pluginName, Assembly assy = null,
            JsonSerializerSettings settings = null) where T : AasxPluginOptionsBase
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
                var optText = File.ReadAllText(optfn);

                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(optText, settings);
            }

            // no
            return null;
        }

        public void TryLoadAdditionalOptionsFromAssemblyDir<T>(
            string pluginName, Assembly assy = null,
            JsonSerializerSettings settings = null,
            LogInstance log = null) where T : AasxPluginOptionsBase
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
                    var optText = File.ReadAllText(fn);
                    optText = AdminShellSerializationHelper.FixSerializedVersionedEntities(optText);
                    var opts = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(optText, settings);
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
