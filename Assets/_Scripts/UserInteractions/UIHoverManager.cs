using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIHoverManager : MonoBehaviour
{
    private readonly HashSet<VisualElement> m_Hovered = new();

    public bool IsPointerOverUI => m_Hovered.Count > 0;

    public void Register(VisualElement element)
    {
        element.RegisterCallback<PointerEnterEvent>(OnEnter);
        element.RegisterCallback<PointerLeaveEvent>(OnLeave);
        // Safety net: PointerLeave may not fire if the element is hidden,
        // disabled, or removed while hovered — so drop it on detach.
        element.RegisterCallback<DetachFromPanelEvent>(OnDetach);
    }

    public void Unregister(VisualElement element)
    {
        element.UnregisterCallback<PointerEnterEvent>(OnEnter);
        element.UnregisterCallback<PointerLeaveEvent>(OnLeave);
        element.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
        m_Hovered.Remove(element);
    }

    private void OnEnter(PointerEnterEvent e) => m_Hovered.Add((VisualElement)e.currentTarget);
    private void OnLeave(PointerLeaveEvent e) => m_Hovered.Remove((VisualElement)e.currentTarget);
    private void OnDetach(DetachFromPanelEvent e) => m_Hovered.Remove((VisualElement)e.currentTarget);
}