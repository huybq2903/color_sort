/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;
using NUnit.Framework.Constraints;

namespace Falcon.Modules.Core.Network
{
    [Serializable]
    public abstract class CSMessage : FMessage
    {
        public delegate void MessageSC<TResponse>(TResponse message, bool timeout, bool success) ;
        
        public delegate void MessageSCExt<TResponse>(CSMessage csMessage, TResponse message, bool timeout, bool success) ;

        private Delegate _callbacks;

        private bool isSCExt = false;
        public CSMessage AddSCListenerExt<TResponse>(MessageSCExt<TResponse> callback, int timeout = 5) where TResponse : SCMessage
        {
            this.isSCExt = true;
            this.scTimeout = timeout;
            this.watingSCMessage = FNetManager.Instance.GetSCEventName(typeof(TResponse));
            _callbacks = callback;
            return this;
        }
        
        public CSMessage AddSCListener<TResponse>(MessageSC<TResponse> callback, int timeout = 5) where TResponse : SCMessage
        {
            this.isSCExt = false;
            this.scTimeout = timeout;
            this.watingSCMessage = FNetManager.Instance.GetSCEventName(typeof(TResponse));
            _callbacks = callback;
            return this;
        }
        
        
        public void OnSCResponse(SCMessage message, bool timeout, bool success)
        {
            var method = _callbacks.GetType().GetMethod("Invoke");
            if (this.isSCExt)
                 method?.Invoke(_callbacks, new object[] {this, message, timeout, success });
            else
                method?.Invoke(_callbacks, new object[] {message, timeout, success });
        }
        
        public delegate void MessageCallback(string response, bool timeout, bool success);
        private event MessageCallback OnMessageCame;
        private event MessageCallback OnMessageDone;
        public CSMessage AddMessageCameCallback(MessageCallback callback, int timeout = 5)
        {
            if (OnMessageCame != null)
            {
                UnityEngine.Debug.LogError("Cannot Add More Than One Received Callback!!! ");
                return this;
            }
            else
            {
                OnMessageCame += callback;
                this.cameTimeout = timeout;
                this.waiting4CameEvent = true;
                return this;
            }
        }

        public CSMessage AddMessageDoneCallback(MessageCallback callback, int timeout = 5)
        {
            if (OnMessageDone != null)
            {
                UnityEngine.Debug.LogError("Cannot Add More Than One Processed Callback!!! ");
                return this;
            }
            else
            {
                OnMessageDone += callback;
                this.doneTimeout = timeout;
                this.waiting4DoneEvent = true;
                return this;
            }
        }
        
        
        internal void OnMessageCameCallback(string response, bool timeout, bool success)
        {
            OnMessageCame?.Invoke(response, timeout, success);
        }

        internal void OnMessageDoneCallback(string response, bool timeout, bool success)
        {
            OnMessageDone?.Invoke(response, timeout, success); 
        }

        public virtual void Send()
        {
            FNetManager.Instance.GetSession()?.Send(this);
        }

        public virtual void SendAsync()
        {
            FNetManager.Instance.GetSession()?.SendAsync(this);
        }

        public virtual void SendAfter(float delay)
        {
            FNetManager.Instance.GetSession()?.SendAfter(this, delay);
        }

        public virtual void SendAsyncAfter(float delay)
        {
            FNetManager.Instance.GetSession()?.SendAsyncAfter(this, delay);
        }
        
        public CSMessage UDP()
        {
            this.transport = TransportType.UDP;
            return this;
        }
        public CSMessage Compress()
        {
            this.isCompressed = true;
            return this;
        }
        
    }

}

