/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using Falcon;
using Falcon.Helpers.FReflection;
using UnityEngine;

namespace Falcon.Modules.Core.Network
{
    public class FSession : ISession
    {
        public static string SESSION_ID = UUIDUtil.UUID16();
        public static int TIMEOUT = 300000;
        private UDPChannel udpChannel = null;
        private FChannel fChannel;
        
        private string sessionId;
        public long maxCSSequence;
        public long maxSCSequence;
        private bool forceDisconnected = false;
        int sessionNumber = 0;
        private long lastMessageTime = -1;

        public FSession(FChannel fChannel)
        {
            this.fChannel = fChannel;
            this.fChannel.SetSession(this);
            this.sessionId = SESSION_ID;
            this.maxCSSequence = 0;
            this.maxSCSequence = 0;
        }
        
        public FChannel GetChannel()
        {
            return fChannel;
        }

        public string GetSessionId()
        {
            return this.sessionId;
        }

        public void OnMessage(FMessage message)
        {
            FNetManager.Instance.Connected = true;
            this.lastMessageTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!(message is SCCompressedMessage))
                LogUtil.Log(message);

            if (message is SCRestartSession)
            {
                RestartSession();
                return;
            }
            if (message is SCInitSession)
            {
                SCInitSession sCInitSession = (SCInitSession)message;

                bool isFirstInitUdp = sCInitSession.udpPort > 0 && udpChannel == null;
                if (isFirstInitUdp)
                {
                    udpChannel = new UDPChannel(this.fChannel.GetServerIp(), sCInitSession.udpPort);
                }
    
                if (sCInitSession.udpPort > 0 && !isFirstInitUdp)
                {
                    udpChannel.Reconnect();
                }
                
                FNetManager.Instance.OnSessionStartedCallback();

                foreach (ISessionListener sessionListener in FNetManager.Instance.GetListeners())
                {
                    if (sessionNumber == 0)
                    {
                        sessionListener.OnFirstSession();
                    }else
                    {
                        sessionListener.OnSessionReset();
                    }
                }
                sessionNumber++;
                
                if (sCInitSession.state == SCInitSession.STATE_OLD_SESSION)
                    return;
            }else if (message is SCCloseSession)
            {
                this.forceDisconnected = true;
                this.fChannel.ClosePermanently();
            }
            else
            {
                if (message is SCCompressedMessage)
                {
                    message = FNetManager.Instance.GetCompressor().Decompress((SCCompressedMessage)message);
                    LogUtil.Log(message);
                }
                
                if (maxSCSequence < message.scSequence)
                    maxSCSequence = message.scSequence;
                else if (message.transport == TransportType.UDP)
                {
                    // return;
                }

                SCMessage scMessage = (SCMessage)message;
                if (!string.IsNullOrEmpty(scMessage.watingSCMessage))
                {
                    FCallbackManager.Instance.OnSCResponse(scMessage);
                }
                scMessage.OnData();
                List<ISCMessageListener> scListeners = FNetManager.Instance.GetSCMessageListeners(scMessage.GetType());
                if (scListeners != null)
                {
                        foreach (ISCMessageListener scListener in scListeners)
                        { 
                           scListener.OnMessage(scMessage);
                        }            
                }

            }
        }

        private void RestartSession()
        {
            this.maxCSSequence = 0;
            this.maxSCSequence = 0;
            new CSInitSession(sessionId).Send();
        }


        public void Send(CSMessage message)
        {
            TransportType transportType = message.transport;
            bool isCompressed = message.isCompressed;
            
            message.timeClient = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            message.sessionId = this.sessionId;
            maxCSSequence++;
            message.csSequence = maxCSSequence;
            message.scSequence = maxSCSequence;
            message.messageId = UUIDUtil.UUID16();

            if (message.waiting4CameEvent || message.waiting4DoneEvent || message.watingSCMessage != null)
            {
                FCallbackManager.Instance.AddMessage2Queue(message);
            }

            LogUtil.Log(message);

            if (isCompressed)
            {
                message = FNetManager.Instance.GetCompressor().Compress(message);
            }
            if (transportType == TransportType.TCP)
            {
                fChannel.Send(message);
            }
            else if (transportType == TransportType.UDP)
            {
                if (udpChannel == null)
                    LogUtil.Error("Server does not support UDP!");
                else
                    udpChannel.Send(message);
            }
        }


        public void Start()
        {
            fChannel.Start();
            CoroutineRunner.Instance.StartRoutine(Ping());
            CoroutineRunner.Instance.StartRoutine(CheckTimeout());
        }

        IEnumerator CheckTimeout()
        {
            while (true)
            {
                long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (this.lastMessageTime != -1 && (currentTime - this.lastMessageTime) > TIMEOUT)
                {
                    this.lastMessageTime = -1;
                    OnTimeout();
                }
                yield return new WaitForSecondsRealtime(1);
            }
        }
        
        IEnumerator Ping()
        {
            while (true)
            {
                Send(new FPing());
                yield return new WaitForSecondsRealtime(5);
            }
        }
        
        
        async public void SendAsync(CSMessage csMessage)
        { 
            await Task.Run(() => { Send(csMessage);});
        }

        public void SendAfter(CSMessage csMessage, float delay)
        {
            DOVirtual.DelayedCall(delay, () => { Send(csMessage);});
        }

        public void SendAsyncAfter(CSMessage csMessage, float delay)
        {
            DOVirtual.DelayedCall(delay, () => { SendAsync(csMessage);});
        }

        public void OnChannelConnected()
        {
            Debug.Log("SESSION: OnChannelConnected");
            FNetManager.Instance.Connected = true;
            this.maxCSSequence = 0;
            this.maxSCSequence = 0;
            new CSInitSession(sessionId).Send();
        }

        public void OnChannelDisconnected()
        {
            Debug.Log("SESSION: OnChannelDisconnected");
            FNetManager.Instance.Connected = false;
            foreach (ISessionListener sessionListener in FNetManager.Instance.GetListeners())
            {
                sessionListener.OnChannelDisconnected(this.fChannel);
            }
        }

        
        public void OnChannelReconnected()
        {
            Debug.Log("SESSION: OnChannelReconnected");
            FNetManager.Instance.Connected = true;
        }

        public void OnChannelReconnecting()
        {
            Debug.Log("SESSION: OnChannelReconnecting");
        }

        private void OnTimeout()
        {
            if (forceDisconnected) return;
            Debug.Log("SESSION: OnTimeout");
            FNetManager.Instance.Connected = false;
            Restart();
        }

        public void Restart()
        {
            if (forceDisconnected) return;
            fChannel.Restart();
        }
    }
}

