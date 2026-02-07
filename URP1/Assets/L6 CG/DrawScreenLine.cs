using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DrawScreenLine : MonoBehaviour
{

	public Color rectColor = Color.green;


	public Material rectMat;//画线的材质 不设定系统会用当前材质画线 结果不可控

	// Use this for initialization

	public List<Vector2> points;

	void Start()
	{
		rectMat.hideFlags = HideFlags.HideAndDontSave;

		rectMat.shader.hideFlags = HideFlags.HideAndDontSave;

	}


	void Update()
	{
	}



	void OnPostRender()
	{
		//画线这种操作推荐在OnPostRender（）里进行 而不是直接放在Update，所以需要标志来开启
		DrawTriangle();
	}

	void DrawTriangle()
	{
		if (points.Count >= 3)
		{
			DrawLine(points[0], points[1]);
			DrawLine(points[1], points[2]);
			DrawLine(points[2], points[0]);
		}
		if (points.Count >= 6)
		{
			DrawLine(points[3], points[4]);
			DrawLine(points[4], points[5]);
			DrawLine(points[5], points[3]);
		}

	}

	void DrawLine(Vector2 start, Vector2 end)
	{
		if (!rectMat)
			return;
		GL.PushMatrix();//保存摄像机变换矩阵
		rectMat.SetPass(0);

		GL.LoadPixelMatrix();//设置用屏幕坐标绘图
		GL.Begin(GL.LINES);

		GL.Color(rectColor);//设置方框的边框颜色 边框不透明

		GL.Vertex3(start.x, start.y, 0);
		GL.Vertex3(end.x, end.y, 0);

		GL.End();
		GL.PopMatrix();//恢复摄像机投影矩阵
	}
}