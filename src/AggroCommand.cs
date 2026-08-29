using System.Globalization;
using Hkmp.Api.Client;
using Hkmp.Api.Command.Client;

namespace HkmpDynamicAggro
{
    /// <summary>
    /// In-game chat command, so aggro behaviour can be toggled and tuned mid-session
    /// without leaving the game or editing the settings file.
    ///
    ///   /aggro                  show current state
    ///   /aggro on | off         master switch
    ///   /aggro self on | off    whether your own Knight is a valid target
    ///   /aggro cooldown 0.75    seconds before an enemy may re-target
    ///   /aggro ratio 0.8        how much closer a rival must be to steal aggro
    ///   /aggro debug on | off   log every switch
    /// </summary>
    public class AggroCommand : IClientCommand
    {
        private readonly IClientApi _api;

        public AggroCommand(IClientApi api)
        {
            _api = api;
        }

        public string Trigger
        {
            get { return "/aggro"; }
        }

        public string[] Aliases
        {
            get { return new string[0]; }
        }

        private void Reply(string message)
        {
            _api.UiManager.ChatBox.AddMessage("[Aggro] " + message);
        }

        public void Execute(string[] arguments)
        {
            // HKMP passes the trigger itself as the first argument. Drop it so the
            // offsets below read naturally, and tolerate it being absent.
            int i = 0;
            if (arguments.Length > 0 && arguments[0].StartsWith("/"))
            {
                i = 1;
            }

            if (arguments.Length <= i)
            {
                ShowStatus();
                return;
            }

            string verb = arguments[i].ToLowerInvariant();
            string value = arguments.Length > i + 1 ? arguments[i + 1].ToLowerInvariant() : null;

            switch (verb)
            {
                case "on":
                    SetEnabled(true);
                    break;

                case "off":
                    SetEnabled(false);
                    break;

                case "status":
                    ShowStatus();
                    break;

                case "self":
                    if (!RequireBool(value))
                    {
                        return;
                    }
                    DynamicAggroMod.Settings.IncludeLocalPlayer = value == "on";
                    SettingsStore.TrySave();
                    Reply("your own Knight is " +
                          (DynamicAggroMod.Settings.IncludeLocalPlayer ? "" : "not ") +
                          "a valid target");
                    break;

                case "debug":
                    if (!RequireBool(value))
                    {
                        return;
                    }
                    DynamicAggroMod.Settings.DebugLog = value == "on";
                    SettingsStore.TrySave();
                    Reply("debug logging " + value);
                    break;

                case "cooldown":
                    SetFloat(value, true);
                    break;

                case "ratio":
                    SetFloat(value, false);
                    break;

                default:
                    Reply("usage: /aggro [on|off|self|cooldown|ratio|debug|status]");
                    break;
            }
        }

        private void SetEnabled(bool enabled)
        {
            DynamicAggroMod.Settings.Enabled = enabled;
            SettingsStore.TrySave();

            // Drop remembered targets so the next tick starts clean either way.
            AggroTracker.Reset();

            Reply(enabled
                ? "on - enemies now chase whoever is closest"
                : "off - enemies chase the scene host, as in vanilla HKMP");
        }

        private bool RequireBool(string value)
        {
            if (value == "on" || value == "off")
            {
                return true;
            }

            Reply("expected 'on' or 'off'");
            return false;
        }

        private void SetFloat(string value, bool isCooldown)
        {
            float parsed;
            if (value == null ||
                !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                Reply("expected a number, e.g. /aggro " + (isCooldown ? "cooldown 0.75" : "ratio 0.8"));
                return;
            }

            if (isCooldown)
            {
                if (parsed < 0f)
                {
                    Reply("cooldown cannot be negative");
                    return;
                }

                DynamicAggroMod.Settings.SwitchCooldown = parsed;
                Reply("switch cooldown set to " + parsed + "s");
            }
            else
            {
                // Above 1.0 a farther player would steal aggro, which inverts the mod.
                if (parsed <= 0f || parsed > 1f)
                {
                    Reply("ratio must be greater than 0 and at most 1");
                    return;
                }

                DynamicAggroMod.Settings.SwitchRatio = parsed;
                Reply("switch ratio set to " + parsed);
            }

            SettingsStore.TrySave();
        }

        private void ShowStatus()
        {
            Settings settings = DynamicAggroMod.Settings;

            Reply(settings.Enabled ? "on" : "off");
            Reply(string.Format(
                "cooldown {0}s, ratio {1}, self {2}",
                settings.SwitchCooldown,
                settings.SwitchRatio,
                settings.IncludeLocalPlayer ? "on" : "off"));

            if (!AggroHooks.Registered)
            {
                Reply("note: hooks are detached (addon disabled in HKMP)");
            }
            else if (!PlayerTargets.AnyRemoteInScene())
            {
                Reply("no other player in this room - behaving as vanilla");
            }
        }
    }
}
