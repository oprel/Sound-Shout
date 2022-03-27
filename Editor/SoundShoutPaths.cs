namespace SoundShout.Editor
{
    internal static class SoundShoutPaths
    {
        internal const string ROOT_PATH = "Packages/se.somethingwemade.soundshout/";
        internal const string SETTINGS_ASSET_PATH = ROOT_PATH + "Editor/Settings/Sound Shout Settings.asset";
        
        
        internal const string CLIENT_SECRET_PATH = TOOL_PATH + "/client_secret.json";
        internal const string TOOL_PATH = ROOT_PATH + "/Editor/EditorWindow";


        internal const string TOOL_LOGO_PATH = SoundShoutPaths.TOOL_PATH + "/SS_Tool_Logo.png";
        internal const string AUDIO_REFERENCE_ICON_PATH = SoundShoutPaths.TOOL_PATH + "/SS_Asset_Logo.png";
    }
}