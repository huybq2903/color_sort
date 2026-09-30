/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public abstract class HttpRequest
    {
        private static readonly HttpClient HttpClient = GetHttpClient();

        private IHttpBody _body;

        protected HttpRequest(string url)
        {
            URL = url;
        }

        public string URL { get; set; }

        public Dictionary<string, string> Headers { get; set; } = new();
        public Dictionary<string, string> Params { get; set; } = new();
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

        public IHttpBody Body
        {
            get => _body ??= SimpleBody.Plain("");
            set => _body = value;
        }

        private static HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.ConnectionClose = false;
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            return httpClient;
        }

        public HttpRequest AddParam(string name, string value)
        {
            Params.Add(name, value);
            return this;
        }

        public HttpRequest AddParams(Dictionary<string, string> values)
        {
            Params.AddAll(values);
            return this;
        }

        public HttpRequest AddHeader(string name, string value)
        {
            Headers.Add(name, value);
            return this;
        }

        public HttpRequest AddHeaders(Dictionary<string, string> values)
        {
            Headers.AddAll(values);
            return this;
        }

        public HttpRequest AddAuthorization(string authorization)
        {
            AddHeader("Authorization", authorization);
            return this;
        }

        protected bool ParamsEmpty => Params.Count == 0;

        protected bool HeadersEmpty() => Headers.Count == 0;

        public HttpRequest SetBody(IHttpBody httpBody)
        {
            this._body = httpBody;
            return this;
        }

        public HttpRequest SetJsonBody(object obj)
        {
            _body = SimpleBody.Json(obj);
            return this;
        }

        protected abstract HttpRequestMessage ToRequestMessage();

        public async Task<HttpResponse> Execute(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(Timeout).Token);

            using var request = ToRequestMessage();
            return new HttpResponse(await HttpClient.SendAsync(request, linkedCts.Token));
        }
    }
}