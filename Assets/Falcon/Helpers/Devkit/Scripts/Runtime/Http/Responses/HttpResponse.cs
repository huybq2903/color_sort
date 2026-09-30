/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class HttpResponse
    {
        private readonly HttpResponseMessage _responseMessage;

        public HttpResponse(HttpResponseMessage responseMessage)
        {
            this._responseMessage = responseMessage;
        }

        public int StatusCode => (int) _responseMessage.StatusCode;

        public HttpStatusCode Status => _responseMessage.StatusCode;

        public bool IsSuccess => Status.Is2xxSuccessful();

        public async Task<string> StrBody()
        {
            try
            {
                return await _responseMessage.Content.ReadAsStringAsync();
            }
            finally
            {
                Close();
            }
        }
        
        public async Task<byte[]> BytesBody()
        {
            try
            {
                return await _responseMessage.Content.ReadAsByteArrayAsync();
            }
            finally
            {
                Close();
            }
        }
        
        public async Task<Stream> StreamBody()
        {
            return new HttpResponseStreamWrapper(_responseMessage, await _responseMessage.Content.ReadAsStreamAsync());
        }
        
        public Task<string> SuccessStrBody()
        {
            EnsureSuccess();
            return StrBody();
        }
        
        public Task<byte[]> SuccessBytesBody()
        {
            EnsureSuccess();
            return BytesBody();
        }
        
        public Task<Stream> SuccessStreamBody()
        {
            EnsureSuccess();
            return StreamBody();
        }
        
        public async Task<T> SuccessObj<T>()
        {
            EnsureSuccess();
            return (await StrBody()).JsonToObj<T>();
        }

        private void EnsureSuccess()
        {
            if (!Status.Is2xxSuccessful())
            {
                throw new HttpRequestException(Status + ":" + StrBody().GetAwaiter().GetResult());
            }
        }

        public void Close()
        {
            _responseMessage.Dispose();
        }
    }
}