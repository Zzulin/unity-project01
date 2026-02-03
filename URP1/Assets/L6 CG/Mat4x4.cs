using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace My
{
    public struct Mat4x4
    {
        private float[,] m;
        public Mat4x4(float[,] m)
        {
            this.m = m;
        }
        // public Mat4x4(Mat4x4 other)
        // {
        //     this.m = (float[,])other.m.Clone();
        // }
        public float this [int row, int col]
        {
            get
            {
                if (row < 0 || row > 4)
                {
                    throw new System.ArgumentOutOfRangeException("row");
                }

                if (col < 0 || col > 4)
                {
                    throw new System.ArgumentOutOfRangeException("col");
                }
                return m[row, col];
            }
            set
            {
                if (row < 0 || row > 4)
                {
                    throw new System.ArgumentOutOfRangeException("row");
                }

                if (col < 0 || col > 4)
                {
                    throw new System.ArgumentOutOfRangeException("col");
                }
                m[row, col] = value;
            }
        }

        public static Mat4x4 operator +(Mat4x4 m1, Mat4x4 m2)
        {
            Mat4x4 result = new Mat4x4(new float[4, 4]);
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    result[row, col] = m1[row, col] + m2[row, col];
                }
            }
            return result;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    sb.AppendFormat("{0:F2} ", m[row, col]);
                }
                sb.AppendFormat("\r\n");
            }
            return sb.ToString();
        }
    }
}

