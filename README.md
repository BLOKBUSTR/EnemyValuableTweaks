# EnemyValuableTweaks

Are you sick of those moments where you've put so much effort into tossing a monster down a pit, only for the orb to bounce everywhere and break on its own without giving you a single chance to get anywhere within grabbing range?

I am, because this happened WAY TOO MANY TIMES to me. So, I decided to fix this annoyance myself with this mod.

❗️ **All players must have this mod installed, and all clients must have the same config settings as the host!** Otherwise, visual desync at the very least will occur.

## ❇️ Current Features

⏲️ Enable, disable, and adjust the length of the indestructible timer duration of the orb.

💨 Automatically disable indestructibility when the orb's velocity slows to a stop. This is intended to cancel the main timer if the orb has already reached a standstill, so that the orb wouldn't remain invincible for longer than it needs to.

👐 Disable indestructibility when a player grabs ahold of it, since at this point the safety of the orb is now in your hands... *get it?*

🛒 Last but not least, disable indestructibility when the orb has been placed in a safe area, such as the C.A.R.T. or an extraction point.

This mod is extensively configurable, so you can fine-tune the orb's behavior to your liking. The default configs are intended to make a slightly more fair experience than vanilla without feeling overpowered. If you *do* want to make it either completely overpowered or brutally unfair, you have all the freedom to do so!

## 🚧 Roadmap

- Automatic config syncing
- Ability to adjust explosion probability for each moon phase?

Suggestions are welcome! Tell me what's on your mind in the [Discord Thread](https://discord.com/channels/1344557689979670578/1421636750174060635).

## 🔧 Configuration

Note that each category somewhat takes priority over the previous. Configs update immediately, especially while in-game if using RepoConfig.

- Timer
    - **EnableTimer**: Whether to enable the main timer that automatically disables indestructibility once expired. If disabled, the orb will never become destructible unless any of the following additional checks are enabled.
    - **TimerLength**: Time in seconds until the orb loses indestructibility. Vanilla default is 5 seconds.
- Additional Checks
    - **AdditionalChecksDelay**: Time in seconds before all following checks activate after the orb has initially spawned. This option reads from the main timer, and will not work if greater than **TimerLength**. Set to 0 to disable.
- Velocity
    - **EnableVelocityCheck**: Automatically disables indestructibility when the orb slows down to a certain velocity threshold. Takes precedence over the timer, meaning it will cut off the timer early if the orb has already reached a standstill.
    - **VelocityThreshold**: The minimum threshold for the velocity check.
- Player Grab
    - **EnablePlayerHold**: Automatically disables indestructibility when the orb is grabbed by a player.
    - **PlayerHoldTime**: Time in seconds that a player must continue holding onto the orb before indestructibility is disabled. Resets when the player lets go, so that it will not prematurely become destructible if the player gets distracted by something else. Can be set to 0 to immediately disable indestructibility.
- Safe Areas
    - **EnableSafeAreaCheck**: Disables indestructibility if the orb has been placed inside a safe area, such as the C.A.R.T. or an extraction point.
    - **SafeAreaTime**: Time in seconds that the orb must remain in a safe area to disable indestructibility. Works exactly like **PlayerHoldTime**.
- Debug
    - **EnableDebugLogs**: Enable all debug logs for this mod. Note that this will create a lot of spam in the console, so please keep this disabled for normal gameplay!

## ⚠️ Compatibility

This mod may potentially conflict with other mods that extensively patch the `Start()` and `Update()` methods of `EnemyValuable`.

## ❤️ Acknowledgements
- Huge thanks to OrigamiCoder and Omniscye in the R.E.P.O. Modding Server for helping me get started with modding, and for guiding me through the making of this mod!

Please report any issues on [GitHub](https://github.com/BLOKBUSTR/EnemyValuableTweaks) or the [Discord Thread](https://discord.com/channels/1344557689979670578/1421636750174060635).
