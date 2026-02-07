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
        private Mat4x4(float[,] m)
        {
            this.m = m;
        }
        public static Mat4x4 Zero=>new Mat4x4(new float[4, 4]);
        // {
        //     get
        //     {
        //         return new Mat4x4(new float[4, 4]);
        //     }
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
        public Mat4x4 Transpose
        {
           get
           {
               Mat4x4 result = new Mat4x4(new float[4, 4]);
               for (int row = 0; row < 4; row++)
               {
                   for (int col = 0; col < 4; col++)
                   {
                       result[row, col] = this[col, row];
                   }
               }

               return result;
           }
        }
        public static Mat4x4 operator *(Mat4x4 m1, Mat4x4 m2)
        {
            Mat4x4 result = new Mat4x4(new float[4, 4]);
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    result[row, col] = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        result[row, col] += m1[row, k] * m2[k, col];
                    }
                }
            }
            return result;
        }
        public static Vec4 operator*(Vec4 v1, Mat4x4 m2)
        {
            //这里new 关键字用在 struct 上时（vec4是struct），主要是为了调用构造函数来初始化值，而不是为了在堆上分配内存。
            Vec4 result = new Vec4(0, 0, 0, 0);
            for (int col = 0; col < 4; col++)
            {
                result[col] = 0;
                for (int k = 0; k < 4; k++)
                {
                    result[col] += v1[k] * m2[k, col];
                }
            }
            return result;
        }
        public static Mat4x4 identity
        {
	        get
	        {
		        return new Mat4x4(new float[4, 4]{
			        {1,0,0,0},
			        {0,1,0,0},
			        {0,0,1,0},
			        {0,0,0,1},
		        });
	        }
        }
        public static Mat4x4 SetTranslate(float x, float y, float z)
		{
			Mat4x4 mt = Mat4x4.identity; // 平移矩阵
			mt[3, 0] = x;
			mt[3, 1] = y;
			mt[3, 2] = z;

			return mt;
		}

		public static Mat4x4 SetRotateX(float xDeg)
		{
			Mat4x4 m = Mat4x4.identity;
			float theta = xDeg * Mathf.Deg2Rad; // 角度转弧度
			m[1, 1] = Mathf.Cos(theta);
			m[1, 2] = Mathf.Sin(theta);
			m[2, 1] = -Mathf.Sin(theta);
			m[2, 2] = Mathf.Cos(theta);

			return m;
		}

		public static Mat4x4 SetRotateY(float yDeg)
		{
			Mat4x4 m = Mat4x4.identity;
			float theta = yDeg * Mathf.Deg2Rad; // 角度转弧度
			m[0, 0] = Mathf.Cos(theta);
			m[0, 2] = -Mathf.Sin(theta);
			m[2, 0] = Mathf.Sin(theta);
			m[2, 2] = Mathf.Cos(theta);

			return m;
		}

		public static Mat4x4 SetRotateZ(float zDeg)
		{
			Mat4x4 m = Mat4x4.identity;
			float theta = zDeg * Mathf.Deg2Rad; // 角度转弧度
			m[0, 0] = Mathf.Cos(theta);
			m[0, 1] = Mathf.Sin(theta);
			m[1, 0] = -Mathf.Sin(theta);
			m[1, 1] = Mathf.Cos(theta);

			return m;
		}

		public static Mat4x4 Scale(float x, float y, float z)
		{
			Mat4x4 m = Mat4x4.identity;
			m[0, 0] = x;
			m[1, 1] = y;
			m[2, 2] = z;

			return m;
		}

		public static Mat4x4 MakeView(Vec3 eye, Vec3 at, Vec3 up)
		{
			Vec3 z = (at - eye).Normalized;
			Vec3 x = Vec3.Cross(up, z).Normalized;
			Vec3 y = Vec3.Cross(z, x);

			Mat4x4 m = Mat4x4.identity;
			m[0, 0] = x.x; m[1, 0] = x.y; m[2, 0] = x.z;
			m[0, 1] = y.x; m[1, 1] = y.y; m[2, 1] = y.z;
			m[0, 2] = z.x; m[1, 2] = z.y; m[2, 2] = z.z;

			m[3, 0] = -Vec3.Dot(x, eye);
			m[3, 1] = -Vec3.Dot(y, eye);
			m[3, 2] = -Vec3.Dot(z, eye);

			return m;
		}

		public static Mat4x4 MakeProject(float fovY, float aspect, float zn, float zf)
		{
			float yScale = Mathf.Cos(fovY / 2);
			float xScale = yScale / aspect;

			Mat4x4 m = Mat4x4.Zero;

			m[0, 0] = xScale;
			m[1, 1] = yScale;
			m[2, 2] = zf / (zf - zn);
			m[2, 3] = 1;
			m[3, 2] = -zn * zf / (zf - zn);

			return m;
		}

		public static Mat4x4 MakeScreen(int w, int h)
		{
			Mat4x4 m = Mat4x4.identity;
			m[0, 0] = w / 2;
			m[3, 0] = w / 2;

			//m[1, 1] = -h / 2;
			m[1, 1] = h / 2; // NOTE: OpenGL在绘制屏幕空间顶点时仍然采用y朝上的坐标系，所以不需要翻转y坐标
			m[3, 1] = h / 2;

			return m;
		}
    }
}

