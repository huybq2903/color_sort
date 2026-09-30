/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-08


using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace Falcon.Modules.Core.Network
{
    public class GzipCompressor : IMessageCompressor
    {
        public CSCompressedMessage Compress(CSMessage message)
        {
            string json = JsonConvert.SerializeObject(message);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress))
                {
                    gzip.Write(jsonBytes, 0, jsonBytes.Length);
                }
                string data = Convert.ToBase64String(output.ToArray());
                return new CSCompressedMessage(FAMessageAttribute.GetEventName(message), data);
            }
        }

        public SCMessage Decompress(SCCompressedMessage compressedMessage)
        {
            String compressedMessageBase64 = compressedMessage.sc_data;
            String eventName = compressedMessage.sc_event;
            Type type = FNetManager.Instance.getSCMessageType(eventName);

            byte[] compressedBytes = Convert.FromBase64String(compressedMessageBase64);

            using (var input = new MemoryStream(compressedBytes))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzip, Encoding.UTF8))
            {
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject(json, type) as SCMessage;
            }
        }
    }
}