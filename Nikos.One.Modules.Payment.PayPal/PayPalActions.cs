using Nikos.One.Runtime;
using Nikos.Toolbelt;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace Nikos.One.Modules
{
    public class PayPalActions
    {
        public static string GetPayPalBaseUrl(bool useSandBox = true)
        {
            return useSandBox ? "https://api.sandbox.paypal.com" : "https://api.paypal.com";
        }

        public static AuthTokenInfo GetAuthenticationToken(ExecutionContext executionContext, string clientId, string clientSecret, bool useSandBox = true)
        {
            return Cache.Get<AuthTokenInfo>(executionContext, "paypal_auth_token", () =>
            {
                var content = Encoding.UTF8.GetBytes("grant_type=client_credentials");
                var request = WebRequest.Create(GetPayPalBaseUrl(useSandBox) + "/v1/oauth2/token");
                request.Method = "POST";
                request.ContentLength = content.Length;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers.Add("Accept-Language", "en_US");
                request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(clientId + ":" + clientSecret));

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(content, 0, content.Length);
                }

                string json;

                using (var response = request.GetResponse())
                using (var r = new StreamReader(response.GetResponseStream()))
                {
                    json = r.ReadToEnd();
                }

                var authInfo = JsonReader.FromJson<AuthTokenInfo>(json);

                // TODO: paypal returns the caching time of the token. Use it for the local cache.
                return authInfo;
            });
        }
    }
}