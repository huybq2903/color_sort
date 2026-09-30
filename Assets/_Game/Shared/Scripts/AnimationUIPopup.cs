/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using DhafinFawwaz.AnimationUILib;
using Falcon.Modules.Core.UI.Runtime;
using UnityEngine;

/// <summary>Cắm AnimationUI vào slot uiAnimation của popup: 1 sequence cho show, 1 cho hide.</summary>
public class AnimationUIPopup : UIAnimation
{
    [SerializeField] private AnimationUI showAnim;
    [SerializeField] private AnimationUI hideAnim;

    // Lấy độ dài thật từ sequence để UIBase tính hidingTime và fade nền cho khớp.
    public override void Init(UIBase parentUIBase)
    {
        isOverrideDuration = true;
        durationShow = showAnim ? showAnim.TotalDuration : 0f;
        durationHide = hideAnim ? hideAnim.TotalDuration : 0f;
    }

    public override void Show(UIBase parentUIBase) => Play(showAnim, hideAnim);

    public override void Hide(UIBase parentUIBase) => Play(hideAnim, showAnim);

    private static void Play(AnimationUI anim, AnimationUI other)
    {
        if (other) other.Stop();
        if (anim) anim.Play();
    }
}
