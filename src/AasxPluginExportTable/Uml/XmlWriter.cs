/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPluginExportTable.Uml
{
    /// <summary>
    /// Base class for the XMI writers.
    /// Very basic utilities to create XML via XmlDocument
    /// </summary>
    public class XmlWriter : BaseWriter
    {
        public XmlDocument Doc;

        private Dictionary<string, string> _namespaceKeyToUri = new Dictionary<string, string>();

        public void AddNamespace(string ns, string nsuri)
        {
            _namespaceKeyToUri.Add(ns, nsuri);
        }

        private void SetAttributes(
            XmlElement node,
            string[] attrs = null)
        {
            if (attrs == null || attrs.Length < 1 || attrs.Length % 2 == 1)
                return;

            for (int i = 0; i < attrs.Length / 2; i++)
            {
                var aname = attrs[2 * i + 0];

                var p = aname.IndexOf(':');
                if (p >= 0)
                {
                    // split
                    var anskey = aname.Substring(0, p);
                    aname = aname.Substring(p).TrimStart(' ', ':');

                    // search
                    if (!_namespaceKeyToUri.ContainsKey(anskey))
                        continue;

                    var ansuri = _namespaceKeyToUri[anskey];

                    node.SetAttribute(aname, ansuri, attrs[2 * i + 1]);
                }
                else
                {
                    node.SetAttribute(aname, attrs[2 * i + 1]);
                }
            }

        }

        public XmlElement CreateAppendElement(
            XmlElement root, string name,
            string[] attrs = null)
        {
            var node = Doc.CreateElement(name);

            SetAttributes(node, attrs);

            root.AppendChild(node);

            return node;
        }

        public XmlElement CreateAppendElement(XmlElement root,
                string prefix, string localName, string namespaceURI,
                string[] attrs = null,
                XmlElement child = null)
        {
            var node = Doc.CreateElement(prefix, localName, namespaceURI);

            SetAttributes(node, attrs);

            if (child != null)
                node.AppendChild(child);

            if (root != null)
                root.AppendChild(node);

            // always return the DEEPEST child!
            if (child != null)
                return child;
            else
                return node;
        }

        public XmlElement CreateAppendElement(XmlElement root,
            Tuple<string, string> ns, string localName,
            string[] attrs = null,
            XmlElement child = null)
        {
            return CreateAppendElement(root, ns.Item1, localName, ns.Item2, attrs, child);
        }
    }
}
