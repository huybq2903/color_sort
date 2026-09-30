using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Path = DG.Tweening.Plugins.Core.PathCore.Path;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif
namespace Falcon.Shared.Common
{
	public static class ColorExtension
	{
		public static void SetAlpha(this Image image, float value)
		{
			var colorNow = image.color;
			image.color = new Color(colorNow.r, colorNow.g, colorNow.b, value);
		}

		public static TweenerCore<Color, Color, ColorOptions> DoAlpha(this Image image, float value, float duration)
		{
			return image.DOFade(value, duration);
		}

		public static TweenerCore<float, float, FloatOptions> DoAlpha(this CanvasGroup canvasGroup, float value, float duration)
		{
			return canvasGroup.DOFade(value, duration);
		}

		public static Color SetAlpha(this Color color, float value)
		{
			return new Color(color.r, color.g, color.b, value);
		}

		public static string ToHexCode(this Color color)
		{
			return "#" + ColorUtility.ToHtmlStringRGB(color);
		}

		public static Color ToColor(this string color)
		{
			return ColorUtility.TryParseHtmlString(color, out var result) ? result : Color.white;
		}
	}

	public static class ContextExtension
	{
		private static readonly NumberFormatInfo _nfi = new()
		{
			NumberGroupSeparator = ","
		};

		public static string FormatNumber(this int number)
		{
			return number.ToString("N0", _nfi);
		}

		public static string FormatNumber(this ObscuredInt number)
		{
			return number.ToString("N0", _nfi);
		}

		public static void StartCoroutine(this MonoBehaviour target, ref IEnumerator ie, IEnumerator coroutine)
		{
			if (ie != null) target.StopCoroutine(ie);
			ie = coroutine;
			target.StartCoroutine(ie);
		}

		public static bool Approximately(this float value, float target, double tolerance = 0.05f) =>
			Math.Abs(value - target) <= tolerance;

		public static Action<T> Add<T>(this Action<T> shit, Action<T> action)
		{
			if (null == shit) goto END_ADD_ACTION;

			if (shit.GetInvocationList().Contains(action))
			{
				return shit;
			}

		END_ADD_ACTION:
			shit += action;
			return shit;
		}

		public static Action<T> Remove<T>(this Action<T> shit, Action<T> action)
		{
			if (null == shit) return null;
			if (shit.GetInvocationList().Contains(action))
			{
				shit -= action;
			}

			return shit;
		}


		public static void ReActive(this GameObject gameObject, bool active)
		{
			if (gameObject.activeSelf != active) gameObject.SetActive(active);
		}
	}

	public static class DOTweenExtension
	{
		public static AnimationCurve MakeSmoothCurve(float start, float mid, float end, float midT = 0.5f, float smoothWeight = 0.5f)
		{
			var curve = new AnimationCurve(
				new Keyframe(0f, start),
				new Keyframe(midT, mid),
				new Keyframe(1f, end)
			);
			for (var i = 0; i < curve.length; i++)
				curve.SmoothTangents(i, smoothWeight);
			return curve;
		}

		public static Tween DOFloatCurve(this AnimationCurve curve, float duration, Action<float> setter)
		{
			if (curve == null || setter == null) return null;
			return DOVirtual.Float(0f, 1f, duration, t => setter(curve.Evaluate(t)));
		}

		public static TweenerCore<int, int, NoOptions> DOTextInt(this TMP_Text text, int initialValue, int finalValue, float duration, Func<int, string> convertor)
		{
			return DOTween.To(
				() => initialValue,
				it => text.SetText(convertor(it)),
				finalValue,
				duration
			);
		}

		public static TweenerCore<int, int, NoOptions> DOTextInt(this TMP_Text text, int initialValue, int finalValue, float duration)
		{
			return text.DOTextInt(initialValue, finalValue, duration, ContextExtension.FormatNumber);
		}

		public static float FormatEulerAngle(this float angle)
		{
			if (angle > 180) return 180 - angle;
			if (angle < -180) return -180 - angle;
			return angle;
		}

		public static YieldInstruction WaitForMove(this Tween t)
		{
			return t.SetSpeedBased().SetEase(Ease.Linear).WaitForCompletion();
		}

		private static Vector3[] GetQuadraticBezierPoints(Vector3 start, Vector3 end, Vector3 curveHeight)
		{
			var heightPoint = start + (end - start) / 2 + curveHeight;
			const int pointCount = 21;
			const int denseSamples = 100;
			var samples = new Vector3[denseSamples + 1];
			var cumulative = new float[denseSamples + 1];

			for (var i = 0; i <= denseSamples; i++)
			{
				var t = i / (float)denseSamples;
				var omt = 1f - t;
				samples[i] = omt * omt * start + 2f * omt * t * heightPoint + t * t * end;
				if (i > 0)
				{
					cumulative[i] = cumulative[i - 1] + Vector3.Distance(samples[i - 1], samples[i]);
				}
			}

			var res = new Vector3[pointCount];
			var totalLen = cumulative[denseSamples];
			var step = totalLen / (pointCount - 1);
			var idx = 0;
			for (var i = 0; i < pointCount; i++)
			{
				var target = step * i;
				while (idx < denseSamples && cumulative[idx] < target) idx++;
				if (idx == 0)
				{
					res[i] = samples[0];
				}
				else
				{
					var len0 = cumulative[idx - 1];
					var len1 = cumulative[idx];
					var lerp = Mathf.Approximately(len1, len0) ? 0f : (target - len0) / (len1 - len0);
					res[i] = Vector3.Lerp(samples[idx - 1], samples[idx], lerp);
				}
			}

			res[pointCount - 1] = end;
			return res;
		}

		public static TweenerCore<Vector3, Path, PathOptions> DoLocalMoveBezier(this Transform target,
			Vector3 end, Vector3 curveHeight, float duration, PathType pathType = PathType.Linear)
		{
			var listPath = GetQuadraticBezierPoints(target.localPosition, end, curveHeight);
			return target.DOLocalPath(listPath, duration, pathType);
		}

		public static TweenerCore<Vector3, Path, PathOptions> DoMoveBezier(this Transform target, Vector3 end,
			Vector3 curveHeight, float duration)
		{
			var listPath = GetQuadraticBezierPoints(target.position, end, curveHeight);
			return target.DOPath(listPath, duration, PathType.CatmullRom);
		}

		public static Tweener DoWidthHeight(this Image image, float target, float duration)
		{
			return DOTween.To(() => image.rectTransform.sizeDelta.x,
				f => image.rectTransform.sizeDelta = new Vector2(f, f), target, duration);
		}

		public static Tweener DoWidth(this Image image, float target, float duration)
		{
			return DOTween.To(() => image.rectTransform.sizeDelta.x,
				f => image.rectTransform.sizeDelta = new Vector2(f, image.rectTransform.sizeDelta.y), target, duration);
		}

		public static TweenerCore<float, float, FloatOptions> DoSnapTo(this ScrollRect scrollRect, Transform target, float duration)
		{
			var end = scrollRect.CalculateFocusedScrollPosition((RectTransform)target).y;
			return DOTween.To(() => scrollRect.verticalNormalizedPosition,
					x => scrollRect.verticalNormalizedPosition = x, end, duration)
				.SetTarget(scrollRect)
				.SetLink(scrollRect.gameObject);
		}
	}

	public static class ScrollViewFocusExtensions
	{
		public static Vector2 CalculateFocusedScrollPosition(this ScrollRect scrollView, Vector2 focusPoint)
		{
			Vector2 contentSize = scrollView.content.rect.size;
			Vector2 viewportSize = ((RectTransform)scrollView.content.parent).rect.size;
			Vector2 contentScale = scrollView.content.localScale;

			contentSize.Scale(contentScale);
			focusPoint.Scale(contentScale);

			Vector2 scrollPosition = scrollView.normalizedPosition;
			if (scrollView.horizontal && contentSize.x > viewportSize.x)
				scrollPosition.x =
					Mathf.Clamp01((focusPoint.x - viewportSize.x * 0.5f) / (contentSize.x - viewportSize.x));
			if (scrollView.vertical && contentSize.y > viewportSize.y)
				scrollPosition.y =
					Mathf.Clamp01((focusPoint.y - viewportSize.y * 0.5f) / (contentSize.y - viewportSize.y));

			return scrollPosition;
		}

		/// <summary>Toạ độ tâm item trong hệ toạ độ root, cộng dồn localPosition nên không dính scale của cha.</summary>
		public static Vector2 LocalPointIn(this RectTransform item, RectTransform root)
		{
			Vector2 point = item.rect.center;
			for (Transform t = item; t && t != root; t = t.parent)
				point += (Vector2)t.localPosition;

			return point;
		}

		public static Vector2 CalculateFocusedScrollPosition(this ScrollRect scrollView, RectTransform item)
		{
			// Popup thường mở lúc anim còn đang scale viewport, đo bằng world lúc đó ra toạ độ vô nghĩa.
			Vector2 itemCenterPoint = item.LocalPointIn(scrollView.content);

			Vector2 contentSizeOffset = scrollView.content.rect.size;
			contentSizeOffset.Scale(scrollView.content.pivot);

			return scrollView.CalculateFocusedScrollPosition(itemCenterPoint + contentSizeOffset);
		}
	}

	public static class RandomExtensions
	{
		public static T GetRandom<T>(this IEnumerable<T> _enumerable)
		{
			if (ReferenceEquals(null, _enumerable)) return default;
			var iEnumerable = _enumerable as T[] ?? _enumerable.ToArray();
			var count = iEnumerable.Length;
			return count > 0 ? iEnumerable.ElementAt(UnityEngine.Random.Range(0, count)) : default;
		}

		public static int GetRandomByWeight<T>(this IEnumerable<T> weights)
		{
			if (weights == null)
				throw new ArgumentException("Weights collection cannot be null.");

			if (weights is T[] arrayWeights)
				return arrayWeights.GetRandomByWeight();

			if (weights is IList<T> listWeights)
				return GetRandomByWeightFromList(listWeights);

			return weights.ToArray().GetRandomByWeight();
		}

		public static int GetRandomByWeight<T>(this T[] weights)
		{
			if (weights == null || weights.Length == 0)
				throw new ArgumentException("Weights array cannot be null or empty.");

			var totalWeight = 0f;
			for (var i = 0; i < weights.Length; i++)
			{
				totalWeight += GetNumericWeight(weights[i], i);
			}

			if (totalWeight <= 0f)
				return 0;

			var randomValue = UnityEngine.Random.Range(0f, totalWeight);

			var cumulativeWeight = 0f;
			for (var i = 0; i < weights.Length; i++)
			{
				var numericWeight = GetNumericWeight(weights[i], i);
				cumulativeWeight += numericWeight;
				if (numericWeight > 0f && randomValue <= cumulativeWeight)
				{
					return i;
				}
			}

			throw new InvalidOperationException("This code should never be reached.");
		}

		private static int GetRandomByWeightFromList<T>(IList<T> weights)
		{
			if (weights == null || weights.Count == 0)
				throw new ArgumentException("Weights list cannot be null or empty.");

			var totalWeight = 0f;
			for (var i = 0; i < weights.Count; i++)
			{
				totalWeight += GetNumericWeight(weights[i], i);
			}

			if (totalWeight <= 0f)
				return 0;

			var randomValue = UnityEngine.Random.Range(0f, totalWeight);
			var cumulativeWeight = 0f;
			for (var i = 0; i < weights.Count; i++)
			{
				var numericWeight = GetNumericWeight(weights[i], i);
				cumulativeWeight += numericWeight;
				if (numericWeight > 0f && randomValue <= cumulativeWeight)
				{
					return i;
				}
			}

			throw new InvalidOperationException("This code should never be reached.");
		}

		private static float GetNumericWeight<T>(T weight, int index)
		{
			float numericWeight;
			if (weight is int intValue)
			{
				numericWeight = intValue;
			}
			else if (weight is float floatValue)
			{
				numericWeight = floatValue;
			}
			else if (weight is ObscuredInt obscuredInt)
			{
				numericWeight = obscuredInt;
			}
			else if (weight is ObscuredFloat obscuredFloat)
			{
				numericWeight = obscuredFloat;
			}
			else
			{
				throw new ArgumentException($"Weight at index {index} is not a valid numeric type.");
			}

			if (numericWeight < 0f)
			{
				throw new ArgumentException($"Weight at index {index} cannot be negative.");
			}

			return numericWeight;
		}
	}

#if UNITY_EDITOR
	public class ObscuredIntDrawer : OdinValueDrawer<ObscuredInt>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			// Lấy giá trị hiện tại
			var value = this.ValueEntry.SmartValue;

			// Hiển thị như int
			var newValue = SirenixEditorFields.IntField(label, value);

			// Gán lại nếu có thay đổi
			if (newValue != value)
			{
				this.ValueEntry.SmartValue = newValue;
			}
		}
	}

	public class ObscuredLongDrawer : OdinValueDrawer<ObscuredLong>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			// Lấy giá trị hiện tại
			var value = this.ValueEntry.SmartValue;

			// Hiển thị như long
			var newValue = SirenixEditorFields.LongField(label, value);

			// Gán lại nếu có thay đổi
			if (newValue != value)
			{
				this.ValueEntry.SmartValue = newValue;
			}
		}
	}

	public class ObscuredStringDrawer : OdinValueDrawer<ObscuredString>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			// Lấy giá trị hiện tại
			var value = this.ValueEntry.SmartValue;

			var newValue = SirenixEditorFields.TextField(label, value);

			// Gán lại nếu có thay đổi
			if (newValue != value)
			{
				this.ValueEntry.SmartValue = newValue;
			}
		}
	}

#endif

	public static class LayerExtensions
	{
		public static void SetLayerRecursively(this Transform root, int layer)
		{
			Messenger<OnLayerChanged>.Emit(new OnLayerChanged(root.gameObject.layer, false));
			root.gameObject.layer = layer;
			for (var i = 0; i < root.childCount; i++)
			{
				root.GetChild(i).SetLayerRecursively(layer);
			}
			Messenger<OnLayerChanged>.Emit(new OnLayerChanged(layer, true));
		}

		public static Vector3 GetPositionAcrossCameras(this Transform target, Camera fromCam, Camera toCam)
		{
			if (!target || !fromCam || !toCam) return Vector3.zero;

			var screenPos = fromCam.WorldToScreenPoint(target.position);
			var depth = Vector3.Dot(target.position - toCam.transform.position, toCam.transform.forward);
			return toCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
		}

		public static void SetPositionAcrossCameras(this Transform target, Camera fromCam, Camera toCam)
		{
			if (!target || !fromCam || !toCam) return;

			var screenPos = fromCam.WorldToScreenPoint(target.position);
			var depth = Vector3.Dot(target.position - toCam.transform.position, toCam.transform.forward);
			var worldPos = toCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
			target.position = worldPos;
		}

		public static void SetTransformAcrossCameras(this Transform target, Camera fromCam, Camera toCam,
			bool syncScale = true)
		{
			if (!target || !fromCam || !toCam) return;

			var originalPos = target.position;
			var screenPos = fromCam.WorldToScreenPoint(originalPos);
			var depth = Vector3.Dot(originalPos - toCam.transform.position, toCam.transform.forward);
			var worldPos = toCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
			target.position = worldPos;

			if (!syncScale) return;

			var fromUnitsPerPixel = GetUnitsPerPixel(fromCam, originalPos);
			var toUnitsPerPixel = GetUnitsPerPixel(toCam, worldPos);
			if (toUnitsPerPixel <= 0f) return;

			var scaleFactor = fromUnitsPerPixel / toUnitsPerPixel;
			target.localScale /= scaleFactor;
		}

		private static float GetUnitsPerPixel(Camera cam, Vector3 worldPos)
		{
			if (cam.pixelHeight <= 0) return 0f;

			if (cam.orthographic)
			{
				return 2f * cam.orthographicSize / cam.pixelHeight;
			}

			var depth = Vector3.Dot(worldPos - cam.transform.position, cam.transform.forward);
			if (depth <= 0f) return 0f;

			var frustumHeight = 2f * depth * Mathf.Tan(0.5f * cam.fieldOfView * Mathf.Deg2Rad);
			return frustumHeight / cam.pixelHeight;
		}

		private const string HighlightSortingLayer = "Highlight";

		/// <summary>Nâng một UI element lên trên phần UI còn lại (lớp tối của popup, HUD…) mà không đụng
		/// hierarchy: cấy một nested Canvas rồi bật overrideSorting sang Sorting Layer "Highlight".
		///
		/// Canvas cấy vào được GIỮ LẠI khi tắt — chỉ overrideSorting bị tắt, nên bật/tắt nhiều lần không
		/// sinh rác. Kèm GraphicRaycaster: Graphic đăng ký raycast theo Canvas gần nhất, nên khi element có
		/// canvas riêng mà thiếu raycaster thì nó nổi lên nhưng bấm không ăn — tutorial cần bấm được.
		///
		/// Chỉ dùng cho UI trong Canvas. Object 3D thì cull theo layer, dùng
		/// <see cref="LayerExtensions.SetLayerRecursively"/> sang layer có camera highlight render.</summary>
		public static void SetHighlight(this RectTransform rect, bool active)
		{
			if (!rect) return;

			if (!rect.TryGetComponent<Canvas>(out var canvas))
			{
				if (!active) return; // chưa từng highlight → không cần cấy Canvas chỉ để tắt

				canvas = rect.gameObject.AddComponent<Canvas>();
			}

			if (active && !rect.TryGetComponent<GraphicRaycaster>(out _))
				rect.gameObject.AddComponent<GraphicRaycaster>();

			canvas.overrideSorting = active;
			if (active) canvas.sortingLayerName = HighlightSortingLayer;
		}
	}

	public struct OnLayerChanged
	{
		public readonly int layer;
		public readonly bool add;
		public OnLayerChanged(int layer, bool add)
		{
			this.layer = layer;
			this.add = add;
		}
	}

	public static class DataTempExtensions<T>
	{
		private static readonly Dictionary<string, T> _cache = new();

		public static bool TryGet(string key, out T value)
		{
			return _cache.TryGetValue(key, out value);
		}

		public static T Get(string key)
		{
			return _cache.GetValueOrDefault(key);
		}

		public static void Set(string key, T value)
		{
			_cache[key] = value;
		}

		public static void Remove(string key)
		{
			_cache.Remove(key);
		}
	}

	public static class AnimatorExtensions
	{
		public static float GetClipLength(this Animator animator, string clipName)
		{
			var controller = animator.runtimeAnimatorController;
			if (!controller) return 0f;

			foreach (var clip in controller.animationClips)
			{
				if (clip && clip.name == clipName) return clip.length;
			}

			return 0f;
		}
	}
}
