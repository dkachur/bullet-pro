using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(SkinManager))]
public class SkinManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("HELP BUTTONS", MessageType.Info);

        if (GUILayout.Button("Print QUEUE"))
        {
            SkinManager.Instance.PrintQueue();
        }

        if (GUILayout.Button("Print MAP"))
        {
            SkinManager.Instance.PrintMap();
        }
    }
}
