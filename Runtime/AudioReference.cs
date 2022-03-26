﻿using UnityEngine;

[CreateAssetMenu]
public class AudioReference : ScriptableObject
{
    [HideInInspector] public string fullEventPath;
    public bool is3D;
    public bool looping;

    public override string ToString()
    {
        return fullEventPath;
    }

    #region Editor Spredsheet Things
#if UNITY_EDITOR
    
    [Header("Spreadsheet")] 
    [TextArea] public string parameters;
    [TextArea] public string description;
    [TextArea] public string feedback;
    
    public ImplementationStatus implementImplementationStatus = ImplementationStatus.TODO;
    [HideInInspector] public string category;
    [HideInInspector] public string eventName;

    public enum ImplementationStatus { Delete, TODO, Created, Implemented, Feedback, Iterate, Done };
    
#endif
    #endregion
}
