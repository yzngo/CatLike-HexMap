using UnityEditor;
using UnityEngine;

namespace HexMap.Scripts.Editor
{
    // 和要绘制的类型联系起来
    [CustomPropertyDrawer(typeof(HexCoordinates))]
    public class HexCoordinatesDrawer : PropertyDrawer
    {
        // 提供了属性绘制的矩形区域，以及属性的值
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            HexCoordinates coordinates = new HexCoordinates(
                property.FindPropertyRelative("x").intValue,
                property.FindPropertyRelative("z").intValue
            );
            // 绘制属性名 Coordinates, 返回调整过的和右边对齐的位置
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            GUI.Label(position, coordinates.ToString());
        }
    }
}