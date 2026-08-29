using System.Collections.Generic;
using Hkmp.Api.Client;
using UnityEngine;

namespace HkmpDynamicAggro
{
    /// <summary>
    /// Enumerates every GameObject an enemy is allowed to aggro onto: the local Knight
    /// plus each HKMP remote player currently standing in this scene.
    /// </summary>
    public static class PlayerTargets
    {
        // Reused across calls; this runs inside FixedUpdate for every chasing enemy,
        // so allocating a list per call would be a steady stream of GC pressure.
        private static readonly List<GameObject> Buffer = new List<GameObject>(4);

        /// <summary>
        /// Fills and returns the shared candidate buffer. The caller must not hold on
        /// to the list past the current call.
        /// </summary>
        public static List<GameObject> Collect()
        {
            Buffer.Clear();

            Settings settings = DynamicAggroMod.Settings;

            if (settings.IncludeLocalPlayer)
            {
                HeroController hero = HeroController.instance;
                if (hero != null && hero.gameObject.activeInHierarchy)
                {
                    Buffer.Add(hero.gameObject);
                }
            }

            IClientApi api = HkmpAddon.Api;
            if (api == null)
            {
                return Buffer;
            }

            IReadOnlyCollection<IClientPlayer> players = api.ClientManager.Players;
            if (players == null)
            {
                return Buffer;
            }

            foreach (IClientPlayer player in players)
            {
                if (player == null || !player.IsInLocalScene)
                {
                    continue;
                }

                GameObject playerObject = player.PlayerObject;
                if (playerObject == null || !playerObject.activeInHierarchy)
                {
                    continue;
                }

                Buffer.Add(playerObject);
            }

            return Buffer;
        }

        /// <summary>
        /// True when at least one remote player shares the scene. Used as an early-out so
        /// that solo play, and play where your friend is in another room, costs nothing
        /// and behaves exactly like vanilla.
        /// </summary>
        public static bool AnyRemoteInScene()
        {
            IClientApi api = HkmpAddon.Api;
            if (api == null)
            {
                return false;
            }

            IReadOnlyCollection<IClientPlayer> players = api.ClientManager.Players;
            if (players == null)
            {
                return false;
            }

            foreach (IClientPlayer player in players)
            {
                if (player != null && player.IsInLocalScene && player.PlayerObject != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
