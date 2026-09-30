/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-15
 */

using UnityEngine;

namespace Falcon.Shared.Audio
{
    public class AudioMonoPlayer : MonoBehaviour
    {
        public void Play(SoundEnum audioName)
        {
            AudioManager.PlaySFX(audioName);
        }
    }
}