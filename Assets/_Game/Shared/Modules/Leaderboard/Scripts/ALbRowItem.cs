// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-08-13

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shared.Leaderboard
{
    /// <summary>Phần chung của mọi dòng xếp hạng: hạng, tên, điểm, huy hiệu top.</summary>
    public abstract class ALbRowItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rank, _name, _score;
        [SerializeField] private Image _topIcon;
        [SerializeField] private Image _bg;

        [Tooltip("Huy hiệu theo hạng 1..N. Để trống thì TopIcon luôn ẩn.")]
        [SerializeField] private Sprite[] _topSprites;
        [SerializeField] private Sprite bgOther, bgMe;

        /// <summary>Kiểu data server trả cho dòng này; panel đọc để biết parse ra gì.</summary>
        public abstract System.Type EntryType { get; }

        public void Bind(LbEntry e)
        {
            // Server trả rank âm nghĩa là "ngoài top |rank|"
            if (_rank != null) _rank.text = e.rank < 0 ? $"{-e.rank}+" : e.rank.ToString();
            if (_name != null) _name.text = e.name;
            if (_score != null) _score.text = e.score.ToString();

            // chua gan du sprite thi giu nguyen nen cua prefab
            if (_bg != null && bgMe != null && bgOther != null) _bg.sprite = e.IsMe ? bgMe : bgOther;

            if (_topIcon != null)
            {
                var hasTop = e.rank > 0 && _topSprites != null && e.rank <= _topSprites.Length;
                _topIcon.gameObject.SetActive(hasTop);
                if (hasTop) _topIcon.sprite = _topSprites[e.rank - 1];
            }

            BindExtra(e);
        }

        protected abstract void BindExtra(LbEntry e);
    }
}
