/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Net.Http;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class SimpleBody : IHttpBody
    {
        public BodyType BodyType { get; set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string Body { get; set; }

        public HttpContent ToContent()
        {
            return new StringContent(Body, Encoding, BodyType.MediaTypeName());
        }

        public static SimpleBody Json(object body)
        {
            return new SimpleBody
            {
                BodyType = BodyType.Json,
                Body = body.ToJson()
            };
        }

        public static SimpleBody Plain(string body)
        {
            return new SimpleBody
            {
                BodyType = BodyType.Text,
                Body = body.ToJson()
            };
        }
    }
}