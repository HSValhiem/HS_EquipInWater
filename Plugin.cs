using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using ServerSync;
using System;
using System.Reflection.Emit;
using BepInEx.Logging;
using HS;

namespace HS_EquipInWater;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class HS_EquipInWater : BaseUnityPlugin
{
    private static readonly ConfigSync configSync = new(MyPluginInfo.PLUGIN_GUID) { DisplayName = MyPluginInfo.PLUGIN_NAME, CurrentVersion = MyPluginInfo.PLUGIN_VERSION, MinimumRequiredVersion = "0.1.6" };

    public static readonly ManualLogSource HS_Logger = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_NAME);

    private static ConfigEntry<Toggle> serverConfigLocked = null!;
    private static ConfigEntry<bool> modEnabled = null!;
    private static ConfigEntry<FilterMode> filterMode = null!;
    private static ConfigEntry<string> itemBlacklist = null!;
    private static ConfigEntry<string> itemWhitelist = null!;

    private static List<string> itemBlacklistStrings = [];
    private static List<string> itemWhitelistStrings = [];

    #region Config Boilerplate
    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
    {
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);
        SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;
        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
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
        if (!VersionChecker.Check(HS_Logger, Info, true, Config)) return;

        serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
        configSync.AddLockingConfigEntry(serverConfigLocked);
        modEnabled = config("1 - General", "Mod Enabled", true, "");

        filterMode = config("2 - Items Filter", "Filter Mode", FilterMode.Blacklist, "Choose the Method of which to Filter Items used in Water");
        itemBlacklist = config("2 - Items Filter", "Items Blacklisted in Water", "Torch;Lantern;", new ConfigDescription("List of Prefab names to Blacklist from the Player being able to use while Swimming"));
        itemBlacklist.SettingChanged += (_, _) => itemBlacklistStrings = [.. itemBlacklist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)];
        itemBlacklistStrings = itemBlacklist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        itemWhitelist = config("2 - Items Filter", "Items Whitelisted in Water", "SpearBronze;", new ConfigDescription("List of Prefab names to Whitelist the Player to be able to use while Swimming"));
        itemWhitelist.SettingChanged += (_, _) => itemWhitelistStrings = [.. itemWhitelist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)];
        itemWhitelistStrings = [.. itemWhitelist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)];


        Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);

        harmony.Patch(
            AccessTools.Method(typeof(Player), "Update"),
            transpiler: new HarmonyMethod(typeof(HS_EquipInWaterPatches), nameof(HS_EquipInWaterPatches.HS_PatchPlayerUpdateWaterCheck))
        );

        harmony.Patch(
            AccessTools.Method(typeof(Humanoid), "EquipItem"),
            transpiler: new HarmonyMethod(typeof(HS_EquipInWaterPatches), nameof(HS_EquipInWaterPatches.HS_InjectWaterItemCheck))
        );

        harmony.Patch(
            AccessTools.Method(typeof(Humanoid), "UpdateEquipment"),
            transpiler: new HarmonyMethod(typeof(HS_EquipInWaterPatches), nameof(HS_EquipInWaterPatches.HS_PatchFixedUpdatedWaterCheck))
        );
    }

    // Return true to Put away All Equipment when in water
    public static bool HS_CheckWaterItem(ItemDrop.ItemData item)
    {
        if (!modEnabled.Value)
            return true;

        if (item == null)
        {
            var player = Player.m_localPlayer;
            if (filterMode.Value == FilterMode.Blacklist)
            {
                if (player.m_leftItem != null && itemBlacklistStrings.Contains(player.m_leftItem.m_dropPrefab.name))
                    player.UnequipItem(player.m_leftItem);

                if (player.m_rightItem != null && itemBlacklistStrings.Contains(player.m_rightItem.m_dropPrefab.name))
                    player.UnequipItem(player.m_rightItem);
            }
            else
            {
                if (player.m_leftItem != null && !itemWhitelistStrings.Contains(player.m_leftItem.m_dropPrefab.name))
                    player.UnequipItem(player.m_leftItem);

                if (player.m_rightItem != null && !itemWhitelistStrings.Contains(player.m_rightItem.m_dropPrefab.name))
                    player.UnequipItem(player.m_rightItem);
            }
            return false;
        }

        return filterMode.Value == FilterMode.Blacklist ? itemBlacklistStrings.Contains(item.m_shared.m_name) : itemWhitelistStrings.Contains(item.m_shared.m_name);
    }

    internal static int FindInstruction(List<CodeInstruction> instructions, OpCode opCode, string operand) =>
        instructions.FindIndex(instruction => instruction.opcode == opCode && instruction.operand?.ToString().Contains(operand) == true);

    public static class HS_EquipInWaterPatches
    {
        // Patch to remove ShowHandItems() Call from Player->Update when Player is OnGround or Swimming.
        // It is not needed since we are now checking during the Humanoid->UpdateEquipment and Humanoid->EquipItem functions.
        public static IEnumerable<CodeInstruction> HS_PatchPlayerUpdateWaterCheck(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            // Dynamically Find instruction start (This should prevent the need for changes when Valhiem updates)
            int startIndex = FindInstruction(instructionList, OpCodes.Call, "Boolean IsSwimming()") - 1;
            if (startIndex == -1 || startIndex + 4 > instructionList.Count) HS_Logger.LogError("Failed to patch Player->Update");
            else for (int i = startIndex; i <= startIndex + 5; i++) instructionList[i].opcode = OpCodes.Nop;

            return instructionList;
        }

        // Patch to Inject HS_CheckWaterItem() Call from Humanoid->EquipItem.
        public static IEnumerable<CodeInstruction> HS_InjectWaterItemCheck(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = new List<CodeInstruction>(instructions);
            int startIndex = FindInstruction(instructionList, OpCodes.Call, "Boolean IsOnGround()");

            if (startIndex == -1) HS_Logger.LogError("Failed to patch Humanoid->EquipItem");
            else
            {
                var injectionInstructions = new List<CodeInstruction>
                {
                    new (OpCodes.Ldarg_1),
                    new (OpCodes.Call, typeof(HS_EquipInWater).GetMethod("HS_CheckWaterItem")),
                    new (OpCodes.Brfalse_S, instructionList[startIndex + 1].operand)
                };

                instructionList.InsertRange(startIndex + 2, injectionInstructions);
            }
            return instructionList;
        }

        // Patch to Inject HS_CheckWaterItem() Call from Humanoid->UpdateEquipment.
        public static IEnumerable<CodeInstruction> HS_PatchFixedUpdatedWaterCheck(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = new List<CodeInstruction>(instructions);
            int startIndex = FindInstruction(instructionList, OpCodes.Call, "Boolean IsOnGround()");

            if (startIndex == -1) HS_Logger.LogError("Failed to patch Humanoid->UpdateEquipment");
            else
            {
                var injectionInstructions = new List<CodeInstruction>
                {
                    new (OpCodes.Ldnull),
                    new (OpCodes.Call, typeof(HS_EquipInWater).GetMethod("HS_CheckWaterItem")),
                    new (OpCodes.Brfalse_S, instructionList[startIndex + 1].operand)
                };

                instructionList.InsertRange(startIndex + 2, injectionInstructions);
            }

            return instructionList;
        }
    }
}
