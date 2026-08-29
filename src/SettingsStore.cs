using System;
using System.Reflection;

namespace HkmpDynamicAggro
{
    /// <summary>
    /// Best-effort persistence for settings changed from the chat command.
    ///
    /// The Modding API writes global settings on its own schedule (mod menu, shutdown).
    /// <c>ModHooks.SaveGlobalSettings</c> would flush immediately but is internal, so we
    /// reach it by reflection. If that ever stops resolving we simply skip the flush: the
    /// in-memory setting still applies for the session, and the API writes it out later.
    /// Nothing here is load-bearing.
    /// </summary>
    public static class SettingsStore
    {
        private static MethodInfo _saveMethod;
        private static bool _resolved;

        public static void TrySave()
        {
            try
            {
                if (!_resolved)
                {
                    _resolved = true;

                    Type modHooks = typeof(Modding.ModHooks);
                    _saveMethod = modHooks.GetMethod(
                        "SaveGlobalSettings",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                        null,
                        Type.EmptyTypes,
                        null);
                }

                if (_saveMethod != null)
                {
                    _saveMethod.Invoke(null, null);
                }
            }
            catch (Exception e)
            {
                DynamicAggroMod.Instance.LogWarn("Could not flush settings: " + e.Message);
            }
        }
    }
}
