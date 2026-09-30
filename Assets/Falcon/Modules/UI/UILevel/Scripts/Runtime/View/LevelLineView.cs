/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-23
*/

using System;
using System.Collections.Generic;
using Falcon.Helpers.UI;
using Falcon.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace Falcon.Modules.UI.Level.Runtime
{
    public class LevelLineView : MonoBehaviour, ILevelLineView
    {
        [SerializeField, Space(10)] private RectTransform _container;
        [SerializeField] private LevelNodeUI _levelNodePrefab;
        [SerializeField] private RectTransform _levelHighlight, _scrollRectContent;
        [SerializeField] private Button _btnPlay;
        [SerializeField] private Image _btnPlayBG;
        [SerializeField] private GameObject _winstreakObj;
        [SerializeField] private TMP_Text _txtWinstreak;
        [SerializeField] private Image _skullImage;
        [SerializeField] private Image _levelLineProgress, _levelLineProgressBG;
        [SerializeField] private Image _winstreakBG, _winstreakIcon;
        
        public ScrollRectEx _scrollView;
        private readonly List<LevelNodeUI> _listLevelNodes = new();
        private RectTransform _currentLevelNode;
        private UILevelConfig _config;

        private void Awake()
        {
            LoadConfig();
            _scrollView.onValueChanged.AddListener(value =>
            {
                UpdateLevelHighlight();
            });
        }

        private void LoadConfig()
        {
            _config = Resources.Load<UILevelConfig>("UILevelConfig");
            _levelLineProgress.sprite = _config.LevelLineProgress;
            _levelLineProgressBG.sprite = _config.LevelLineProgressBG;
            _winstreakBG.sprite = _config.WinstreakBG;
            _winstreakIcon.sprite = _config.WinstreakIcon;
        }

        private void UpdateLevelHighlight()
        {
            if (_currentLevelNode == null) return;
            
            _levelHighlight.sizeDelta = new Vector2(_levelHighlight.sizeDelta.x,
                _currentLevelNode.anchoredPosition.y + _scrollRectContent.sizeDelta.y + _scrollRectContent.anchoredPosition.y);
        }

        public void ResetAllNodes()
        {
            _currentLevelNode = null;
            foreach (var node in _listLevelNodes)
            {
                node.gameObject.SetActive(false);
            }
        }

        public void ShowLevelNode(LevelNodeModel model, Action<LevelNodeModel> onClickCallback)
        {
            var nodeUI = GetLevelNodeUI();

            if (model.isCurrent)
                _currentLevelNode = nodeUI.GetComponent<RectTransform>();

            Sprite bgSprite;
            if (!model.isCurrent && model.difficulty == 0)
            {
                bgSprite = _config.NotReachLevelNodeBG;
            }
            else
            {
                int bgIndex = (model.difficulty < _config.NodeDifficultyBGs.Length) ? model.difficulty : 0;
                bgSprite = _config.NodeDifficultyBGs[bgIndex];
            }

            nodeUI.Setup(model, bgSprite, onClickCallback);

            var animator = nodeUI.GetComponent<Animator>();
            if (!model.isCurrent && animator != null)
                Destroy(animator);

            nodeUI.gameObject.SetActive(true);
        }

        public void UpdateBtnPlay(int difficulty, Action onClick)
        {
            _btnPlay.onClick.RemoveAllListeners();
            _btnPlay.onClick.AddListener(() =>
            {
                AudioManager.PlaySFX("Click");
                onClick();
            });
            
            int bgIndex = (difficulty < _config.BtnPlayDifficultBGs.Length) ? difficulty : 0;
            _btnPlayBG.sprite = _config.BtnPlayDifficultBGs[bgIndex];

            if (difficulty > 0 && difficulty < _config.SkullSprites.Length)
            {
                _skullImage.sprite = _config.SkullSprites[difficulty - 1];
                _skullImage.gameObject.SetActive(true);
            }
            else
            {
                _skullImage.gameObject.SetActive(false);
            }
        }

        public void SetWinstreak(int winstreak)
        {
            if (winstreak < 2)
            {
                _winstreakObj.SetActive(false);
                return;
            }
            
            _txtWinstreak.text = $"{winstreak}";
            _winstreakObj.SetActive(true);
        }

        public void UpdateRemainThings()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRectContent);
            UpdateLevelHighlight();
        }

        private LevelNodeUI GetLevelNodeUI()
        {
            foreach (var node in _listLevelNodes)
            {
                if (!node.isActiveAndEnabled) return node;
            }

            var nodeGo = Instantiate(_levelNodePrefab, _container);
            _listLevelNodes.Add(nodeGo);
            return nodeGo;
        }
    }
}