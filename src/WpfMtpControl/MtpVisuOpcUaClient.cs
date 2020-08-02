using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfMtpControl.DataSources;

namespace WpfMtpControl
{
    /// <summary>
    /// Simple client, implementing the Interface IMtpDataSourceOpcUaFactory and all necessary details
    /// </summary>
    public class MtpVisuOpcUaClient : IMtpDataSourceFactoryOpcUa, IMtpDataSourceStatus
    {
        public enum ItemChangeType { Value }
        public delegate void ItemChangedDelegate(IMtpDataSourceStatus dataSource, DetailItem itemRef, ItemChangeType changeType);
        public event ItemChangedDelegate ItemChanged = null;

        public ObservableCollection<DetailItem> Items = new ObservableCollection<DetailItem>();

        public class DetailItem : MtpDataSourceOpcUaItem, IEquatable<DetailItem>, INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(propertyName));
            }

            public DetailServer Server = null;

            public Opc.Ua.NodeId nid = null;

            public bool Equals(DetailItem other)
            {
                if (other == null || this.Server == null || other.Server == null)
                    return false;

                return this.Server == other.Server
                    && this.Identifier == other.Identifier
                    && this.Namespace == other.Namespace
                    && this.Access == other.Access;
            }

            public string DisplayEndpoint { get { return "" + this.Server?.Endpoint; } }
            public string DisplayNamespace { get { return "" + this.Namespace; } }
            public string DisplayIdentifier { get { return "" + this.Identifier; } }

            public bool ValueTouched = true;
            public object Value = null;

            private string displayValue = null;
            public string DisplayValue{ get { return displayValue; } set { displayValue = value; OnPropertyChanged("DisplayValue"); } }
        }

        public class DetailServer : DataSources.MtpDataSourceOpcUaServer, IEquatable<DetailServer>
        {
            MtpVisuOpcUaClient ParentRef = null;

            public int msToNextState = 3000;
            public int state = 0;

            public List<DetailItem> ItemRefs = new List<DetailItem>();

            public Dictionary<Opc.Ua.NodeId, DetailItem> nodeIdToItemRef = new Dictionary<Opc.Ua.NodeId, DetailItem>();

            public AasOpcUaClient uaClient = null;

            public DetailServer(MtpVisuOpcUaClient parentRef)
            {
                this.ParentRef = parentRef;
            }

            public void Tick(int ms)
            {
                // next state?
                msToNextState -= ms;
                if (msToNextState < 0)
                {
                    msToNextState = 3000;
                    var nextState = -1;

                    if (state == 0)
                    {
                        // try to initialize OPC UA server
                        this.uaClient = new AasOpcUaClient(this.Endpoint, _autoAccept: true, _stopTimeout: 9999, _userName: this.User, _password: this.Password);
                        this.uaClient.Run();
                        // go on for a checking state
                        nextState = 1;
                    }

                    if (state == 1)
                    {
                        // stable connection
                        if (this.uaClient != null && this.uaClient.StatusCode == AasOpcUaClientStatus.Running)
                        {
                            // add subscriptions for all nodes
                            var nids = new List<Opc.Ua.NodeId>();
                            foreach (var ir in this.ItemRefs)
                            {
                                ir.DisplayValue = null;
                                try
                                {
                                    // make node id?
                                    ir.nid = this.uaClient.CreateNodeId(ir.Identifier, ir.Namespace);
                                    if (ir.nid == null)
                                        continue;

                                    // inital read possible?
                                    var dv = this.uaClient.ReadNodeId(ir.nid);
                                    ir.DisplayValue = "" + dv.Value;
                                    ir.Value = dv.Value;

                                    // send event
                                    if (this.ParentRef?.ItemChanged != null)
                                        this.ParentRef.ItemChanged.Invoke(this.ParentRef, ir, ItemChangeType.Value);

                                    // try subscribe!
                                    nids.Add(ir.nid);
                                    this.nodeIdToItemRef.Add(ir.nid, ir);
                                }
                                catch { }
                            }

                            // try add these
                            this.uaClient.SubscribeNodeIds(nids.ToArray(), OnNotification, publishingInteral: 200);

                            // go on
                            nextState = 2;
                        }
                    }

                    // move?
                    if (nextState >= 0)
                        this.state = nextState;
                }                
            }

            private void OnNotification(Opc.Ua.Client.MonitoredItem item, Opc.Ua.Client.MonitoredItemNotificationEventArgs e)
            {
                foreach (var value in item.DequeueValues())
                {
                    // Console.WriteLine("{0}: {1}, {2}, {3}", item.DisplayName, value.Value, value.SourceTimestamp, value.StatusCode);
                    if (this.nodeIdToItemRef != null && this.nodeIdToItemRef.ContainsKey(item.StartNodeId))
                    {
                        // get item ref
                        var ir = this.nodeIdToItemRef[item.StartNodeId];

                        // change data (and notify ObservableCollection)
                        ir.DisplayValue = "" + value.Value;
                        ir.Value = value.Value;
                        ir.ValueTouched = true;

                        // business logic event
                        if (this.ParentRef?.ItemChanged != null)
                            this.ParentRef.ItemChanged.Invoke(this.ParentRef, ir, ItemChangeType.Value);
                    }
                }
            }

            public bool Equals(DetailServer other)
            {
                if (other == null)
                    return false;

                return this.Endpoint == other.Endpoint;
            }
        }

        private List<DetailServer> servers = new List<DetailServer>();

        //
        // Interface: IMtpDataSourceFactoryOpcUa
        //

        public MtpDataSourceOpcUaServer CreateOrUseUaServer(string Endpoint, bool allowReUse = false)
        {
            if (Endpoint == null)
                return null;

            // make one
            var s = new DetailServer(this);
            s.Endpoint = Endpoint;

            // or try re-use?
            var doAdd = true;
            if (allowReUse && servers.Contains(s))
            {
                s = servers.Find(x => x.Equals(s));
                doAdd = false;
            }

            // add?
            if (doAdd)
                servers.Add(s);
            return s;
        }

        public MtpDataSourceOpcUaItem CreateOrUseItem(
            MtpDataSourceOpcUaServer server, 
            string Identifier, string Namespace, string Access, string mtpSourceItemId,
            bool allowReUse = false)
        {
            // need server
            if (!servers.Contains(server) || Identifier == null || Namespace == null || Access == null)
                return null;
            var ds = (server as DetailServer);

            // TODO: remove HACK
            if (!Identifier.Contains(".L001."))
                return null;

            // directly use server
            var i = new DetailItem();
            i.Server = ds;
            i.Identifier = Identifier;
            i.Namespace = Namespace;
            i.Access = MtpDataSourceOpcUaItem.AccessType.ReadWrite;
            i.MtpSourceItemId = mtpSourceItemId;

            // try re-use?
            var doAdd = true;
            if (allowReUse && this.Items.Contains(i))
            {
                foreach (var it in this.Items)
                    if (it.Equals(i))
                    {
                        i = it;
                        break;
                    }
                doAdd = false;
            }

            // add?
            if (doAdd)
            {
                this.Items.Add(i);
                ds.ItemRefs.Add(i);
            }
            return i;
        }

        public void Tick(int ms)
        {
            foreach (var s in this.servers)
                s.Tick(ms);
        }

        //
        // Interface IMtpDataSourceStatus
        //

        public string GetStatus()
        {
            var numit = this.Items.Count;
            var servinfo = "";
            var i = 0;
            foreach (var s in this.servers)
            {
                // numit += s.Items.Count;
                servinfo += $"({i} {s.state},{s.msToNextState} ms) ";
                i++;
            }
            return $"{this.servers.Count} servers, {numit} items";
        }

        public void ViewDetails()
        {
            ;
        }

    }
}
