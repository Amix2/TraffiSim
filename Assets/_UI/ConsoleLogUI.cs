using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ConsoleLogUI : MonoBehaviour
{
    public UIFocusManager m_FocusManager;

    static List<string> m_Messages = new List<string>();
    static int m_MaxMsgCount = 100;

    static public void Add(string message)
    {
        m_Messages.Insert(0, "[" + DateTime.Now.ToString("hh:mm:ss:fff") + "] " +  message);
        if(m_Messages.Count > m_MaxMsgCount)
            m_Messages.RemoveRange(m_MaxMsgCount, m_Messages.Count - m_MaxMsgCount);
    }

    ListView m_LogView;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        m_LogView = root.Q<ListView>("ConsoleLog");
        Unity.Assertions.Assert.IsNotNull(m_LogView);

        m_LogView.makeItem = () => new Label();
        m_LogView.bindItem = (e, i) => (e as Label).text = m_Messages[i];
        m_LogView.itemsSource = m_Messages;
        m_FocusManager.RegisterCallbacks(m_LogView);
    }

    void Update()
    {
        m_LogView.Rebuild();
    }
}
