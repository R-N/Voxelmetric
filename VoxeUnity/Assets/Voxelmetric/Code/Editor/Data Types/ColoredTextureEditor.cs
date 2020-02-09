using UnityEditor;
using UnityEngine;

namespace Voxelmetric.Editor
{
    [CustomPropertyDrawer(typeof(ColoredTexture))]
    public class ColoredTextureEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect texRect = new Rect(position.x, position.y, position.width - 64, EditorGUIUtility.singleLineHeight);
            Rect colorRect = new Rect(position.x + position.width - 60, position.y, 60, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(texRect, property.FindPropertyRelative("texture"), label);
            EditorGUI.PropertyField(colorRect, property.FindPropertyRelative("color"), GUIContent.none);
        }
    }
}