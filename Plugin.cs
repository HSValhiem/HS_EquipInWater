using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using BepInEx.Logging;
using System.Linq;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace HS_EquipInWater
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class HS_EquipInWater : BaseUnityPlugin
    {
        private const string ModName = "HS_EquipInWater";
        private const string ModVersion = "1.0.1";
        private const string ModGUID = "hs_equipinwater";

        public static readonly ManualLogSource MlLogger = BepInEx.Logging.Logger.CreateLogSource(ModGUID);
        private List<ConfigEntry<bool>> configEntries = new();
        private static List<string> deniedItems = new();
        private Items itemList = new();

        void Awake()
        {
            configEntries.Add(Config.Bind("ItemsToAllow", "allowAxes", true, "Allow axes to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowBows", true, "Allow bows to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowAtgeirs", true, "Allow atgeirs to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowKnives", true, "Allow knives to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowMaces", true, "Allow maces to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowShields", true, "Allow shields to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowSledge", true, "Allow sledge to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowSpears", true, "Allow spears to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowSwords", true, "Allow swords to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowClub", true, "Allow club to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowTorch", false, "Allow torch to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowPickaxes", true, "Allow pickaxes to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowCultivator", true, "Allow cultivator to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowFishingRod", true, "Allow fishing rod to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowHammer", true, "Allow hammer to be used in Water"));
            configEntries.Add(Config.Bind("ItemsToAllow", "allowHoe", true, "Allow hoe to be used in Water"));

            foreach (ConfigEntry<bool> confEntry in configEntries)
            {
                if (!confEntry.Value)
                {
                    List<string> stringList = itemList.GetType().GetField(confEntry.Definition.Key).GetValue(itemList) as List<string>;
                    foreach (string a in stringList)
                    {
                        deniedItems.Add(a);
                    }
                }
            }
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

            // Example of How to Patch Protected Methods for LVH-IT, in case they ever read this.
            // harmony.Patch(AccessTools.Method(typeof(Humanoid), "HideHandItems"), prefix: new HarmonyMethod(typeof(HS_EquipmentWaterFix), nameof(PatchHideHandItems)));
        }

        // Example of How to Patch Protected Methods for LVH-IT, in case they ever read this.
        //private static void PatchHideHandItems(Humanoid __instance, ref bool __runOriginal)
        //{
        //    __runOriginal = false;

        //    if (__instance.m_leftItem == null && __instance.m_rightItem == null)
        //    {
        //        return;
        //    }

        //    ItemDrop.ItemData leftItem = __instance.m_leftItem;
        //    ItemDrop.ItemData rightItem = __instance.m_rightItem;
        //    __instance.UnequipItem(__instance.m_leftItem, true);
        //    __instance.UnequipItem(__instance.m_rightItem, true);
        //    __instance.m_hiddenLeftItem = leftItem;
        //    __instance.m_hiddenRightItem = rightItem;
        //    __instance.SetupVisEquipment(__instance.m_visEquipment, false);
        //    __instance.m_zanim.SetTrigger("equip_hip");

        //}

        // Return True to Put away Equipment
        public static bool HS_CheckWaterItem(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                var player = Player.m_localPlayer;
                if (player.m_leftItem != null && deniedItems.Contains(player.m_leftItem.m_shared.m_name))
                    return true;

                if (player.m_rightItem != null && deniedItems.Contains(player.m_rightItem.m_shared.m_name))
                    return true;

                return false;
            }
            return deniedItems.Contains(item.m_shared.m_name);
        }

        public static class HS_EquipInWaterPatches
        {
            // Remove (Is in Water) check for when Player uses Hide Key
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


            // Inject a Call to our custom function "HS_CheckWaterItem" within the If block to check if Item is Permissable to be used in water.
            public static IEnumerable<CodeInstruction> HS_InjectWaterItemCheck(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = new List<CodeInstruction>(instructions);
                var injectionInstructions = new List<CodeInstruction>
                {
                    new (OpCodes.Ldarg_1),
                    new (OpCodes.Call, typeof(HS_EquipInWater).GetMethod("HS_CheckWaterItem")),
                    new (OpCodes.Brfalse_S, instructionList[29].operand) // Reuse Label from Instruction 29 for JMP
                };

                instructionList.InsertRange(33, injectionInstructions);
                return instructionList;
            }

            // Remove (Is in Water) check during Humanoid Fixed Update
            public static IEnumerable<CodeInstruction> HS_PatchFixedUpdatedWaterCheck(IEnumerable<CodeInstruction> instructions)
            {

                var instructionList = new List<CodeInstruction>(instructions);
                var injectionInstructions = new List<CodeInstruction>
                {
                    new (OpCodes.Ldnull),
                    new (OpCodes.Call, typeof(HS_EquipInWater).GetMethod("HS_CheckWaterItem")),
                    new (OpCodes.Brfalse_S, instructionList[9].operand) // Reuse Label from Instruction 9 for JMP
                };

                instructionList.InsertRange(10, injectionInstructions);
                return instructionList;
            }
        }
    }
}
