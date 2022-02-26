using FMOD.Studio;

public static class AudioReferenceUtilities
{
    /// <summary>
    /// Get parameter from FMOD event 
    /// </summary>
    public static PARAMETER_ID GetParameterID(this EventInstance instance, string parameterName)
    {
        instance.getDescription(out var adjustZoomSoundDescription);
        adjustZoomSoundDescription.getParameterDescriptionByName(parameterName, out var parameterDescription);
        return parameterDescription.id;
    }
}