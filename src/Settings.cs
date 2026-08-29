namespace HkmpDynamicAggro
{
    /// <summary>
    /// Global settings, persisted by the Modding API to
    /// AppData\LocalLow\Team Cherry\Hollow Knight\HkmpDynamicAggro.GlobalSettings.json
    /// </summary>
    public class Settings
    {
        /// <summary>Master switch. When false the mod is completely inert.</summary>
        public bool Enabled = true;

        /// <summary>Whether the local Knight is a valid aggro target.</summary>
        public bool IncludeLocalPlayer = true;

        /// <summary>
        /// Minimum seconds an enemy must keep a target before it may switch.
        /// Stops the enemy jittering between two players standing close together.
        /// </summary>
        public float SwitchCooldown = 0.75f;

        /// <summary>
        /// A rival only steals aggro if it is this much closer than the current target.
        /// 0.8 means "at least 20% closer". 1.0 disables the margin.
        /// </summary>
        public float SwitchRatio = 0.8f;

        /// <summary>Log every aggro switch to ModLog.txt.</summary>
        public bool DebugLog = false;
    }
}
