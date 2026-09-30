/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-08


using System;

namespace Falcon.Modules.Core.Network
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class FAMessageAttribute: Attribute
    {
        public string EventName { get; }

        public FAMessageAttribute(string eventName)
        {
            EventName = eventName;
        }
        
        public static FAMessageAttribute GetAttribute(Type type)
        {
            if (type == null) return null;
            var attributes = type.GetCustomAttributes(typeof(FAMessageAttribute), false);
            if (attributes.Length > 0)
            {
                return (FAMessageAttribute)attributes[0];
            }
            return null;
        }
        
        public static FAMessageAttribute GetAttribute(FMessage message)
        {
            return GetAttribute(message.GetType());
        }
        
        public static string GetEventName(FMessage message)
        {
            var attribute = GetAttribute(message);
            return attribute != null ? attribute.EventName : string.Empty;
        }
        public static string GetEventName(Type type)
        {
            var attribute = GetAttribute(type);
            return attribute != null ? attribute.EventName : string.Empty;
        }
    }
}