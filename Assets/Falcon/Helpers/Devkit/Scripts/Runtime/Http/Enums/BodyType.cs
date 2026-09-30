/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public enum BodyType
    {
        Form,
        XWwwForm,
        Text,
        Json,
        JavaScript,
        Html,
        XML,
        Binary
    }

    public static class BodyTypeExtensions
    {
        public static string MediaTypeName(this BodyType bodyType)
        {
            return bodyType switch
            {
                BodyType.Form => "multipart/form-data",
                BodyType.XWwwForm => "application/x-www-form-urlencoded",
                BodyType.Text => "text/plain",
                BodyType.Json => "application/json",
                BodyType.JavaScript => "application/javascript",
                BodyType.Html => "text/html",
                BodyType.XML => "text/xml",
                BodyType.Binary => "application/x-msdownload",
                _ => "application/x-msdownload"
            };
        }
    }
}