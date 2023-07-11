/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxIntegrationBase;

namespace WpfMtpControl.DataSources
{
    /// <summary>
    /// This class is expected to be sub-classed by a specufic OPC UA client implementation
    /// </summary>
    public class MtpDataSourceOpcUaServer
    {
        public string Endpoint = "";
        public string User = "";
        public string Password = "";
    }

    /// <summary>
    /// This class is expected to be sub-classed by a specufic OPC UA client implementation
    /// </summary>
    public class MtpDataSourceOpcUaItem
    {
        public enum AccessType { Undefined, ReadOnly, WriteOnly, ReadWrite }

        public string Identifier = "";
        public string Namespace = "";
        public AccessType Access = AccessType.ReadWrite;

        public string MtpSourceItemId = "";
    }

    /// <summary>
    /// This class holds information to manage renaming of endpoints for different MTP instances
    /// </summary>
    public class MtpDataSourceOpcUaEndpointMapping
    {
        public string NewEndpoint = null;
        public string ForName = null;
        public string ForId = null;

        public MtpDataSourceOpcUaEndpointMapping() { }

        public MtpDataSourceOpcUaEndpointMapping(string NewEndpoint, string ForName = null, string ForId = null)
        {
            this.ForName = ForName;
            this.ForId = ForId;
            this.NewEndpoint = NewEndpoint;
        }

        public bool IsValid
        {
            get
            {
                return NewEndpoint.HasContent()
                    && (ForName.HasContent() || ForId.HasContent());
            }
        }
    }

    /// <summary>
    /// This class allows for string replacement of information given on data sources
    /// </summary>
    public class MtpDataSourceStringReplacement
    {
        public string OldText = null;
        public string NewText = null;

        public MtpDataSourceStringReplacement() { }

        public MtpDataSourceStringReplacement(string OldText, string NewText)
        {
            this.OldText = OldText;
            this.NewText = NewText;
        }

        public bool IsValid
        {
            get
            {
                return this.OldText.HasContent() && this.NewText.HasContent();
            }
        }

        public string DoReplacement(string input)
        {
            if (input == null)
                return null;
            if (!this.IsValid)
                return input;
            return Regex.Replace(input, this.OldText, this.NewText);
        }
    }

    /// <summary>
    /// This class holds information, which is to be used prior to load / setup MTP data acqusition.
    /// </summary>
    public class MtpDataSourceOpcUaPreLoadInfo
    {
        public List<MtpDataSourceOpcUaEndpointMapping> EndpointMapping = new List<MtpDataSourceOpcUaEndpointMapping>();
        public List<MtpDataSourceStringReplacement> IdentifierRenaming = new List<MtpDataSourceStringReplacement>();
        public List<MtpDataSourceStringReplacement> NamespaceRenaming = new List<MtpDataSourceStringReplacement>();
    }

    /// <summary>
    /// This interface is expected from every OPC UA client to be implemented
    /// </summary>
    public interface IMtpDataSourceFactoryOpcUa
    {
        MtpDataSourceOpcUaServer CreateOrUseUaServer(string Endpoint, bool allowReUse = false);
        MtpDataSourceOpcUaItem CreateOrUseItem(MtpDataSourceOpcUaServer server,
            string Identifier, string Namespace, string Access, string mtpSourceItemId, bool allowReUse = false);
        void Tick(int ms);
    }

    /// <summary>
    /// Interfaces for a graphical status display of a data source factory
    /// </summary>
    public interface IMtpDataSourceStatus
    {
        string GetStatus();
        void ViewDetails();
    }
}
