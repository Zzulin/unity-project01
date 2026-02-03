using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using My;
public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Vec3 v = new Vec3(1, 2, 3);
        print(v.Magitude);
        Mat4x4 m1 = new Mat4x4(new float[4, 4]);
        Mat4x4 m2 = new Mat4x4(new float[4, 4]);
        m1[1,2] = 3;
        m1[2,1] = 4;
        m2[1,2] = 5;
        m2[2,1] = 6;
        Mat4x4 m3 = m1 + m2;
        print(m3);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
