using System.Collections.Generic;
using UnityEngine;

namespace HkmpDynamicAggro
{
    /// <summary>
    /// Decides who each enemy should be chasing, based on live distance rather than on
    /// HeroController.instance.
    ///
    /// Straight nearest-player would make an enemy jitter between two players who are
    /// roughly equidistant, changing target every physics tick. So a switch has to clear
    /// two gates: a cooldown since the last switch, and a distance margin over the
    /// incumbent. The result is aggro that visibly slides from one player to the other as
    /// you move, but commits once it has chosen.
    /// </summary>
    public static class AggroTracker
    {
        private class Entry
        {
            public GameObject Target;
            public float LastSwitchTime;
        }

        // Keyed by enemy GetInstanceID(). Cleared on scene change, which is also when
        // every enemy in it is destroyed.
        private static readonly Dictionary<int, Entry> Entries = new Dictionary<int, Entry>();

        /// <summary>Drops all per-enemy state. Called on scene change and on disconnect.</summary>
        public static void Reset()
        {
            Entries.Clear();
        }

        /// <summary>
        /// Returns the GameObject <paramref name="enemy"/> should target right now, or
        /// null to leave the vanilla target alone.
        /// </summary>
        public static GameObject Resolve(GameObject enemy)
        {
            if (enemy == null)
            {
                return null;
            }

            List<GameObject> candidates = PlayerTargets.Collect();
            if (candidates.Count == 0)
            {
                return null;
            }

            if (candidates.Count == 1)
            {
                // Nobody to compete over aggro; behave exactly like vanilla.
                return candidates[0];
            }

            Vector3 origin = enemy.transform.position;

            GameObject nearest = null;
            float nearestSqr = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                float sqr = (candidates[i].transform.position - origin).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = candidates[i];
                }
            }

            if (nearest == null)
            {
                return null;
            }

            int key = enemy.GetInstanceID();

            Entry entry;
            if (!Entries.TryGetValue(key, out entry))
            {
                entry = new Entry();
                entry.Target = nearest;
                entry.LastSwitchTime = Time.time;
                Entries[key] = entry;
                return nearest;
            }

            // Incumbent left the scene, died, or was recycled by HKMP's player pool.
            if (entry.Target == null
                || !entry.Target.activeInHierarchy
                || !candidates.Contains(entry.Target))
            {
                entry.Target = nearest;
                entry.LastSwitchTime = Time.time;
                return nearest;
            }

            if (nearest == entry.Target)
            {
                return entry.Target;
            }

            Settings settings = DynamicAggroMod.Settings;

            if (Time.time - entry.LastSwitchTime < settings.SwitchCooldown)
            {
                return entry.Target;
            }

            float currentSqr = (entry.Target.transform.position - origin).sqrMagnitude;

            // Ratio is on distance, so square it to compare against squared distances.
            float ratio = settings.SwitchRatio;
            if (nearestSqr >= currentSqr * ratio * ratio)
            {
                return entry.Target;
            }

            if (settings.DebugLog)
            {
                DynamicAggroMod.Instance.Log(string.Format(
                    "{0} switched aggro: {1} -> {2}",
                    enemy.name, entry.Target.name, nearest.name));
            }

            entry.Target = nearest;
            entry.LastSwitchTime = Time.time;
            return nearest;
        }
    }
}
