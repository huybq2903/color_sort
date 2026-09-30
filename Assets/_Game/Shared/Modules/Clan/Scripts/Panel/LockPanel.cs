
using TMPro;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class LockPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;

        protected virtual void OnEnable()
        {
            _titleText.text = $"Reach <color=yellow>Level {Center.GetOrCreate<ClanService>().LevelUnlock}</color>";
        }
    }
}
