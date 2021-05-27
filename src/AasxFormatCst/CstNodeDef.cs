﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// see: https://json2csharp.com/

namespace AasxFormatCst
{
    public class CstNodeDef
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 

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

            public void Add (NodeDefinition nd)
            {
                if (NodeDefinitions != null)
                    NodeDefinitions.Add(nd);
            }
        }

    }
}