using FMODUnity;
using UnityEngine;

public class AmbienceEmitter : MonoBehaviour
{
    // Is found by the AudioReferenceHandler on scene load
    public AudioReference ambienceAudio;
    public EventReference snapshotEffect;

    public Parameter[] parameters;
    [System.Serializable]
    public class Parameter
    {
        public string parameterName;
        public float parameterValue;
    }
}