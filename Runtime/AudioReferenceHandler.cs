//#define DEBUGGING

using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class AudioReferenceHandler : MonoBehaviour
{
    public static EventInstance CreateEventInstance(AudioReference soundToPlay)
    {
#if DEBUGGING
        Debug.Log($"AudioReferenceHandler: CreateEventInstance {soundToPlay.fmodName}", soundToPlay);
#endif
        return RuntimeManager.CreateInstance(soundToPlay.fmodName);
    }

    public static EventInstance CreateEventInstance(string eventName)
    {
#if DEBUGGING
        Debug.Log($"AudioReferenceHandler: CreateEventInstance {eventName}");
#endif
        return RuntimeManager.CreateInstance(eventName);
    }

    public static EventInstance CreateAndPlayEventInstance(AudioReference soundToPlay)
    {
        var instance = CreateEventInstance(soundToPlay.fmodName);
        instance.start();
        instance.release();
        return instance;
    }
    
    public static bool DoesEventExistInFmod(string eventName)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("AudioReferenceHandler: Something tried playing an AudioReference sound which has a fmod name that is null or empty!");
            return false;
        }
            
        RESULT result = RuntimeManager.StudioSystem.getEvent(eventName, out _);
        if (result != RESULT.OK)
        {
            Debug.LogError($"AudioReferenceHandler: Could not play \"{eventName}\": {result.ToString()}");

            return false;
        }
        return true;
    }
    
    /// <summary>
    /// Create an event AND get a cached parameter 
    /// </summary>
    public static EventInstance CreateEventAndGetParameterID(AudioReference soundToPlay, string parameterName,  out PARAMETER_ID parameterID)
    {
        if (!DoesEventExistInFmod(soundToPlay.fmodName))
        {
            parameterID = new PARAMETER_ID();
            return new EventInstance();
        }
        
        EventInstance instance = CreateEventInstance(soundToPlay);
        parameterID = instance.GetParameterID(parameterName);
        return instance;
    }
    
    public static void PlayOneShotWithParameter(AudioReference soundToPlay, string parameter, float value)
    {
        EventInstance eventInstance = CreateEventInstance(soundToPlay);
        eventInstance.setParameterByName(parameter, value);
        eventInstance.start();
        eventInstance.release();
    }

    public static void PlayOneShot3DWithParameter(AudioReference soundToPlay, string parameter, float value, Vector3 pos)
    {
        PlayOneShot3DWithParameter(soundToPlay.fmodName, parameter, value, pos);
    }

    public static void PlayOneShot3DWithParameter(string soundToPlay, string parameter, float value, Vector3 pos)
    {
        EventInstance eventInstance = CreateEventInstance(soundToPlay);
        eventInstance.setParameterByName(parameter, value);
        eventInstance.set3DAttributes(pos.To3DAttributes());
        eventInstance.start();
        eventInstance.release();
    }

    // -- ONE SHOT --
    public static void PlayOneShot(AudioReference soundToPlay)
    {
        if (!DoesEventExistInFmod(soundToPlay.fmodName)) { return; }
        
        EventInstance instance = CreateEventInstance(soundToPlay);
        instance.start();
        instance.release();
    }

    public static void PlayOneShot3D(AudioReference soundToPlay, Vector3 pos)
    {
        if (!DoesEventExistInFmod(soundToPlay.fmodName)) { return; }
        
        EventInstance instance = CreateEventInstance(soundToPlay);
        instance.set3DAttributes(pos.To3DAttributes());
        instance.start();
        instance.release();
    }
    
    public static void PlayOneShot3D(AudioReference soundToPlay, Vector3 pos, Vector2 audioDistanceOverride)
    {
        if (!DoesEventExistInFmod(soundToPlay.fmodName)) { return; }
        
        EventInstance instance = CreateEventInstance(soundToPlay);
        instance.set3DAttributes(pos.To3DAttributes());
        instance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, audioDistanceOverride.x);
        instance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, audioDistanceOverride.y);
        instance.start();
        instance.release();
    }
}
