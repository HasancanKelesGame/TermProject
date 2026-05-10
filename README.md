# TermProject - FPS Arena Deathmatch

Singleplayer first-person arena deathmatch prototype built in Unity.

## Project Info

- Unity version: 2022.3.62f3
- Menu scene: `Assets/_Project/Scenes/Main_Menu.unity`
- Main scene: `Assets/_Project/Scenes/Arena_Main.unity`
- Project source folder: `Assets/_Project`
- Development checklist: `DEVELOPMENT_TODO.md`
- Report/profiling notes: `REPORT_NOTES.md`

## Current State

- First-person player movement with mouse look, sprint, jump, crouch, gravity, and runtime hitbox/camera height changes while crouching.
- Shared `Damageable` health component.
- Arena scene with floor, perimeter walls, cover props, colliders, pickups, spawn points, patrol points, and a baked NavMesh.
- Hitscan rifle and semi-auto pistol with weapon switching, separate ammo, reload, muzzle flash, hit impact, recoil, view bob, and reload motion.
- Rifle spray and pistol recoil affect aim/crosshair behavior.
- Hit marker, headshot damage, body damage, and player damage overlay provide combat feedback.
- SFX are wired for weapon fire/reload/switching, rifle release tail, bot attack/death, player death, pickups, movement loop, and landing.
- NavMesh-based bot with finite-state patrol/detect/chase/attack/death behavior.
- Reusable bot prefab uses an imported SciFi Space Soldier visual, bot weapon visual, body/head hitboxes, Animator-driven locomotion/death, and a muzzle light flash.
- Bot attacks use line-of-sight raycasts from the bot weapon to the player camera target, so cover blocks damage.
- Scene `GameManager` tracks wave, kills, and score; bot deaths update the HUD.
- `WaveManager` spawns bots from configured spawn points, tracks active bots, advances waves, and scales count/health/damage/speed.
- Normal waves keep scaling past wave 4, and optional Crazy Mode starts denser enemy waves with unlimited ammo/high rifle fire rate while using tuned caps to stay playable.
- Main menu includes an optional Crazy Mode toggle before starting the arena scene.
- Bot patrol behavior uses configured patrol points, NavMesh movement, patrol wait timing, and optional CSV debug logging for stuck-path diagnosis.
- Player death stops the run, shows final wave/kills/score, unlocks the cursor, and supports restarting or returning to the main menu.
- Pause menu freezes gameplay, unlocks the cursor, and supports resume, restart, or returning to the main menu.
- Health and ammo pickups restore player resources, hide when collected, and respawn after a short delay.
- Health and ammo pickups use readable project-local materials.
- Main menu scene provides Play/Quit entry flow and Build Settings scene order.
- Custom `PerformanceLogger` can write CSV performance samples for report evidence.
- Imported visual assets are used only for presentation.

## Controls

- `WASD`: move
- `Mouse`: look
- `Left Mouse`: fire current weapon
- `R`: reload current weapon
- `1` / `2`: switch weapon
- `Mouse Wheel`: switch weapon
- `Left Shift`: sprint
- `Left Control`: crouch
- `Space`: jump
- `Esc`: pause/resume

## Original Code Rule

Gameplay code and project logic are original to this project. Imported packages may be used for visuals, models, textures, materials, animations, sounds, music, fonts, particles, and editor-only workflow help. Imported gameplay systems, FPS controllers, AI controllers, weapon systems, wave managers, inventory systems, and health/damage logic should not be used.

## Imported Assets

- Synty `POLYGON Starter Pack` / `POLYGON Generic` visual assets from the Unity Asset Store.
- Imported Synty assets are used as visual/environment assets only.
- Fun Assets `Guns Pack: Low Poly Guns Collection` visual weapon models/textures from the Unity Asset Store.
- Imported weapon assets are used as visual rifle/pistol assets only.
- `SciFi Space Soldier` visual/animation asset is used for the enemy character model only.
- Imported MP3 sound effects under `Assets/_Project/Audio/SFX`: `rifle`, `pistol`, `reload`, `gunswitch`, `botfiresound`, `botdeath`, `playerdeath`, `pickup`, `walking`, and `jump`.
- Imported `headshot.mp3` is used as a presentation-only headshot feedback sound.
- Imported audio assets are used as presentation-only SFX; audio playback logic is original project code.
- No imported `.cs`, `.asmdef`, `.asmref`, or `.dll` gameplay code is present under imported asset folders.
- TextMesh Pro essentials are included for HUD text rendering.
- Unity AI Navigation is included for NavMesh baking and navigation components.

## Notes

Some imported Synty materials use Shader Graph shaders. If a material appears pink, create a project-local material using Unity's `Standard` shader and assign the imported texture to its Albedo slot. `M_Bot_StarterAtlas` is used as the project-local bot character material.
