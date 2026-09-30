using System.Collections;
using System.Collections.Generic;

namespace Falcon.Modules.Core.Network
{
    public class FMessageSender : PersistentSingleton<FMessageSender>
    {
        public Queue<CSMessage> queue = new Queue<CSMessage>();

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
                    if (queue.Count > 0)
                    {
                        CSMessage cs = queue.Dequeue();
                        cs.Send();
                    }
                }
            }

            StartCoroutine(CoroutineDeQueue());
        }

        public void AddQueue(CSMessage cSMessage) { queue.Enqueue(cSMessage); }
    }
}