# EnemyValuableTweaks

This mod introduces various mechanical tweaks to the Enemy Valuable (the pink soul orb that drops from killed enemies) to make it a little more fair and interesting in certain scenarios.

Want to know what my first motivation for this mod was? You know those moments when you throw a monster down a pit, only for the orb to bounce everywhere and break on its own without giving you a chance to get anywhere close to catch it? Yeah, I know it too well because it happened WAY TOO MANY TIMES to me. So, I decided to make this mod as my own solution. On top of that, I figured it would be nice to implement some extra functionality to make the orb a little more interesting from vanilla gameplay.

❗️ Unless Host-Only Mode is enabled, **all players must have this mod installed, and all clients must have the same config settings as the host.** Otherwise, visual desync at the very least will occur.

## ❇️ Features

⏲️ Enable, disable, and adjust the length of the indestructible timer duration of the orb.

💨 Automatically disable indestructibility when the orb's velocity slows to a stop. This is intended to cancel the main timer if the orb has already reached a standstill, so that the orb wouldn't remain invincible for longer than it needs to.

👐 Disable indestructibility when a player grabs ahold of it, since at this point the safety of the orb is now in your hands... *get it?*

🛒 Disable indestructibility when the orb has been placed in a safe area, such as the C.A.R.T. or an extraction point.

🎲 Configure the probability of an orb exploding upon destruction for each Moon Phase! This can add a little more tension and unpredictability when handling orbs.

🔢 Configure the maximum amount of orbs that can spawn per enemy, per level. You can have as many or as few as you want, or even disable the limit entirely!

💀 Configure the probability of an orb being spawned when a monster is killed, depending on its difficulty level. This does not interfere with the maximum spawn amount.

🖥 Host-Only Mode, which can be enabled in the config. It disables all parts of the mod that are required to be synced with clients, leaving only behaviors that are host-authoritative.\
*Note: currently, this disables everything except features under the "Spawning" category in the config.*

This mod is extensively configurable, so you can fine-tune the orb's behavior to your liking. The default configs are intended to make a slightly more fair and interesting experience than vanilla without feeling overpowered. If you *do* want to make it either completely overpowered or brutally unfair, you have all the freedom to do so!

## 🚧 Roadmap
- Make multiplayer synchronization more robust
- Automatically edit Moon Phase modifier descriptions to tell current explosion probability

Suggestions are welcome! Tell me what's on your mind in the [Discord Thread](https://discord.com/channels/1344557689979670578/1421636750174060635).

## 🔧 Configuration

Configs update immediately, especially while in-game if using RepoConfig, unless otherwise stated.

<details>
<summary>Click to expand config list:</summary>

| Category                             | ConfigEntry                      | Default Value | Description                                                                                                                                                                                                                                                                                           |
|--------------------------------------|----------------------------------|:-------------:|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Timer**                            |                                  |               |                                                                                                                                                                                                                                                                                                       |
| &#124;                               | EnableTimer                      |     true      | Whether to enable the main timer that automatically disables indestructibility once expired. If disabled, the orb will never become destructible unless any of the following conditions are enabled.                                                                                                  |
| ↳                                    | TimerLength                      |      10f      | Time in seconds until the orb loses indestructibility. Vanilla default is 5 seconds.                                                                                                                                                                                                                  |
| **Additional Checks**                |                                  |               |                                                                                                                                                                                                                                                                                                       |
| ↳                                    | AdditionalChecksDelay            |      5f       | Time in seconds before all following checks activate after the orb has initially spawned. Will not work if greater than **TimerLength**. Additional checks take precedence over the main timer, meaning they will cut off the timer early if any one of their conditions have been satisfied.         |
| **Velocity**                         |                                  |               |                                                                                                                                                                                                                                                                                                       |
| &#124;                               | EnableVelocityCheck              |     true      | Automatically disables indestructibility when the orb slows down to the specified velocity threshold.                                                                                                                                                                                                 |
| ↳                                    | VelocityThreshold                |     0.01f     | The minimum threshold for the velocity check.                                                                                                                                                                                                                                                         |
| **Player Grab**                      |                                  |               |                                                                                                                                                                                                                                                                                                       |
| &#124;                               | EnablePlayerHold                 |     true      | Automatically disables indestructibility when the orb is grabbed by a player.                                                                                                                                                                                                                         |
| ↳                                    | PlayerHoldTime                   |      1f       | Time in seconds that a player must continue holding onto the orb before indestructibility is disabled. Resets when the player lets go, so that it will not prematurely become destructible if the player gets distracted by something else. Can be set to 0 to immediately disable indestructibility. |
| **Safe Areas**                       |                                  |               |                                                                                                                                                                                                                                                                                                       |
| &#124;                               | EnableSafeAreaCheck              |     true      | Disables indestructibility if the orb has been placed inside a safe area, such as the C.A.R.T. or an extraction point.                                                                                                                                                                                |
| ↳                                    | SafeAreaTime                     |      1f       | Time in seconds that the orb must remain in a safe area to disable indestructibility. Works exactly like **PlayerHoldTime**.                                                                                                                                                                          |
| **Moon Phase Explosion Probability** |                                  |               |                                                                                                                                                                                                                                                                                                       |
| &#124;                               | InitialExplosionProbability      |      0f       | The probability of orbs exploding at the start of a new game, before any moon phases have even taken effect.                                                                                                                                                                                          |
| &#124;                               | CrescentMoonExplosionProbability |     0.01f     | The probability of orbs exploding during the Crescent Moon phase, beginning on Level 5.                                                                                                                                                                                                               |
| &#124;                               | HalfMoonExplosionProbability     |     0.1f      | The probability of orbs exploding during the Half Moon phase, beginning on Level 10.                                                                                                                                                                                                                  |
| &#124;                               | FullMoonExplosionProbability     |     0.2f      | The probability of orbs exploding during the Full Moon phase, beginning on Level 15.                                                                                                                                                                                                                  |
| ↳                                    | SuperMoonExplosionProbability    |     0.95f     | The probability of orbs exploding during the Super Moon phase, beginning on Level 20.                                                                                                                                                                                                                 |
| **Spawning**                         |                                  |               |                                                                                                                                                                                                                                                                                                       |
| &#124;                               | MaxSpawnAmount                   |       3       | The maximum amount of orbs that can spawn per level. Vanilla default is 3; set to 0 for no limit. This option only applies on level reload.                                                                                                                                                           |
| &#124;                               | SmallSpawnProbability            |      1f       | The probability for small orbs to drop from Difficulty 1 monsters on death.                                                                                                                                                                                                                           |
| &#124;                               | MediumSpawnProbability           |      1f       | The probability for medium orbs to drop from Difficulty 2 monsters on death.                                                                                                                                                                                                                          |
| ↳                                    | LargeSpawnProbability            |      1f       | The probability for large orbs to drop from Difficulty 3 monsters on death.                                                                                                                                                                                                                           |
| **Hosting**                          |                                  |               |                                                                                                                                                                                                                                                                                                       |
| ↳                                    | HostOnlyMode                     |     false     | Disables all features that require clients to also have the mod installed. Currently, this disables everything except the "Spawning" options.                                                                                                                                                         |
| **Debug**                            |                                  |               |                                                                                                                                                                                                                                                                                                       |
| &#124;                               | EnableDebug                      |     false     | Whether to enable debug logging. Keep this disabled for normal gameplay.                                                                                                                                                                                                                              |
| ↳                                    | EnableTimerDebug                 |     false     | Whether to enable debug logging for this mod's timers. Note that this can create a lot of spam in the log. Keep this disabled for normal gameplay.                                                                                                                                                    |

</details>

## ⚠️ Known Issues

- Visual synchronization doesn't always work correctly on clients. I will be fixing these issues soon.

## ⚠️ Compatibility

✅ This mod is safe to use with:
- [RemoveOrbExplosion](https://thunderstore.io/c/repo/p/yazirushi/RemoveOrbExplosion/) by [yazirushi](https://thunderstore.io/c/repo/p/yazirushi/)

There are no known incompatibilities yet, but this mod may potentially conflict with other mods that extensively patch these methods:
- `EnemyValuable.Start`
- `EnemyValuable.Update`
- `EnemyHealth.Awake`
- `EnemyParent.Despawn`
- `RunManager.ChangeLevel`

## ❤️ Acknowledgements
- Huge thanks to [OrigamiCoder](https://thunderstore.io/c/repo/p/OrigamiCoder/) and [Omniscye](https://thunderstore.io/c/repo/p/Omniscye/) in the R.E.P.O. Modding Server for helping me get started with modding, and for guiding me through the making of this mod!

Please report any issues on [GitHub](https://github.com/BLOKBUSTR/EnemyValuableTweaks) or the [Discord Thread](https://discord.com/channels/1344557689979670578/1421636750174060635).
