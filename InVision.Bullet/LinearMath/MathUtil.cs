﻿/*
 * C# / XNA  port of Bullet (c) 2011 Mark Neale <xexuxjy@hotmail.com>
 *
 * Bullet Continuous Collision Detection and Physics Library
 * Copyright (c) 2003-2008 Erwin Coumans  http://www.bulletphysics.com/
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose, 
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using System;
using System.Diagnostics;
using System.IO;
using InVision.Bullet.Collision.NarrowPhaseCollision;
using InVision.GameMath;

namespace InVision.Bullet.LinearMath
{
    public class MathUtil
    {
        public static void SetBasis(ref Matrix m, ref Vector3 right, ref Vector3 up, ref Vector3 backward)
        {
            m.M11 = right.X;
            m.M12 = right.Y;
            m.M13 = right.Z;

            m.M21 = up.X;
            m.M22 = up.Y;
            m.M23 = up.Z;

            m.M31 = backward.X;
            m.M32 = backward.Y;
            m.M33 = backward.Z;
        }
        
        public static Matrix ScaleBasis(ref Matrix m, ref Vector3 v)
        {
            Matrix m1 = Matrix.Identity;
            //m1.M11 = m.M11 * v.X;
            //m1.M12 = m.M12 * v.Y;
            //m1.M13 = m.M13 * v.Z;

            //m1.M21 = m.M21 * v.X;
            //m1.M22 = m.M22 * v.Y;
            //m1.M23 = m.M23 * v.Z;

            //m1.M31 = m.M31 * v.X;
            //m1.M32 = m.M32 * v.Y;
            //m1.M33 = m.M33 * v.Z;

            m1.M11 = m.M11 * v.X;
            m1.M12 = m.M12 * v.X;
            m1.M13 = m.M13 * v.X;

            m1.M21 = m.M21 * v.Y;
            m1.M22 = m.M22 * v.Y;
            m1.M23 = m.M23 * v.Y;

            m1.M31 = m.M31 * v.Z;
            m1.M32 = m.M32 * v.Z;
            m1.M33 = m.M33 * v.Z;


            //m1.Right = m.Right*v;
            //m1.Up = m.Up * v;
            //m1.Backward = m.Backward * v;
            return m1;

        }

        public static Vector3 InverseTransform(ref Matrix m, ref Vector3 v)
        {
            Vector3 v1 = v - m.Translation;
            Matrix m2 = TransposeBasis(ref m);
            return Vector3.TransformNormal(v1, m2);
        }

        //public static Matrix TransposeTimesBasis(ref Matrix a, ref Matrix b)
        public static Matrix TransposeTimesBasis(ref Matrix mA, ref Matrix mB)

        {
            //Matrix mb1 = Matrix.Identity;
            //BasisMatrix(ref a, ref mb1);
            //mb1 = Matrix.Transpose(mb1);
            //Matrix mb2 = Matrix.Identity;
            //BasisMatrix(ref b, ref mb2);
            //return BulletMatrixMultiply(ref mb1, ref mb2);			
            return new Matrix(
				mA.M11 * mB.M11 + mA.M21 * mB.M21 + mA.M31 * mB.M31,
				mA.M11 * mB.M12 + mA.M21 * mB.M22 + mA.M31 * mB.M32,
				mA.M11 * mB.M13 + mA.M21 * mB.M23 + mA.M31 * mB.M33,
				0,
				mA.M12 * mB.M11 + mA.M22 * mB.M21 + mA.M32 * mB.M31,
				mA.M12 * mB.M12 + mA.M22 * mB.M22 + mA.M32 * mB.M32,
				mA.M12 * mB.M13 + mA.M22 * mB.M23 + mA.M32 * mB.M33,
				0,
				mA.M13 * mB.M11 + mA.M23 * mB.M21 + mA.M33 * mB.M31,
				mA.M13 * mB.M12 + mA.M23 * mB.M22 + mA.M33 * mB.M32,
				mA.M13 * mB.M13 + mA.M23 * mB.M23 + mA.M33 * mB.M33,
				0, 0, 0, 0, 1);

        }

        public static Matrix InverseTimes(Matrix a, Matrix b)
        {
            return InverseTimes(ref a, ref b);
        }

        public static Matrix InverseTimes(ref Matrix a, ref Matrix b)
        {
            Vector3 v = b.Translation - a.Translation;
            Matrix a1 = TransposeTimesBasis(ref a, ref b);
			v = TransposeTransformNormal(ref v, ref a);
            a1.Translation = v;
            return a1;
        }

        public static Matrix TransposeBasis(Matrix m)
        {
            return TransposeBasis(ref m);
        }

        public static Matrix TransposeBasis(ref Matrix m)
        {
            return Matrix.Transpose(BasisMatrix(ref m));
        }

        public static Matrix InverseBasis(Matrix m)
        {
            return InverseBasis(ref m);
        }

        public static Matrix InverseBasis(ref Matrix m)
        {
            Matrix b = BasisMatrix(ref m);
            b = Matrix.Invert(b);
            return b;
        }

        public static float Cofac(ref Matrix m,int r1, int c1, int r2, int c2)
		{
            float a = MatrixComponent(ref m, r1, c1);
            float b = MatrixComponent(ref m, r2, c2);
            float c = MatrixComponent(ref m, r1, c2);
            float d = MatrixComponent(ref m, r2, c1);

			return a * b - c * d;
		}


        public static float FSel(float a, float b, float c)
        {
            // dodgy but seems necessary for rounding issues.
            //return a >= -0.00001 ? b : c;
            return a >= 0 ? b : c;
        }
        
        public static int MaxAxis(ref Vector3 a)
        {
            return a.X < a.Y ? (a.Y < a.Z ? 2 : 1) : (a.X < a.Z ? 2 : 0);
        }

        public static int MaxAxis(Vector4 a)
        {
            return MaxAxis(ref a);
        }

        public static int MaxAxis(ref Vector4 a)
        {
            int maxIndex = -1;
            float maxVal = -BT_LARGE_FLOAT;
            if (a.X > maxVal)
            {
                maxIndex = 0;
                maxVal = a.X;
            }
            if (a.Y > maxVal)
            {
                maxIndex = 1;
                maxVal = a.Y;
            }
            if (a.Z > maxVal)
            {
                maxIndex = 2;
                maxVal = a.Z;
            }
            if (a.W > maxVal)
            {
                maxIndex = 3;
                maxVal = a.W;
            }
            return maxIndex;
        }

        public static int ClosestAxis(ref Vector4 a)
        {
            return MaxAxis(AbsoluteVector4(ref a));
        }


        public static Vector4 AbsoluteVector4(ref Vector4 vec)
        {
            return new Vector4(System.Math.Abs(vec.X), System.Math.Abs(vec.Y), System.Math.Abs(vec.Z), System.Math.Abs(vec.W));
        }

        public static float VectorComponent(Vector3 v, int i)
        {
            return VectorComponent(ref v, i);
        }

        public static float VectorComponent(ref Vector3 v, int i)
        {
            if (i == 0) return v.X;
            if (i == 1) return v.Y;
            if (i == 2) return v.Z;
            Debug.Assert(false);
            return 0.0f;
        }

        public static void VectorComponent(ref Vector3 v, int i, float f)
        {
            if (i == 0)
            {
                v.X = f;
                return;
            }
            if (i == 1)
            {
                v.Y = f;
                return;
            }
            if (i == 2)
            {
                v.Z = f;
                return;
            }
            Debug.Assert(false);
        }

        public static void VectorComponentAddAssign(ref Vector3 v, int i, float f)
        {
            if (i == 0)
            {
                v.X += f;
                return;
            }
            if (i == 1)
            {
                v.Y += f;
                return;
            }
            if (i == 2)
            {
                v.Z += f;
                return;
            }
            Debug.Assert(false);
        }

        public static void VectorComponentMinusAssign(ref Vector3 v, int i, float f)
        {
            if (i == 0)
            {
                v.X -= f;
                return;
            }
            if (i == 1)
            {
                v.Y -= f;
                return;
            }
            if (i == 2)
            {
                v.Z -= f;
                return;
            }
            Debug.Assert(false);
        }

        public static void VectorComponentMultiplyAssign(ref Vector3 v, int i, float f)
        {
            if (i == 0)
            {
                v.X *= f;
                return;
            }
            if (i == 1)
            {
                v.Y *= f;
                return;
            }
            if (i == 2)
            {
                v.Z *= f;
                return;
            }
            Debug.Assert(false);
        }

        public static void VectorComponentDivideAssign(ref Vector3 v, int i, float f)
        {
            if (i == 0)
            {
                v.X /= f;
                return;
            }
            if (i == 1)
            {
                v.Y /= f;
                return;
            }
            if (i == 2)
            {
                v.Z /= f;
                return;
            }
            Debug.Assert(false);
        }


        public static float VectorComponent(Vector4 v, int i)
        {
            return VectorComponent(ref v, i);
        }

        public static float VectorComponent(ref Vector4 v, int i)
        {
            if (i == 0) return v.X;
            if (i == 1) return v.Y;
            if (i == 2) return v.Z;
            if (i == 3) return v.W;
            Debug.Assert(false);
            return 0.0f;
        }

        public static void VectorComponent(ref Vector4 v, int i, float f)
        {
            if (i == 0)
            {
                v.X = f;
                return;
            }
            if (i == 1)
            {
                v.Y = f;
                return;
            }
            if (i == 2)
            {
                v.Z = f;
                return;
            }
            if (i == 3)
            {
                v.W = f;
                return;
            }
            Debug.Assert(false);
        }

        public static Matrix AbsoluteMatrix(Matrix input)
        {
            return AbsoluteMatrix(ref input);
        }

        public static Matrix AbsoluteMatrix(ref Matrix input)
        {
            Matrix output = Matrix.Identity;
            AbsoluteMatrix(ref input, ref output);
            return output;
        }

        public static void AbsoluteMatrix(ref Matrix input, ref Matrix output)
        {
            output.M11 = System.Math.Abs(input.M11);
            output.M12 = System.Math.Abs(input.M12);
            output.M13 = System.Math.Abs(input.M13);
            output.M14 = System.Math.Abs(input.M14);
            output.M21 = System.Math.Abs(input.M21);
            output.M22 = System.Math.Abs(input.M22);
            output.M23 = System.Math.Abs(input.M23);
            output.M24 = System.Math.Abs(input.M24);
            output.M31 = System.Math.Abs(input.M31);
            output.M32 = System.Math.Abs(input.M32);
            output.M33 = System.Math.Abs(input.M33);
            output.M34 = System.Math.Abs(input.M34);
            output.M41 = System.Math.Abs(input.M41);
            output.M42 = System.Math.Abs(input.M42);
            output.M43 = System.Math.Abs(input.M43);
            output.M44 = System.Math.Abs(input.M44);
        }

        public static void AbsoluteVector(ref Vector3 input, ref Vector3 output)
        {
            output.X = System.Math.Abs(input.X);
            output.Y = System.Math.Abs(input.Y);
            output.Z = System.Math.Abs(input.Z);
        }

        public static void RotateVector(ref Vector3 vec, ref Matrix m, ref Vector3 output)
        {
            Quaternion rotation;
            Vector3 scale;
            Vector3 translation;
            m.Decompose(out scale, out rotation, out translation);
            output = Vector3.Transform(vec, rotation);
        }

        public static void TransformAabb(Vector3 halfExtents, float margin, Matrix trans, ref Vector3 aabbMinOut, ref Vector3 aabbMaxOut)
        {
            TransformAabb(ref halfExtents,margin,ref trans,ref aabbMinOut,ref aabbMaxOut);
        }

        public static void TransformAabb(ref Vector3 halfExtents, float margin, ref Matrix trans, ref Vector3 aabbMinOut, ref Vector3 aabbMaxOut)
        {
            Vector3 halfExtentsWithMargin = halfExtents + new Vector3(margin, margin, margin);
            Vector3 center = Vector3.Zero, extent = Vector3.Zero;
            AbsoluteExtents(ref trans, ref halfExtentsWithMargin, ref center, ref extent);
            aabbMinOut = center - extent;
            aabbMaxOut = center + extent;
        }

        public static void TransformAabb(Vector3 localAabbMin, Vector3 localAabbMax, float margin, Matrix trans, ref Vector3 aabbMinOut, ref Vector3 aabbMaxOut)
        {
            TransformAabb(ref localAabbMin, ref localAabbMax, margin, ref trans, ref aabbMinOut, ref aabbMaxOut);
        }

        public static void TransformAabb(ref Vector3 localAabbMin, ref Vector3 localAabbMax, float margin, ref Matrix trans, ref Vector3 aabbMinOut, ref Vector3 aabbMaxOut)
        {
            Debug.Assert(localAabbMin.X <= localAabbMax.X);
            Debug.Assert(localAabbMin.Y <= localAabbMax.Y);
            Debug.Assert(localAabbMin.Z <= localAabbMax.Z);
            Vector3 localHalfExtents = 0.5f * (localAabbMax - localAabbMin);
            localHalfExtents += new Vector3(margin, margin, margin);

            Vector3 localCenter = 0.5f * (localAabbMax + localAabbMin);
            Matrix abs_b = MathUtil.AbsoluteMatrix(MathUtil.BasisMatrix(ref trans));

            Vector3 center = Vector3.Transform(localCenter, trans);

            Vector3 extent = new Vector3(Vector3.Dot(abs_b.Right, localHalfExtents),
                                            Vector3.Dot(abs_b.Up, localHalfExtents),
                                            Vector3.Dot(abs_b.Backward, localHalfExtents));

            aabbMinOut = center - extent;
            aabbMaxOut = center + extent;
        }

        public static void AbsoluteExtents(ref Matrix trans, ref Vector3 vec, ref Vector3 center, ref Vector3 extent)
        {
            Matrix abs_b = Matrix.Identity;
            AbsoluteMatrix(ref trans, ref abs_b);

            center = trans.Translation;
            extent = new Vector3(Vector3.Dot(abs_b.Right, vec),
                                            Vector3.Dot(abs_b.Up, vec),
                                            Vector3.Dot(abs_b.Backward, vec));
        }

        public static void SetMatrixVector(ref Matrix matrix, int row, Vector3 vector)
        {
            SetMatrixVector(ref matrix, row, ref vector);
        }

        public static void SetMatrixVector(ref Matrix matrix, int row, ref Vector3 vector)
        {
            if (row == 0)
            {
                matrix.M11 = vector.X;
                matrix.M12 = vector.Y;
                matrix.M13 = vector.Z;
            }
            else if (row == 1)
            {
                matrix.M21 = vector.X;
                matrix.M22 = vector.Y;
                matrix.M23 = vector.Z;
            }
            else if (row == 2)
            {
                matrix.M31 = vector.X;
                matrix.M32 = vector.Y;
                matrix.M33 = vector.Z;
            }
            else if (row == 4)
            {
                matrix.M41 = vector.X;
                matrix.M42 = vector.Y;
                matrix.M43 = vector.Z;
            }
            else
            {
                Debug.Assert(false);
            }
        }

        public static void AddMatrixVector(ref Matrix matrix, int row, ref Vector3 vector)
        {
            if (row == 0)
            {
                matrix.M11 += vector.X;
                matrix.M12 += vector.Y;
                matrix.M13 += vector.Z;
            }
            else if (row == 1)
            {
                matrix.M21 += vector.X;
                matrix.M22 += vector.Y;
                matrix.M23 += vector.Z;
            }
            else if (row == 2)
            {
                matrix.M31 += vector.X;
                matrix.M32 += vector.Y;
                matrix.M33 += vector.Z;
            }
            else if (row == 3)
            {
                matrix.M41 += vector.X;
                matrix.M42 += vector.Y;
                matrix.M43 += vector.Z;
            }
        }

        public static float Vector3Triple(ref Vector3 a, ref Vector3 b, ref Vector3 c)
        {
            return a.X * (b.Y * c.Z - b.Z * c.Y) +
                a.Y * (b.Z * c.X - b.X * c.Z) +
                a.Z * (b.X * c.Y - b.Y * c.X);
        }
        // FIXME - MAN - make sure this is being called how we'd expect , may need to
        // swap i,j for row/column differences
        
        public static float MatrixComponent(ref Matrix m, int index)
        {
            //int i = index % 4;
            //int j = index / 4;

            int j = index % 4;
            int i = index / 4;
            
            return MatrixComponent(ref m,i,j);
        }
        
        public static float MatrixComponent(ref Matrix m, int row, int column)
        {
            if (row == 0 && column == 0) return m.M11;
            if (row == 0 && column == 1) return m.M12;
            if (row == 0 && column == 2) return m.M13;
            if (row == 0 && column == 3) return m.M14;

            if (row == 1 && column == 0) return m.M21;
            if (row == 1 && column == 1) return m.M22;
            if (row == 1 && column == 2) return m.M23;
            if (row == 1 && column == 3) return m.M24;

            if (row == 2 && column == 0) return m.M31;
            if (row == 2 && column == 1) return m.M32;
            if (row == 2 && column == 2) return m.M33;
            if (row == 2 && column == 3) return m.M34;

            if (row == 3 && column == 0) return m.M41;
            if (row == 3 && column == 1) return m.M42;
            if (row == 3 && column == 2) return m.M43;
            if (row == 3 && column == 3) return m.M44;
            return 0;
        }

        public static void MatrixComponent(ref Matrix m, int row, int column, float val)
        {
            if (row == 0 && column == 0) { m.M11 = val; return; }
            if (row == 0 && column == 1) {m.M12 = val; return;}
            if (row == 0 && column == 2) {m.M13 = val; return;}
            if (row == 0 && column == 3) {m.M14 = val; return;}

            if (row == 1 && column == 0) {m.M21 = val; return;}
            if (row == 1 && column == 1) {m.M22 = val; return;}
            if (row == 1 && column == 2) {m.M23 = val; return;}
            if (row == 1 && column == 3) {m.M24 = val; return;}

            if (row == 2 && column == 0) {m.M31 = val; return;}
            if (row == 2 && column == 1) {m.M32 = val; return;}
            if (row == 2 && column == 2) {m.M33 = val; return;}
            if (row == 2 && column == 3) {m.M34 = val; return;}

            if (row == 3 && column == 0) {m.M41 = val; return;}
            if (row == 3 && column == 1) {m.M42 = val; return;}
            if (row == 3 && column == 2) {m.M43 = val; return;}
            if (row == 3 && column == 3) {m.M44 = val; return;}
        }

        public static Vector3 MatrixColumn(Matrix matrix, int row)
        {
            return MatrixColumn(ref matrix, row);
        }

        public static Vector3 MatrixColumn(ref Matrix matrix, int row)
        {
            Vector3 vectorRow = Vector3.Zero;
            MatrixColumn(ref matrix, row, ref vectorRow);
            return vectorRow;
        }

        public static void MatrixColumn(Matrix matrix, int row, ref Vector3 vectorRow)
        {
            MatrixColumn(ref matrix,row,ref vectorRow);
        }

        public static void MatrixColumn(ref Matrix matrix, int row, ref Vector3 vectorRow)
        {
            if (row == 0)
            {
                vectorRow = new Vector3(matrix.M11,matrix.M12,matrix.M13);
            }
            else if (row == 1)
            {
                vectorRow = new Vector3(matrix.M21, matrix.M22, matrix.M23);
            }
            else if (row == 2)
            {
                vectorRow = new Vector3(matrix.M31, matrix.M32, matrix.M33);
            }
            else if (row == 3)
            {
                vectorRow = new Vector3(matrix.M41, matrix.M42, matrix.M43);
            }
            else
            {
                vectorRow = Vector3.Zero;
            }
        }

        public static Vector3 MatrixRow(ref Matrix matrix, int row)
        {
            Vector3 result = Vector3.Zero;
            MatrixRow(ref matrix, row, ref result);
            return result;
        }

        public static void MatrixRow(ref Matrix matrix, int row, ref Vector3 vectorRow)
        {
            if (row == 0)
            {
                vectorRow = new Vector3(matrix.M11, matrix.M21, matrix.M31);
            }
            else if (row == 1)
            {
                vectorRow = new Vector3(matrix.M12, matrix.M22, matrix.M32);
            }
            else if (row == 2)
            {
                vectorRow = new Vector3(matrix.M13, matrix.M23, matrix.M33);
            }
            else if (row == 3)
            {
                vectorRow = new Vector3(matrix.M41, matrix.M42, matrix.M43);
            }
            else
            {
                vectorRow = Vector3.Zero;
            }
        }



        public static int GetQuantized(float x)
        {
            if (x < 0.0)
            {
                return (int)(x - 0.5);
            }
            return (int)(x + 0.5);
        }

        public static void VectorClampMax(ref Vector3 input, ref Vector3 bounds)
        {
            input.X = System.Math.Min(input.X, bounds.X);
            input.Y = System.Math.Min(input.Y, bounds.Y);
            input.Z = System.Math.Min(input.Z, bounds.Z);
        }


        public static void VectorClampMin(ref Vector3 input, ref Vector3 bounds)
        {
            input.X = System.Math.Max(input.X, bounds.X);
            input.Y = System.Math.Max(input.Y, bounds.Y);
            input.Z = System.Math.Max(input.Z, bounds.Z);
        }

        public static void VectorMin(Vector3 input, ref Vector3 output)
        {
            VectorMin(ref input, ref output);
        }

        public static void VectorMin(ref Vector3 input, ref Vector3 output)
        {
            output.X = System.Math.Min(input.X, output.X);
            output.Y = System.Math.Min(input.Y, output.Y);
            output.Z = System.Math.Min(input.Z, output.Z);
        }

        public static void VectorMin(ref Vector3 input1, ref Vector3 input2 ,ref Vector3 output)
        {
            output.X = System.Math.Min(input1.X, input2.X);
            output.Y = System.Math.Min(input1.Y, input2.Y);
            output.Z = System.Math.Min(input1.Z, input2.Z);
        }

        public static void VectorMax(Vector3 input, ref Vector3 output)
        {
            VectorMax(ref input, ref output);
        }

        public static void VectorMax(ref Vector3 input, ref Vector3 output)
        {
            output.X = System.Math.Max(input.X, output.X);
            output.Y = System.Math.Max(input.Y, output.Y);
            output.Z = System.Math.Max(input.Z, output.Z);
        }

        public static void VectorMax(ref Vector3 input1, ref Vector3 input2 ,ref Vector3 output)
        {
            output.X = System.Math.Max(input1.X, input2.X);
            output.Y = System.Math.Max(input1.Y, input2.Y);
            output.Z = System.Math.Max(input1.Z, input2.Z);
        }

        public static float RecipSqrt(float a)
        {
            return (float)(1 / System.Math.Sqrt(a));
        }

        public static bool CompareFloat(float val1, float val2)
        {
            float holder = System.Math.Abs(val1 - val2);
            return holder <= SIMD_EPSILON;
        }

        public static bool FuzzyZero(float val)
        {
            float holder = System.Math.Abs(val);
            return holder <= SIMD_EPSILON;
        }

        public static uint Select(uint condition, uint valueIfConditionNonZero, uint valueIfConditionZero) 
        {
            // Set testNz to 0xFFFFFFFF if condition is nonzero, 0x00000000 if condition is zero
            // Rely on positive value or'ed with its negative having sign bit on
            // and zero value or'ed with its negative (which is still zero) having sign bit off 
            // Use arithmetic shift right, shifting the sign bit through all 32 bits
            uint testNz = (uint)(((int)condition | -(int)condition) >> 31);
            uint testEqz = ~testNz;
            return ((valueIfConditionNonZero & testNz) | (valueIfConditionZero & testEqz)); 
        }
        public static void BasisMatrix(Matrix matrixIn, ref Matrix matrixOut)
        {
            BasisMatrix(ref matrixIn, ref matrixOut);
        }
        public static void BasisMatrix(ref Matrix matrixIn, ref Matrix matrixOut)
        {
            matrixOut = Matrix.Identity;
            matrixOut.Right = matrixIn.Right;
            matrixOut.Up = matrixIn.Up;
            matrixOut.Backward = matrixIn.Backward;
        }

        public static Matrix BasisMatrix(Matrix matrixIn)
        {
            return BasisMatrix(ref matrixIn);
        }
        public static Matrix BasisMatrix(ref Matrix matrixIn)
        {
            Matrix matrixOut = Matrix.Identity;
            matrixOut.Right = matrixIn.Right;
            matrixOut.Up = matrixIn.Up;
            matrixOut.Backward = matrixIn.Backward;
            return matrixOut;
        }

        public static Quaternion ShortestArcQuat(Vector3 axisInA, Vector3 axisInB)
        {
            return ShortestArcQuat(ref axisInA, ref axisInB);
        }
        public static Quaternion ShortestArcQuat(ref Vector3 axisInA, ref Vector3 axisInB)
        {
            Vector3 c = Vector3.Cross(axisInA,axisInB);
            float d = Vector3.Dot(axisInA,axisInB);

            if (d < -1.0 + SIMD_EPSILON)
            {
                return new Quaternion(0.0f, 1.0f, 0.0f, 0.0f); // just pick any vector
            }

            float s = (float)System.Math.Sqrt((1.0f + d) * 2.0f);
            float rs = 1.0f / s;

            return new Quaternion(c.X * rs, c.Y * rs, c.Z * rs, s * 0.5f);

        }

        public static float QuatAngle(ref Quaternion quat)
        {
            return 2f * (float)System.Math.Acos(quat.W);
		}

        public static Quaternion QuatFurthest(ref Quaternion input1,ref Quaternion input2)
        {
		    Quaternion diff,sum;
		    diff = input1 - input2;
		    sum = input1 + input2;
		    if( Quaternion.Dot(diff,diff) > Quaternion.Dot(sum,sum))
            {
			    return input2;
            }
		    return (-input2);
	    }

        public static Vector3 QuatRotate(Quaternion rotation, Vector3 v)
        {
            return QuatRotate(ref rotation, ref v);
        }

        public static Vector3 QuatRotate(ref Quaternion rotation, ref Vector3 v) 
        {   
            Quaternion q = QuatVectorMultiply(ref rotation,ref v);
            q *= QuaternionInverse(ref rotation);
            return new Vector3(q.X,q.Y,q.Z);
        }


        public static Quaternion QuatVectorMultiply(ref Quaternion q, ref Vector3 w)
        {
            return new Quaternion(q.W * w.X + q.Y * w.Z - q.Z * w.Y,
                                    q.W * w.Y + q.Z * w.X - q.X * w.Z,
                                    q.W * w.Z + q.X * w.Y - q.Y * w.X,
                                    -q.X * w.X - q.Y * w.Y - q.Z * w.Z); 
        }

      /**@brief diagonalizes this matrix by the Jacobi method.
       * @param rot stores the rotation from the coordinate system in which the matrix is diagonal to the original
       * coordinate system, i.e., old_this = rot * new_this * rot^T. 
       * @param threshold See iteration
       * @param iteration The iteration stops when all off-diagonal elements are less than the threshold multiplied 
       * by the sum of the absolute values of the diagonal, or when maxSteps have been executed. 
       * 
       * Note that this matrix is assumed to be symmetric. 
       */
        public static void Diagonalize(ref Matrix inMatrix,ref Matrix rot, float threshold, int maxSteps)
		{
            Debug.Assert(false);
            rot = Matrix.Identity;
            for (int step = maxSteps; step > 0; step--)
            {
                // find off-diagonal element [p][q] with largest magnitude
                int p = 0;
                int q = 1;
                int r = 2;
                float max = System.Math.Abs(inMatrix.M12);
                float v = System.Math.Abs(inMatrix.M13);
                if (v > max)
                {
                   q = 2;
                   r = 1;
                   max = v;
                }
                v = System.Math.Abs(inMatrix.M23);
                if (v > max)
                {
                   p = 1;
                   q = 2;
                   r = 0;
                   max = v;
                }

                float t = threshold * (System.Math.Abs(inMatrix.M11) + System.Math.Abs(inMatrix.M22) + System.Math.Abs(inMatrix.M33));
                if (max <= t)
                {
                   if (max <= SIMD_EPSILON * t)
                   {
	                  return;
                   }
                   step = 1;
                }

                // compute Jacobi rotation J which leads to a zero for element [p][q] 
                float mpq = MathUtil.MatrixComponent(ref inMatrix,p,q);
                float theta = (MathUtil.MatrixComponent(ref inMatrix,q,q)-MathUtil.MatrixComponent(ref inMatrix,p,p)) / (2 * mpq);
                float theta2 = theta * theta;
                float cos;
                float sin;
                if (theta2 * theta2 < 10f / SIMD_EPSILON)
                {
                   t = (theta >= 0f) ? (float)(1f / (theta + System.Math.Sqrt(1 + theta2)))
							                : (float)(1f / (theta - System.Math.Sqrt(1 + theta2)));
                   cos = (float)(1f / System.Math.Sqrt(1 + t * t));
                   sin = cos * t;
                }
                else
                {
                   // approximation for large theta-value, i.e., a nearly diagonal matrix
                   t = 1 / (theta * (2 + 0.5f / theta2));
                   cos = 1 - 0.5f * t * t;
                   sin = cos * t;
                }

                // apply rotation to matrix (this = J^T * this * J)
                MathUtil.MatrixComponent(ref inMatrix,p,q,0f);
                MathUtil.MatrixComponent(ref inMatrix,q,p,0f);
                MathUtil.MatrixComponent(ref inMatrix,p,p,MathUtil.MatrixComponent(ref inMatrix,p,p)-t*mpq);
                MathUtil.MatrixComponent(ref inMatrix,q,q,MathUtil.MatrixComponent(ref inMatrix,q,q)+t*mpq);

                float  mrp = MathUtil.MatrixComponent(ref inMatrix,r,p);
                float  mrq = MathUtil.MatrixComponent(ref inMatrix,r,q);

                MathUtil.MatrixComponent(ref inMatrix,r,p,cos * mrp - sin * mrq);
                MathUtil.MatrixComponent(ref inMatrix,p,r,cos * mrp - sin * mrq);

                MathUtil.MatrixComponent(ref inMatrix,r,q,cos * mrp + sin * mrq);
                MathUtil.MatrixComponent(ref inMatrix,q,r,cos * mrp + sin * mrq);

                // apply rotation to rot (rot = rot * J)
                for (int i = 0; i < 3; i++)
                {
                    float  mrp2 = MathUtil.MatrixComponent(ref rot,i,p);
                    float  mrq2 = MathUtil.MatrixComponent(ref rot,i,q);
                    MathUtil.MatrixComponent(ref rot, i, p, cos * mrp - sin * mrq);
                    MathUtil.MatrixComponent(ref rot, i, q, cos * mrp + sin * mrq);
                }
            }
        }

        public static void	GetSkewSymmetricMatrix(ref Vector3 vecin,ref Vector3 v0,ref Vector3 v1,ref Vector3 v2)
		{
            v0 = new Vector3(0f,-vecin.Z,vecin.Y);
            v1 = new Vector3(vecin.Z,0f,-vecin.X);
            v2 = new Vector3(-vecin.Y,vecin.X,0f);
		}
        public static void SanityCheckVector(Vector3 v)
        {
#if DEBUG
            SanityCheckVector(ref v);
#endif
        }
        public static void ZeroCheckVector(Vector3 v)
        {
#if DEBUG

            ZeroCheckVector(ref v);
#endif        
		}

        public static void ZeroCheckVector(ref Vector3 v)
        {
#if DEBUG

            if(FuzzyZero(v.LengthSquared()))
            {
                int ibreak = 0;
                //Debug.Assert(false);
            }
#endif
        }
        public static void SanityCheckVector(ref Vector3 v)
        {
#if DEBUG

            if (float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z))
            {
                int ibreak = 0;
                Debug.Assert(false);
            }
#endif
        }

        public static void SanityCheckFloat(float f)
        {
#if DEBUG

            Debug.Assert(!float.IsInfinity(f) && !float.IsNaN(f));
#endif

        }

        public static void Vector3FromFloat(ref Vector3 v, float[] fa)
        {
            v.X = fa[0];
            v.Y = fa[1];
            v.Z = fa[2];
        }

		//public static void FloatFromVector3(Vector3 v, float[] fa)
		//{
		//    FloatFromVector3(ref v, fa);
		//}

		//public static void FloatFromVector3(ref Vector3 v, float[] fa)
		//{
		//    fa[0] = v.X;
		//    fa[1] = v.Y;
		//    fa[2] = v.Z;
		//}

		//public static float[] FloatFromVector3(Vector3 v)
		//{
		//    return FloatFromVector3(ref v);
		//}

		//public static float[] FloatFromVector3(ref Vector3 v)
		//{
		//    return new float[] { v.X, v.Y, v.Z };
		//}

        public static bool MatrixToEulerXYZ(ref Matrix mat,ref Vector3 xyz)
        {
	        //	// rot =  cy*cz          -cy*sz           sy
	        //	//        cz*sx*sy+cx*sz  cx*cz-sx*sy*sz -cy*sx
	        //	//       -cx*cz*sy+sx*sz  cz*sx+cx*sy*sz  cx*cy
	        //

            float matElem0 = MathUtil.MatrixComponent(ref mat,0) ;
            float matElem1 = MathUtil.MatrixComponent(ref mat,1) ;
            float matElem2 = MathUtil.MatrixComponent(ref mat,2) ;
            float matElem3 = MathUtil.MatrixComponent(ref mat,4) ;
            float matElem4 = MathUtil.MatrixComponent(ref mat,5) ;
            float matElem5 = MathUtil.MatrixComponent(ref mat,6) ;
            float matElem6 = MathUtil.MatrixComponent(ref mat,8) ;
            float matElem7 = MathUtil.MatrixComponent(ref mat,9) ;
            float matElem8 = MathUtil.MatrixComponent(ref mat,10) ;

            float fi = matElem2;
	        if (fi < 1.0f)
	        {
		        if (fi > -1.0f)
		        {
			        xyz.X = (float)System.Math.Atan2(-matElem5,matElem8);
                    xyz.Y = (float)System.Math.Asin(matElem2);
                    xyz.Z = (float)System.Math.Atan2(-matElem1, matElem0);
			        return true;
		        }
		        else
		        {
			        // WARNING.  Not unique.  XA - ZA = -atan2(r10,r11)
                    xyz.X = (float)-System.Math.Atan2(matElem3, matElem4);
			        xyz.Y = -SIMD_HALF_PI;
			        xyz.Z = 0f;
			        return false;
		        }
	        }
	        else
	        {
		        // WARNING.  Not unique.  XAngle + ZAngle = atan2(r10,r11)
                xyz.X = (float)System.Math.Atan2(matElem3, matElem4);
		        xyz.Y = SIMD_HALF_PI;
		        xyz.Z = 0.0f;
	        }
	        return false;
        }



        public static Vector3 MatrixToEuler(ref Matrix m)
        {
            Vector3 translate;
            Vector3 scale;
            Quaternion rotate;
            m.Decompose(out scale, out rotate, out translate);
            return quaternionToEuler(ref rotate);
        }

        // Taken from Fabian Vikings post at : http://forums.xna.com/forums/p/4574/23763.aspx  
        public static Vector3 quaternionToEuler(ref Quaternion q)
        {
            Vector3 v = Vector3.Zero;

            v.X = (float)System.Math.Atan2
            (
                2 * q.Y * q.W - 2 * q.X * q.Z,
                   1 - 2 * System.Math.Pow(q.Y, 2) - 2 * System.Math.Pow(q.Z, 2)
            );

            v.Z = (float)System.Math.Asin
            (
                2 * q.X * q.Y + 2 * q.Z * q.W
            );

            v.Y = (float)System.Math.Atan2
            (
                2 * q.X * q.W - 2 * q.Y * q.Z,
                1 - 2 * System.Math.Pow(q.X, 2) - 2 * System.Math.Pow(q.Z, 2)
            );

            if (q.X * q.Y + q.Z * q.W == 0.5)
            {
                v.X = (float)(2 * System.Math.Atan2(q.X, q.W));
                v.Y = 0;
            }

            else if (q.X * q.Y + q.Z * q.W == -0.5)
            {
                v.X = (float)(-2 * System.Math.Atan2(q.X, q.W));
                v.Y = 0;
            }

            return v;
        }

        public static Quaternion QuaternionInverse(Quaternion q)
        {
            return QuaternionInverse(ref q);
        }

		public static Quaternion QuaternionInverse(ref Quaternion q)
		{
			return new Quaternion(-q.X, -q.Y, -q.Z, q.W);
		}

        public static Quaternion QuaternionMultiply(Quaternion a, Quaternion b)
        {
            return QuaternionMultiply(ref a, ref b);
        }

        public static Quaternion QuaternionMultiply(ref Quaternion a, ref Quaternion b)
        {
			return a * b;
			//return b * a;
        }

		public static Matrix BulletMatrixMultiply(Matrix m1, Matrix m2)
		{
			return BulletMatrixMultiply(ref m1, ref m2);
		}

		public static Matrix BulletMatrixMultiply(ref Matrix m1, ref Matrix m2)
		{
			return Matrix.Multiply(m1, m2);
		}

        public static Matrix BulletMatrixMultiplyBasis(Matrix m1, Matrix m2)
        {
            return BulletMatrixMultiplyBasis(ref m1, ref m2);
        }

        public static Matrix BulletMatrixMultiplyBasis(ref Matrix m1, ref Matrix m2)
        {
			Matrix mb1 = Matrix.Identity;
			BasisMatrix(ref m1,ref mb1);
			Matrix mb2 = Matrix.Identity;
			BasisMatrix(ref m2, ref mb2);
			return BulletMatrixMultiply(ref mb1, ref mb2);
        }



        public static float NormalizeAngle(float angleInRadians)
        {
            // Need to check this mod operator works with floats...
            angleInRadians = angleInRadians %  SIMD_2_PI;
            if (angleInRadians < -SIMD_PI)
            {
                return angleInRadians + SIMD_2_PI;
            }
            else if (angleInRadians > SIMD_PI)
            {
                return angleInRadians - SIMD_2_PI;
            }
            else
            {
                return angleInRadians;
            }
        }

        public static Vector3 Interpolate3(Vector3 v0, Vector3 v1, float rt)
        {
            return Interpolate3(ref v0, ref v1, rt);
        }


        public static Vector3 Interpolate3(ref Vector3 v0, ref Vector3 v1, float rt)
        {
            Vector3 result = Vector3.Zero;
            float s = 1.0f - rt;
            result.X = s * v0.X + rt * v1.X;
            result.Y = s * v0.Y + rt * v1.Y;
            result.Z = s * v0.Z + rt * v1.Z;

            return result;
        }

		public static Matrix SetEulerZYX(float eulerX, float eulerY, float eulerZ)
		{
			//return Matrix.CreateFromYawPitchRoll(y, x,z);
			// This version tested and compared to c++ version. don't break it.
			// note that the row/column settings are switched from c++
			Matrix m = Matrix.Identity;
			float ci = (float)System.Math.Cos(eulerX);
			float cj = (float)System.Math.Cos(eulerY);
			float ch = (float)System.Math.Cos(eulerZ);
			float si = (float)System.Math.Sin(eulerX);
			float sj = (float)System.Math.Sin(eulerY);
			float sh = (float)System.Math.Sin(eulerZ);
			float cc = ci * ch;
			float cs = ci * sh;
			float sc = si * ch;
			float ss = si * sh;

			Vector3 a = new Vector3(cj * ch, cj * sh, -sj);
			Vector3 b = new Vector3(sj * sc - cs, sj * ss + cc, cj * si);
			Vector3 c = new Vector3(sj * cc + ss, sj * cs - sc, cj * ci);

			m.Right = a;
			m.Up = b;
			m.Backward = c;
			return m;

		}

        public static Vector3 MatrixToVector(Matrix m, Vector3 v)
        {
            return new Vector3(
                Vector3.Dot(new Vector3(m.M11, m.M12, m.M13), v) + m.Translation.X,
                Vector3.Dot(new Vector3(m.M21, m.M22, m.M23), v) + m.Translation.Y,
                Vector3.Dot(new Vector3(m.M31, m.M32, m.M33), v) + m.Translation.Z
                );
        }



		public static Vector3 TransposeTransformNormal(Vector3 v,Matrix m)
		{
			return TransposeTransformNormal(ref v, ref m);
		}

		public static Vector3 TransposeTransformNormal(ref Vector3 v,ref Matrix m)
		{
			Matrix mt = TransposeBasis(ref m);
			return Vector3.TransformNormal(v, mt);
		}

		public static void PrintQuaternion(TextWriter writer, Quaternion q)
		{
			writer.Write(String.Format("{{X:{0:0.00000000} Y:{1:0.00000000} Z:{2:0.00000000} W:{3:0.00000000}}}", q.X, q.Y, q.Z,q.W));
		}

		public static void PrintVector3(TextWriter writer, Vector3 v)
		{
			writer.WriteLine(String.Format("{{X:{0:0.00000000} Y:{1:0.00000000} Z:{2:0.00000000}}}", v.X, v.Y, v.Z));
		}

		public static void PrintVector3(TextWriter writer, String name ,Vector3 v)
		{
			writer.WriteLine(String.Format("[{0}] {{X:{1:0.00000000} Y:{2:0.00000000} Z:{3:0.00000000}}}", name,v.X, v.Y, v.Z));
		}

		public static void PrintVector4(TextWriter writer, Vector4 v)
		{
			writer.WriteLine(String.Format("{{X:{0:0.00000000} Y:{1:0.00000000} Z:{2:0.00000000} W:{3:0.00000000}}}", v.X, v.Y, v.Z,v.W));
		}

		public static void PrintVector4(TextWriter writer, String name, Vector4 v)
		{
			writer.WriteLine(String.Format("[{0}] {{X:{1:0.00000000} Y:{2:0.00000000} Z:{3:0.00000000} W:{4:0.00000000}}}", name, v.X, v.Y, v.Z,v.W));
		}


		public static void PrintMatrix(TextWriter writer, Matrix m)
		{
			PrintMatrix(writer, null, m);
		}

		public static void PrintMatrix(TextWriter writer, String name,Matrix m)
		{
			if (writer != null)
			{
				if (name != null)
				{
					writer.WriteLine(name);
				}
				writer.Write("[Right       ]");
				PrintVector3(writer, m.Right);
				writer.Write("[Up          ]");
				PrintVector3(writer, m.Up);
				writer.Write("[Backward    ]");
				PrintVector3(writer, m.Backward);
				writer.Write("[Translation ]");
				PrintVector3(writer, m.Translation);
			}
		}



		public static void PrintContactPoint(StreamWriter streamWriter, ManifoldPoint mp)
		{
			if (streamWriter != null)
			{
				streamWriter.WriteLine("ContactPoint");
				PrintVector3(streamWriter, "localPointA", mp.m_localPointA);
				PrintVector3(streamWriter, "localPointB", mp.m_localPointB);
				PrintVector3(streamWriter, "posWorldA", mp.m_positionWorldOnA);
				PrintVector3(streamWriter, "posWorldB", mp.m_positionWorldOnB);
				PrintVector3(streamWriter, "normalWorldB", mp.m_normalWorldOnB);
			}
		}

        //public const float SIMD_EPSILON = 0.0000001f;
        public const float SIMD_EPSILON = 1.192092896e-07f;
        public const float SIMDSQRT12 = 0.7071067811865475244008443621048490f;

        public const float BT_LARGE_FLOAT = 1e18f;
        public static Vector3 MAX_VECTOR = new Vector3(BT_LARGE_FLOAT, BT_LARGE_FLOAT, BT_LARGE_FLOAT);
        public static Vector3 MIN_VECTOR = new Vector3(-BT_LARGE_FLOAT, -BT_LARGE_FLOAT, -BT_LARGE_FLOAT);
        public const float SIMD_2_PI = 6.283185307179586232f;
        public const float SIMD_PI = SIMD_2_PI * 0.5f;
        public const float SIMD_HALF_PI = SIMD_PI * 0.5f;
        public const float SIMD_QUARTER_PI = SIMD_PI * 0.25f;

        public const float SIMD_INFINITY = float.MaxValue;
        public const float SIMD_RADS_PER_DEG = (SIMD_2_PI / 360.0f);
        public const float SIMD_DEGS_PER_RAD = (360.0f / SIMD_2_PI);


    }
}
