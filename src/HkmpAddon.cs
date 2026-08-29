using Hkmp.Api.Client;

namespace HkmpDynamicAggro
{
    /// <summary>
    /// HKMP client addon. Two jobs: hand us the <see cref="IClientApi"/> (which is how we
    /// enumerate remote players and their in-scene GameObjects), and register the chat
    /// command.
    ///
    /// It derives from <see cref="TogglableClientAddon"/> rather than plain ClientAddon so
    /// HKMP can switch it on and off at runtime via /addon. Toggling here physically
    /// attaches and detaches the hooks, which is a stronger off than the Enabled setting:
    /// that one leaves the hooks in place and makes them return early.
    ///
    /// NeedsNetwork is false on purpose. Every decision this mod makes is local and
    /// deterministic on the machine that simulates the enemy (the HKMP scene host), so
    /// there is nothing to send over the wire and no need for the other side to run a
    /// matching addon version.
    /// </summary>
    public class HkmpAddon : TogglableClientAddon
    {
        /// <summary>Set once HKMP initialises us; null while HKMP is not loaded.</summary>
        public static IClientApi Api;

        private AggroCommand _command;

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

            _command = new AggroCommand(clientApi);
            clientApi.CommandManager.RegisterCommand(_command);

            AggroHooks.Register();

            Logger.Info("DynamicAggro hooked into the HKMP client API; /aggro registered");
        }

        protected override void OnEnable()
        {
            AggroHooks.Register();
            Logger.Info("DynamicAggro enabled");
        }

        protected override void OnDisable()
        {
            AggroHooks.Deregister();

            // Stale targets would be wrong if the addon is switched back on later.
            AggroTracker.Reset();

            Logger.Info("DynamicAggro disabled");
        }
    }
}
