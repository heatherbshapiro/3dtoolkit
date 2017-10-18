using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Xamarin_Android.Droid
{
    public static class MatrixMath
    {
        public static float[,] MatCreate()
        {
            var output = new float[4, 4] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 1, 0 }, { 0, 0, 0, 1 } };
            return output;
        }

        public static float[,] MatClone(float[,] original)
        {
            var output = new float[4, 4] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 1, 0 }, { 0, 0, 0, 1 } };
            output[0, 0] = original[0, 0];
            output[0, 1] = original[0, 1];
            output[0, 2] = original[0, 2];
            output[0, 3] = original[0, 3];
            output[1, 0] = original[1, 0];
            output[1, 1] = original[1, 1];
            output[1, 2] = original[1, 2];
            output[1, 3] = original[1, 3];
            output[2, 0] = original[2, 0];
            output[2, 1] = original[2, 1];
            output[2, 2] = original[2, 2];
            output[2, 3] = original[2, 3];
            output[3, 0] = original[3, 0];
            output[3, 1] = original[3, 1];
            output[3, 2] = original[3, 2];
            output[3, 3] = original[3, 3];

            return output;
        }

        public static float[,] MatMultiply(float[,] a, float[,] b)
        {
            var output = new float[4, 4] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 1, 0 }, { 0, 0, 0, 1 } };

            output[0, 0] = b[0, 0] * a[0, 0] + b[0, 1] * a[1, 0] + b[0, 2] * a[2, 0] + b[0, 3] * a[3, 0];
            output[0, 1] = b[0, 0] * a[0, 1] + b[0, 1] * a[1, 1] + b[0, 2] * a[2, 1] + b[0, 3] * a[3, 1];
            output[0, 2] = b[0, 0] * a[0, 2] + b[0, 1] * a[1, 2] + b[0, 2] * a[2, 2] + b[0, 3] * a[3, 2];
            output[0, 3] = b[0, 0] * a[0, 3] + b[0, 1] * a[1, 3] + b[0, 2] * a[2, 3] + b[0, 3] * a[3, 3];

            output[1, 0] = b[1, 0] * a[0, 0] + b[1, 1] * a[1, 0] + b[1, 2] * a[2, 0] + b[1, 3] * a[3, 0];
            output[1, 1] = b[1, 0] * a[0, 1] + b[1, 1] * a[1, 1] + b[1, 2] * a[2, 1] + b[1, 3] * a[3, 1];
            output[1, 2] = b[1, 0] * a[0, 2] + b[1, 1] * a[1, 2] + b[1, 2] * a[2, 2] + b[1, 3] * a[3, 2];
            output[1, 3] = b[1, 0] * a[0, 3] + b[1, 1] * a[1, 3] + b[1, 2] * a[2, 3] + b[1, 3] * a[3, 3];

            output[2, 0] = b[2, 0] * a[0, 0] + b[2, 1] * a[1, 0] + b[2, 2] * a[2, 0] + b[2, 3] * a[3, 0];
            output[2, 1] = b[2, 0] * a[0, 1] + b[2, 1] * a[1, 1] + b[2, 2] * a[2, 1] + b[2, 3] * a[3, 1];
            output[2, 2] = b[2, 0] * a[0, 2] + b[2, 1] * a[1, 2] + b[2, 2] * a[2, 2] + b[2, 3] * a[3, 2];
            output[2, 3] = b[2, 0] * a[0, 3] + b[2, 1] * a[1, 3] + b[2, 2] * a[2, 3] + b[2, 3] * a[3, 3];

            output[3, 0] = b[3, 0] * a[0, 0] + b[3, 1] * a[1, 0] + b[3, 2] * a[2, 0] + b[3, 3] * a[3, 0];
            output[3, 1] = b[3, 0] * a[0, 1] + b[3, 1] * a[1, 1] + b[3, 2] * a[2, 1] + b[3, 3] * a[3, 1];
            output[3, 2] = b[3, 0] * a[0, 2] + b[3, 1] * a[1, 2] + b[3, 2] * a[2, 2] + b[3, 3] * a[3, 2];
            output[3, 3] = b[3, 0] * a[0, 3] + b[3, 1] * a[1, 3] + b[3, 2] * a[2, 3] + b[3, 3] * a[3, 3];

            return output;
        }

        public static float[,] MatTranslate(float[] v)
        {
            var output = new float[4, 4] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 1, 0 }, { v[0], v[1], v[2], 1 } };
            return output;
        }

        public static float[,] MatRotateX(float rad)
        {
            var s = (float)Math.Sin(rad);
            var c = (float)Math.Cos(rad);

            var output = new float[4, 4] { { 1, 0, 0, 0 }, { 0, c, s, 0 }, { 0, -s, c, 0 }, { 0, 0, 0, 1 } };
            return output;
        }

        public static float[,] MatRotateY(float rad)
        {
            var s = (float)Math.Sin(rad);
            var c = (float)Math.Cos(rad);

            var output = new float[4, 4] { { c, 0, -s, 0 }, { 0, 1, 0, 0 }, { s, 0, c, 0 }, { 0, 0, 0, 1 } };
            return output;
        }

        public static float[,] MatRotateZ(float rad)
        {
            var s = (float)Math.Sin(rad);
            var c = (float)Math.Cos(rad);

            var output = new float[4, 4] { { c, s, 0, 0 }, { -s, c, 0, 0 }, { 0, 0, 1, 0 }, { 0, 0, 0, 1 } };
            return output;
        }
    }
}