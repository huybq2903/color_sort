/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-22
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Falcon.Shared.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.RewardFlow
{
    public class UIResource : MonoBehaviour
    {
        public TMP_Text textAmount;
        public Image icon;
        public string resourceName, where;

        private int _currentAmount, _previousAmount;
        private FConfigRuntimeSO _config;
        private CancellationTokenSource _cts;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _config = Resources.Load<FConfigRuntimeSO>("ConfigRuntimeUIResource");
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable() => UIResourceManager.AddResource(this);

        private void OnDisable() => UIResourceManager.RemoveResource(this);

        public UIResource MoreValue(int more)
        {
            _currentAmount += more;
            return this;
        }

        public UIResource ReplaceValue(int newValue)
        {
            _currentAmount = newValue;
            return this;
        }

        public void DoImmediately()
        {
            if (!textAmount) return;
            textAmount.text = FormatValue(_currentAmount);
            _previousAmount = _currentAmount;
        }

        public void DoAnim() => DoAnimAsync().Forget();

        public Func<int, string> FormatValue { get; set; } = t => t.ToString();

        public async UniTask DoAnimAsync()
        {
            if (!textAmount) return;

            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            _cts = new CancellationTokenSource();
            var duration = _config.GetFloat("durationResourceTextAnim", 1f);
            // SetLink: counter bị destroy lúc unload scene thì DOTween tự kill, khỏi tween lên object đã chết
            textAmount.DOTextInt(_previousAmount, _currentAmount, duration, FormatValue).SetLink(gameObject);
            await UniTask.WaitForSeconds(duration + 0.5f);
            if (_canvasGroup)
            {
                _canvasGroup.DOFade(0, 0.4f).SetLink(gameObject);
            }
            _previousAmount = _currentAmount;
        }
    }

    public static class UIResourceManager
    {
        private static readonly Dictionary<(string, string), List<UIResource>> _dictResource = new();

        public static UIResource GetResource(string resourceName, string where)
        {
            if (!_dictResource.TryGetValue((resourceName, where), out var list))
            {
                list = new List<UIResource>();
                _dictResource[(resourceName, where)] = list;
            }

            var found = list.LastOrDefault(r => r.resourceName == resourceName && r.where == where);
            if (found) return found;

            // Fallback về counter mặc định (where rỗng): các UIResource trong prefab đều để where trống
            // trong khi caller truyền tab ("home"...) — match cứng theo cặp làm gold fly miss ở mọi nơi.
            if (!string.IsNullOrEmpty(where) && _dictResource.TryGetValue((resourceName, ""), out var defaults))
                return defaults.LastOrDefault(r => r.resourceName == resourceName);

            return null;
        }

        public static void AddResource(UIResource resource)
        {
            if (string.IsNullOrEmpty(resource.resourceName)) return;
            if (!_dictResource.TryGetValue((resource.resourceName, resource.where), out var list))
            {
                list = new List<UIResource>();
                _dictResource[(resource.resourceName, resource.where)] = list;
            }

            list.Add(resource);
        }

        public static void RemoveResource(UIResource resource)
        {
            if (!_dictResource.TryGetValue((resource.resourceName, resource.where), out var list)) return;
            list.Remove(resource);
        }
    }
}