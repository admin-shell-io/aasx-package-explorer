using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public enum AccessType { Undefined, ReadOnly, WriteOnly, ReadWrite}

        public string Identifier = "";
        public string Namespace = "";
        public AccessType Access = AccessType.ReadWrite;

        public string MtpSourceItemId = "";
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
