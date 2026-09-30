using System;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Falcon.Shared.Common
{
    public enum FConfigValueType
    {
        Int,
        Float,
        Bool,
        String,
        Curve,
        Ease,
        Vector2,
        Vector3,
        Color,
        Object
    }

    /// <summary>Một cặp key-value trong <see cref="FConfigRuntimeSO"/>.</summary>
    [Serializable]
    public class FConfigEntry
    {
        public string key;
        public FConfigValue value = new();
    }

    /// <summary>
    /// Một giá trị config đa kiểu: chọn <see cref="type"/> trong inspector,
    /// chỉ field tương ứng được hiển thị (xem FConfigEntryDrawer).
    /// </summary>
    [Serializable]
    public class FConfigValue
    {
        public FConfigValueType type = FConfigValueType.Float;
        public int intValue;
        public float floatValue;
        public bool boolValue;
        public string stringValue;
        public AnimationCurve curveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public Ease easeValue = Ease.Linear;
        public Vector2 vector2Value;
        public Vector3 vector3Value;
        public Color colorValue = Color.white;
        public Object objectValue;

        /// <summary>Tên field ứng với mỗi type, dùng cho PropertyDrawer.</summary>
        public static string FieldNameOf(FConfigValueType type) => type switch
        {
            FConfigValueType.Int => nameof(intValue),
            FConfigValueType.Float => nameof(floatValue),
            FConfigValueType.Bool => nameof(boolValue),
            FConfigValueType.String => nameof(stringValue),
            FConfigValueType.Curve => nameof(curveValue),
            FConfigValueType.Ease => nameof(easeValue),
            FConfigValueType.Vector2 => nameof(vector2Value),
            FConfigValueType.Vector3 => nameof(vector3Value),
            FConfigValueType.Color => nameof(colorValue),
            FConfigValueType.Object => nameof(objectValue),
            _ => nameof(floatValue)
        };

        /// <summary>Giá trị đang chọn dưới dạng object (có boxing, chỉ nên dùng lúc setup).</summary>
        public object Value => type switch
        {
            FConfigValueType.Int => intValue,
            FConfigValueType.Float => floatValue,
            FConfigValueType.Bool => boolValue,
            FConfigValueType.String => stringValue,
            FConfigValueType.Curve => curveValue,
            FConfigValueType.Ease => easeValue,
            FConfigValueType.Vector2 => vector2Value,
            FConfigValueType.Vector3 => vector3Value,
            FConfigValueType.Color => colorValue,
            FConfigValueType.Object => objectValue,
            _ => null
        };

        // Int <-> Float cho phép đọc chéo, các kiểu khác phải khớp đúng type.
        public int AsInt(int defaultValue = 0) => type switch
        {
            FConfigValueType.Int => intValue,
            FConfigValueType.Float => Mathf.RoundToInt(floatValue),
            _ => defaultValue
        };

        public float AsFloat(float defaultValue = 0f) => type switch
        {
            FConfigValueType.Float => floatValue,
            FConfigValueType.Int => intValue,
            _ => defaultValue
        };

        public bool AsBool(bool defaultValue = false) =>
            type == FConfigValueType.Bool ? boolValue : defaultValue;

        public string AsString(string defaultValue = null) =>
            type == FConfigValueType.String ? stringValue : defaultValue;

        public AnimationCurve AsCurve(AnimationCurve defaultValue = null) =>
            type == FConfigValueType.Curve ? curveValue : defaultValue;

        public Ease AsEase(Ease defaultValue = Ease.Linear) =>
            type == FConfigValueType.Ease ? easeValue : defaultValue;

        public Vector2 AsVector2(Vector2 defaultValue = default) =>
            type == FConfigValueType.Vector2 ? vector2Value : defaultValue;

        public Vector3 AsVector3(Vector3 defaultValue = default) =>
            type == FConfigValueType.Vector3 ? vector3Value : defaultValue;

        public Color AsColor(Color defaultValue = default) =>
            type == FConfigValueType.Color ? colorValue : defaultValue;

        public T AsObject<T>(T defaultValue = null) where T : Object =>
            type == FConfigValueType.Object && objectValue is T typed ? typed : defaultValue;
    }
}
