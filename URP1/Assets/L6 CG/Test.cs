using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using My;
public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Vec3 v = new Vec3(1, 2, 3);
        // print(v.Magitude);
        // Mat4x4 m1 = Mat4x4.Zero;
        // Mat4x4 m2 = Mat4x4.Zero;
        // m1[1,2] = 3;
        // m1[2,1] = 4;
        // m2[1,2] = 5;
        // m2[2,1] = 6;
        // Mat4x4 m3 = Mat4x4.SetTranslate(1,2,3);
        // Mat4x4 m4 = Mat4x4.SetRotateX(90);
        // Vec4 v2 = new Vec4(0,0,1, 1);//点
        // v2 = v2*m4;
        // print(v2);
        // Matrix4x4 mat = Matrix4x4.identity;
        // mat.SetTRS(new Vector3(1, 2, 3), Quaternion.identity,
        //     Vector3.one);
        // print(mat);
     
    }

    // Update is called once per frame
    void Update()
    {
        Vec4 v0=new Vec4(-5,-5,0,1);
        Vec4 v1=new Vec4(0,5,0,1);
        Vec4 v2=new Vec4(5,-5,0,1);
        Mat4x4 mview = Mat4x4.MakeView(new Vec3(0,0,-10),
            new Vec3(0,0,0),new Vec3(0,1,0));
        Mat4x4 mpers  = Mat4x4.MakeProject(90,1920.0f/1080.0f,1,100);
        Mat4x4 msport =Mat4x4.MakeScreen(1920,1080);
        Vec4 v0s = v0*mview*mpers;
        Vec4 v1s = v1*mview*mpers;
        Vec4 v2s = v2*mview*mpers;
        v0s.Project();
        v1s.Project();
        v2s.Project();
        // var points=GetComponent<DrawScreenLine>().points;
        // points.Clear();
        // points.Add(new Vector2(v0s.x,v0s.y));
        // points.Add(new Vector2(v1s.x,v1s.y));
        // points.Add(new Vector2(v2s.x,v2s.y));
        
        print(v0s);
        print(v1s);
        print(v2s);

    }
}
