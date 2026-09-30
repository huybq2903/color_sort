/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using UnityEngine;

namespace Falcon.Modules.CDN
{
    public class SOCdnSettings : ScriptableObject
    {
        [SerializeField] public string cdnKey;
        [SerializeField] public bool syncAllFilesAutomatically = true;
        [SerializeField] public int retryAttempts = 1;
    }
}