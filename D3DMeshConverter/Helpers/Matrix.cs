/*
 * Copyright (c) 2015 Stefan Wijnker
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE.
*/

using System;

namespace JPAssetReader {
    public struct Matrix {
        float m00, m01, m02, m03;
        float m10, m11, m12, m13;
        float m20, m21, m22, m23;
        float m30, m31, m32, m33;

        public static Matrix operator *(Matrix matrix, Matrix other) {
            Matrix m = new Matrix();
            m.m00 = matrix.m00 * other.m00 + matrix.m01 * other.m10 + matrix.m02 * other.m20 + matrix.m03 * other.m30;
            m.m01 = matrix.m00 * other.m01 + matrix.m01 * other.m11 + matrix.m02 * other.m21 + matrix.m03 * other.m31;
            m.m02 = matrix.m00 * other.m02 + matrix.m01 * other.m12 + matrix.m02 * other.m22 + matrix.m03 * other.m32;
            m.m03 = matrix.m00 * other.m03 + matrix.m01 * other.m13 + matrix.m02 * other.m23 + matrix.m03 * other.m33;

            m.m10 = matrix.m10 * other.m00 + matrix.m11 * other.m10 + matrix.m12 * other.m20 + matrix.m13 * other.m30;
            m.m11 = matrix.m10 * other.m01 + matrix.m11 * other.m11 + matrix.m12 * other.m21 + matrix.m13 * other.m31;
            m.m12 = matrix.m10 * other.m02 + matrix.m11 * other.m12 + matrix.m12 * other.m22 + matrix.m13 * other.m32;
            m.m13 = matrix.m10 * other.m03 + matrix.m11 * other.m13 + matrix.m12 * other.m23 + matrix.m13 * other.m33;

            m.m20 = matrix.m20 * other.m00 + matrix.m21 * other.m10 + matrix.m22 * other.m20 + matrix.m23 * other.m30;
            m.m21 = matrix.m20 * other.m01 + matrix.m21 * other.m11 + matrix.m22 * other.m21 + matrix.m23 * other.m31;
            m.m22 = matrix.m20 * other.m02 + matrix.m21 * other.m12 + matrix.m22 * other.m22 + matrix.m23 * other.m32;
            m.m23 = matrix.m20 * other.m03 + matrix.m21 * other.m13 + matrix.m22 * other.m23 + matrix.m23 * other.m33;

            m.m30 = matrix.m30 * other.m00 + matrix.m31 * other.m10 + matrix.m32 * other.m20 + matrix.m33 * other.m30;
            m.m31 = matrix.m30 * other.m01 + matrix.m31 * other.m11 + matrix.m32 * other.m21 + matrix.m33 * other.m31;
            m.m32 = matrix.m30 * other.m02 + matrix.m31 * other.m12 + matrix.m32 * other.m22 + matrix.m33 * other.m32;
            m.m33 = matrix.m30 * other.m03 + matrix.m31 * other.m13 + matrix.m32 * other.m23 + matrix.m33 * other.m33;

            return m;
        }

        public static Matrix identity {
            get {
                Matrix m = new Matrix();
                m.m01 = m.m02 = m.m03 = m.m10 = m.m12 = m.m13 = m.m20 = m.m21 = m.m23 = m.m30 = m.m31 = m.m32 = 0.0f;
                m.m00 = m.m11 = m.m22 = m.m33 = 1.0f;
                return m;
            }
        }

        // http://en.wikipedia.org/wiki/Determinant
        public float determinant() {
            return
            m30 * m21 * m12 * m03 - m20 * m31 * m12 * m03 -
            m30 * m11 * m22 * m03 + m10 * m31 * m22 * m03 +
            m30 * m11 * m32 * m03 - m10 * m21 * m32 * m03 -
            m30 * m21 * m02 * m13 + m20 * m31 * m02 * m13 +
            m30 * m01 * m22 * m13 - m00 * m31 * m22 * m13 -
            m20 * m01 * m32 * m13 + m00 * m21 * m32 * m13 +
            m30 * m11 * m02 * m23 - m10 * m31 * m02 * m23 -
            m30 * m01 * m12 * m23 + m00 * m31 * m12 * m23 +
            m10 * m01 * m32 * m23 - m00 * m11 * m32 * m23 -
            m20 * m11 * m02 * m33 + m10 * m21 * m02 * m33 +
            m20 * m01 * m12 * m33 - m00 * m21 * m12 * m33 -
            m10 * m01 * m22 * m33 + m00 * m11 * m22 * m33;
        }

        public void Translate(Vector3 position) {
            Matrix r = Matrix.identity;
            r.m03 = position.X;
            r.m13 = position.Y;
            r.m23 = position.Z;

            this *= r;
        }

        public void Scale(Vector3 scale) {
            Matrix r = Matrix.identity;
            r.m00 = scale.X;
            r.m11 = scale.Y;
            r.m22 = scale.Z;

            this *= r;
        }

        public void Rotate(Quaternion q) {
            q.Normalize();
            Matrix r = new Matrix();
            r.m00 = 1.0f - 2.0f * q.Y * q.Y - 2.0f * q.Z * q.Z;
            r.m01 = 2.0f * q.X * q.Y - 2.0f * q.Z * q.W;
            r.m02 = 2.0f * q.X * q.Z + 2.0f * q.Y * q.W;
            r.m03 = 0.0f;

            r.m10 = 2.0f * q.X * q.Y + 2.0f * q.Z * q.W;
            r.m11 = 1.0f - 2.0f * q.X * q.X - 2.0f * q.Z * q.Z;
            r.m12 = 2.0f * q.Y * q.Z - 2.0f * q.X * q.W;
            r.m13 = 0.0f;

            r.m20 = 2.0f * q.X * q.Z - 2.0f * q.Y * q.W;
            r.m21 = 2.0f * q.Y * q.Z + 2.0f * q.X * q.W;
            r.m22 = 1.0f - 2.0f * q.X * q.X - 2.0f * q.Y * q.Y;
            r.m23 = 0.0f;

            r.m30 = 0.0f;
            r.m31 = 0.0f;
            r.m32 = 0.0f;
            r.m33 = 1.0f;

            this *= r;
        }

        public static Vector3 operator *(Matrix m, Vector3 v) {
            return new Vector3(m.m00*v.X+m.m01*v.Y+m.m02*v.Z+m.m03,m.m10*v.X+m.m11*v.Y+m.m12*v.Z+m.m13,m.m20*v.X+m.m21*v.Y+m.m22*v.Z+m.m23);
        }
    }

    /// <summary>
    /// Quaternion class
    /// </summary>
    public struct Quaternion {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public Quaternion(float x, float y, float z, float w) {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Quaternion identity {
            get { return new Quaternion(0, 0, 0, 1); }
        }

        public void Normalize() {
            float n = (float)(1.0f / Math.Sqrt(X * X + Y * Y + Z * Z + W * W));
            X *= n;
            Y *= n;
            Z *= n;
            W *= n;
        }

        public override string ToString() {
            return "( " + X + ", " + Y + ", " + Z + ", " + W + " )";
        }
    }

    /// <summary>
    /// Vector2 class
    /// </summary>
    public class Vector2 {
        public float X;
        public float Y;

        public Vector2(float x, float y) {
            X = x;
            Y = y;
        }

        public static Vector2 zero {
            get {
                return new Vector2(0, 0);
            }
        }

        public override string ToString() {
            return "(" + X + "," + Y + ")";
        }
    }

    /// <summary>
    /// Vector3 class
    /// </summary>
    public class Vector3 {
        public float X = 0;
        public float Y = 0;
        public float Z = 0;

        public Vector3(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 zero {
            get { return new Vector3(0, 0, 0); }
        }

        public static Vector3 one {
            get { return new Vector3(1, 1, 1); }
        }

        public static Vector3 up {
            get { return new Vector3(0, 1, 0); }
        }

        public override string ToString() {
            return "(" + X + "," + Y + "," + Z + ")";
        }
    }
}
