﻿namespace Nikos.One.Modules
{
    public class AuthTokenInfo
    {
        public string scope { get; set; }

        public string access_token { get; set; }

        public string token_type { get; set; }

        public string app_id { get; set; }

        public int expires_in { get; set; }
    }
}