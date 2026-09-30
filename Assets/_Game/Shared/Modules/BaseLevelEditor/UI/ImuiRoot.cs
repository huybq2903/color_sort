/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-25
 */

using Imui.IO.UGUI;
using Imui.Rendering;
using UnityEngine;

namespace Falcon.Shared.BaseLevelEditor
{
    [RequireComponent(typeof(ImuiUnityGUIBackend))]
    public abstract class ImuiRoot : MonoBehaviour
    {
        [SerializeField] private Font defaultFont;

        private ImuiUnityGUIBackend _backend;

        private static int _rootsWithActiveControl;

        private bool _hadActiveControl;

        public static bool AnyControlActive => _rootsWithActiveControl > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _rootsWithActiveControl = 0;

        protected Imui.Core.ImGui Gui { get; private set; }

        private void OnEnable()
        {
            _backend = GetComponent<ImuiUnityGUIBackend>();
            Gui = new Imui.Core.ImGui(_backend, _backend);

            if (defaultFont != null)
                Gui.TextDrawer.LoadFont(defaultFont, defaultFont.fontSize > 0 ? defaultFont.fontSize : 48, ImGlyphRenderMode.Sdf);
        }

        private void Update()
        {
            Gui.BeginFrame();
            OnDrawGui();
            Gui.EndFrame();
            Gui.Render();

            SetActiveControl(Gui.GetActiveControl() != 0);
        }

        private void SetActiveControl(bool active)
        {
            if (active == _hadActiveControl) return;

            _rootsWithActiveControl += active ? 1 : -1;
            _hadActiveControl = active;
        }

        private void OnDisable()
        {
            SetActiveControl(false);

            Gui?.Dispose();
            Gui = null;
        }

        protected abstract void OnDrawGui();
    }
}
