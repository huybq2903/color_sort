/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-28
 */

using System;
using UnityEngine;

namespace Falcon.Shared.Common
{
    public class FParticleListener : MonoBehaviour
    {
        public Action OnParticleStop;
        
        private void OnParticleSystemStopped()
        {
            OnParticleStop?.Invoke();
        }
    }
}