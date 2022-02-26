using UnityEngine;

[CreateAssetMenu]
public class AudioReference : ScriptableObject
{
    public string fmodName;

    public void PlaySound() => AudioReferenceHandler.PlayOneShot(this);
    public void PlaySound3D(Vector3 worldPos) => AudioReferenceHandler.PlayOneShot3D(this, worldPos);
    
    public override string ToString()
    {
        return fmodName;
    }
    
    [Header("Spreadsheet")] 
    public bool is3D;
    public bool looping;
    
    #region Editor Things
    
#if UNITY_EDITOR
    [TextArea] public string parameters;
    [TextArea] public string description;
    [TextArea] public string feedback;
    
    public Status implementStatus = Status.Todo;
    public enum Status { Delete, Todo, Created, Implemented, Feedback, Iterate, Done };
    
    [HideInInspector] public string eventName;
    [HideInInspector] public string category;
    
    public void UpdateName()
    {
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        assetPath = assetPath.Replace("Assets/Audio/", "");
        assetPath = assetPath.Replace(".asset", "");
        eventName = assetPath;
        category = assetPath.Split('/')[0];

        string finalEventName = "event:/" + assetPath;

        if (fmodName != finalEventName)
        {
            fmodName = finalEventName;

            UnityEditor.Undo.RecordObject(this, "Updated AudioReference name");
        }
        
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void ApplyChanges(bool is3DSound, bool isLooping, string parameters, string description, string feedback, Status status)
    {
        bool saveUpdates = false;
        string changes = null;
        UnityEditor.Undo.RecordObject(this, "Change AudioReference Info");
        if (!is3D.Equals(is3DSound))
        {
            changes += $"3D: {is3D}->{is3DSound} ";
            is3D = is3DSound;
            saveUpdates = true;
        }
        
        if (!looping.Equals(isLooping))
        {
            changes += $"Looping: {looping}->{isLooping} ";
            looping = isLooping;
            saveUpdates = true;
        }
        
        if (this.parameters != parameters)
        {
            changes += $"Parameters: \"{this.parameters}\" -> \"{parameters}\" ";
            this.parameters = parameters;
            saveUpdates = true;
        }
        
        if (this.description != description)
        {
            changes += $"Description: \"{this.description}\" -> \"{description}\" ";
            this.description = description;
            saveUpdates = true;
        }
        
        if (this.feedback != feedback)
        {
            changes += $"Feedback: \"{this.feedback}\" -> \"{feedback}\" ";
            this.feedback = feedback;
            saveUpdates = true;
        }

        if (implementStatus != status)
        {
            changes += $"Status: {implementStatus}->{status} ";
            implementStatus = status;
            saveUpdates = true;
        }

        if (saveUpdates)
        {
            Debug.Log($"AudioReferenceExporter: Updated \"{name}\": {changes}");
        }
    }

    public void SetupVariables(bool is3DSound, bool isLooping, string parameters, string description, string feedback, Status status)
    {
        is3D = is3DSound;
        looping = isLooping;
        this.parameters = parameters;
        this.description = description;
        this.feedback = feedback;
        implementStatus = status;
    }
    
#endif

    #endregion
}
