using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using ServerSync;
using System;
using System.Reflection.Emit;

namespace HS_EquipInWater
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class HS_EquipInWater : BaseUnityPlugin
    {
        private const string ModName = "HS_EquipInWater";
        private const string ModVersion = "1.0.4";
        private const string ModGUID = "hs_equipinwater";

        private static readonly ConfigSync configSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = "1.0.4" };

        private static ConfigEntry<Toggle> serverConfigLocked = null!;
        private static ConfigEntry<bool> modEnabled = null!;
        private static ConfigEntry<string> itemBlacklist = null!;
        private static List<string> itemBlacklistStrings = new ();

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

        #endregion

        private void Awake()
        {
            serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
            configSync.AddLockingConfigEntry(serverConfigLocked);
            UseBlackList = config("1 - General", "Use Blacklist", true, "");

            itemBlacklist = config("2 - Blacklist Items", "Items Blacklisted in Water", "Torch;Lantern;", new ConfigDescription("List of Prefab names to Blacklist from the Player being able to use while Swimming"));
            itemBlacklist.SettingChanged += (_, _) => itemBlacklistStrings = itemBlacklist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            itemBlacklistStrings = itemBlacklist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            UseWhiteList = config("1 - General", "Use Whitelist", true, "");

            itemWhitelist = config("2 - Whitelist Items", "Items Whitelisted in Water", "Torch;Lantern;", new ConfigDescription("List of prefab names for WhiteList Which the player can use in the water"));
            itemWhitelist.SettingChanged += (_, _) => itemWhitelistStrings = itemWhitelist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            itemWhitelistStrings = itemWhitelist.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            Harmony harmony = new Harmony(ModGUID);

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

        public static bool HS_CheckWaterItem(ItemDrop.ItemData item)
        {
            if (!UseBlackList.Value)
                return true;

            if (item == null)
            {
                var player = Player.m_localPlayer;
                if (player.m_leftItem != null && itemBlacklistStrings.Contains(player.m_leftItem.m_dropPrefab.name))
                    return true;

                if (player.m_rightItem != null && itemBlacklistStrings.Contains(player.m_rightItem.m_dropPrefab.name))
                    return true;

                return false;
            }

            return itemBlacklistStrings.Contains(item.m_shared.m_name);

            if (!UseWhiteList.Value)
                return true;

            if (item == null)
            {
                var player = Player.m_localPlayer;
                if (player.m_leftItem != null && itemWhitelistStrings.Contains(player.m_leftItem.m_dropPrefab.name))
                    return true;

                if (player.m_rightItem != null && itemWhitelistStrings.Contains(player.m_rightItem.m_dropPrefab.name))
                    return true;

                return false;
            }

            return itemWhitelistStrings.Contains(item.m_shared.m_name);
        }

        public static class HS_EquipInWaterPatches
        {
            public static IEnumerable<CodeInstruction> HS_PatchPlayerUpdateWaterCheck(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionList = instructions.ToList();

                for (int i = 202; i <= 207; i++)
                {
                    CodeInstruction instruction = instructionList[i];
                    instruction.opcode = OpCodes.Nop;
                }

                return instructionList;
            }

            public static IEnumerable<CodeInstruction> HS_InjectWaterItemCheck(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = new List<CodeInstruction>(instructions);
                var injectionInstructions = new List<CodeInstruction>
                {
                    new (OpCodes.Ldarg_1),
                    new (OpCodes.Call, typeof(HS_EquipInWater).GetMethod("HS_CheckWaterItem")),
                    new (OpCodes.Brfalse_S, instructionList[29].operand)
                };

                instructionList.InsertRange(33, injectionInstructions);
                return instructionList;
            }

            public static IEnumerable<CodeInstruction> HS_PatchFixedUpdatedWaterCheck(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = new List<CodeInstruction>(instructions);
                var injectionInstructions = new List<CodeInstruction>
                {
                    new (OpCodes.Ldnull),
                    new (OpCodes.Call, typeof(HS_EquipInWater).GetMethod("HS_CheckWaterItem")),
                    new (OpCodes.Brfalse_S, instructionList[9].operand)
                };

                instructionList.InsertRange(10, injectionInstructions);
                return instructionList;
            }
        }
    }
}
