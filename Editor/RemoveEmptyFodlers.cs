using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Remove empty folders automatically.
/// </summary>
public static class RemoveEmptyFolders
{
    static readonly StringBuilder sLog = new StringBuilder();
    static readonly List<DirectoryInfo> sResults = new List<DirectoryInfo>();

    [MenuItem("Tools/Remove Empty Folders")]
    public static void RemoveAllEmptyFolders()
    {
        // Get empty directories in Assets directory
        sResults.Clear();
        var assetsDir = Application.dataPath + Path.DirectorySeparatorChar;
        GetEmptyDirectories(new DirectoryInfo(assetsDir), sResults);

        // When empty directories has detected, remove the directory.
        if (0 < sResults.Count)
        {
            sLog.Length = 0;
            sLog.AppendFormat("Remove {0} empty directories as following:\n", sResults.Count);
            foreach (var d in sResults)
            {
                sLog.AppendFormat("- {0}\n", d.FullName.Replace(assetsDir, ""));
                FileUtil.DeleteFileOrDirectory(d.FullName);
            }

            // UNITY BUG: Debug.Log can not set about more than 15000 characters.
            sLog.Length = Mathf.Min(sLog.Length, 15000);
            Debug.Log(sLog.ToString());
            sLog.Length = 0;

            AssetDatabase.Refresh();
        }
    }
	
    /// <summary>
    /// Get empty directories.
    /// </summary>
    static bool GetEmptyDirectories(DirectoryInfo dir, ICollection<DirectoryInfo> results)
    {
        bool isEmpty = true;
        try
        {
            isEmpty = dir.GetDirectories().Count(x => !GetEmptyDirectories(x, results)) == 0	// Are sub directories empty?
                      && dir.GetFiles("*.*").All(x => x.Extension == ".meta");	// No file exist?
        }
        catch
        {
            // ignored
        }

        // Store empty directory to results.
        if (isEmpty)
            results.Add(dir);
        return isEmpty;
    }
}