/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-28
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Falcon.Helpers.UI
{
    public class UIColorGroupOverlay : MonoBehaviour
    {
        /// <summary>
        /// The color that will be applied to all child UI graphics as an overlay.
        /// The original colors will be restored when the component is destroyed.
        /// </summary>
        [ReadOnly, SerializeField]
        private Color _overlayColor = new Color(1f, 1f, 1f, 1f); // Default: opaque white

        [Title("UI Graphics")]
        public List<Graphic> graphics = new List<Graphic>();

        private Dictionary<Graphic, Color> _originalGraphicColors = new();

        private void ChangeColorAllGraphic()
        {
            foreach (var g in graphics)
                if (g) g.color = _overlayColor;
        }

        /// <summary>
        /// Sets the overlay color via code and applies it immediately.
        /// </summary>
        /// <param name="color">The new overlay color.</param>
        public void SetOverlayColor(Color color)
        {
            _overlayColor = color;
            ChangeColorAllGraphic();
        }

        [Button(ButtonSizes.Medium)]
        public void SetOverlayColor()
        {
            ChangeColorAllGraphic();
        }

        [Button(ButtonSizes.Medium)]
        public void Scan()
        {
            graphics.Clear();
            _originalGraphicColors.Clear();

            graphics.AddRange(GetComponentsInChildren<Graphic>(true));

            CacheOriginalColors();
            ChangeColorAllGraphic();
        }

        [Button(ButtonSizes.Medium)]
        public void CacheOriginalColors()
        {
            foreach (var g in graphics)
            {
                if (g && !_originalGraphicColors.ContainsKey(g))
                    _originalGraphicColors[g] = g.color;
            }
        }

        [Button(ButtonSizes.Medium)]
        public void RestoreOriginalColors()
        {
            foreach (var g in graphics)
                if (g && _originalGraphicColors.TryGetValue(g, out var original))
                    g.color = original;
        }
        
        [Button(ButtonSizes.Medium)]
        public void AddCanvasGroup()
        {
            if (!TryGetComponent<CanvasGroup>(out _))
            {
                gameObject.AddComponent<CanvasGroup>();
                Debug.Log("[UIColorGroupOverlay] CanvasGroup added.", this);
            }
            else
            {
                Debug.Log("[UIColorGroupOverlay] This GameObject already has a CanvasGroup.", this);
            }
        }
    }
}