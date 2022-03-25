namespace SoundShout.Editor
{
    using UnityEditor;
    using UnityEngine;
    
    [CustomEditor(typeof(AudioReference))] 
    public class AudioReferenceAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AudioReference myScript = (AudioReference)target;
            if (GUILayout.Button("Update Event Name"))
            {
                myScript.UpdateEventName();
            }
        }
    }
}