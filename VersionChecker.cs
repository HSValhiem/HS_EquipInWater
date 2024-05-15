//VERSIONCHECKER VERSION 0.0.2
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using Color = UnityEngine.Color;
using FontStyle = UnityEngine.FontStyle;

namespace HS;

internal static class VersionChecker
{
    // GENENERATED AT RUNTIME DO NOT SET MANUALLY
    #region Compiler Generated Values
    private const string ValheimVersion = "0.218.15";
    #endregion

    public static bool Check(ManualLogSource logger, PluginInfo info, bool allowWrongVersion = false, ConfigFile? config = null)
    {
        // Check Valheim Version
        var currentValheimVersion = Version.CurrentVersion.ToString();
        if (currentValheimVersion == ValheimVersion) return true;

        var wrongVersionMessage =
            $"The current version of {info.Metadata.Name} (v{info.Metadata.Version}) is designed for Valheim version {ValheimVersion}," +
            $" however, your current Valheim version was {currentValheimVersion}.\n";

        if (!allowWrongVersion)
        {
            var errorMessage =
                $"ERROR: {wrongVersionMessage}" +
                "To resolve this issue, please obtain the appropriate plugin version, or alternatively, await an update if one is not currently available.\n" +
                "Please download the correct plugin version or wait for the plugin to be updated if a new version is not available.";
            logger.LogError(errorMessage);
            Chainloader.DependencyErrors.Add(errorMessage);
            if (config != null) SetConfigAlert(config, errorMessage);
            return false;
        }

        logger.LogWarning(
            $"WARNING: {wrongVersionMessage}" +
            "Although the plugin author has permitted this version to work with an outdated Valheim version," +
            " it's important to note that unforeseen complications may occur\n" +
            "Please download the correct plugin version or wait for the plugin to be updated if a new version is not available and any issues arise.");
        
        return true;
    }

    private static void SetConfigAlert(ConfigFile? config, string errorMessage)
    {
        config?.Bind("", "NotCompatable", "", new ConfigDescription(
            errorMessage, null, new ConfigurationManagerAttributes
            {
                CustomDrawer = ErrorLabelDrawer,
                ReadOnly = true,
                HideDefaultButton = true,
                HideSettingName = true,
                Category = null!
            }
        ));
    }

    private static void ErrorLabelDrawer(ConfigEntryBase entry)
    {
        GUIStyle styleNormal = new(GUI.skin.label)
        {
            wordWrap = true,
            stretchWidth = true
        };

        GUIStyle styleError = new(GUI.skin.label)
        {
            stretchWidth = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.red },
            fontStyle = FontStyle.Bold
        };

        // General notice that we're the wrong version
        GUILayout.BeginVertical();
        GUILayout.Label(entry.Description.Description, styleNormal, GUILayout.ExpandWidth(true));

        // Centered red disabled text
        GUILayout.Label("Plugin has been disabled!", styleError, GUILayout.ExpandWidth(true));
        GUILayout.EndVertical();
    }
}