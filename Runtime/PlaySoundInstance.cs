
public class PlaySoundInstance : PlaySound
{
    public AudioReference soundToPlay;

    private void OnEnable()
    {
        PlaySound();
    }

    public void PlaySound()
    {
        if (soundToPlay.is3D)
        {
            if (soundToPlay.looping)
            {
                PlayLoop3D(soundToPlay);
            }
            else
            {
                PlayOneShot3D(soundToPlay);
            }
        }
        else
        {
            if (soundToPlay.looping)
            {
                PlayLoop(soundToPlay);
            }
            else
            {
                PlayOneShot(soundToPlay);
            }
        }
    }
}
