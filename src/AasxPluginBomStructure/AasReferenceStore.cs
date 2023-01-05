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
using AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

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

            public bool ContainsKey(K key) => dict.ContainsKey(key);

            public List<V> this[K key] => dict[key];
        }

        protected MultiValueDictionary<uint, T> dict =
            new MultiValueDictionary<uint, T>();

        protected uint ComputeHashOnReference(AasCore.Aas3_0_RC02.Reference r)
        {
            // access
            if (r == null || r.Keys == null)
                return 0;

            // use memory stream for effcient behaviour
            byte[] dataBytes = null;
            using (var mems = new MemoryStream())
            {
                foreach (var k in r.Keys)
                {
                    // DO NOT include the Type into the hash, as this would render it impossible
                    // to find CDs with either "ConceptDescription" / "GlobalReference"
                    //// var bs = BitConverter.GetBytes((int) k.Type);
                    //// mems.Write(bs, 0, bs.Length);

                    var bs = System.Text.Encoding.UTF8.GetBytes(k.Value.Trim().ToLower());
                    mems.Write(bs, 0, bs.Length);
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

        public void Index(AasCore.Aas3_0_RC02.Reference rf, T elem)
        {
            // access
            if (elem == null || rf == null)
                return;

            // make curr ref and index
            dict.Add(ComputeHashOnReference(rf), elem);
        }

        public T FindElementByReference(
            AasCore.Aas3_0_RC02.Reference r,
            MatchMode matchMode = MatchMode.Strict)
        {
            var hk = ComputeHashOnReference(r);
            if (hk == 0 || !dict.ContainsKey(hk))
                return default(T);

            foreach (var test in dict[hk])
            {
                var xx = (test as AasCore.Aas3_0_RC02.IReferable)?.GetReference();
                if (xx != null && xx.Matches(r, matchMode))
                    return test;
            }

            return default(T);
        }

    }

    public class AasReferableStore : AasReferenceStore<AasCore.Aas3_0_RC02.IReferable>
    {
        private void RecurseIndexSME(AasCore.Aas3_0_RC02.Reference currRef, AasCore.Aas3_0_RC02.ISubmodelElement sme)
        {
            // access
            if (currRef == null || sme == null)
                return;

            // add to the currRef
            currRef.Keys.Add(
                new AasCore.Aas3_0_RC02.Key(
                    sme.GetSelfDescription().KeyType ?? AasCore.Aas3_0_RC02.KeyTypes.GlobalReference, sme.IdShort));

            // index
            var hk = ComputeHashOnReference(currRef);
            dict.Add(hk, sme);

            // recurse
            var childs = sme?.EnumerateChildren();
            if (childs != null)
                foreach (var sme2 in childs)
                    RecurseIndexSME(currRef, sme2);

            // remove from currRef
            currRef.Keys.RemoveAt(currRef.Keys.Count - 1);
        }

        public void Index(ConceptDescription cd)
        {
            // access
            if (cd == null)
                return;

            // make curr ref and index
            var currRef = cd.GetReference();
            dict.Add(ComputeHashOnReference(currRef), cd);
        }

        public void Index(Submodel sm)
        {
            // access
            if (sm == null)
                return;

            // make curr ref and index
            var currRef = sm.GetReference();
            dict.Add(ComputeHashOnReference(currRef), sm);

            // recurse
            foreach (var sme in sm.EnumerateChildren())
                RecurseIndexSME(currRef, sme);
        }

        public void Index(AasCore.Aas3_0_RC02.Environment env)
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
