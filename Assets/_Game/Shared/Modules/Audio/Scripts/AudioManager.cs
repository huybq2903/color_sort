/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-15
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Shared.Addressable;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;
using Falcon.Shared.Common;

// ReSharper disable once CheckNamespace
namespace Falcon.Shared.Audio
{
    public static class AudioManager
    {
        private const float FadeDuration = 0.15f;
        private const string SettingsMediaGet = "falcon.modules.ui.settings_get_media";

        private static AudioSourcePool _sourcePool;

        // Không cần InitSettings: ctor không đụng Unity API, mixer nạp lazy ở lần dùng đầu
        private static readonly AudioBus _audioBus = new();
        private static readonly SfxThrottle _sfxThrottle = new();
        private static readonly List<AudioSource> _stopBuffer = new();

        // Enum.GetNames xếp theo giá trị member; SoundEnum auto-gen đánh số liền nên (int)key làm index được.
        private static readonly string[] SoundEnumNames = Enum.GetNames(typeof(SoundEnum));

        // Dựng sẵn address biến thể một lần, khỏi nối chuỗi mỗi lượt phát. null = key chỉ có 1 clip.
        private static readonly string[][] VariantAddresses = BuildVariantAddresses();

        private static string[][] BuildVariantAddresses()
        {
            var counts = SoundEnumVariants.Counts;
            var table = new string[SoundEnumNames.Length][];

            for (var i = 0; i < table.Length; i++)
            {
                var total = i < counts.Length ? counts[i] : 1;
                if (total <= 1) continue;

                table[i] = new string[total];
                for (var v = 0; v < total; v++)
                    table[i][v] = $"{SoundEnumNames[i]}_{v}";
            }

            return table;
        }

        private static string AddressOf(SoundEnum key)
        {
            var i = (int)key;
            var variants = VariantAddresses[i];
            return variants == null ? SoundEnumNames[i] : variants[UnityEngine.Random.Range(0, variants.Length)];
        }

        public static void InitSettings()
        {
            GameEvent<(string name, int value)>.Register(GameKeys.SETTINGS_MEDIA_SAVE, DoSetting, null);
            GameEvent.Register(GameKeys.STOP_MUSIC, StopMusic, null);
            GameEvent<string>.Register(GameKeys.PLAY_MUSIC, PlayMusic, null);
            GameEvent<string>.Register(GameKeys.PLAY_SFX, PlaySFX, null);
            GameEvent<string>.Register(GameKeys.STOP_MUSIC, StopMusic, null);

            InitPools();
            var (sound, vibrate, music) = GameRequest<(int sound, int vibrate, int music)>.Request(SettingsMediaGet);
            _audioBus.SetSfxOnOff(sound > 0);
            _audioBus.SetMusicOnOff(music > 0);
            HapticManager.SetOnOff(vibrate > 0);
        }

        #region Settings

        private static void DoSetting((string name, int value) d)
        {
            switch (d.name)
            {
                case "sound":
                    _audioBus.SetSfxOnOff(d.value > 0);
                    break;
                case "music":
                    _audioBus.SetMusicOnOff(d.value > 0);
                    break;
                case "vibrate":
                    HapticManager.SetOnOff(d.value > 0);
                    break;
            }
        }

        #endregion

        public static void PlayMusic(string nameSound)
        {
            var source = GetFromPool(nameSound, _audioBus.Music, out var playId);
            if (source)
            {
                PlayLoop(source, playId);
            }
        }

        public static void PlayMusic(SoundEnum key) => PlayMusic(AddressOf(key));

        // GameEvent<string> cần Action<string> thuần, không nhận method group có tham số optional
        private static void PlaySFX(string nameSound) => PlaySFX(nameSound, default);

        public static void PlaySFX(string nameSound, SfxOptions options = default)
        {
            if (!_sfxThrottle.TryReserve(nameSound))
            {
                return;
            }

            var source = GetFromPool(nameSound, _audioBus.Sfx, out var playId);
            if (source)
            {
                PlayOneTime(source, playId, options);
            }
            else
            {
                _sfxThrottle.Release(nameSound);
            }
        }

        public static void PlaySFX(SoundEnum key, SfxOptions options = default) => PlaySFX(AddressOf(key), options);

        public static void StopMusic() => StopByGroup(_audioBus.Music, null);

        public static void StopMusic(string nameSound) => StopByGroup(_audioBus.Music, nameSound);

        /// Dừng mọi SFX đang phát, có fade nên không cụp tai.
        public static void StopSFX() => StopByGroup(_audioBus.Sfx, null);

        // nameSound null = stop mọi tiếng trong group
        private static void StopByGroup(AudioMixerGroup group, string nameSound)
        {
            if (_sourcePool == null) return;

            _stopBuffer.Clear();
            foreach (var src in _sourcePool.AudioSources)
            {
                if (!src || src.outputAudioMixerGroup != group)
                    continue;
                if (nameSound != null && (!src.clip || src.clip.name != nameSound))
                    continue;
                _stopBuffer.Add(src);
            }

            foreach (var src in _stopBuffer)
                FadeOutAndRelease(src);
            _stopBuffer.Clear();
        }

        private static void FadeOutAndRelease(AudioSource source)
        {
            // TryRenew fail = source đã về pool -> kệ nó.
            // Renew cấp playId mới để task fade-in / chờ-hết-clip của lượt trước tự rút lui, khỏi giành volume.
            if (!_sourcePool.TryRenew(source, out var playId))
                return;

            FadeSourceAsync(source, playId, 0f, true).Forget();
        }

        private static AudioClip GetAudioClip(string name) => AddressableExtensions.Load<AudioClip>(name);

        private static void InitPools()
        {
            var parent = new GameObject("[SourcesAudio]").AddComponent<AudioListener>().transform;
            Object.DontDestroyOnLoad(parent);
            _sourcePool = new AudioSourcePool(parent);
        }

        private static AudioSource GetFromPool(string nameSound, AudioMixerGroup group, out uint playId)
        {
            playId = 0;
            if (string.IsNullOrEmpty(nameSound))
            {
                return null;
            }

            var clip = GetAudioClip(nameSound);
            if (!clip)
            {
                Debug.LogWarning($"AudioManager: AudioClip '{nameSound}' not found.");
                return null;
            }

            return _sourcePool.Get(clip, nameSound, group, out playId);
        }

        private static void PlayOneTime(AudioSource source, uint playId, SfxOptions options)
        {
            source.loop = false;
            source.volume = options.Volume ?? 1f;
            source.pitch = options.Pitch ?? 1f;
            source.panStereo = options.Pan ?? 0f;
            source.Play();
            ReleaseWhenFinishedAsync(source, playId).Forget();
        }

        // Ngủ đúng thời lượng còn lại rồi trả pool, không tick mỗi frame như tween
        private static async UniTaskVoid ReleaseWhenFinishedAsync(AudioSource source, uint playId)
        {
            var clip = source.clip;

            try
            {
                while (clip && source)
                {
                    // chia pitch: clip.length là độ dài ở pitch 1, phát chậm/nhanh thì thời gian thực khác
                    var remaining = (clip.length - source.time) / Mathf.Max(0.01f, source.pitch);
                    if (remaining <= 0f || !source.isPlaying)
                    {
                        break;
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(remaining), DelayType.UnscaledDeltaTime,
                        PlayerLoopTiming.Update, Application.exitCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (IsPlayIdCurrent(source, playId))
            {
                ReleasePooledSource(source);
            }
        }

        private static void PlayLoop(AudioSource source, uint playId)
        {
            source.loop = true;
            source.volume = 0f;
            source.Play();
            FadeSourceAsync(source, playId, 1f, false).Forget();
        }

        // Fade volume tới target; releaseAfter = trả source về pool khi fade xong (dùng cho Stop)
        private static async UniTaskVoid FadeSourceAsync(AudioSource source, uint playId, float target,
            bool releaseAfter)
        {
            var start = source.volume;
            var timer = 0f;

            try
            {
                while (timer < FadeDuration)
                {
                    if (!IsPlayIdCurrent(source, playId))
                    {
                        return;
                    }

                    timer += Time.unscaledDeltaTime;
                    source.volume = Mathf.Lerp(start, target, timer / FadeDuration);
                    await UniTask.Yield(PlayerLoopTiming.Update, Application.exitCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!IsPlayIdCurrent(source, playId))
            {
                return;
            }

            source.volume = target;
            if (releaseAfter)
            {
                ReleasePooledSource(source);
            }
        }

        // playId khớp = source vẫn đang phục vụ đúng lượt phát đã tạo ra task này
        private static bool IsPlayIdCurrent(AudioSource source, uint playId)
            => _sourcePool.IsCurrent(source, playId);

        private static void ReleasePooledSource(AudioSource source)
        {
            if (!source)
            {
                return;
            }

            var isSfx = source.outputAudioMixerGroup == _audioBus.Sfx;
            var clipName = _sourcePool.Release(source);
            if (isSfx && clipName != null)
            {
                _sfxThrottle.Release(clipName);
            }
        }
    }
}