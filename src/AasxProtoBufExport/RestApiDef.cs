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