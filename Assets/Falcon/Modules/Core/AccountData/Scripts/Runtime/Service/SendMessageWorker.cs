/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;
using System.Collections;
using System.Collections.Generic;
using Falcon.Helpers.Singleton;

namespace Falcon.Modules.Core.AccountData 
{
    public class SendMessageWorker : PersistentSingleton<SendMessageWorker>
    {
        public Queue<CSMessageWaitLoginSuccess> queue = new Queue<CSMessageWaitLoginSuccess>();

        protected override void Awake()
        {
            base.Awake();
            Init();
        }
        
        private void Init()
        {
            IEnumerator CoroutineDeQueue()
            {
                while (true)
                {
                    yield return null;
                    if (queue.Count > 0 && AccountManager.Instance.IsLogin)
                    {
                        CSMessageWaitLoginSuccess cs = queue.Dequeue();
                        cs.SendNow();
                    }
                }
            }

            StartCoroutine(CoroutineDeQueue());
        }

        public void AddQueue(CSMessageWaitLoginSuccess cSMessage) { queue.Enqueue(cSMessage); }

        public bool IsEmpty => queue.Count == 0;

        public void RemoveUpdateGameDataMessages()
        {
            var tempQueue = new Queue<CSMessageWaitLoginSuccess>();
    
            while (queue.Count > 0)
            {
                var cs = queue.Dequeue();
                if (!(cs is CSUpdateGameData))
                {
                    tempQueue.Enqueue(cs);
                }
            }
    
            queue = tempQueue;
        }
    }
}