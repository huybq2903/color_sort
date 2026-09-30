/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-02
*/

using UnityEngine;

namespace Falcon.Modules.UI.Level.Runtime
{
    [CreateAssetMenu(fileName = "UILevelConfig", menuName = "Scriptable Objects/Falcon/Modules/UILevel/UILevelConfig")]
    public class UILevelConfig : ScriptableObject
    {
        [SerializeField] private Sprite[] _nodeDifficultyBGs;
        [SerializeField, Tooltip("9-slices")] private Sprite[] _btnPlayDifficultBGs;
        [SerializeField] private Sprite[] _skullSprites;
        [SerializeField] private Sprite _notReachLevelNodeBG;
        [SerializeField, Tooltip("9-slices")] private Sprite _winstreakBG;
        [SerializeField] private Sprite _winstreakIcon;
        [SerializeField, Tooltip("9-slices")] private Sprite _levelLineProgress, _levelLineProgressBG;
        
        public Sprite[] NodeDifficultyBGs => _nodeDifficultyBGs;
        public Sprite[] BtnPlayDifficultBGs => _btnPlayDifficultBGs;
        public Sprite[] SkullSprites => _skullSprites;
        public Sprite NotReachLevelNodeBG => _notReachLevelNodeBG;
        public Sprite WinstreakBG => _winstreakBG;
        public Sprite WinstreakIcon => _winstreakIcon;
        public Sprite LevelLineProgress => _levelLineProgress;
        public Sprite LevelLineProgressBG => _levelLineProgressBG;
    }
}