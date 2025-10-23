using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using GlobalSettings;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[BepInPlugin("com.NoBrainer.SSEasyMode", "SSEasyMode", "1.1.0")]
public class SSEasyMode : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;

    static int nailRangeMultiplier = 2;
    static float damageDealtMultiplier = 2.0f;
    static float damageTakenReduction = 0.5f; // % of damage received (1.0 - 100%, 0.5 - 50%)

    static float damageTakenTracker = 0f;
    public static ConfigEntry<bool> EnableHalfDamageTaken;
    public static ConfigEntry<bool> EnablePlayerDoubleDamage;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo("Plugin loaded and initialized.");
        EnableHalfDamageTaken = Config.Bind("General", "EnableHalfDamageTaken", true, "Enable or disable half damage taken for player.");
        EnablePlayerDoubleDamage = Config.Bind("General", "EnablePlayerDoubleDamage", true, "Enable or disable double damage done by the player.");

        Harmony.CreateAndPatchAll(typeof(SSEasyMode), null);
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
    private static void TakeDamagePrefix(HealthManager __instance, ref HitInstance hitInstance)
    {
        if (!EnablePlayerDoubleDamage.Value) return;
        if (!hitInstance.IsHeroDamage) return;

        Logger.LogInfo("MULTIPLYING PLAYER DAMAGE BY x" + damageDealtMultiplier);
        hitInstance.Multiplier *= damageDealtMultiplier;
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerData), "TakeHealth")]
    private static bool TakeHealthPrefix(PlayerData __instance, int amount, bool hasBlueHealth, bool allowFracturedMaskBreak)
    {
        if (!EnableHalfDamageTaken.Value)
        {   
            return true;
        }
            
        Logger.LogInfo("Amount: " + amount);
        Logger.LogInfo("damageTakenTracker: " + SSEasyMode.damageTakenTracker);
        SSEasyMode.damageTakenTracker += amount * damageTakenReduction;

        if (SSEasyMode.damageTakenTracker >= 1.0f)
        {
            amount = 1; // not more than 1 HP damage per hit
            SSEasyMode.damageTakenTracker = 0f; // reset damage tracker
        }
        else
            return false;


        if (amount > 0 && __instance.health == __instance.maxHealth && __instance.health != __instance.CurrentMaxHealth)
        {
            __instance.health = __instance.CurrentMaxHealth;
        }

        __instance.damagedBlue = hasBlueHealth;
        if (!__instance.damagedBlue)
        {
            __instance.damagedPurple = false;
        }

        if (__instance.healthBlue > 0)
        {
            int num = amount - __instance.healthBlue;
            __instance.damagedBlue = true;
            __instance.damagedPurple = false;
            if (__instance.damagedBlue)
            {
                EventRegister.SendEvent("PURPLE HEALTH CHECK");
            }

            __instance.healthBlue -= amount;
            if (__instance.healthBlue < 0)
            {
                __instance.healthBlue = 0;
            }

            if (num > 0)
            {
                __instance.TakeHealth(num, hasBlueHealth: true, allowFracturedMaskBreak);
            }

            return false;
        }

        int num2 = __instance.health - amount;
        ToolItem fracturedMaskTool = Gameplay.FracturedMaskTool;
        if (num2 <= 0 && (bool)fracturedMaskTool && fracturedMaskTool.IsEquipped && fracturedMaskTool.SavedData.AmountLeft > 0)
        {
            if (allowFracturedMaskBreak)
            {
                ToolItemsData.Data savedData = fracturedMaskTool.SavedData;
                savedData.AmountLeft = 0;
                fracturedMaskTool.SavedData = savedData;
            }

            amount = __instance.health - 1;
        }

        if (__instance.health - amount <= 0)
        {
            __instance.health = ((CheatManager.Invincibility == CheatManager.InvincibilityStates.PreventDeath) ? 1 : 0);
        }
        else
        {
            __instance.health -= amount;
        }
        return false; // replace original method
    }


}

