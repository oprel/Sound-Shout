using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioReference))]
public class AudioReferenceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        AudioReference myScript = (AudioReference)target;
        if(GUILayout.Button("Update FMOD Name"))
        {
            myScript.UpdateName();
        }
    }
}
