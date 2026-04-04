using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class EditorCoroutineLooper
{

    private static Dictionary<IEnumerator,MonoBehaviour> m_loopers = new Dictionary<IEnumerator,MonoBehaviour> ();
    private static bool M_Started = false;
    /// <summary>
    /// 开启Loop
    /// </summary>
    /// <param name="mb">脚本</param>
    /// <param name="iterator">方法</param>
    public static void StartLoop(MonoBehaviour mb, IEnumerator iterator)
    {
        if(mb!=null && iterator != null)
        {
            if(!m_loopers.ContainsKey(iterator))
            {
                m_loopers.Add(iterator,mb);
            }
            else
            {
                m_loopers[iterator]=mb;
            }
        }
        if (!M_Started)
        {
            M_Started = true;
            EditorApplication.update += Update;
        }
    }
    private static List<IEnumerator> M_DropItems=new List<IEnumerator>();
    private static void Update()
    {
        if (m_loopers.Count > 0)
        {
			
            var allItems = m_loopers.GetEnumerator();
            while(allItems.MoveNext())
            {
                var item = allItems.Current;
                var mb = item.Value;
                //卸载时丢弃Looper
                if(mb == null)
                {
                    M_DropItems.Add(item.Key);
                    continue;
                }
                //隐藏时别执行Loop
                if(!mb.gameObject.activeInHierarchy)
                {
                    continue;
                }
                //执行Loop，执行完毕也丢弃Looper
                IEnumerator ie = item.Key;
                if(!ie.MoveNext())
                {
                    M_DropItems.Add(item.Key);
                }
            }
            //集中处理丢弃的Looper
            for(int i = 0;i < M_DropItems.Count;i++)
            {
                if(M_DropItems[i] != null)
                {
                    m_loopers.Remove(M_DropItems[i]);
                }
            }
            M_DropItems.Clear();
        }


        if (m_loopers.Count == 0)
        {
            EditorApplication.update -= Update;
            M_Started = false;
        }
    }
}