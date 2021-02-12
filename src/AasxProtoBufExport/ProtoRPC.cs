namespace AasxProtoBufExport
{

    public class ProtoRPC
    {
        private string name;
        private ProtoMessage inputMsg;
        private ProtoMessage outputMsg;
        private RestApiDef rest;

        public ProtoRPC(string name, ProtoMessage inputMsg, ProtoMessage outputMsg, RestApiDef rest)
        {
            this.name = name;
            this.inputMsg = inputMsg;
            this.outputMsg = outputMsg;
            this.rest = rest;
        }

        public ProtoRPC()
        {
            throw new System.NotImplementedException();
        }
    }
}