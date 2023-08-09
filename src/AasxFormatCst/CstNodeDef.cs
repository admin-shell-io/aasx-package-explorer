/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// see: https://json2csharp.com/

// ReSharper disable ClassNeverInstantiated.Global

namespace AasxFormatCst
{
    public class CstNodeDef
    {
        //// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 

        public class NodeDefinition : CstIdObjectBase
        {
            public NodeDefinition Parent, ApplicationClass;

            public NodeDefinition() : base() { }

            public NodeDefinition(CstIdObjectBase id)
                : this()
            {
                if (id == null)
                    return;

                Namespace = id.Namespace;
                ID = id.ID;
                Revision = id.Revision;
                Name = id.Name;
                MinorRevision = id.MinorRevision;
                Status = id.Status;
            }
        }

        public class Root : CstRootBase
        {
            public string SchemaVersion = "1.1.0";
            public string Locale = "en_US";
            public List<NodeDefinition> NodeDefinitions = new List<NodeDefinition>();

            public void Add(NodeDefinition nd)
            {
                if (NodeDefinitions != null)
                    NodeDefinitions.Add(nd);
            }
        }

    }
}
