/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-15
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Falcon.Shared.Audio
{
    // Cho mượn AudioSource và đánh dấu từng lượt phát bằng playId, để task async biết mình còn hợp lệ hay không
    internal class AudioSourcePool
    {
        private readonly ObjectPool<AudioSource> _pool;
        private readonly Transform _parent;
        private readonly Dictionary<AudioSource, Retry> _entry = new();
        private uint _playCounter;

        private struct Retry
        {
            public string clipName;
            public uint playId;
        }

        public AudioSourcePool(Transform parent)
        {
            _parent = parent;
            _pool = new ObjectPool<AudioSource>(
                createFunc: CreateNewSource,
                actionOnGet: obj => obj.gameObject.SetActive(true),
                actionOnRelease: obj => obj.gameObject.SetActive(false),
                actionOnDestroy: obj => Object.Destroy(obj.gameObject));
        }

        public IReadOnlyCollection<AudioSource> AudioSources => _entry.Keys;

        public AudioSource Get(AudioClip clip, string clipName, AudioMixerGroup group, out uint playId)
        {
            // Reset về mốc: đúng bằng những property mà API cho phép đổi, không hơn
            var source = _pool.Get();
            source.clip = clip;
            source.loop = false;
            source.volume = 0f;
            source.pitch = 1f;
            source.panStereo = 0f;
            source.outputAudioMixerGroup = group;
#if UNITY_EDITOR
            source.name = clipName;
#endif
            playId = ++_playCounter;
            _entry[source] = new Retry { clipName = clipName, playId = playId };
            return source;
        }

        // Cấp lượt mới cho source đang mượn -> task của lượt cũ tự rút lui khi IsCurrent trả false
        public bool TryRenew(AudioSource source, out uint playId)
        {
            playId = 0;
            if (!source || !_entry.TryGetValue(source, out var rented))
            {
                return false;
            }

            playId = ++_playCounter;
            _entry[source] = new Retry { clipName = rented.clipName, playId = playId };
            return true;
        }

        public bool IsCurrent(AudioSource source, uint playId)
            => source && _entry.TryGetValue(source, out var rented) && rented.playId == playId;

        // Trả về clipName của lượt vừa kết thúc; null = source không phải đang mượn (đã trả rồi / không phải của pool)
        public string Release(AudioSource source)
        {
            if (!source || !_entry.Remove(source, out var rented))
            {
                return null;
            }

            source.Stop();
            source.clip = null;
#if UNITY_EDITOR
            source.name = "[SourcePool]";
#endif
            _pool.Release(source);
            return rented.clipName;
        }

        private AudioSource CreateNewSource()
        {
            var go = new GameObject("[Audio]");
            var src = go.AddComponent<AudioSource>();
            src.transform.SetParent(_parent);
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            return src;
        }
    }
}
