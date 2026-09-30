/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-22
 */

using DhafinFawwaz.AnimationUILib;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Shared.Mediation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Falcon.Shared.Common;

namespace Falcon.InGame.UI
{
    public class UIPopupWin : MonoBehaviour
    {
        [SerializeField] private Button btnSkip, btnAds, btnContinue;
        [SerializeField] private TMP_Text txtLevel, txtGold;

        private AnimationUI _anim;
        private int _rateAdsMultiple, _goldWin;
        private void Awake()
        {
            _anim = GetComponent<AnimationUI>();
            btnSkip.onClick.AddListener(_anim.SkipToEnd);

            btnAds.onClick.AddListener(OnClickAds);
            btnContinue.onClick.AddListener(OnClickNext);
        }

        private void OnClickNext()
        {
            btnAds.interactable = false;
            btnContinue.interactable = false;
            MediationHelpers.Instance.ShowIntersAndRecommendRemoveAds(PlacementAds.WIN_GAME, () =>
            {
                GameEvent.Emit(GameKeys.NEXT_ON_WIN);
            });
        }

        private void OnClickAds()
        {
            btnAds.interactable = false;
            btnContinue.interactable = false;
            MediationHelpers.Instance.ShowRewardedVideo(PlacementAds.WIN_GAME, () =>
            {
                var goldAds = _goldWin * (_rateAdsMultiple - 1);
                ResourceCollector.Instance.ResourceAdd("gold", goldAds, "win", "ads win game");
                ResourceCollector.Instance.SaveAndUpdateServerOfResourceData("gold");
                GameEvent.Emit(GameKeys.NEXT_ON_WIN);
            }, () =>
            {
                btnAds.interactable = true;
                btnContinue.interactable = true;
            });
        }

        public void SetData(int level, int difficulty, bool isCanAds)
        {
            txtLevel.SetText($"Lv. {level}");
            _goldWin = GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, $"goldWin_{difficulty}");
            _rateAdsMultiple = GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, "rate_ads_gold_win");
            txtGold.SetText($"{_goldWin}");
            btnAds.gameObject.SetActive(isCanAds);
        }
    }
}
