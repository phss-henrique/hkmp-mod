using Hkmp.Api.Client;

namespace HkmpDynamicAggro
{
    /// <summary>
    /// Thin HKMP client addon. Its only job is to hand us the <see cref="IClientApi"/>,
    /// which is how we enumerate remote players and their in-scene GameObjects.
    ///
    /// NeedsNetwork is false on purpose: every decision this mod makes is local and
    /// deterministic on the machine that simulates the enemy (the HKMP scene host),
    /// so there is nothing to send over the wire.
    /// </summary>
    public class HkmpAddon : ClientAddon
    {
        /// <summary>Set once HKMP initialises us; null while disconnected or not loaded.</summary>
        public static IClientApi Api;

        protected override string Name
        {
            get { return "DynamicAggro"; }
        }

        protected override string Version
        {
            get { return DynamicAggroMod.ModVersion; }
        }

        public override bool NeedsNetwork
        {
            get { return false; }
        }

        public override void Initialize(IClientApi clientApi)
        {
            Api = clientApi;
            Logger.Info("DynamicAggro hooked into the HKMP client API");
        }
    }
}
