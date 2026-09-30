/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Net.Http;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface IHttpBody
    {
        HttpContent ToContent();
    }
}