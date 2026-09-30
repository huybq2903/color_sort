/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Bcpg;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using Best.SocketIO;
using Best.SocketIO.Events;
using Best.SocketIO.Parsers;
using Best.SocketIO.Transports;

namespace Falcon.Modules.Core.Network
{
    public class FChannel : IChannel
    {
        private SocketManager socket;
        private string uri;
        private FSession fSession;
        protected string serverIp;

        public FChannel(string uri)
        {
            this.uri = uri;

            Uri uRI = new Uri(uri);

            string hostname = uRI.Host;

            if (hostname == "localhost")
            {
                this.serverIp = "127.0.0.1";
            }
            else
            {
                try
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(hostname);
                    this.serverIp = addresses[0].ToString();
                }catch (SocketException ex)
                {
                    LogUtil.Error($"DNS resolution failed: {ex.Message}");
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Unexpected error: {ex.Message}");
                }
            }
        }

        internal void SetSession(FSession fSession)
        {
            this.fSession = fSession;
        }

        public string GetServerIp()
        {
            return serverIp;
        }

        public void Start()
        {
            Debug.Log("FChannelX start");
            SocketOptions options = new SocketOptions();
            options.Timeout = TimeSpan.FromMilliseconds(300000);;
            options.ConnectWith = TransportTypes.WebSocket;
            options.AutoConnect = false;
            socket = new SocketManager(new Uri(uri), options);
            socket.Parser = new JsonDotNetParser();

            socket.Socket.On<ConnectResponse>("connect", OnConnected);
            socket.Socket.On<Error>(SocketIOEventTypes.Error, OnError);
            socket.Socket.On("disconnect", OnDisconnected);
            socket.Socket.On("reconnecting", OnReconnecting);
            socket.Socket.On("reconnect", OnReconnected);

            foreach (var type in FNetManager.Instance.GetSCClasses())
            {
                if (type.IsSubclassOf(typeof(SCMessage)))
                {
                    if (type.IsSubclassOf(typeof(ISCBinary)))
                    {
                        int a = 0;
                    }
                    socket.Socket.On<String>(FAMessageAttribute.GetEventName(type), (String jsonData) =>
                    {
                        SCMessage message = (SCMessage)JsonConvert.DeserializeObject(jsonData, type);
                        this.fSession.OnMessage(message);
                    });

                    socket.Socket.On<string>("fbi_" + FAMessageAttribute.GetEventName(type), (string args) =>
                    {
                            if (args is string base64)
                            {
                                byte[] bytes = Convert.FromBase64String(base64);
                                ISCBinary message = Activator.CreateInstance(type) as ISCBinary;
                                message.FromBytes(bytes);
                                this.fSession.OnMessage((SCMessage)message);
                            }
                    });
                }
            }

            socket.Open();
        }

        private void OnError(Error error)
        {
            Debug.Log("FChannel error " + error.message);
        }

        internal void AddSCType(Type scType)
        {
            if (!scType.IsSubclassOf(typeof(SCMessage)))
                return;
            socket.Socket.On<String>(FAMessageAttribute.GetEventName(scType), (String jsonData) =>
            {
                JArray array = JArray.Parse(jsonData);
                var json = array[1].ToString();

                SCMessage message = (SCMessage)JsonConvert.DeserializeObject(json, scType);
                this.fSession.OnMessage(message);
            });
        }

        public void Send(FMessage message)
        {
            if (!(message is ICSBinary))
                socket.Socket.Emit(FAMessageAttribute.GetEventName(message), message);
            else
            {
                socket.Socket.Emit("fbi_" + FAMessageAttribute.GetEventName(message), 
                    new object[]{((ICSBinary)message).ToBytes()});
            }
        }

        private long lastTimeConnected = 0;
        void OnConnected(ConnectResponse arg)
        {
            if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastTimeConnected > 1000)
            {
                lastTimeConnected = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                Debug.Log("FChannelX connected " + this.GetHashCode());
                this.fSession.OnChannelConnected();
            }
        }

        void OnDisconnected()
        {
            Debug.Log("FChannelX disconnected");
            this.fSession.OnChannelDisconnected();
        }

        void OnReconnected()
        {
            Debug.Log("FChannelX reconnected");
            this.fSession.OnChannelReconnected();
        }

        void OnReconnecting()
        {
            Debug.Log("FChannelX reconnecting");
            this.fSession.OnChannelReconnecting();
        }

        public void Close()
        {
            socket.Close();
        }

        public void Restart()
        {
            socket.Close();
            Start();       
        }
        
        /// <summary>
        /// Close socket and disable automatic reconnection permanently until Start() is called again.
        /// </summary>
        public void ClosePermanently()
        {
            if (socket != null)
            {
                // Try to disable reconnection option if available on the SocketManager/Options
                try
                {
                    var optsProp = socket.GetType().GetProperty("Options");
                    var opts = optsProp?.GetValue(socket);
                    var reconProp = opts?.GetType().GetProperty("Reconnection");
                    if (reconProp != null)
                    {
                        reconProp.SetValue(opts, false);
                    }
                }
                catch
                {
                    // ignore reflection failures
                }

                try
                {
                    socket.Close();
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error closing socket permanently: {ex.Message}");
                }
            }
        }
    }
}

