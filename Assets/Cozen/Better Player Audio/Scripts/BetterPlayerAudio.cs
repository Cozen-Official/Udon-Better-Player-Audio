using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Cozen
{
    /// <summary>
    /// Interpolation curve types for audio settings.
    /// </summary>
    public enum AudioInterpolationCurve
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        Smoothstep,
        Smootherstep,
        Exponential,
        Diminishing,
        LogLike
    }
    
    /// <summary>
    /// Dynamically adjusts player audio settings based on the number of players in the instance.
    /// Voice gain and far distance are interpolated between start and end values as player count
    /// changes between the configured min and max thresholds.
    /// 
    /// This script runs locally on every player's client. Each player's instance of this script
    /// adjusts how they hear other players' voices. The settings are applied to all non-local
    /// players, meaning each client controls their own audio experience based on the current
    /// player count in the instance.
    /// </summary>
    [AddComponentMenu("Cozen/Better Player Audio")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BetterPlayerAudio : UdonSharpBehaviour
    {		
        [Tooltip("The minimum number of players before interpolation kicks in. Below this count, audio settings remain at their start values.")]
        public int minPlayers = 1;
        
        [Tooltip("The maximum number of players at which interpolation reaches its end values. Above this count, audio settings remain at their end values.")]
        public int maxPlayers = 60;
        
        [Tooltip("Enable extended player range (1-100) for special VRChat events. Normal instances cap at 80 players.")]
        public bool extendedPlayerRange = false;
        
        [Tooltip("The curve type used to interpolate between start and end values as player count changes.")]
        public AudioInterpolationCurve curveType = AudioInterpolationCurve.Linear;
        
        [Tooltip("The voice far distance (in meters) when player count is at or below Min Players. This is the range at which you can hear other players.")]
        [Min(0f)]
        public float startDistance = 25f;
        
        [Tooltip("The voice far distance (in meters) when player count reaches or exceeds Max Players.")]
        [Min(0f)]
        public float endDistance = 10f;
        
        [Tooltip("The voice gain (in decibels, 0-24) when player count is at or below Min Players.")]
        [Range(0f, 24f)]
        public float startGain = 15f;
        
        [Tooltip("The voice gain (in decibels, 0-24) when player count reaches or exceeds Max Players.")]
        [Range(0f, 24f)]
        public float endGain = 15f;
        
        [Tooltip("Whether to enable the low-pass filter on distant voices. When enabled, voices sound muffled at distance which helps with understanding in noisy worlds.")]
        public bool voiceLowPass = true;
        
        [Tooltip("Interval in seconds between periodic player count checks. This serves as a fallback in case join/leave events are missed. Set to 0 to disable periodic checks.")]
        [Min(0f)]
        public float checkInterval = 120f;
        
        // Internal state
        private int lastKnownPlayerCount = -1;
        private VRCPlayerApi[] playerBuffer;
        private const int PlayerBufferSize = 100;
        
        private void Start()
        {
            // Validate configuration
            if (minPlayers < 1) minPlayers = 1;
            if (maxPlayers <= minPlayers) maxPlayers = minPlayers + 1;
            
            // Initialize player buffer
            EnsurePlayerBuffer();
            
            // Apply initial settings
            UpdatePlayerAudioSettings();
            
            // Start periodic check if enabled
            if (checkInterval > 0f)
            {
                SendCustomEventDelayedSeconds(nameof(PeriodicCheck), checkInterval);
            }
        }
        
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            UpdatePlayerAudioSettings();
        }
        
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            // Delay the update slightly to allow VRChat to update the player count
            SendCustomEventDelayedSeconds(nameof(UpdatePlayerAudioSettings), 0.1f);
        }
        
        /// <summary>
        /// Periodic check to ensure audio settings stay in sync with player count.
        /// This is a fallback in case join/leave events are missed.
        /// </summary>
        public void PeriodicCheck()
        {
            int currentCount = VRCPlayerApi.GetPlayerCount();
            if (currentCount != lastKnownPlayerCount)
            {
                UpdatePlayerAudioSettings();
            }
            
            // Schedule next check
            if (checkInterval > 0f)
            {
                SendCustomEventDelayedSeconds(nameof(PeriodicCheck), checkInterval);
            }
        }
        
        /// <summary>
        /// Updates audio settings for all players based on current player count.
        /// </summary>
        public void UpdatePlayerAudioSettings()
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            lastKnownPlayerCount = playerCount;
            
            // Calculate interpolation factor (0 = at or below min, 1 = at or above max)
            float t = CalculateInterpolationFactor(playerCount);
            
            // Calculate interpolated values
            float currentDistance = Mathf.Lerp(startDistance, endDistance, t);
            float currentGain = Mathf.Lerp(startGain, endGain, t);
            
            // Apply settings to all players
            ApplyAudioSettingsToAllPlayers(currentDistance, currentGain);
            
            Debug.Log($"[BetterPlayerAudio] Updated audio settings: {playerCount} players, t={t:F2}, distance={currentDistance:F1}m, gain={currentGain:F1}dB");
        }
        
        /// <summary>
        /// Calculates the interpolation factor based on current player count.
        /// Returns 0 when at or below minPlayers, 1 when at or above maxPlayers,
        /// and a curved value in between based on the selected curve type.
        /// </summary>
        private float CalculateInterpolationFactor(int playerCount)
        {
            if (playerCount <= minPlayers)
            {
                return 0f;
            }
            
            if (playerCount >= maxPlayers)
            {
                return 1f;
            }
            
            // Linear interpolation between min and max
            float range = maxPlayers - minPlayers;
            float position = playerCount - minPlayers;
            float t = position / range;
            
            // Apply curve transformation
            return ApplyCurve(t);
        }
        
        /// <summary>
        /// Applies the selected curve transformation to the linear interpolation factor.
        /// </summary>
        private float ApplyCurve(float t)
        {
            switch (curveType)
            {
                case AudioInterpolationCurve.EaseIn:
                    return t * t;
                    
                case AudioInterpolationCurve.EaseOut:
                    return 1f - (1f - t) * (1f - t);
                    
                case AudioInterpolationCurve.EaseInOut:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                    
                case AudioInterpolationCurve.Smoothstep:
                    return t * t * (3f - 2f * t);
                    
                case AudioInterpolationCurve.Smootherstep:
                    return t * t * t * (t * (6f * t - 15f) + 10f);
                    
                case AudioInterpolationCurve.Exponential:
                    return Mathf.Pow(2f, t) - 1f;
                    
                case AudioInterpolationCurve.Diminishing:
                    return Mathf.Sqrt(t);
                    
                case AudioInterpolationCurve.LogLike:
                    return Mathf.Log(Mathf.Lerp(1f, Mathf.Exp(1f), t));
                    
                case AudioInterpolationCurve.Linear:
                default:
                    return t;
            }
        }
        
        /// <summary>
        /// Applies the calculated audio settings to all non-local players in the instance.
        /// </summary>
        private void ApplyAudioSettingsToAllPlayers(float distance, float gain)
        {
            EnsurePlayerBuffer();
            
            int playerCount = VRCPlayerApi.GetPlayerCount();
            if (playerCount <= 0)
            {
                return;
            }
            
            VRCPlayerApi.GetPlayers(playerBuffer);
            
            for (int i = 0; i < playerCount && i < playerBuffer.Length; i++)
            {
                VRCPlayerApi player = playerBuffer[i];
                if (player == null || !Utilities.IsValid(player))
                {
                    continue;
                }
                
                // Only apply settings to non-local players
                // (we can't change our own voice settings)
                if (player.isLocal)
                {
                    continue;
                }
                
                player.SetVoiceDistanceFar(distance);
                player.SetVoiceGain(gain);
                player.SetVoiceLowpass(voiceLowPass);
            }
        }
        
        /// <summary>
        /// Ensures the player buffer is initialized with adequate capacity.
        /// </summary>
        private void EnsurePlayerBuffer()
        {
            if (playerBuffer == null || playerBuffer.Length < PlayerBufferSize)
            {
                playerBuffer = new VRCPlayerApi[PlayerBufferSize];
            }
        }
        
        /// <summary>
        /// Forces an immediate update of all player audio settings.
        /// Can be called externally if needed.
        /// </summary>
        public void ForceUpdate()
        {
            UpdatePlayerAudioSettings();
        }
    }
}
