/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Net.Http;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class GetRequest : HttpRequest
    {
        public GetRequest(string url) : base(url)
        {
        }

        protected override HttpRequestMessage ToRequestMessage()
        {
            var httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Method = HttpMethod.Get;
            
            if (!ParamsEmpty)
            {
                var uri = new UriBuilder(URL);
                var query = new FormUrlEncodedContent(Params).ReadAsStringAsync().Result;
                uri.Query = query;
                httpRequestMessage.RequestUri = uri.Uri;
            }
            else
            {
                httpRequestMessage.RequestUri = new Uri(URL);
            }
            foreach (var (key, value) in Headers)
            {
                httpRequestMessage.Headers.Add(key, value);
            }
            return httpRequestMessage;
        }
    }
}