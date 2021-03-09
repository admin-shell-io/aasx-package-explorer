/*
Copyright (c) 2020 TU Dresden Institute of Applied Computer Science <https://tu-dresden.de/inf/pk>
Author: Nico Braunisch <nico.braunisch@tu-dresden.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
namespace AasxProtoBufExport
{

    public class RestApiDef
    {
        private string url;
        private string httpRequestMethod;

        public RestApiDef(string url, string httpRequestMethod)
        {
            this.url = url;
            this.httpRequestMethod = httpRequestMethod;
        }
    }
}