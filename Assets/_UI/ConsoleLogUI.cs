using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public class ConsoleLogUI : MonoBehaviour
{
    public UIFocusManager m_FocusManager;

    private static List<string> m_Messages = new List<string>();
    private static int m_MaxMsgCount = 5;

    public static void Log(string message)
    {
        Debug.Log(message);
        m_Messages.Insert(0, "[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] " + message);
        if (m_Messages.Count > m_MaxMsgCount)
            m_Messages.RemoveRange(m_MaxMsgCount, m_Messages.Count - m_MaxMsgCount);
    }
    static string ToString<T>(T v) { if (v != null) return v.ToString(); else return "NULL"; }

    public static void Log<T>(T value) { Log(ToString(value)); }
    public static void Log<T1, T2>(T1 v1, T2 v2) { Log(ToString(v1) + "; " + ToString(v2)); }

    public static void Log<T>(T value, string name) { Log(ToString(name) + " : " + ToString(value)); }

    private ListView m_LogView;

    private void Start()
    {
        m_Messages.Clear();
        var root = GetComponent<UIDocument>().rootVisualElement;
        m_LogView = root.Q<ListView>("ConsoleLog");
        Unity.Assertions.Assert.IsNotNull(m_LogView);

        m_LogView.makeItem = () => new Label();
        m_LogView.bindItem = (e, i) => (e as Label).text = m_Messages[i];
        m_LogView.itemsSource = m_Messages;
        m_FocusManager.RegisterCallbacks(m_LogView);
    }

    private void Update()
    {
        m_LogView.Rebuild();
    }
}