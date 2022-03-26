using UnityEngine;

[CreateAssetMenu]
public class AudioReference : ScriptableObject
{
    public string fullEventPath;
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
    public string category;
    public string eventName;

    public enum ImplementationStatus { Delete, TODO, Created, Implemented, Feedback, Iterate, Done };
    
#endif
    #endregion
}
