﻿using AasxUtils;
using AdminShellNS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginBomStructure
{
    public class AasReferenceStore
    {
        private MultiTupleDictionary<uint, AdminShell.Referable> dict = new MultiTupleDictionary<uint, AdminShellV20.Referable>();

        private static System.Security.Cryptography.SHA256 HashProvider = System.Security.Cryptography.SHA256.Create();

        private uint ComputeHashOnReference(AdminShell.Reference r)
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
                    var bs = System.Text.Encoding.UTF8.GetBytes(k.type.Trim().ToLower());
                    mems.Write(bs, 0, bs.Length);

                    bs = System.Text.Encoding.UTF8.GetBytes(k.idType.Trim().ToLower());
                    mems.Write(bs, 0, bs.Length);

                    bs = System.Text.Encoding.UTF8.GetBytes(k.value.Trim().ToLower());
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

        private void RecurseIndexSME(AdminShell.Reference currRef, AdminShell.SubmodelElement sme)
        {
            // access 
            if (currRef == null || sme == null)
                return;

            // add to the currRef
            currRef.Keys.Add(new AdminShell.Key(sme.GetElementName(), false, AdminShell.Identification.IdShort, sme.idShort));

            // index
            dict.Add(ComputeHashOnReference(currRef), sme);

            // recurse
            var childs = (sme as AdminShell.IEnumerateChildren)?.EnumerateChildren();
            if (childs != null)
                foreach (var sme2 in childs)
                    RecurseIndexSME(currRef, sme2?.submodelElement);

            // remove from currRef
            currRef.Keys.RemoveAt(currRef.Keys.Count - 1);
        }

        public void Index(AdminShell.Submodel sm)
        {
            // access
            if (sm == null)
                return;

            // make curr ref and index
            var currRef = sm.GetReference();
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
        }

        public AdminShell.Referable FindReferableByReference(AdminShell.Reference r)
        {
            var hk = ComputeHashOnReference(r);
            if (hk == 0 || !dict.ContainsKey(hk))
                return null;

            foreach (var test in dict[hk])
            {
                var xx = (test as AdminShell.IGetReference)?.GetReference();
                if (xx != null && xx.Matches(r))
                    return test;
            }

            return null;
        }

    }
}
