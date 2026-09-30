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
    public class GeneralHttpRequest : HttpRequest
    {
        public GeneralHttpRequest(string url, HttpMethod method) : base(url)
        {
            Method = method;
        }

        public HttpMethod Method { get; set; }
        protected override HttpRequestMessage ToRequestMessage()
        {
            var httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Method = Method;
            
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

            if (Method != HttpMethod.Get)
            {
                httpRequestMessage.Content = Body.ToContent();
            }
            return httpRequestMessage;
        }
    }
}