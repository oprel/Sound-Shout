using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;

public class PlaySound : MonoBehaviour
{
    [SerializeField] protected bool overrideAudioDistance;
    [SerializeField] private Vector2 audioDistanceMinMax = new Vector2(0, 10);
    private readonly List<EventInstance> looping3DInstancesOnObjectList = new List<EventInstance>(); 
    
    private void OnDisable()
    {
        StopLoopingSounds();
    }

    private void OnValidate()
    {
        if(audioDistanceMinMax.y < audioDistanceMinMax.x)
        {
            audioDistanceMinMax.y = audioDistanceMinMax.x;
        }
    }
    
    public void PlayOneShot(AudioReference audioReferenceToPlay)
    {
        if (audioReferenceToPlay.is3D)
        {
            Debug.LogError($"Tried to play 2D OneShot {audioReferenceToPlay.fmodName} is marked as 3D, playing OneShot3D instead.");
            PlayOneShot3D(audioReferenceToPlay);    
        }
        else
        {
            AudioReferenceHandler.PlayOneShot(audioReferenceToPlay);
        }
    }

    public void PlayOneShot3D(AudioReference audioReferenceToPlay)
    {
        if (!overrideAudioDistance)
        {
            AudioReferenceHandler.PlayOneShot3D(audioReferenceToPlay, transform.position);
        }
        else
        {
            AudioReferenceHandler.PlayOneShot3D(audioReferenceToPlay, transform.position, audioDistanceMinMax);
        }
    }

    public void PlayLoop(AudioReference audioReferenceToPlay)
    {
        if (!AudioReferenceHandler.DoesEventExistInFmod(audioReferenceToPlay.fmodName))
        {
            Debug.Log($"AudioManger: Can't play 3D audio: \"{audioReferenceToPlay.fmodName}\". No FMOD Event exist", audioReferenceToPlay);
            return;
        }

        if (audioReferenceToPlay.is3D)
        {
            PlayLoop3D(audioReferenceToPlay);
        }
        else
        {
            // If the audio will be played in 3D, aka, on the object itself. Then we need to manually 
            EventInstance instance = AudioReferenceHandler.CreateEventInstance(audioReferenceToPlay);

            looping3DInstancesOnObjectList.Add(instance);
            instance.start();
            instance.release();
        }
    }

    public void PlayLoop3D(AudioReference audioReferenceToPlay)
    {
        if (!AudioReferenceHandler.DoesEventExistInFmod(audioReferenceToPlay.fmodName))
        {
            Debug.Log($"AudioManger: Can't play 3D audio: \"{audioReferenceToPlay.fmodName}\". No FMOD Event exist", audioReferenceToPlay);
            return;
        }    
        
        // If the audio will be played in 3D, aka, on the object itself. Then we need to manually 
        EventInstance instance = AudioReferenceHandler.CreateEventInstance(audioReferenceToPlay);
        instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
        
        if (overrideAudioDistance)
        {
            if (overrideAudioDistance)
            {
                instance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, audioDistanceMinMax.x);
                instance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, audioDistanceMinMax.y);
            }
        }

        looping3DInstancesOnObjectList.Add(instance);
        instance.start();
        instance.release();
    }

    private void LateUpdate()
    {
        for (int i = 0; i < looping3DInstancesOnObjectList.Count; i++)
        {
            looping3DInstancesOnObjectList[i].set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
        }
    }

    public void StopLoopingSounds()
    {
        for (int i = 0; i < looping3DInstancesOnObjectList.Count; i++)
        {
            looping3DInstancesOnObjectList[i].stop(STOP_MODE.ALLOWFADEOUT);
            looping3DInstancesOnObjectList[i].release();
        }

        looping3DInstancesOnObjectList.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (overrideAudioDistance)
        {
            var position = transform.position;
            
            Gizmos.color = new Color(255, 149, 79, 1);
            Gizmos.DrawWireSphere(position, audioDistanceMinMax.x);
            
            Gizmos.color = new Color(255, 238, 0, 1);
            Gizmos.DrawWireSphere(position, audioDistanceMinMax.y);
        }
    }
}
