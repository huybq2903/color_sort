using Falcon.Modules.Core.UI.Runtime;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class EditClanController : MonoBehaviour
    {
        [SerializeField] private ClanLogoUI _logo;
        [SerializeField] private TMP_InputField _nameInput, _descriptionInput;
        [SerializeField] private TMP_InputField _requiredLevelInput;
        [SerializeField] private TextMeshProUGUI _txtOpen;

        public ClanEditingData EditingData { get; set; }

        private void OnEnable()
        {
            ResetOpenCloseUI();
        }

        public void Init(ClanEditingData teamData)
        {
            EditingData = teamData;
            _logo?.Init(teamData.avatar_id);
            if (_nameInput != null) _nameInput.text = teamData.name;
            if (_descriptionInput != null) _descriptionInput.text = teamData.description;
            if (_txtOpen != null) _txtOpen.text = teamData.open ? ClanOpenType.Public.ToString() : ClanOpenType.Private.ToString();
            if (_requiredLevelInput != null) _requiredLevelInput.text = teamData.required_level.ToString();
        }

        public void ShowClanLogoPopup()
        {
            UIWrapper.OpenPopup("UIPopupChooseLogo", popup =>
            {
                popup.GetComponent<UIPopupChooseLogo>().Bind(id =>
                {
                    if (EditingData == null) return;
                    EditingData.avatar_id = id;
                    _logo?.Init(id);
                });
            });
        }

        public void OnEndNameEdit()
        {
            if (_nameInput == null || EditingData == null) return;
            var raw = _nameInput.text ?? string.Empty;
            _nameInput.text = string.IsNullOrWhiteSpace(raw) ? string.Empty : Regex.Replace(raw.Trim(), @"\s{2,}", " ");
            EditingData.name = _nameInput.text;
        }

        public void OnEndDescriptionEdit()
        {
            if (EditingData == null || _descriptionInput == null) return;
            EditingData.description = _descriptionInput.text ?? string.Empty;
        }

        public void OpenOrCloseChange()
        {
            if (EditingData == null) return;
            EditingData.open = !EditingData.open;
            ResetOpenCloseUI();
        }

        public void ResetOpenCloseUI()
        {
            if (EditingData == null || _txtOpen == null) return;
            _txtOpen.text = EditingData.open ? "Public" : "Private";
        }

        public void OnRequiredLevelChange()
        {
            if (EditingData == null || _requiredLevelInput == null) return;
            int parsed;
            if (!int.TryParse(_requiredLevelInput.text, out parsed))
            {
                // giu nguyen gia tri hien tai neu parse fail
                _requiredLevelInput.text = EditingData.required_level.ToString();
                return;
            }
            EditingData.required_level = ClampRequiredLevel(parsed);
            _requiredLevelInput.text = EditingData.required_level.ToString();
        }

        public void NextRequiredLevel()
        {
            if (EditingData == null || _requiredLevelInput == null) return;
            if (EditingData.required_level < Center.GetOrCreate<ClanService>().MaxLevelLimit)
            {
                EditingData.required_level = ClampRequiredLevel(EditingData.required_level + 1);
                _requiredLevelInput.text = EditingData.required_level.ToString();
            }
        }

        public void PreviosRequiredLevel()
        {
            if (EditingData == null || _requiredLevelInput == null) return;
            if (EditingData.required_level > Center.GetOrCreate<ClanService>().LevelUnlock)
            {
                EditingData.required_level = ClampRequiredLevel(EditingData.required_level - 1);
                _requiredLevelInput.text = EditingData.required_level.ToString();
            }
        }

        private int ClampRequiredLevel(int value)
        {
            if (value < Center.GetOrCreate<ClanService>().LevelUnlock) return Center.GetOrCreate<ClanService>().LevelUnlock;
            if (value > Center.GetOrCreate<ClanService>().MaxLevelLimit) return Center.GetOrCreate<ClanService>().MaxLevelLimit;
            return value;
        }
    }
}
