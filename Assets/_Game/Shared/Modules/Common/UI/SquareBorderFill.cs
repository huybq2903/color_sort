using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
[AddComponentMenu("UI/Square Border Fill", 12)]
public class SquareBorderFill : MaskableGraphic
{
    public enum FillDirection
    {
        Clockwise = 0,
        CounterClockwise = 1
    }

    public enum StartCorner
    {
        TopLeft = 0,
        TopRight = 1,
        BottomRight = 2,
        BottomLeft = 3
    }

    [Range(0f, 1f)]
    [SerializeField] private float m_FillAmount = 1f;

    [SerializeField] private float m_Thickness = 10f;

    [SerializeField] private FillDirection m_Direction = FillDirection.Clockwise;

    [SerializeField] private StartCorner m_StartCorner = StartCorner.TopLeft;

    [SerializeField] private float m_SquareCornerRadius = 8f;

    [SerializeField] private int m_SquareCornerSegments = 4;

    [SerializeField] private float m_SegmentOverlap = 1f;

    [SerializeField] private bool m_IgnoreTop;

    [SerializeField] private bool m_IgnoreBottom;

    [SerializeField] private bool m_IgnoreLeft;

    [SerializeField] private bool m_IgnoreRight;

    private static readonly List<Vector2> s_WorkPath = new List<Vector2>(64);

    public float fillAmount
    {
        get { return m_FillAmount; }
        set
        {
            var clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(m_FillAmount, clamped)) return;
            m_FillAmount = clamped;
            SetVerticesDirty();
        }
    }

    public float thickness
    {
        get { return m_Thickness; }
        set
        {
            var clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(m_Thickness, clamped)) return;
            m_Thickness = clamped;
            SetVerticesDirty();
        }
    }

    public FillDirection direction
    {
        get { return m_Direction; }
        set
        {
            if (m_Direction == value) return;
            m_Direction = value;
            SetVerticesDirty();
        }
    }

    public StartCorner startCorner
    {
        get { return m_StartCorner; }
        set
        {
            if (m_StartCorner == value) return;
            m_StartCorner = value;
            SetVerticesDirty();
        }
    }

    public float squareCornerRadius
    {
        get { return m_SquareCornerRadius; }
        set
        {
            var clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(m_SquareCornerRadius, clamped)) return;
            m_SquareCornerRadius = clamped;
            SetVerticesDirty();
        }
    }

    public int squareCornerSegments
    {
        get { return m_SquareCornerSegments; }
        set
        {
            var clamped = Mathf.Clamp(value, 1, 32);
            if (m_SquareCornerSegments == clamped) return;
            m_SquareCornerSegments = clamped;
            SetVerticesDirty();
        }
    }

    public float segmentOverlap
    {
        get { return m_SegmentOverlap; }
        set
        {
            var clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(m_SegmentOverlap, clamped)) return;
            m_SegmentOverlap = clamped;
            SetVerticesDirty();
        }
    }

    public bool ignoreTop
    {
        get { return m_IgnoreTop; }
        set { if (m_IgnoreTop == value) return; m_IgnoreTop = value; SetVerticesDirty(); }
    }

    public bool ignoreBottom
    {
        get { return m_IgnoreBottom; }
        set { if (m_IgnoreBottom == value) return; m_IgnoreBottom = value; SetVerticesDirty(); }
    }

    public bool ignoreLeft
    {
        get { return m_IgnoreLeft; }
        set { if (m_IgnoreLeft == value) return; m_IgnoreLeft = value; SetVerticesDirty(); }
    }

    public bool ignoreRight
    {
        get { return m_IgnoreRight; }
        set { if (m_IgnoreRight == value) return; m_IgnoreRight = value; SetVerticesDirty(); }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (m_FillAmount <= 0f || m_Thickness <= 0f)
            return;

        var rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        GenerateSquare(vh, rect);
    }

    private void GenerateSquare(VertexHelper vh, Rect rect)
    {
        BuildSquareBasePath(rect, s_WorkPath);
        if (s_WorkPath.Count < 2)
            return;

        PrepareDirectedPath(rect, s_WorkPath);

        var totalVisibleLength = GetVisibleLength(rect, s_WorkPath);
        if (totalVisibleLength <= 0f)
            return;

        var remaining = totalVisibleLength * Mathf.Clamp01(m_FillAmount);
        for (var i = 0; i < s_WorkPath.Count - 1; i++)
        {
            var a = s_WorkPath[i];
            var b = s_WorkPath[i + 1];

            if (!IsSegmentVisible(rect, a, b))
                continue;

            var len = Vector2.Distance(a, b);
            if (len <= 0.0001f)
                continue;

            if (remaining >= len)
            {
                AddLineSegment(vh, a, b, m_Thickness, m_SegmentOverlap, color);
                remaining -= len;
                continue;
            }

            if (remaining > 0f)
            {
                var t = remaining / len;
                AddLineSegment(vh, a, Vector2.LerpUnclamped(a, b, t), m_Thickness, m_SegmentOverlap, color);
            }

            return;
        }
    }

    private float GetVisibleLength(Rect rect, List<Vector2> path)
    {
        var sum = 0f;
        for (var i = 0; i < path.Count - 1; i++)
        {
            var a = path[i];
            var b = path[i + 1];
            if (!IsSegmentVisible(rect, a, b))
                continue;

            sum += Vector2.Distance(a, b);
        }

        return sum;
    }

    private bool IsSegmentVisible(Rect rect, Vector2 a, Vector2 b)
    {
        var d = b - a;
        var adx = Mathf.Abs(d.x);
        var ady = Mathf.Abs(d.y);
        var mid = (a + b) * 0.5f;
        var cx = rect.center.x;
        var cy = rect.center.y;

        if (adx > ady * 2f)
        {
            if (mid.y >= cy) return !m_IgnoreTop;
            return !m_IgnoreBottom;
        }

        if (ady > adx * 2f)
        {
            if (mid.x >= cx) return !m_IgnoreRight;
            return !m_IgnoreLeft;
        }

        // Corner arc segment: require both connected sides to be visible.
        if (mid.x >= cx && mid.y >= cy) return !m_IgnoreTop && !m_IgnoreRight;
        if (mid.x >= cx && mid.y < cy) return !m_IgnoreBottom && !m_IgnoreRight;
        if (mid.x < cx && mid.y < cy) return !m_IgnoreBottom && !m_IgnoreLeft;
        return !m_IgnoreTop && !m_IgnoreLeft;
    }

    private void BuildSquareBasePath(Rect rect, List<Vector2> output)
    {
        output.Clear();

        var radius = Mathf.Clamp(m_SquareCornerRadius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
        var cornerSegments = Mathf.Clamp(m_SquareCornerSegments, 1, 32);

        if (radius <= 0.001f)
        {
            output.Add(new Vector2(rect.xMin, rect.yMax));
            output.Add(new Vector2(rect.xMax, rect.yMax));
            output.Add(new Vector2(rect.xMax, rect.yMin));
            output.Add(new Vector2(rect.xMin, rect.yMin));
            output.Add(new Vector2(rect.xMin, rect.yMax));
            return;
        }

        var tl = new Vector2(rect.xMin + radius, rect.yMax);
        var tr = new Vector2(rect.xMax - radius, rect.yMax);
        var rb = new Vector2(rect.xMax, rect.yMin + radius);
        var bl = new Vector2(rect.xMin + radius, rect.yMin);
        var lt = new Vector2(rect.xMin, rect.yMax - radius);

        output.Add(tl);
        output.Add(tr);
        AddArcClockwise(output, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 90f, 0f, cornerSegments);
        output.Add(rb);
        AddArcClockwise(output, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 0f, -90f, cornerSegments);
        output.Add(bl);
        AddArcClockwise(output, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, -90f, -180f, cornerSegments);
        output.Add(lt);
        AddArcClockwise(output, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 180f, 90f, cornerSegments);
    }

    private static void AddArcClockwise(List<Vector2> output, Vector2 center, float radius, float startDeg, float endDeg, int segments)
    {
        var step = (startDeg - endDeg) / segments;
        for (var i = 1; i <= segments; i++)
        {
            var deg = startDeg - step * i;
            var rad = deg * Mathf.Deg2Rad;
            output.Add(center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius);
        }
    }

    private void PrepareDirectedPath(Rect rect, List<Vector2> path)
    {
        if (path.Count > 1 && Approximately(path[0], path[path.Count - 1]))
            path.RemoveAt(path.Count - 1);

        if (m_Direction == FillDirection.CounterClockwise)
            path.Reverse();

        var cornerPoint = GetCornerPoint(rect, m_StartCorner);
        var startIndex = 0;
        var bestDist = float.MaxValue;

        for (var i = 0; i < path.Count; i++)
        {
            var d = (path[i] - cornerPoint).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                startIndex = i;
            }
        }

        if (startIndex > 0)
        {
            var temp = new List<Vector2>(path.Count);
            for (var i = 0; i < path.Count; i++)
                temp.Add(path[(startIndex + i) % path.Count]);

            path.Clear();
            path.AddRange(temp);
        }

        path.Add(path[0]);
    }

    private static Vector2 GetCornerPoint(Rect rect, StartCorner corner)
    {
        switch (corner)
        {
            case StartCorner.TopLeft:
                return new Vector2(rect.xMin, rect.yMax);
            case StartCorner.TopRight:
                return new Vector2(rect.xMax, rect.yMax);
            case StartCorner.BottomRight:
                return new Vector2(rect.xMax, rect.yMin);
            case StartCorner.BottomLeft:
                return new Vector2(rect.xMin, rect.yMin);
            default:
                return new Vector2(rect.xMin, rect.yMax);
        }
    }

    private static bool Approximately(Vector2 a, Vector2 b)
    {
        return (a - b).sqrMagnitude <= 0.0001f * 0.0001f;
    }

    private static void AddLineSegment(VertexHelper vh, Vector2 a, Vector2 b, float thickness, float overlap, Color32 color)
    {
        var direction = b - a;
        var length = direction.magnitude;
        if (length <= 0.0001f)
            return;

        direction /= length;

        if (overlap > 0f)
        {
            var extend = direction * overlap;
            a -= extend;
            b += extend;
        }

        var normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);

        var v0 = a - normal;
        var v1 = a + normal;
        var v2 = b + normal;
        var v3 = b - normal;

        AddQuad(vh, v0, v1, v2, v3, color);
    }

    private static void AddQuad(VertexHelper vh, Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, Color32 color)
    {
        var startIndex = vh.currentVertCount;

        vh.AddVert(v0, color, Vector2.zero);
        vh.AddVert(v1, color, Vector2.zero);
        vh.AddVert(v2, color, Vector2.zero);
        vh.AddVert(v3, color, Vector2.zero);

        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }

    public TweenerCore<float,float,FloatOptions> DOFillAmount(float endValue, float duration)
    {
        if (endValue > 1f) endValue = 1f;
        else if (endValue < 0f) endValue = 0f;

        var tween = DOTween.To(() => fillAmount, x => fillAmount = x, endValue, duration);
        tween.SetTarget(this);
        return tween;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        m_FillAmount = Mathf.Clamp01(m_FillAmount);
        m_Thickness = Mathf.Max(0f, m_Thickness);
        m_SquareCornerRadius = Mathf.Max(0f, m_SquareCornerRadius);
        m_SquareCornerSegments = Mathf.Clamp(m_SquareCornerSegments, 1, 32);
        m_SegmentOverlap = Mathf.Max(0f, m_SegmentOverlap);
    }
#endif
}


