namespace DhafinFawwaz.AnimationUILib
{
    public static class Ease
    {
        public static float InQuint(float x) => x * x * x * x * x;
        public static float OutQuint(float x) => -((1 - x) * (1 - x) * (1 - x) * (1 - x) * (1 - x)) + 1;

        public static float InOutQuint(float x) => x < 0.5
            ? 8 * x * x * x * x * x
            : 1 -
              ((-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2)) / 2;

        public static float InQuart(float x) => x * x * x * x;
        public static float OutQuart(float x) => -((1 - x) * (1 - x) * (1 - x) * (1 - x)) + 1;

        public static float InOutQuart(float x) => x < 0.5
            ? 8 * x * x * x * x
            : 1 -
              ((-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2)) / 2;

        public static float InCubic(float x) => x * x * x;
        public static float OutCubic(float x) => -((1 - x) * (1 - x) * (1 - x)) + 1;

        public static float InOutCubic(float x) => x < 0.5
            ? 4 * x * x * x
            : 1 -
              ((-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2)) / 2;

        public static float InQuad(float x) => x * x;
        public static float OutQuad(float x) => -((1 - x) * (1 - x)) + 1;

        public static float InOutQuad(float x) => x < 0.5
            ? 2 * x * x
            : 1 -
              ((-2 * x + 2) * (-2 * x + 2)) / 2;

        public static float Linear(float x) => x;

        public static float InBack(float x) => 2.70158f * x * x * x - 1.70158f * x * x;

        public static float OutBack(float x) =>
            1 + 2.70158f * (x - 1) * (x - 1) * (x - 1) + 1.70158f * (x - 1) * (x - 1);

        public static float InOutBack(float x)
        {
            const float c2 = 2.5949095f;
            return x < 0.5
                ? (4 * x * x * ((c2 + 1) * 2 * x - c2)) / 2
                : ((2 * x - 2) * (2 * x - 2) * ((c2 + 1) * (2 * x - 2) + c2) + 2) / 2;
        }

        public enum Type
        {
            In,
            Out,
            InOut
        }

        public enum Power
        {
            Linear,
            Quad,
            Cubic,
            Quart,
            Quint,
            Back
        }

        // Func<float,float> is longer and harder to type than Ease.Function
        // Also it make sure the kind of function that gets passed are function that has
        // a return float that is normalized between 0 and 1 (can also overshoot but the main part is between 0 and 1)
        public delegate float Function(float x);

        public static Function GetEase(Type type, Power power)
        {
            if (power == Power.Linear) return Linear;
            if (power == Power.Back)
            {
                if (type == Type.In) return InBack;
                if (type == Type.Out) return OutBack;
                if (type == Type.InOut) return InOutBack;
            }
            else if (power == Power.Quad)
            {
                if (type == Type.In) return InQuad;
                if (type == Type.Out) return OutQuad;
                if (type == Type.InOut) return InOutQuad;
            }
            else if (power == Power.Cubic)
            {
                if (type == Type.In) return InCubic;
                if (type == Type.Out) return OutCubic;
                if (type == Type.InOut) return InOutCubic;
            }
            else if (power == Power.Quart)
            {
                if (type == Type.In) return InQuart;
                if (type == Type.Out) return OutQuart;
                if (type == Type.InOut) return InOutQuart;
            }
            else if (power == Power.Quint)
            {
                if (type == Type.In) return InQuint;
                if (type == Type.Out) return OutQuint;
                if (type == Type.InOut) return InOutQuint;
            }

            return Linear;
        }
    }
}