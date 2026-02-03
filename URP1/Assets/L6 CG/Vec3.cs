using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My
{
    //希望对象赋值是拷贝元对象内容到目标对象 用struct
    //希望对象赋值是拷贝元对象引用到目标对象 用class
    public struct Vec3
    {
        public float x, y, z;
        
        public Vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public float Magitude => Mathf.Sqrt(x * x + y * y + z * z);

        public static Vec3 operator *(Vec3 v,float k) 
        {
            return new Vec3(v.x * k, v.y * k, v.z * k);
        }   
        public static Vec3 operator *(float k, Vec3 v)
        {
            return v * k;
        }

        public static float Dot(Vec3 v1, Vec3 v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }
        public static Vec3 Cross(Vec3 v1, Vec3 v2)
        {
            return new Vec3(
                v1.y * v2.z - v1.z * v2.y,
                v1.z * v2.x - v1.x * v2.z,
                v1.x * v2.y - v1.y * v2.x
            );
        }
    }
}
