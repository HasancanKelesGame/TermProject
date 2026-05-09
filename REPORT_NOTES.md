# FPS Arena Deathmatch Report Notes

Use this file while testing/profiling, then copy the final content into the submitted report.

## Project Summary

- Project: FPS Arena Deathmatch, singleplayer survival arena.
- Engine: Unity 2022.3.62f3.
- Main scenes:
  - `Assets/_Project/Scenes/Main_Menu.unity`
  - `Assets/_Project/Scenes/Arena_Main.unity`
- Core loop: player starts from the main menu, enters the arena, fights waves of NavMesh bots, collects pickups, gains score/kills, and survives until death.

## Main Systems

### FPS Controller

- Implemented with Unity `CharacterController`.
- Supports WASD movement, mouse look, sprinting, jumping, gravity, cursor lock, and pause/game-over input disabling.
- Uses collision flags plus ground probing for stable grounded/jump behavior.

### Weapon System

- `WeaponData` ScriptableObjects store weapon tuning: damage, range, fire rate, ammo, reload time, spread, spray growth, recoil, crosshair kick, and audio.
- `WeaponController` handles input, weapon switching, ammo, reload, hitscan raycasts, damage application, muzzle flash, hit impact, and weapon audio.
- Rifle supports automatic fire with spray growth and recovery.
- Pistol is semi-auto with stronger recoil/crosshair kick.

### Damage And Pickups

- Shared `Damageable` component handles health, healing, damage, death events, and health UI updates.
- Health and ammo pickups use a reusable `Pickup` script with type, amount, respawn, visual bob/rotation, and pickup sound.

### Bot AI

- Bots use Unity NavMeshAgent and an original finite state machine.
- States: Patrol, Detect, Chase, Attack, Retreat, Dead.
- Bots use detection range, attack range, field-of-view angle, and line-of-sight raycast.
- Bots chase the player, attack at cooldown intervals, stop on death, and notify score/wave systems.

### Wave System

- `WaveManager` spawns bots from configured spawn points.
- Spawn positions are validated against NavMesh.
- Waves scale bot count, health, damage, speed, and attack cooldown.
- `GameManager` tracks wave, kills, score, game state, pause, restart, and return-to-menu flow.

### UI And Feedback

- HUD shows health, ammo, wave, kills, score, and crosshair.
- Combat feedback includes hit marker, damage overlay, crosshair recoil lift, muzzle flash, hit impact, weapon bob/recoil, reload motion, and SFX.
- Menus include main menu, pause menu, game over menu, restart, and return-to-main-menu flow.

## Technical Challenges

- Stable first-person jumping: early grounded detection flickered, so the final controller combines `CharacterController.Move` collision flags with a short ground probe and jump buffering.
- Weapon audio: automatic rifle fire initially stacked one-shot clips, so automatic fire now uses a controlled AudioSource plus a release tail.
- Imported materials: some Synty materials appeared pink because of shader mismatch, so project-local Standard materials were created for affected visuals.
- Bot spawning: spawn points are sampled against NavMesh and filtered to avoid spawning too close to or directly in front of the player.

## Profiler Metrics

Source: custom `PerformanceLogger` CSV generated from Unity Editor Play Mode on May 9, 2026. The Game view was uncapped, so FPS values are mainly useful as evidence that the current arena load is light. The logger samples frame time, main-thread CPU time through `ProfilerRecorder`, GC allocation per frame, and memory usage.

- Test machine:
  - Mac model: MacBook Pro (Mac16,8).
  - CPU: Apple M4 Pro, 12-core (8 performance cores, 4 efficiency cores).
  - GPU: Apple M4 Pro, 16-core integrated GPU, Metal supported.
  - RAM: 24 GB.
- Unity play mode or build: Unity Editor Play Mode.
- Target resolution: Game view Free Aspect for this capture.
- Capture length: 98 samples across about 99 seconds.
- Normal combat:
  - Average FPS: 1937.2.
  - Approx frame time: most sampled averages were about 0.45-0.57 ms.
  - Main CPU cost: normally about 0.4-0.9 ms sampled main-thread time; highest sampled value was 5.54 ms.
  - Memory used: about 514-517 MB allocated during capture.
  - GC allocations: normally about 7.4 KB/frame sampled; highest sampled value was 10.917 KB/frame.
- Peak combat:
  - Wave tested: wave 6 reached.
  - Active bot count: 4 active bots.
  - Lowest FPS: 880.3.
  - Approx frame time: 1.136 ms for the lowest-FPS sample; one early single-frame spike reached 66.725 ms.
  - Main CPU cost: highest sampled main-thread CPU time was 5.54 ms, with normal combat samples staying far below the 16.67 ms budget for 60 FPS.
  - Memory used: highest allocated memory was 516.828 MB.
  - GC allocations: highest sampled GC allocation was 10.917 KB/frame.

## Optimization Notes

- Current bot count is capped to avoid excessive NavMeshAgent cost.
- Hitscan shooting avoids projectile physics overhead.
- HUD text only updates when values change instead of rebuilding every frame.
- Muzzle flash uses an existing particle system instead of spawning new objects per shot.
- Hit impact objects are short-lived; if profiling shows instantiate/destroy spikes, pooling is a possible future optimization.

Add profiler-driven decision:

- Observed bottleneck: no gameplay bottleneck appeared during the logged run. The game reached wave 6 with 4 active bots, and the lowest sampled FPS remained above 800.
- Change made or design decision: keep active bot count capped, continue using hitscan weapons, update HUD text only when values change, and defer pooling unless Unity Profiler later shows instantiate/destroy spikes at higher wave counts.
- Result: current scope has enough performance headroom for the final project. Main-thread CPU time, memory use, and GC allocation all stayed low for the required arena scale.

## Asset And Audio Credits

- Synty `POLYGON Starter Pack` / `POLYGON Generic`: environment, props, and character visual assets.
- Fun Assets `Guns Pack: Low Poly Guns Collection`: rifle/pistol visual weapon models and textures.
- TextMesh Pro essentials: HUD/menu text rendering.
- Unity AI Navigation: NavMeshSurface and NavMeshAgent support.
- Imported MP3 SFX under `Assets/_Project/Audio/SFX`:
  - `rifle.mp3`
  - `pistol.mp3`
  - `reload.mp3`
  - `gunswitch.mp3`
  - `botfiresound.mp3`
  - `botdeath.mp3`
  - `playerdeath.mp3`
  - `pickup.mp3`
  - `walking.mp3`
  - `jump.mp3`

All gameplay code is original project code. Imported assets are used for visuals/audio only.

## Gameplay Video Checklist

- Show main menu.
- Start arena from Play button.
- Show movement, sprint, jump, and weapon switching.
- Shoot rifle spray and pistol recoil.
- Kill bots and show score/kill HUD update.
- Show wave progression.
- Collect health/ammo pickups.
- Show pause menu resume/restart/main menu.
- Show game over and restart or main menu.
