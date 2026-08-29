# Dynamic Aggro

An HKMP addon that makes Hollow Knight enemies chase **whichever player is closest**,
re-evaluated continuously, instead of always chasing `HeroController.instance` (the scene
host's Knight).

## The problem it solves

HKMP designates a **scene host** per room — the first player to enter. Only that machine
runs the real enemy FSMs; on the other machine enemies are replicas driven over the wire.
HKMP even ships `CheckCanSeeHero` and `ChaseObject*` in its list of *replicated* actions,
meaning the client never evaluates targeting at all.

Vanilla enemy AI resolves its target as `HeroController.instance` — the *local* Knight. So
on the scene host's machine, that is always the scene host. The other player is invisible
to the AI and can only be hit by walking into a hitbox. One player tanks everything, the
other is a permanent sidekick.

## How it works

Instead of `HeroController.instance`, the target is chosen by live distance:

1. `PlayerTargets` collects the candidates — the local Knight plus every HKMP remote player
   with `IsInLocalScene` and a live `PlayerObject`.
2. `AggroTracker` picks the nearest to that specific enemy, with two dampers so aggro does
   not stutter between two players standing together:
   - **`SwitchCooldown`** (0.75s) — minimum time an enemy keeps a target.
   - **`SwitchRatio`** (0.8) — a rival must be at least 20% closer to steal aggro.
3. `AggroHooks` swaps the chosen player into the action's target field, runs the vanilla
   logic, then restores the old value.

Hooked actions (all verified present in this install's `Assembly-CSharp.dll`):

| Action | Hook point | Field swapped |
| --- | --- | --- |
| `ChaseObject` | `OnFixedUpdate` | `target` |
| `ChaseObjectV2` | `OnFixedUpdate` | `target` |
| `ChaseObjectGround` | `OnFixedUpdate` | `target` |
| `DistanceFly` | `OnFixedUpdate` | `target` |
| `DistanceFlyV2` | `OnFixedUpdate` | `target` |
| `FaceObject` | `DoFace` | `objectB` |
| `GetHero` | `OnEnter` | `storeResult` |

Two deliberate choices:

- **`OnFixedUpdate`, not `OnEnter`.** This is what makes aggro *dynamic*. The target is
  recomputed every physics tick, so it slides between players as they move, rather than
  being locked in when the FSM state was entered.
- **`GetHero` is the force multiplier.** Many enemy FSMs call it once to fill a variable
  that the rest of the FSM reads. Rewriting its result retargets every downstream action in
  that FSM at once — including ones not hooked individually.

## Safety rails

- Swaps only when the field currently points at the local Knight. Bosses aim these same
  actions at spawn markers and arena corners; those are left alone.
- Completely inert when no remote player shares the scene — solo play is bit-for-bit vanilla.
- Original field values are restored after each call, so shared FSM variables do not leak.
- No networking. HKMP disables enemy FSMs on non-host clients, so these hooks only ever fire
  on the machine that simulates the enemy. The decision is local and needs no agreement.

## Build

Needs no .NET SDK — it uses the compiler shipped with Windows.

```powershell
.\build.ps1     # -> build\HkmpDynamicAggro.dll
.\install.ps1   # -> Mods\HkmpDynamicAggro\
```

Both players must install it. Uninstall by deleting the `Mods\HkmpDynamicAggro` folder.

## Settings

`AppData\LocalLow\Team Cherry\Hollow Knight\HkmpDynamicAggro.GlobalSettings.json`,
created on first run.

| Key | Default | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Master switch |
| `IncludeLocalPlayer` | `true` | Whether your own Knight can be targeted |
| `SwitchCooldown` | `0.75` | Seconds before an enemy may re-target |
| `SwitchRatio` | `0.8` | How much closer a rival must be to steal aggro |
| `DebugLog` | `false` | Log every switch to `ModLog.txt` |

## Known limits

- **Bosses are not covered by default.** Many use bespoke FSMs with hardcoded hero
  references rather than the generic `ChaseObject*` actions. They need case-by-case work.
- **`CheckCanSeeHero` is not hooked.** It resolves the hero through a `LineOfSightDetector`
  component, not a swappable field, so initial *detection* still keys off the scene host.
  Once an enemy is awake and chasing, targeting is dynamic. Making wake-up dynamic too
  means patching the detector, which is a larger change.
- Built and verified against **HKMP 2.4.3.0**. The hooks target vanilla PlayMaker actions,
  so they should survive HKMP updates; an HKMP API change would break `HkmpAddon` only.
