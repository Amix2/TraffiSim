using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public struct Optional<T>
{
    [SerializeField] private bool enabled;
    [SerializeField] private T value;

    public bool Enabled => enabled;

    public T Value => enabled
        ? value
        : throw new System.InvalidOperationException(
            $"Optional<{typeof(T).Name}> has no value (Enabled is false).");

    public Optional(T initialValue)
    {
        enabled = true;
        value = initialValue;
    }

    public static Optional<T> None => new Optional<T>() { enabled = false, value = default };
    public static implicit operator bool(Optional<T> optional) => optional.enabled;
    public static implicit operator Optional<T>(T val) => new Optional<T>(val);
    public static implicit operator T(Optional<T> optional) => optional.Value;  // throws via Value
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Optional<>))]
public class OptionalPropertyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var value = property.FindPropertyRelative("value");
        return EditorGUI.GetPropertyHeight(value);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var value = property.FindPropertyRelative("value");
        var enabled = property.FindPropertyRelative("enabled");

        // Draw the value field (greyed out when disabled)
        EditorGUI.BeginDisabledGroup(!enabled.boolValue);
        EditorGUI.PropertyField(position, value, label, true);
        EditorGUI.EndDisabledGroup();

        // Draw the toggle on the right edge of the label
        var toggleRect = new Rect(
            position.x + EditorGUIUtility.labelWidth - 14f,
            position.y, 16f, EditorGUIUtility.singleLineHeight);
        toggleRect.x = Mathf.Max(toggleRect.x, position.x);
        enabled.boolValue = EditorGUI.Toggle(toggleRect, enabled.boolValue);
    }
}
#endif