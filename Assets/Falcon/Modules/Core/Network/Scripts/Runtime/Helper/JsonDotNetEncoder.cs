/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
#if !BESTHTTP_DISABLE_SOCKETIO

using System;
using System.Collections.Generic;
using Best.SocketIO.Transports;
using Newtonsoft.Json;

namespace Falcon.Modules.Core.Network
{
    public sealed class JsonDotNetEncoder
    {
        private Formatting formatting = Formatting.None;

        public JsonDotNetEncoder()
        {
            
        }

        public JsonDotNetEncoder(Formatting formatting)
        {
            this.formatting = formatting;
        }
        
        public List<object> Decode(string json)
        {
            return JsonConvert.DeserializeObject<List<object>>(json);
        }

        
        public object Decode(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type);
        }
        
        public string Encode(List<object> obj)
        {
            return JsonConvert.SerializeObject(obj, formatting);
        }
    }
}

#endif