using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using ServerSync;
using System;
using System.Reflection.Emit;
using BepInEx.Logging;
using HS;
using System.Reflection;

namespace HS_EquipInWater;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static readonly ConfigSync ConfigSync = new(MyPluginInfo.PLUGIN_GUID) { DisplayName = MyPluginInfo.PLUGIN_NAME, CurrentVersion = MyPluginInfo.PLUGIN_VERSION, MinimumRequiredVersion = "0.1.9" };

    public new static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_NAME);

    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    private static ConfigEntry<bool> _modEnabled = null!;
    private static ConfigEntry<FilterMode> _filterMode = null!;
    private static ConfigEntry<string> _itemBlacklist = null!;
    private static ConfigEntry<string> _itemWhitelist = null!;

    private static List<string> _itemBlacklistStrings = [];
    private static List<string> _itemWhitelistStrings = [];

    #region Config Boilerplate
    private new ConfigEntry<T> Config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
    {
        var configEntry = base.Config.Bind<T>(group, name, value, description);
        var syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;
        return configEntry;
    }

    private new ConfigEntry<T> Config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
    {
        return Config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    private enum Toggle
    {
        On = 1,
        Off = 0
    }

    private enum FilterMode
    {
        Blacklist = 0,
        Whitelist = 1
    }

    #endregion

    private void Awake()
    {
        // Check if Plugin was Built for Current Version of Valheim
        if (!VersionChecker.Check(Logger, Info, true, base.Config)) return;

        _serverConfigLocked = Config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can only be changed by server admins.");
        ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
        _modEnabled = Config("1 - General", "Mod Enabled", true, "");

        _filterMode = Config("2 - Items Filter", "Filter Mode", FilterMode.Blacklist, "Choose the Method of which to Filter Items used while Swimming");
        _itemBlacklist = Config("2 - Items Filter", "Items Blacklisted in Water", "Torch;Lantern;", new ConfigDescription("List of Prefab names to Blacklist the Player from using while Swimming"));
        _itemBlacklist.SettingChanged += (_, _) => _itemBlacklistStrings = [.. _itemBlacklist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)];
        _itemBlacklistStrings = [.. _itemBlacklist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)];

        _itemWhitelist = Config("2 - Items Filter", "Items Whitelisted in Water", "SpearBronze;", new ConfigDescription("List of Prefab names to Whitelist the Player to allow use while Swimming"));
        _itemWhitelist.SettingChanged += (_, _) => _itemWhitelistStrings = [.. _itemWhitelist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)];
        _itemWhitelistStrings = [.. _itemWhitelist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)];

        Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
        try
        {
            harmony.PatchAll();
        }
        catch (HarmonyException)
        {
            Logger.LogError("Unable to start due to Harmony Unable to Find Patch Methods, Please Update/Remove Plugin or Report Error to Author, Stopping Valheim Now!");
            Environment.Exit(1);
        }
    }

    // Return true to Put away All Equipment when in water
    public static bool HS_CheckWaterItem(ItemDrop.ItemData? item)
    {
        if (!_modEnabled.Value)
            return true;

        if (item == null)
        {
            var player = Player.m_localPlayer;
            if (_filterMode.Value == FilterMode.Blacklist)
            {
                if (player.m_leftItem != null && _itemBlacklistStrings.Contains(player.m_leftItem.m_dropPrefab.name))
                    player.UnequipItem(player.m_leftItem);

                if (player.m_rightItem != null && _itemBlacklistStrings.Contains(player.m_rightItem.m_dropPrefab.name))
                    player.UnequipItem(player.m_rightItem);
            }
            else
            {
                if (player.m_leftItem != null && !_itemWhitelistStrings.Contains(player.m_leftItem.m_dropPrefab.name))
                    player.UnequipItem(player.m_leftItem);

                if (player.m_rightItem != null && !_itemWhitelistStrings.Contains(player.m_rightItem.m_dropPrefab.name))
                    player.UnequipItem(player.m_rightItem);
            }
            return false;
        }

        return _filterMode.Value == FilterMode.Blacklist ? _itemBlacklistStrings.Contains(item.m_shared.m_name) : _itemWhitelistStrings.Contains(item.m_shared.m_name);
    }

    [HarmonyPatch]
    private static class EquipInWaterPatches
    {
        // Target Patch Methods
        private static IEnumerable<MethodBase> TargetMethods() => [
            AccessTools.Method(typeof(Player), nameof(Player.Update)),
            AccessTools.Method(typeof(Humanoid), nameof(Humanoid.EquipItem)),
            AccessTools.Method(typeof(Humanoid), nameof(Humanoid.UpdateEquipment))];

        // IL Filter
        static readonly CodeMatch[] Matches = [
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Character), nameof(Character.IsSwimming))),
            new CodeMatch(OpCodes.Brfalse),
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Character), nameof(Character.IsOnGround)))];

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            // Search Instructions for Call to IsSwimming() and IsOnGround() using the filter.
            var codeMatcher = new CodeMatcher(instructions).MatchStartForward(Matches);
            try
            {
                switch ((original.DeclaringType?.FullName, original.Name))
                {
                    case (nameof(Player), nameof(Player.Update)):
                        // Remove Calls to IsSwimming() and IsOnGround() from Player->Update to allow the ShowHandItems() Call to run.
                        codeMatcher.Advance(1).RemoveInstructions(6);
                        break;
                    case (nameof(Humanoid), nameof(Humanoid.EquipItem)):
                        // Inject Call to HS_CheckWaterItem() from Humanoid->EquipItem.
                        codeMatcher.Advance(6).Insert(new List<CodeInstruction>
                        {
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Call, typeof(Plugin).GetMethod(nameof(HS_CheckWaterItem))),
                            new(OpCodes.Brfalse, codeMatcher.InstructionAt(-1).operand)
                        });
                        break;
                    case (nameof(Humanoid), nameof(Humanoid.UpdateEquipment)):
                        // Inject Call to HS_CheckWaterItem() from Humanoid->UpdateEquipment.
                        codeMatcher.Advance(6).Insert(new List<CodeInstruction>
                        {
                            new(OpCodes.Ldnull),
                            new(OpCodes.Call, typeof(Plugin).GetMethod(nameof(HS_CheckWaterItem))),
                            new(OpCodes.Brfalse, codeMatcher.InstructionAt(-1).operand)
                        });
                        break;
                }
            }
            catch (ArgumentException)
            {
                Logger.LogError($"Unable to Start due to IL Transpiler Errors, Please Update/Remove Plugin or Report Error to Author, Stopping Valheim Now!");
                Environment.Exit(1);
            }
            return codeMatcher.InstructionEnumeration();
        }
    }
}
