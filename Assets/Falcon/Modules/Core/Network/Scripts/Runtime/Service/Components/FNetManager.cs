/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Falcon;
using Falcon.Helpers.FReflection;
using UnityEngine;

namespace Falcon.Modules.Core.Network
{
    public class FNetManager
    {
        private FNetManager()
        {
        }

        private static FNetManager _instance = null;
        private string uri;
        public bool Connected { get; internal set; }

        public static FNetManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FNetManager();
                    _instance.Init();
                }

                return _instance;
            }
        }

        private IMessageCompressor _compressor;
        private FSession fSession;

        private Dictionary<string, Type> scEvt2Type = new Dictionary<string, Type>();
        private Dictionary<string, Type> csEvt2Type = new Dictionary<string, Type>();
        private Dictionary<Type, string> scClass2EventName = new Dictionary<Type, string>();
        private List<ISessionListener> listeners = new List<ISessionListener>();
        private Dictionary<Type, List<ISCMessageListener>> dicScMessage2Listeners = new Dictionary<Type, List<ISCMessageListener>>();
        private void Init()
        {
            _compressor = new GzipCompressor();

            IEnumerable<Type> scClasses = FReflection.Instance.GetTypes().Where(t => t.IsSubclassOf(typeof(SCMessage)));
            IEnumerable<Type> csClasses = FReflection.Instance.GetTypes().Where(t => t.IsSubclassOf(typeof(CSMessage)));
            IEnumerable<Type> listenerClasses = FReflection.Instance.GetTypes()
                .Where(t => typeof(ISessionListener).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            IEnumerable<Type> scListenerTypes = FReflection.Instance.GetTypes()
                .Where(t => typeof(ISCMessageListener).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            // instantiate each listener type once and register under each message T it handles
            foreach (var listenerType in scListenerTypes)
            {
                try
                {
                    ISCMessageListener instance = (ISCMessageListener)Activator.CreateInstance(listenerType);
                    
                    // Tìm tham số generic T từ các interfaces mà listener implement
                    foreach (var iface in listenerType.GetInterfaces())
                    {
                        if (iface.IsGenericType)
                        {
                            var genericTypeDef = iface.GetGenericTypeDefinition();
                            var fullName = genericTypeDef.FullName ?? "";
                            
                            // Kiểm tra xem có phải ISCMessageListener<> hoặc ISCMesageListener<> không (do có lỗi chính tả trong tên)
                            if (fullName.Contains("ISCMessageListener`1") || fullName.Contains("ISCMesageListener`1"))
                            {
                                var scMessageType = iface.GetGenericArguments()[0];
                                
                                if (!dicScMessage2Listeners.ContainsKey(scMessageType))
                                {
                                    dicScMessage2Listeners[scMessageType] = new List<ISCMessageListener>();
                                }
                                
                                dicScMessage2Listeners[scMessageType].Add(instance);
                            }
                        }
                    }
                }
                catch
                {
                    // skip types that cannot be instantiated
                    continue;
                }

            }
            
            foreach (var type in scClasses)
            {
                scEvt2Type[FAMessageAttribute.GetEventName(type)] = type;
                scClass2EventName[type] = FAMessageAttribute.GetEventName(type);
            }

            foreach (var type in csClasses)
            {
                csEvt2Type[FAMessageAttribute.GetEventName(type)] = type;
            }

            foreach (var type in listenerClasses)
            {
                ISessionListener listener = (ISessionListener)Activator.CreateInstance(type);
                listeners.Add(listener);
            }
        }

        internal List<ISCMessageListener> GetSCMessageListeners(Type scType)
        {
            if (dicScMessage2Listeners.ContainsKey(scType))
                return dicScMessage2Listeners[scType];
            else
                return null;
        }


        public void AddSCType(Type scType)
        {
            fSession.GetChannel().AddSCType(scType);
            scEvt2Type[FAMessageAttribute.GetEventName(scType)] = scType;
            scClass2EventName[scType] = FAMessageAttribute.GetEventName(scType);
        }
        public void SetCompressor(IMessageCompressor compressor)
        {
            _compressor = compressor;
        }

        internal IMessageCompressor GetCompressor()
        {
            return _compressor;
        }

        internal string GetSCEventName(Type type)
        {
            return scClass2EventName[type];
        }

        internal List<ISessionListener> GetListeners()
        {
            return listeners;
        }

        public delegate void SessionStarted();

        private event SessionStarted OnSessionStartedEvent;

        public void OnSessionStarted(SessionStarted callback)
        {
            OnSessionStartedEvent += callback;
        }

        internal void OnSessionStartedCallback()
        {
            OnSessionStartedEvent?.Invoke();
        }

        internal IEnumerable<Type> GetSCClasses()
        {
            return scEvt2Type.Values;
        }

        internal Type getSCMessageType(string evt)
        {
            return scEvt2Type[evt];
        }

        internal Type getCSMessageType(string evt)
        {
            return csEvt2Type[evt];
        }

        public void Start(string uri)
        {
            this.uri = uri;
            FChannel fChannel = new FChannel(uri);
            fSession = new FSession(fChannel);
            fSession.Start();
        }

        public FSession GetSession()
        {
            return fSession;
        }

        public void Restart()
        {
            fSession.Restart();
        }
    }
}