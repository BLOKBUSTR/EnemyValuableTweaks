# Changelog

### 1.3.0

- Updated to R.E.P.O. 0.4.0.
- Upgraded BepInEx dependency.
- Various code optimizations, tweaks and cleanup.
- Fixed an oversight where the explosion probability logic was being skipped in Singleplayer.
- Altered the default config values for random spawn chance to 100% for all difficulties, making this feature completely opt-in.
- README tweaks.

The config has slightly changed again, so I suggest deleting/resetting it.

---

### 1.2.0
- Added configurable probabilities for the orb to drop when a monster is killed, depending on its difficulty level. This does not interfere with the maximum spawn amount.
- Added a "Host-Only Mode" option to the config, which disables all features that require multiplayer synchronization, leaving only behaviors that are host-authoritative. Currently, this disables everything except the "Spawning" options.
- General code and README tweaks

**Note**: The "7 - Max Spawn Amount" config category has been renamed to "7 - Spawning". This will not break the mod, but the "Max Spawn Amount" option may end up being duplicated if you've played with a previous version of this mod.

---

### 1.1.0
- Added configurable max spawn amount.
- Upgrade BepInEx dependency.

---

### 1.0.3
- Recompiled for R.E.P.O. 0.3.0 (The Monster Update) to indicate compatibility.
- Refactored project structure.

### 1.0.2
- Implemented configurable explosion probability for each moon phase.
- Cleaned up debug logging implementation and split logs into two types: Timers and General.
- Adjusted README's "Configuration" section to use an expandable list to slightly reduce initial clutter, and fixed some other discrepancies.

### 1.0.1
- Update README to properly link to this mod's Discord thread

### 1.0.0
- Initial release 🎉
