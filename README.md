# TermProject - FPS Arena Deathmatch

Singleplayer first-person arena deathmatch prototype built in Unity.

## Project Info

- Unity version: 2022.3.62f3
- Main scene: `Assets/_Project/Scenes/Arena_Main.unity`
- Project source folder: `Assets/_Project`
- Development checklist: `DEVELOPMENT_TODO.md`

## Current State

- First-person player movement with mouse look, sprint, jump, and gravity.
- Shared `Damageable` health component.
- Simple arena scene with floor, perimeter walls, visual cover props, and colliders.
- Hitscan rifle and semi-auto pistol with weapon switching, separate ammo, reload, muzzle flash, hit impact, recoil, view bob, and reload motion.
- Hit marker and player damage overlay provide combat feedback.
- NavMesh-based test bot with finite-state patrol/detect/chase/attack/death behavior.
- Reusable basic bot prefab with Synty character visual feedback.
- Scene `GameManager` tracks wave, kills, and score; bot deaths update the HUD.
- `WaveManager` spawns bots from configured spawn points, tracks active bots, advances waves, and scales count/health/damage/speed.
- Player death stops the run, shows final wave/kills/score, unlocks the cursor, and supports restarting the scene.
- Health and ammo pickups restore player resources, hide when collected, and respawn after a short delay.
- Imported visual assets are used only for presentation.

## Controls

- `WASD`: move
- `Mouse`: look
- `Left Mouse`: fire current weapon
- `R`: reload current weapon
- `1` / `2`: switch weapon
- `Mouse Wheel`: switch weapon
- `Left Shift`: sprint
- `Space`: jump
- `Esc`: unlock/lock cursor

## Original Code Rule

Gameplay code and project logic are original to this project. Imported packages may be used for visuals, models, textures, materials, animations, sounds, music, fonts, particles, and editor-only workflow help. Imported gameplay systems, FPS controllers, AI controllers, weapon systems, wave managers, inventory systems, and health/damage logic should not be used.

## Imported Assets

- Synty `POLYGON Starter Pack` / `POLYGON Generic` visual assets from the Unity Asset Store.
- Imported Synty assets are used as visual/environment assets only.
- Fun Assets `Guns Pack: Low Poly Guns Collection` visual weapon models/textures from the Unity Asset Store.
- Imported weapon assets are used as visual rifle/pistol assets only.
- No imported `.cs`, `.asmdef`, `.asmref`, or `.dll` gameplay code is present under imported asset folders.
- TextMesh Pro essentials are included for HUD text rendering.
- Unity AI Navigation is included for NavMesh baking and navigation components.

## Notes

Some imported Synty materials use Shader Graph shaders. If a material appears pink, create a project-local material using Unity's `Standard` shader and assign the imported texture to its Albedo slot. `M_Bot_StarterAtlas` is used as the project-local bot character material.
