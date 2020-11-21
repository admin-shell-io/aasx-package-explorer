/*
Copyright (c) 2020 see https://github.com/IdentityServer/IdentityServer4

We adapted the code marginally and removed the parts that we do not use.
*/

// ReSharper disable All .. as this is code from others (adapted from IdentityServer4).

namespace AasxOpenIdClient
{
    public class Constants
    {
        public const string Authority = "https://localhost:5001";
        public const string AuthorityMtls = "https://identityserver.local";

        public const string SampleApi = "https://localhost:5005/";
        public const string SampleApiMtls = "https://api.identityserver.local/";
    }
}
