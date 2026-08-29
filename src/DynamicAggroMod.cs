using Hkmp.Api.Client;
using Modding;
using UnityEngine.SceneManagement;

namespace HkmpDynamicAggro
{
    /// <summary>
    /// Entry point. Registers the HKMP client addon and installs the aggro hooks.
    /// </summary>
    public class DynamicAggroMod : Mod, IGlobalSettings<Settings>
    {
        public const string ModVersion = "1.0.0";

        public static DynamicAggroMod Instance;
        public static Settings Settings = new Settings();

        public DynamicAggroMod() : base("Dynamic Aggro")
        {
        }

        public override string GetVersion()
        {
            return ModVersion;
        }

        public override void Initialize()
        {
            Instance = this;

            ClientAddon.RegisterAddon(new HkmpAddon());
            AggroHooks.Register();

            // Enemies do not survive a scene change, so their tracked aggro should not either.
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;

            Log("Dynamic Aggro " + ModVersion + " initialized");
        }

        private void OnSceneChanged(Scene from, Scene to)
        {
            AggroTracker.Reset();
        }

        public void OnLoadGlobal(Settings settings)
        {
            Settings = settings ?? new Settings();
        }

        public Settings OnSaveGlobal()
        {
            return Settings;
        }
    }
}
