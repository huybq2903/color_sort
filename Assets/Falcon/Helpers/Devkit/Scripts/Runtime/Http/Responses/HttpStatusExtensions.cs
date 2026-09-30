/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Diagnostics.CodeAnalysis;
using System.Net;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class HttpStatusExtensions
    {
        public static bool Is1XxInformational(this HttpStatusCode statusCode) => statusCode.ToSeries() == Series.Informational;
        public static bool Is2xxSuccessful(this HttpStatusCode statusCode) => statusCode.ToSeries() == Series.Successful;
        public static bool Is3xxRedirection(this HttpStatusCode statusCode) => statusCode.ToSeries() == Series.Redirection;
        public static bool Is4xxClientError(this HttpStatusCode statusCode) => statusCode.ToSeries() == Series.ClientError;
        public static bool Is5xxServerError(this HttpStatusCode statusCode) => statusCode.ToSeries() == Series.ServerError;

        public static bool IsError(this HttpStatusCode statusCode) =>
            statusCode.Is4xxClientError() || statusCode.Is5xxServerError();
    }
}