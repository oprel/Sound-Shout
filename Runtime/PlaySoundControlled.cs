using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class PlaySoundControlled : MonoBehaviour
{
    [SerializeField] private AudioReference soundToPlay;
    [SerializeField] private STOP_MODE stopMode;
    
    [SerializeField] private bool createInstanceOnEnable;
    
    private EventInstance soundInstance;

    private void OnDisable()
    {
        StopSound();
        soundInstance.release();
    }

    private void OnEnable()
    {
        if (createInstanceOnEnable)
            soundInstance = AudioReferenceHandler.CreateEventInstance(soundToPlay);
    }

    public void PlaySound()
    {
        if (soundToPlay.looping)
        {
            Debug.LogError($"PlaySoundControlled does not currently support looping sounds. Will not play {soundToPlay.fmodName}");
            return;
        }
        
        if (!createInstanceOnEnable && !soundInstance.isValid())
            soundInstance = AudioReferenceHandler.CreateEventInstance(soundToPlay);

        if (soundToPlay.is3D)
        {
            soundInstance.set3DAttributes(transform.position.To3DAttributes());
        }
        
        
        soundInstance.start();
    }

    public void StopSound()
    {
        soundInstance.stop(stopMode);
    }
}
