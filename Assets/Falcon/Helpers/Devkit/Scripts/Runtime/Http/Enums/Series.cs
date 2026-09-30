/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.ComponentModel;
using System.Net;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public enum Series
    {
        Informational = 1,
        Successful = 2,
        Redirection = 3,
        ClientError = 4,
        ServerError = 5
    }

    public static class SeriesExtensions
    {
        public static Series ToSeries(this HttpStatusCode code)
        {
            return TryToSeries(code) ?? throw new InvalidEnumArgumentException($"No series found for code {code}");
        }

        public static Series? TryToSeries(this HttpStatusCode code)
        {
            var seriesCode = (int) code / 100;
            if (Enum.IsDefined(typeof(Series), seriesCode)) return (Series)seriesCode;
            return null;
        }
    }
}