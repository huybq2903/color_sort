/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-15
 */

using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace Falcon.Shared.Audio
{
    // Bọc AudioMixer: mixer group để route source, và volume bus cho setting bật/tắt nhạc-tiếng
    internal class AudioBus
    {
        private const float VolumeOnDb = 0f;
        private const float VolumeOffDb = -80f;
        private const string MixerResourceName = "MasterMixer";
        private const string MusicVolumeParam = "MusicVol";
        private const string SfxVolumeParam = "SFXVol";
        private const string MusicGroupName = "Music";
        private const string SfxGroupName = "SFX";

        private AudioMixer _mixer;
        private AudioMixerGroup _musicGroup;
        private AudioMixerGroup _sfxGroup;

        private AudioMixer Mixer => _mixer ? _mixer : _mixer = Resources.Load<AudioMixer>(MixerResourceName);

        public AudioMixerGroup Music => _musicGroup ? _musicGroup : _musicGroup = GetGroup(MusicGroupName);

        public AudioMixerGroup Sfx => _sfxGroup ? _sfxGroup : _sfxGroup = GetGroup(SfxGroupName);

        public void SetMusicOnOff(bool isOn) => FadeVolume(MusicVolumeParam, isOn, 0.5f);

        // duration 0 + delay: tắt/bật dứt khoát nhưng lùi lại chút cho khớp animation của popup setting
        public void SetSfxOnOff(bool isOn) => FadeVolume(SfxVolumeParam, isOn, 0f, 0.2f);

        private void FadeVolume(string param, bool isOn, float duration, float delay = 0)
        {
            var target = isOn ? VolumeOnDb : VolumeOffDb;
            if (!Mixer.GetFloat(param, out var start))
            {
                start = target;
            }

            // SetId(param) để lần fade sau kill lần trước trên cùng param, không đánh nhau
            DOTween.Kill(param);
            DOTween.To(() => start, value =>
                {
                    start = value;
                    Mixer.SetFloat(param, value);
                },
                target, duration).SetDelay(delay).SetEase(Ease.Linear).SetUpdate(true).SetId(param);
        }

        private AudioMixerGroup GetGroup(string groupName)
        {
            var groups = Mixer.FindMatchingGroups(groupName);
            return groups is { Length: > 0 } ? groups[0] : null;
        }
    }
}
