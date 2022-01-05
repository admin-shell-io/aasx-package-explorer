/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPluginBomStructure
{
    public class AasReferenceStore<T>
    {
        protected class MultiValueDictionary<K, V>
        {
            private Dictionary<K, List<V>> dict = new Dictionary<K, List<V>>();
            public void Add(K key, V value)
            {
                if (dict.TryGetValue(key, out var list))
                    list.Add(value);
                else
                    dict.Add(key, new List<V> { value });
            }

            public void Clear() => dict.Clear();

            public bool ContainsKey(K key) => dict.ContainsKey(key);

            public List<V> this[K key] => dict[key];
        }

        protected MultiValueDictionary<uint, T> dict =
            new MultiValueDictionary<uint, T>();

        protected uint ComputeHashOnReference(AdminShell.Reference r)
        {
            // use memory stream for effcient behaviour
            byte[] dataBytes = null;
            using (var mems = new MemoryStream())
            {
                if (r is AdminShell.ModelReference modrf)
                {
                    // access
                    if (r == null || modrf.Keys == null)
                        return 0;

                    foreach (var k in modrf.Keys)
                    {
                        var bs = System.Text.Encoding.UTF8.GetBytes(k.type.Trim().ToLower());
                        mems.Write(bs, 0, bs.Length);

                        bs = System.Text.Encoding.UTF8.GetBytes(k.value.Trim().ToLower());
                        mems.Write(bs, 0, bs.Length);
                    }
                }

                if (r is AdminShell.GlobalReference glbrf)
                {
                    // access
                    if (r == null || glbrf.Value == null)
                        return 0;

                    foreach (var v in glbrf.Value)
                    {
                        var bs = System.Text.Encoding.UTF8.GetBytes(v.value.Trim().ToLower());
                        mems.Write(bs, 0, bs.Length);
                    }
                }

                dataBytes = mems.ToArray();
            }

            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode
            if (dataBytes == null)
                return 0;
            // ReSharper enable ConditionIsAlwaysTrueOrFalse
            // ReSharper enable HeuristicUnreachableCode

            uint sum = 0;
            foreach (var b in dataBytes)
                sum += b;
            return sum;
        }

        public void Clear() => dict.Clear();

        public void Index(AdminShell.Reference rf, T elem)
        {
            // access
            if (elem == null || rf == null)
                return;

            // make curr ref and index
            dict.Add(ComputeHashOnReference(rf), elem);
        }

        public T FindElementByReference(
            AdminShell.Reference r,
            AdminShell.Key.MatchMode matchMode = AdminShell.Key.MatchMode.Relaxed)
        {
            var hk = ComputeHashOnReference(r);
            if (hk == 0 || !dict.ContainsKey(hk))
                return default(T);

            foreach (var test in dict[hk])
            {
                var xx = (test as AdminShell.IGetModelReference)?.GetModelReference();
                if (r is AdminShell.ModelReference modrf 
                    && xx != null && xx.Matches(modrf, matchMode))
                    return test;
                if (r is AdminShell.GlobalReference glbrf)
                    // TODO: MAKE THIS MORE PRECISE
                    return test;
            }

            return default(T);
        }

    }

    public class AasReferableStore : AasReferenceStore<AdminShell.Referable>
    {
        private void RecurseIndexSME(AdminShell.ModelReference currRef, AdminShell.SubmodelElement sme)
        {
            // access
            if (currRef == null || sme == null)
                return;

            // add to the currRef
            currRef.Keys.Add(
                new AdminShell.Key(
                    sme.GetElementName(), sme.idShort));

            // index
            var hk = ComputeHashOnReference(currRef);
            dict.Add(hk, sme);

            // recurse
            var childs = (sme as AdminShell.IEnumerateChildren)?.EnumerateChildren();
            if (childs != null)
                foreach (var sme2 in childs)
                    RecurseIndexSME(currRef, sme2?.submodelElement);

            // remove from currRef
            currRef.Keys.RemoveAt(currRef.Keys.Count - 1);
        }

        public void Index(AdminShell.ConceptDescription cd)
        {
            // access
            if (cd == null)
                return;

            // make curr ref and index
            var currRef = cd.GetModelReference();
            dict.Add(ComputeHashOnReference(currRef), cd);
        }

        public void Index(AdminShell.Submodel sm)
        {
            // access
            if (sm == null)
                return;

            // make curr ref and index
            var currRef = sm.GetModelReference();
            dict.Add(ComputeHashOnReference(currRef), sm);

            // recurse
            foreach (var sme in sm.EnumerateChildren())
                RecurseIndexSME(currRef, sme?.submodelElement);
        }

        public void Index(AdminShell.AdministrationShellEnv env)
        {
            // access
            if (env == null || env.Submodels == null)
                return;

            // iterate
            foreach (var sm in env.Submodels)
                this.Index(sm);

            // Concept Descriptions
            foreach (var cd in env.ConceptDescriptions)
                this.Index(cd);
        }
    }
}
