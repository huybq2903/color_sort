/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-23
*/

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.UI.Level.Runtime
{
    public class LevelNodeUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Image _background;
        [SerializeField] private Button _btnLevelNode;

        public virtual void Setup(LevelNodeModel model, Sprite bgSprite, Action<LevelNodeModel> onClick)
        {
            _levelText.text = $"{model.index}";
            _background.sprite = bgSprite;
            
            _btnLevelNode.onClick.RemoveAllListeners();
            _btnLevelNode.onClick.AddListener(() => onClick.Invoke(model));
        }
    }
}