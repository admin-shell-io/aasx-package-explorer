/*
Copyright (c) 2020 TU Dresden Institute of Applied Computer Science <https://tu-dresden.de/inf/pk>
Author: Nico Braunisch <nico.braunisch@tu-dresden.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
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