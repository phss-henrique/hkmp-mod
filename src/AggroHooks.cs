using HutongGames.PlayMaker;
using UnityEngine;
using Actions = HutongGames.PlayMaker.Actions;

namespace HkmpDynamicAggro
{
    /// <summary>
    /// Substitutes the aggro target inside the PlayMaker actions Hollow Knight enemies use
    /// to chase and face the player.
    ///
    /// The swap is scoped to the original call: we write the chosen player into the target
    /// field, run the vanilla logic, then put the old value back. The FsmGameObject we write
    /// to is often a shared FSM variable, so leaving it modified would leak into unrelated
    /// states of the same FSM.
    ///
    /// Chase and fly actions are hooked on OnFixedUpdate rather than OnEnter, which is what
    /// makes the aggro dynamic: the target is re-evaluated every physics tick, so it tracks
    /// players as they move instead of being fixed when the state was entered.
    ///
    /// Note on multiplayer authority: HKMP only runs enemy FSMs on the scene host and
    /// disables them on client entities. So these hooks naturally only fire on the machine
    /// that actually simulates the enemy, and no networking is required.
    /// </summary>
    public static class AggroHooks
    {
        // Register/Deregister are driven both by mod startup and by HKMP enabling or
        // disabling the addon at runtime, so they have to be safe to call twice.
        private static bool _registered;

        public static bool Registered
        {
            get { return _registered; }
        }

        public static void Register()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;

            On.HutongGames.PlayMaker.Actions.ChaseObject.OnFixedUpdate += ChaseObjectOnFixedUpdate;
            On.HutongGames.PlayMaker.Actions.ChaseObjectV2.OnFixedUpdate += ChaseObjectV2OnFixedUpdate;
            On.HutongGames.PlayMaker.Actions.ChaseObjectGround.OnFixedUpdate += ChaseObjectGroundOnFixedUpdate;
            On.HutongGames.PlayMaker.Actions.DistanceFly.OnFixedUpdate += DistanceFlyOnFixedUpdate;
            On.HutongGames.PlayMaker.Actions.DistanceFlyV2.OnFixedUpdate += DistanceFlyV2OnFixedUpdate;
            On.HutongGames.PlayMaker.Actions.FaceObject.DoFace += FaceObjectDoFace;
            On.GetHero.OnEnter += GetHeroOnEnter;
        }

        public static void Deregister()
        {
            if (!_registered)
            {
                return;
            }
            _registered = false;

            On.HutongGames.PlayMaker.Actions.ChaseObject.OnFixedUpdate -= ChaseObjectOnFixedUpdate;
            On.HutongGames.PlayMaker.Actions.ChaseObjectV2.OnFixedUpdate -= ChaseObjectV2OnFixedUpdate;
            On.HutongGames.PlayMaker.Actions.ChaseObjectGround.OnFixedUpdate -= ChaseObjectGroundOnFixedUpdate;
            On.HutongGames.PlayMaker.Actions.DistanceFly.OnFixedUpdate -= DistanceFlyOnFixedUpdate;
            On.HutongGames.PlayMaker.Actions.DistanceFlyV2.OnFixedUpdate -= DistanceFlyV2OnFixedUpdate;
            On.HutongGames.PlayMaker.Actions.FaceObject.DoFace -= FaceObjectDoFace;
            On.GetHero.OnEnter -= GetHeroOnEnter;
        }

        // --- shared swap logic -------------------------------------------------------

        /// <summary>
        /// If the field currently points at the local Knight, replace it with the nearest
        /// player and report the old value through <paramref name="saved"/>. Returns false
        /// (and swaps nothing) whenever vanilla behaviour should stand.
        /// </summary>
        private static bool Begin(FsmStateAction action, FsmGameObject field, out GameObject saved)
        {
            saved = null;

            if (!DynamicAggroMod.Settings.Enabled)
            {
                return false;
            }

            if (field == null)
            {
                return false;
            }

            // Solo, or the other player is in another room: nothing to arbitrate.
            if (!PlayerTargets.AnyRemoteInScene())
            {
                return false;
            }

            GameObject current = field.Value;
            if (current == null)
            {
                return false;
            }

            HeroController hero = HeroController.instance;
            if (hero == null)
            {
                return false;
            }

            // Only hijack actions that are aimed at the player. Bosses and scripted
            // sequences point these same actions at spawn markers, arena corners and
            // siblings, and retargeting those would break their choreography.
            if (current != hero.gameObject)
            {
                return false;
            }

            GameObject enemy = action.Owner;
            if (enemy == null)
            {
                return false;
            }

            GameObject chosen = AggroTracker.Resolve(enemy);
            if (chosen == null || chosen == current)
            {
                return false;
            }

            saved = current;
            field.Value = chosen;
            return true;
        }

        private static void End(FsmGameObject field, bool swapped, GameObject saved)
        {
            if (swapped)
            {
                field.Value = saved;
            }
        }

        // --- chase actions -----------------------------------------------------------

        private static void ChaseObjectOnFixedUpdate(
            On.HutongGames.PlayMaker.Actions.ChaseObject.orig_OnFixedUpdate orig,
            Actions.ChaseObject self)
        {
            GameObject saved;
            bool swapped = Begin(self, self.target, out saved);
            try { orig(self); }
            finally { End(self.target, swapped, saved); }
        }

        private static void ChaseObjectV2OnFixedUpdate(
            On.HutongGames.PlayMaker.Actions.ChaseObjectV2.orig_OnFixedUpdate orig,
            Actions.ChaseObjectV2 self)
        {
            GameObject saved;
            bool swapped = Begin(self, self.target, out saved);
            try { orig(self); }
            finally { End(self.target, swapped, saved); }
        }

        private static void ChaseObjectGroundOnFixedUpdate(
            On.HutongGames.PlayMaker.Actions.ChaseObjectGround.orig_OnFixedUpdate orig,
            Actions.ChaseObjectGround self)
        {
            GameObject saved;
            bool swapped = Begin(self, self.target, out saved);
            try { orig(self); }
            finally { End(self.target, swapped, saved); }
        }

        private static void DistanceFlyOnFixedUpdate(
            On.HutongGames.PlayMaker.Actions.DistanceFly.orig_OnFixedUpdate orig,
            Actions.DistanceFly self)
        {
            GameObject saved;
            bool swapped = Begin(self, self.target, out saved);
            try { orig(self); }
            finally { End(self.target, swapped, saved); }
        }

        private static void DistanceFlyV2OnFixedUpdate(
            On.HutongGames.PlayMaker.Actions.DistanceFlyV2.orig_OnFixedUpdate orig,
            Actions.DistanceFlyV2 self)
        {
            GameObject saved;
            bool swapped = Begin(self, self.target, out saved);
            try { orig(self); }
            finally { End(self.target, swapped, saved); }
        }

        // objectA is the enemy doing the turning, objectB is what it turns towards.
        private static void FaceObjectDoFace(
            On.HutongGames.PlayMaker.Actions.FaceObject.orig_DoFace orig,
            Actions.FaceObject self)
        {
            GameObject saved;
            bool swapped = Begin(self, self.objectB, out saved);
            try { orig(self); }
            finally { End(self.objectB, swapped, saved); }
        }

        // --- GetHero -----------------------------------------------------------------

        /// <summary>
        /// GetHero writes HeroController.instance into an FSM variable that the rest of the
        /// FSM then reads. Rewriting the result here retargets every downstream action in
        /// that FSM at once, including ones we do not hook individually.
        ///
        /// Unlike the chase hooks this one is not restored afterwards: the whole point is
        /// that the stored value persists for the FSM to use. It is refreshed each time the
        /// FSM re-enters a state containing GetHero.
        /// </summary>
        private static void GetHeroOnEnter(On.GetHero.orig_OnEnter orig, GetHero self)
        {
            orig(self);

            if (!DynamicAggroMod.Settings.Enabled)
            {
                return;
            }

            if (self.storeResult == null || !PlayerTargets.AnyRemoteInScene())
            {
                return;
            }

            HeroController hero = HeroController.instance;
            if (hero == null || self.storeResult.Value != hero.gameObject)
            {
                return;
            }

            GameObject enemy = self.Owner;
            if (enemy == null)
            {
                return;
            }

            GameObject chosen = AggroTracker.Resolve(enemy);
            if (chosen != null)
            {
                self.storeResult.Value = chosen;
            }
        }
    }
}
