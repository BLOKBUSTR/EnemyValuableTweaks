using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

#pragma warning disable CS8618
namespace EnemyValuableTweaks
{
    [BepInPlugin("BLOKBUSTR.EnemyValuableTweaks", "EnemyValuableTweaks", "1.3.0")]
    public class EnemyValuableTweaks : BaseUnityPlugin
    {
        internal static EnemyValuableTweaks Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger => Instance._logger;
        [SuppressMessage("ReSharper", "InconsistentNaming")] private ManualLogSource _logger => base.Logger;
        internal Harmony? Harmony { get; set; }
        
        #region ConfigEntries
        
        public static ConfigEntry<bool> configEnableTimer;
        public static ConfigEntry<float> configTimerLength;
        
        public static ConfigEntry<float> configAdditionalChecksDelay;
        
        public static ConfigEntry<bool> configEnableVelocityCheck;
        public static ConfigEntry<float> configVelocityThreshold;
        
        public static ConfigEntry<bool> configEnablePlayerHold;
        public static ConfigEntry<float> configPlayerHoldTime;
        
        public static ConfigEntry<bool> configEnableSafeAreaCheck;
        public static ConfigEntry<float> configSafeAreaTime;
        
        public static ConfigEntry<float> configInitialExplosionProbability;
        public static ConfigEntry<float> configCrescentMoonExplosionProbability; // Level 5
        public static ConfigEntry<float> configHalfMoonExplosionProbability; // Level 10
        public static ConfigEntry<float> configFullMoonExplosionProbability; // Level 15
        public static ConfigEntry<float> configSuperMoonExplosionProbability; // Level 20
        
        public static ConfigEntry<int> configMaxSpawnAmount;
        public static ConfigEntry<float> configSmallSpawnProbability;
        public static ConfigEntry<float> configMediumSpawnProbability;
        public static ConfigEntry<float> configLargeSpawnProbability;
        
        public static ConfigEntry<bool> configHostOnlyMode;
        
        private static ConfigEntry<bool> configEnableDebug;
        private static ConfigEntry<bool> configEnableTimerDebug;
        
        #endregion
        
        private void Awake()
        {
            Instance = this;
            
            gameObject.transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            
            RegisterConfig();
            Patch();
            
            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version}: it's tweakin' time!");
            Debug("Debug logging is enabled.");
            DebugTimer("Timer logging is enabled.");
            if (configEnableTimer.Value && configAdditionalChecksDelay.Value > configTimerLength.Value)
                Logger.LogWarning($"AdditionalChecksDelay ({configAdditionalChecksDelay.Value}) is greater than TimerLength ({configTimerLength.Value})! Please adjust your config.");
        }
        
        private void RegisterConfig()
        {
            configEnableTimer = Config.Bind("1 - Timer", "EnableTimer", true,
                new ConfigDescription("Whether to enable the main timer that automatically disables indestructibility once expired. If disabled, the orb will never become destructible unless any of the following conditions are enabled."));
            configTimerLength = Config.Bind("1 - Timer", "TimerLength", 10f,
                new ConfigDescription("Time in seconds until the orb loses indestructibility. Vanilla default is 5 seconds.",
                new AcceptableValueRange<float>(1f, 60f)));
            
            configAdditionalChecksDelay = Config.Bind("2 - Additional Checks", "AdditionalChecksDelay", 5f,
                new ConfigDescription("Time in seconds before all following checks activate after the orb has initially spawned. Will not work if greater than TimerLength. Additional checks take precedence over the main timer, meaning they will cut off the timer early if any one of their conditions have been satisfied.",
                new AcceptableValueRange<float>(1f, 60f)));
            
            configEnableVelocityCheck = Config.Bind("3 - Velocity", "EnableVelocityCheck", true,
                new ConfigDescription("Automatically disables indestructibility when the orb slows down to the specified velocity threshold."));
            configVelocityThreshold = Config.Bind("3 - Velocity", "VelocityThreshold", .5f,
                new ConfigDescription("The minimum threshold for the velocity check.",
                new AcceptableValueRange<float>(0f, 2f)));
            
            configEnablePlayerHold = Config.Bind("4 - Player Hold", "EnablePlayerHold", true,
                new ConfigDescription("Automatically disables indestructibility when the orb is grabbed by a player."));
            configPlayerHoldTime = Config.Bind("4 - Player Hold", "PlayerHoldTime", 1f,
                new ConfigDescription("Time in seconds that a player must continue holding onto the orb before indestructibility is disabled. Resets when the player lets go, so that it will not prematurely become destructible if the player gets distracted by something else. Can be set to 0 to immediately disable indestructibility.",
                new AcceptableValueRange<float>(0f, 3f)));
            
            configEnableSafeAreaCheck = Config.Bind("5 - Safe Areas", "EnableSafeAreaCheck", true,
                new ConfigDescription("Disables indestructibility if the orb has been placed inside a safe area, such as the C.A.R.T. or an extraction point."));
            configSafeAreaTime = Config.Bind("5 - Safe Areas", "SafeAreaTime", 1f,
                new ConfigDescription("Time in seconds that the orb must remain in a safe area to disable indestructibility. Works very much like PlayerHoldTime.",
                new AcceptableValueRange<float>(0f, 3f)));
            
            configInitialExplosionProbability = Config.Bind("6 - Moon Phase Explosion Probability", "InitialExplosionProbability", 0f,
                new ConfigDescription("The probability of orbs exploding at the start of a new game, before any moon phases have even taken effect.",
                new AcceptableValueRange<float>(0f, 1f)));
            configCrescentMoonExplosionProbability = Config.Bind("6 - Moon Phase Explosion Probability", "CrescentMoonExplosionProbability", .01f, // this 1% chance might be pretty funny
                new ConfigDescription("The probability of orbs exploding during the Crescent Moon phase, beginning on Level 5.",
                new AcceptableValueRange<float>(0f, 1f)));
            configHalfMoonExplosionProbability = Config.Bind("6 - Moon Phase Explosion Probability", "HalfMoonExplosionProbability", .1f,
                new ConfigDescription("The probability of orbs exploding during the Half Moon phase, beginning on Level 10.",
                new AcceptableValueRange<float>(0f, 1f)));
            configFullMoonExplosionProbability = Config.Bind("6 - Moon Phase Explosion Probability", "FullMoonExplosionProbability", .2f,
                new ConfigDescription("The probability of orbs exploding during the Full Moon phase, beginning on Level 15.",
                new AcceptableValueRange<float>(0f, 1f)));
            configSuperMoonExplosionProbability = Config.Bind("6 - Moon Phase Explosion Probability", "SuperMoonExplosionProbability", .95f,
                new ConfigDescription("The probability of orbs exploding during the Super Moon phase, beginning on Level 20.",
                new AcceptableValueRange<float>(0f, 1f)));
            
            configMaxSpawnAmount = Config.Bind("7 - Spawning", "MaxSpawnAmount", 3,
                new ConfigDescription("The maximum amount of orbs that can spawn per enemy. Vanilla default is 3; set to 0 for no limit. This option only applies on level reload.",
                new AcceptableValueRange<int>(0, 100)));
            configSmallSpawnProbability = Config.Bind("7 - Spawning", "SmallSpawnProbability", 1f,
                new ConfigDescription("The probability for small orbs to drop from Difficulty 1 monsters on death.", 
                new AcceptableValueRange<float>(0f, 1f)));
            configMediumSpawnProbability = Config.Bind("7 - Spawning", "MediumSpawnProbability", 1f,
                new ConfigDescription("The probability for medium orbs to drop from Difficulty 2 monsters on death.",
                new AcceptableValueRange<float>(0f, 1f)));
            configLargeSpawnProbability = Config.Bind("7 - Spawning", "LargeSpawnProbability", 1f,
                new ConfigDescription("The probability for large orbs to drop from Difficulty 3 monsters on death.",
                new AcceptableValueRange<float>(0f, 1f)));
            
            // Host-only mode
            configHostOnlyMode = Config.Bind("Hosting", "HostOnlyMode", false,
                new ConfigDescription("Disables all features that require clients to also have the mod installed. Currently, this disables everything except the \"Spawning\" options."));
            
            // Debug
            configEnableDebug = Config.Bind("Debug", "EnableDebug", false,
                new ConfigDescription("Whether to enable debug logging. Keep this disabled for normal gameplay."));
            configEnableTimerDebug = Config.Bind("Debug", "EnableTimerDebug", false,
                new ConfigDescription("Whether to enable debug logging for this mod's timers. Note that this can create a lot of spam in the log. Keep this disabled for normal gameplay."));
        }
        
        internal void Patch()
        {
            Harmony ??= new Harmony(Info.Metadata.GUID);
            Harmony.PatchAll();
        }
        
        internal static void Debug(string message, MonoBehaviour? instance = null)
        {
            if (configEnableDebug.Value) Logger.LogDebug((bool)instance ? instance + ": " + message : message);
        }
        
        internal static void DebugTimer(string message, MonoBehaviour? instance = null)
        {
            if (configEnableTimerDebug.Value) Logger.LogDebug((bool)instance ? instance + ": " + message : message);
        }
    }
}
