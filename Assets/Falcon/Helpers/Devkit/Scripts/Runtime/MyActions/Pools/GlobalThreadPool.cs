/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class GlobalThreadPool : MonoSingleton<GlobalThreadPool>, IThreadPool
    {
        private readonly MyConcurrentQueue<IMyAction> _actionQueue = new();

        private void Update()
        {
            foreach (var action in _actionQueue.DrainAll())
                if (action.CanInvoke())
                    Task.Run(action.Invoke);
                else
                    _actionQueue.Enqueue(action);
        }

        public int Size
        {
            get => 1;
            set => UtilLogger.Instance.Warning($"UnityThreadPool size set to 1, ignoring set value {value}");
        }

        public void Add(IMyAction action)
        {
            _actionQueue.Enqueue(action);
        }

        public bool Remove(IMyAction action)
        {
            return _actionQueue.Remove(action);
        }
    }
}