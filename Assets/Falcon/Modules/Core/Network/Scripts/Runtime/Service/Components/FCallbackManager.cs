/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Falcon.Modules.Core.Network
{
    public class FCallbackManager
    {
        private static FCallbackManager _instance = null;
        private FCallbackManager()
        {
        }

        public static FCallbackManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FCallbackManager();
                    _instance.Start1SecondCoroutine();
                }    
                return _instance;
            }
        }

        private void Start1SecondCoroutine()
        {
            CoroutineRunner.Instance.RunEverySecond(ScanQueue);
        }

        private void ScanQueue()
        {
            //Process Received
            if (timeoutCame2Messages.ContainsKey(1))
            {
                HashSet<CSMessage> cSMessages = timeoutCame2Messages[1];
                foreach (CSMessage cSMessage in cSMessages)
                {
                    if (messageId2Message.ContainsKey(cSMessage.messageId))
                    {
                        cSMessage.waiting4CameEvent = false;
                        if (!cSMessage.waiting4DoneEvent && cSMessage.watingSCMessage == null)
                            messageId2Message.Remove(cSMessage.messageId, out CSMessage cSMessage1);
                        cSMessage.OnMessageCameCallback("", true, false);
                    }
                }
            }
            timeoutCame2Messages.Remove(1);
            var keysToProcess = new List<int>(timeoutCame2Messages.Keys);
            foreach (var timeout in keysToProcess)
            {
                HashSet<CSMessage> cSMessages = timeoutCame2Messages[timeout];
                timeoutCame2Messages.Add(timeout - 1, cSMessages);
                timeoutCame2Messages.Remove(timeout);
            }


            //Process Processed
            if (timeoutDone2Messages.ContainsKey(1))
            {
                HashSet<CSMessage> cSMessages = timeoutDone2Messages[1];
                foreach (CSMessage cSMessage in cSMessages)
                {
                    if (messageId2Message.ContainsKey(cSMessage.messageId))
                    {
                        cSMessage.waiting4DoneEvent = false;
                        if (!cSMessage.waiting4CameEvent && cSMessage.watingSCMessage == null)
                            messageId2Message.Remove(cSMessage.messageId, out CSMessage cSMessage1);
                        cSMessage.OnMessageDoneCallback("", true, false);
                    }
                }
            }
            timeoutDone2Messages.Remove(1);
            keysToProcess = new List<int>(timeoutDone2Messages.Keys);
            foreach (var timeout in keysToProcess)
            {
                HashSet<CSMessage> cSMessages = timeoutDone2Messages[timeout];
                timeoutDone2Messages.Add(timeout - 1, cSMessages);
                timeoutDone2Messages.Remove(timeout);
            }

            //Process SC
            if (timeoutSC2Messages.ContainsKey(1))
            {
                HashSet<CSMessage> cSMessages = timeoutSC2Messages[1];
                foreach (CSMessage cSMessage in cSMessages)
                {
                    if (messageId2Message.ContainsKey(cSMessage.messageId))
                    {
                        if (!cSMessage.waiting4CameEvent && !cSMessage.waiting4DoneEvent)
                            messageId2Message.Remove(cSMessage.messageId, out CSMessage cSMessage1);
                        cSMessage.OnSCResponse(null, true, false);
                    }
                }
            }
            timeoutSC2Messages.Remove(1);
            keysToProcess = new List<int>(timeoutSC2Messages.Keys);
            foreach (var timeout in keysToProcess)
            {
                HashSet<CSMessage> cSMessages = timeoutSC2Messages[timeout];
                timeoutSC2Messages.Add(timeout - 1, cSMessages);
                timeoutSC2Messages.Remove(timeout);
            }
        }

        internal void OnResponse(CSSCResponse cSSCResponse)
        {
            if (!messageId2Message.ContainsKey(cSSCResponse.messageId))
                return;
            CSMessage cSMessage = messageId2Message[cSSCResponse.messageId];
            
            if (cSSCResponse.waiting4CameEvent)
            {
                cSMessage.OnMessageCameCallback(cSSCResponse.message, false, true);
                cSMessage.waiting4CameEvent = false;
                if (!cSMessage.waiting4DoneEvent)
                    messageId2Message.TryRemove(cSMessage.messageId, out cSMessage);
            }

            if (cSSCResponse.waiting4DoneEvent)
            {
                cSMessage.OnMessageDoneCallback(cSSCResponse.message, false, true);
                cSMessage.waiting4DoneEvent = false;
                if (!cSMessage.waiting4CameEvent)
                    messageId2Message.TryRemove(cSMessage.messageId, out cSMessage);
            }
        }

        private ConcurrentDictionary<string, CSMessage> messageId2Message = new ConcurrentDictionary<string, CSMessage>();
        private SortedDictionary<int, HashSet<CSMessage>> timeoutCame2Messages = new SortedDictionary<int, HashSet<CSMessage>>();
        private SortedDictionary<int, HashSet<CSMessage>> timeoutDone2Messages = new SortedDictionary<int, HashSet<CSMessage>>();
        private SortedDictionary<int, HashSet<CSMessage>> timeoutSC2Messages = new SortedDictionary<int, HashSet<CSMessage>>();

        internal void AddMessage2Queue(CSMessage message)
        {
            messageId2Message.TryAdd(message.messageId, message);
            if (message.waiting4CameEvent)
            {
                if (!timeoutCame2Messages.ContainsKey(message.cameTimeout)) {
                    timeoutCame2Messages.TryAdd(message.cameTimeout, new HashSet<CSMessage>());
                }
                timeoutCame2Messages[message.cameTimeout].Add(message);
            }

            if (message.waiting4DoneEvent)
            {
                if (!timeoutDone2Messages.ContainsKey(message.doneTimeout))
                {
                    timeoutDone2Messages.TryAdd(message.doneTimeout, new HashSet<CSMessage>());
                }
                timeoutDone2Messages[message.doneTimeout].Add(message);
            }

            if (message.watingSCMessage != null)
            {
                if (!timeoutSC2Messages.ContainsKey(message.scTimeout))
                {
                    timeoutSC2Messages.TryAdd(message.scTimeout, new HashSet<CSMessage>());
                }
                timeoutSC2Messages[message.scTimeout].Add(message);
            }
        }

        public void OnSCResponse(SCMessage scMessage)
        {
            if (!messageId2Message.ContainsKey(scMessage.messageId))
                return;
            CSMessage cSMessage = messageId2Message[scMessage.messageId];
            cSMessage.OnSCResponse(scMessage, false, true);
            cSMessage.watingSCMessage = null;
            if (!cSMessage.waiting4CameEvent && !cSMessage.waiting4DoneEvent)
                messageId2Message.TryRemove(cSMessage.messageId, out cSMessage);
        }
    }
}

