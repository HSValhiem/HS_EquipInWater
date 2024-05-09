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
    private const string ValheimVersion = "0.218.14";
    #endregion

    public static bool Check(ManualLogSource logger, PluginInfo info, bool allowWrongVersion = false, ConfigFile? config = null)
    {
        // Check Valheim Version
        var currentValheimVersion = Version.CurrentVersion.ToString();
        if (currentValheimVersion != ValheimVersion && !allowWrongVersion)
        {
            var errorMessage = $"ERROR: This version of {info.Metadata.Name} v{info.Metadata.Version} was built for Valheim {ValheimVersion}," +
                $" but you are running {currentValheimVersion}." +
                $" Please download the correct plugin version or Wait for the Plugin to be Updated if a new Version is not Available";
            logger.LogError(errorMessage);
            Chainloader.DependencyErrors.Add(errorMessage);
            if (config != null) SetConfigAlert(config, errorMessage);
            return false;
        }
        else if(currentValheimVersion != ValheimVersion && allowWrongVersion)
        {
            logger.LogWarning($"Warning: This version of {info.Metadata.Name} v{info.Metadata.Version} was built for Valheim {ValheimVersion}," +
                $" but you are running {currentValheimVersion}." +
                $" The plugin author has permitted its use nonetheless. Be aware that unexpected issues may arise.");
        }

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