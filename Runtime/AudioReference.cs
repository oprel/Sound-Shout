using System;
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
    
    public void UpdateEventName()
    {
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        if (!IsAssetPlacedInValidFolder(assetPath))
        {
            return;
        }
        
        assetPath = assetPath.Replace("Assets/Audio/", "");
        assetPath = assetPath.Replace(".asset", "");
        
        int lastSlashIndex = assetPath.IndexOf('/');
        string unityAssetFolderPath = assetPath.Substring(0, lastSlashIndex);
        category = unityAssetFolderPath;

        eventName = assetPath;
        
        string finalEventName = "event:/" + assetPath;

        if (fullEventPath != finalEventName)
        {
            fullEventPath = finalEventName;

            UnityEditor.Undo.RecordObject(this, "Updated AudioReference name");
        }
        
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static bool IsAssetPlacedInValidFolder(string assetPath)
    {
        System.IO.DirectoryInfo parentFolder = System.IO.Directory.GetParent(assetPath);
        switch (parentFolder.Name)
        {
            case "Assets":
                throw new NotSupportedException($"AudioReference \"{assetPath}\" is placed outside \"Audio\" folder!");
            case "Audio":
                throw new NotSupportedException($"AudioReference \"{assetPath}\" is placed in root \"Audio\" folder. Please place it inside a sub-folder.");
            default:
                return true;
        }
    }
    
    public void ApplyChanges(bool is3DSound, bool isLooping, string parameters, string description, string feedback, ImplementationStatus implementationStatus)
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

        if (implementImplementationStatus != implementationStatus)
        {
            changes += $"Status: {implementImplementationStatus}->{implementationStatus} ";
            implementImplementationStatus = implementationStatus;
            saveUpdates = true;
        }

        if (saveUpdates)
        {
            Debug.Log($"AudioReferenceExporter: Updated \"{name}\": {changes}");
        }
    }

    public void SetupVariables(bool is3DSound, bool isLooping, string parameters, string description, string feedback, ImplementationStatus implementationStatus)
    {
        is3D = is3DSound;
        looping = isLooping;
        this.parameters = parameters;
        this.description = description;
        this.feedback = feedback;
        implementImplementationStatus = implementationStatus;
    }

#endif
    
    #endregion
}
