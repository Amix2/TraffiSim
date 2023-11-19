using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIFocusManager : MonoBehaviour
{
    private List<CallbackEventHandler> m_FocusedHandlers = new List<CallbackEventHandler>();

    public bool IsAnyFocused => m_FocusedHandlers.Count > 0;

    public void RegisterCallbacks(CallbackEventHandler handler)
    {
        handler.RegisterCallback<MouseEnterEvent>((e) =>
        {
            if (!m_FocusedHandlers.Contains(handler))
            {
                m_FocusedHandlers.Add(handler);
            }
        });
        handler.RegisterCallback<MouseLeaveEvent>((e) =>
        {
            m_FocusedHandlers.Remove(handler);
        });
    }
}