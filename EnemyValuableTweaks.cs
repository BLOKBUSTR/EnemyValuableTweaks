using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace EnemyValuableTweaks
{
    [BepInPlugin("BLOKBUSTR.EnemyValuableTweaks", "EnemyValuableTweaks", "1.0.3")]
    public class EnemyValuableTweaks : BaseUnityPlugin
    {
        internal static EnemyValuableTweaks Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger => Instance._logger;
        private ManualLogSource _logger => base.Logger;
        internal Harmony? Harmony { get; set; }
        
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
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
        
        private static ConfigEntry<bool> _configEnableDebugTimerLogs;
        private static ConfigEntry<bool> _configEnableDebugGeneralLogs;
        #pragma warning restore CS8618
        
        private void Awake()
        {
            // Config setup
            configEnableTimer = Config.Bind("1 - Timer", "EnableTimer", true,
                new ConfigDescription("Whether to enable the main timer that automatically disables indestructibility once expired."));
            configTimerLength = Config.Bind("1 - Timer", "TimerLength", 10f,
                new ConfigDescription("Time in seconds until the orb loses indestructibility. Vanilla default is 5 seconds.",
                new AcceptableValueRange<float>(1f, 60f)));
            
            configAdditionalChecksDelay = Config.Bind("2 - Additional Checks", "AdditionalChecksDelay", 5f,
                new ConfigDescription("Time in seconds before all following checks activate after the orb has initially spawned. Will not work if greater than TimerLength. Additional checks take precedence over the main timer, meaning they will cut off the timer early if any one of their conditions have been satisfied.",
                new AcceptableValueRange<float>(1f, 60f)));
            
            configEnableVelocityCheck = Config.Bind("3 - Velocity", "EnableVelocityCheck", true,
                new ConfigDescription("Automatically disables indestructibility when the orb slows down to the specified velocity threshold."));
            configVelocityThreshold = Config.Bind("3 - Velocity", "VelocityThreshold", .01f,
                new ConfigDescription("The minimum threshold for the velocity check.",
                new AcceptableValueRange<float>(0f, 1f)));
            
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
            configFullMoonExplosionProbability = Config.Bind("6 - Moon Phase Explosion Probability", "FullMoonExplosionProbability", .35f,
                new ConfigDescription("The probability of orbs exploding during the Full Moon phase, beginning on Level 15.",
                new AcceptableValueRange<float>(0f, 1f)));
            configSuperMoonExplosionProbability = Config.Bind("6 - Moon Phase Explosion Probability", "SuperMoonExplosionProbability", .95f,
                new ConfigDescription("The probability of orbs exploding during the Super Moon phase, beginning on Level 20.",
                new AcceptableValueRange<float>(0f, 1f)));
            
            _configEnableDebugTimerLogs = Config.Bind("Debug", "EnableDebugTimerLogs", false,
                new ConfigDescription("Enable debug logs for this mod's timers. \"Debug\" or \"All\" must be included in Logging.Console.LogLevels in the BepInEx config to be able to see these logs. Note that this will create a lot of spam in the console, so please keep this disabled for normal gameplay!"));
            _configEnableDebugGeneralLogs = Config.Bind("Debug", "EnableDebugGeneralLogs", false,
                new ConfigDescription("Enable debug logs for other calculations and logic performed by this mod."));
            
            Instance = this;
            
            this.gameObject.transform.parent = null;
            this.gameObject.hideFlags = HideFlags.HideAndDontSave;
            
            Patch();
            
            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version}: it's tweakin' time!");
            
            if (configEnableTimer.Value && configAdditionalChecksDelay.Value > configTimerLength.Value)
                Logger.LogWarning($"AdditionalChecksDelay ({configAdditionalChecksDelay.Value}) is greater than TimerLength ({configTimerLength.Value})! Please adjust your config.");
        }
        
        internal void Patch()
        {
            Harmony ??= new Harmony(Info.Metadata.GUID);
            Harmony.PatchAll();
        }
        
        internal void Unpatch()
        {
            Harmony?.UnpatchSelf();
        }
        
        internal static void LogDebugTimers(string message)
        {
            if (!_configEnableDebugTimerLogs.Value) return;
            Logger.LogDebug(message);
        }
        internal static void LogDebugGeneral(string message)
        {
            if (!_configEnableDebugGeneralLogs.Value) return;
            Logger.LogDebug(message);
        }
    }
}
