/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-03
 */

using UnityEngine.UI;

namespace Game.Shared.Profile
{
    public class UIProfile_Frame : UIProfileElement
    {
        private Image _image;
        
        public override void ActiveItem(int id)
        {
            _image ??= GetComponentInChildren<Image>();
            _image.sprite = ProfileManager.GetFrame(id);
        }
    }
}