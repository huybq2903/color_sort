/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using System.Net;
using System.Net.Http;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.CDN
{
    [Serializable]
    public class ApiResponse<T>
    {
        public T body;
        public HttpStatusCode code;
        public string message;
        public string status;

        public T GetBodyEnsureSuccess()
        {
            return code.Is2xxSuccessful() ? body : throw new HttpRequestException("Error getting CDN file: " + message);
        }
    }
}