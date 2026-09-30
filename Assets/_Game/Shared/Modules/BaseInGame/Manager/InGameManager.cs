using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using UnityEngine;
using System;
using Falcon.Shared.Common;

namespace Falcon.Shared.BaseInGame
{
    [DefaultExecutionOrder(-100)]
    public class InGameManager : MonoBehaviour
    {
        private readonly Dictionary<Type, ISubManager> _dictionaryManager = new();
        private string _phase;

        private void Awake()
        {
            var managers = GetComponents<ISubManager>();
            foreach (var manager in managers)
            {
                _dictionaryManager[manager.GetType()] = manager;
            }

            foreach (var manager in _dictionaryManager.Values)
            {
                Inject(manager);
            }

            foreach (var manager in _dictionaryManager.Values)
            {
                try
                {
                    manager.Initialized();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to initialize manager {manager.GetType().Name}: {e}");
                }
            }
        }

        /// <summary>Register a runtime-created manager; call before Start so it joins every phase.</summary>
        public void Add(ISubManager manager)
        {
            _dictionaryManager[manager.GetType()] = manager;
            Inject(manager);
            manager.Initialized();
        }

        private void Start() => RunLevelStart().Forget();

        private async UniTaskVoid RunLevelStart()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            try
            {
                GameEvent.Emit(GameKeys.LOAD_SCENE_LEVEL_COMPLETE);
                await RunPhase<ILevelBeforeSpawn>(m => m.BeforeSpawn(), ct);
                await RunPhase<ILevelSpawn>(m => m.Spawn(), ct);
                await RunPhase<ILevelAfterSpawn>(m => m.AfterSpawn(), ct);
                GameEvent.Emit(GameKeys.SETUP_LEVEL_COMPLETE);
            }
            catch (OperationCanceledException)
            {
                // Thoát level giữa chừng, không phải lỗi.
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelStart] pha {_phase} lỗi: {e}");
            }
        }

        // Duyệt manager theo thứ tự component, chỉ gọi ai implement pha đó.
        private async UniTask RunPhase<T>(Func<T, UniTask> run, CancellationToken ct)
        {
            _phase = typeof(T).Name;
            foreach (var manager in _dictionaryManager.Values)
            {
                if (manager is not T typed) continue;
                ct.ThrowIfCancellationRequested();
                await run(typed);
            }
        }

        // Fill các field có [Inject] bằng manager tương ứng trong dictionary. Nhận object để mode POCO dùng chung.
        // ponytail: quét field mỗi Awake bằng reflection — managers chỉ vài chục cái nên chấp nhận;
        // nếu cần nhanh hơn thì cache type->fields theo từng loại manager.
        public void Inject(object target)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var t = target.GetType(); t != null; t = t.BaseType)
            {
                foreach (var field in t.GetFields(flags))
                {
                    if (!Attribute.IsDefined(field, typeof(InjectAttribute))) continue;
                    var dep = Resolve(field.FieldType);
                    if (dep != null)
                        field.SetValue(target, dep);
                    else
                        Debug.LogWarning($"[Inject] {t.Name}.{field.Name}: không tìm thấy manager {field.FieldType.Name}");
                }
            }
        }

        // Khớp concrete type trước, không có thì lấy manager đầu tiên assignable (base class / interface).
        // ponytail: lấy match đầu tiên, nếu có 2 manager cùng kế thừa 1 base thì kết quả không xác định
        private ISubManager Resolve(Type type)
        {
            if (_dictionaryManager.TryGetValue(type, out var dep)) return dep;
            foreach (var pair in _dictionaryManager)
                if (type.IsAssignableFrom(pair.Key))
                    return pair.Value;
            return null;
        }
    }
}
