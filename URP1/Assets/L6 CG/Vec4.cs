using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace My
{
    public struct Vec4 
    {
        public float x, y, z, w;

        public Vec4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public Vec4(Vec3 v, float w)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
            this.w = w;
        }
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    case 3: return w;
                    default: return 0;
                }
            }
            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    case 3: w = value; break;
                }
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("({0:F2}, {1:F2}, {2:F2}, {3:F2})", x, y, z, w);
            return sb.ToString();
        }

        public void Project()
        {
           x/= w;
           y/= w;
           z/= w;
           w/= w;
        }
        public static implicit operator Vector3(Vec4 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
    }  
}

