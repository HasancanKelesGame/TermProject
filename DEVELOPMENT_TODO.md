# FPS Arena Deathmatch Development TODO

Project: TermProject - Option B: FPS Arena Deathmatch, singleplayer  
Unity version: 2022.3.62f3  
Working folder: `/Users/hasancankeles/Workspace/TermProject/TermProject`  
Assumed final target: May 10, 2026, based on the proposal roadmap

## How We Will Use This File

- Check items off with `[x]` only after the feature works in Play Mode.
- Keep implementation small and complete before adding polish.
- Every major milestone should end with a quick test pass and a git commit.
- If a task becomes too large, split it into smaller checklist items before coding.
- Prefer simple, readable systems over complex architecture.

## Target Scope

The goal is a compact, playable FPS arena game:

- First-person movement and camera look.
- Hitscan shooting with reload and weapon switching.
- AI bots using NavMesh and a finite state machine.
- Wave-based spawning with increasing difficulty.
- Player health, bot health, pickups, score, kill count, HUD, menu, restart, and game over.
- Basic arena blockout with cover and spawn points.
- Profiling notes for FPS, CPU, and memory.
- Final report and short gameplay video.

## Original Code Rule

- All gameplay code and project logic must be written specifically for this project.
- Do not import third-party controller, weapon, AI, wave, inventory, health, UI logic, or complete game-template systems.
- External packages/assets are allowed for visuals, models, textures, animations, sounds, music, fonts, particles, and editor-only workflow help.
- Prefer importing free visual/audio assets over creating custom art from scratch, as long as imported C# gameplay code is not used.
- Every external visual/audio asset used in the final project must be credited in the report.
- Tutorials can be referenced for learning, but copied code should be avoided. Any tutorial-inspired implementation must be rewritten and clearly understood.

## Estimated Time Budget

- Minimum playable version: 15-25 hours.
- Solid proposal-matching version: 40-60 hours.
- Submission-ready version with report/video/profiling: 55-80 hours.

## Priority Rules

1. Make the game playable first.
2. Make bots functional before making them smart.
3. Make one weapon complete before adding another.
4. Make one full wave loop complete before adding difficulty tuning.
5. Save polish, audio, and report work until the core loop works.

---

# Milestone 0 - Initial Setup

Goal: Prepare the Unity project so development is organized and safe to commit.

## Git And Project Baseline

- [x] Confirm `.gitignore` ignores Unity-generated folders.
- [x] Confirm only `Assets/`, `Packages/`, `ProjectSettings/`, and `.gitignore` are intended for version control.
- [x] Make initial commit with the fresh Unity project.
- [ ] Create a development branch if needed.
- [x] Confirm project opens without Unity package errors.
- [x] Confirm `SampleScene` opens in Unity.

## Unity Settings

- [x] Confirm visible meta files are enabled.
- [x] Confirm force text serialization is enabled.
- [ ] Set project name/player product name.
- [ ] Set target platform for final build.
- [ ] Decide target resolution/fullscreen behavior.
- [ ] Disable unused packages only if they create problems.

## Folder Structure

Create the initial folder structure under `Assets/`.

- [x] `Assets/_Project/Scenes`
- [x] `Assets/_Project/Scripts`
- [x] `Assets/_Project/Scripts/Player`
- [x] `Assets/_Project/Scripts/Weapons`
- [x] `Assets/_Project/Scripts/AI`
- [x] `Assets/_Project/Scripts/Game`
- [x] `Assets/_Project/Scripts/UI`
- [x] `Assets/_Project/Scripts/Pickups`
- [x] `Assets/_Project/Prefabs`
- [x] `Assets/_Project/Prefabs/Player`
- [x] `Assets/_Project/Prefabs/Weapons`
- [x] `Assets/_Project/Prefabs/AI`
- [x] `Assets/_Project/Prefabs/Pickups`
- [x] `Assets/_Project/Materials`
- [x] `Assets/_Project/Audio`
- [x] `Assets/_Project/Art`
- [x] `Assets/_Project/Settings`

## Definition Of Done

- [x] Project opens cleanly.
- [x] Folder structure exists.
- [x] Git status is clean after commit.

---

# Milestone 1 - Playable Prototype

Goal: Create a basic first-person player in a simple arena.

## Scene Setup

- [x] Create main gameplay scene: `Arena_Main`.
- [x] Move or replace `SampleScene`.
- [x] Add ground plane.
- [x] Add walls around the arena.
- [x] Add simple cover blocks.
- [x] Add lighting.
- [x] Add player spawn point.
- [ ] Add temporary bot spawn points.
- [ ] Add scene objects to clear hierarchy groups:
  - [x] `Environment`
  - [ ] `SpawnPoints`
  - [ ] `Gameplay`
  - [ ] `UI`

## First-Person Controller

- [x] Create `PlayerController` script.
- [x] Implement WASD movement.
- [x] Implement mouse look.
- [x] Clamp vertical camera rotation.
- [x] Add sprint.
- [x] Add gravity.
- [x] Add jump only if needed.
- [x] Use `CharacterController.Move` collision flags for stable grounded jump detection.
- [x] Lock and hide cursor during gameplay.
- [x] Unlock cursor on pause/game over.
- [x] Add player camera.
- [x] Tune movement speed.
- [x] Tune sprint speed.
- [x] Tune mouse sensitivity.

## Player Health

- [x] Create `Health` or `Damageable` script.
- [x] Add max health.
- [x] Add current health.
- [x] Add damage method.
- [x] Add heal method.
- [x] Add death event.
- [x] Attach health to player.
- [ ] Trigger game over when player dies.

## Basic HUD

- [x] Create original `HudController` script for health, ammo, wave, kills, and score text.
- [x] Create HUD canvas.
- [x] Add health display.
- [x] Add ammo display placeholder.
- [x] Add wave number placeholder.
- [x] Add score/kill count placeholder.
- [x] Add center crosshair.
- [x] Wire HUD text references to `HudController`.
- [x] Make HUD readable at target resolution.

## Prototype Test

- [x] Press Play and move around the arena.
- [x] Camera look feels usable.
- [x] Player cannot fall out of the arena.
- [x] Player collides with cover objects.
- [x] HUD appears.
- [x] No console errors.

## Definition Of Done

- [x] A player can move around the arena in first person.
- [x] Player has health.
- [x] Basic HUD is visible.
- [x] Scene is saved as the main gameplay scene.

---

# Milestone 2 - Combat Core

Goal: Implement one complete weapon and a shared damage system.

## Weapon Architecture

- [x] Create `WeaponData` ScriptableObject.
- [x] Add weapon name.
- [x] Add damage.
- [x] Add fire rate.
- [x] Add range.
- [x] Add magazine size.
- [x] Add reserve ammo.
- [x] Add reload time.
- [x] Add weapon spread if needed.
- [x] Create `WeaponController` script.
- [x] Create `Weapon` runtime script or component.
- [x] Decide if weapons are hitscan only for project scope.

## First Weapon

- [x] Create rifle weapon data.
- [x] Create rifle prefab or placeholder object.
- [x] Add muzzle transform.
- [x] Implement input for fire.
- [x] Implement raycast hit detection.
- [x] Apply damage to `Damageable` targets.
- [x] Add magazine ammo.
- [x] Add reserve ammo.
- [x] Add reload input.
- [x] Prevent shooting while reloading.
- [x] Prevent shooting with empty magazine.
- [x] Add simple muzzle flash placeholder.
- [x] Add simple hit impact placeholder.
- [x] Add visual rifle model from imported asset pack.
- [x] Add weapon sway/bob/recoil motion.
- [x] Add simple reload motion.
- [x] Add shot sound placeholder if available.
- [x] Add reload sound placeholder if available.
- [x] Add weapon switch sound.
- [x] Add automatic rifle audio stop and release tail.

## Weapon Switching

- [x] Add weapon slot list.
- [x] Add number key switching.
- [x] Add scroll wheel switching if time permits.
- [ ] Update HUD weapon name.
- [x] Update HUD ammo values.

## Damage Feedback

- [x] Show hit marker when player hits a bot.
- [x] Add screen feedback when player takes damage.
- [ ] Add simple bot damage flash if time permits.

## Combat Test

- [x] Place a dummy damageable target in scene.
- [x] Shoot target and confirm health decreases.
- [x] Reload works.
- [x] Ammo display updates correctly.
- [x] No shooting during reload.
- [x] No console errors.

## Definition Of Done

- [x] Player can shoot, damage, reload, and see ammo.
- [x] Damage system can be shared by player and bots.
- [x] One weapon is complete enough for the full game loop.

---

# Milestone 3 - Bot AI Foundation

Goal: Create one bot that navigates, detects the player, attacks, and dies.

## Bot Prefab

- [x] Create simple bot model placeholder.
- [x] Add collider.
- [x] Confirm Rigidbody is not needed for current NavMeshAgent setup.
- [x] Add NavMeshAgent.
- [x] Add `Damageable` or `Health`.
- [x] Add bot death handling.
- [x] Add bot visual feedback on hit/death.
- [x] Create bot prefab.

## NavMesh Setup

- [x] Install or enable AI Navigation package if needed.
- [x] Add NavMeshSurface to arena.
- [x] Mark walkable environment.
- [x] Mark obstacles.
- [x] Bake NavMesh.
- [x] Confirm bot can reach player area.
- [ ] Add spawn point validation for NavMesh.

## Bot FSM

Create `BotAI` using a simple finite state machine.

- [x] Define bot states:
  - [x] Patrol
  - [x] Detect
  - [x] Chase
  - [x] Attack
  - [x] Retreat
  - [x] Dead
- [x] Implement state enum.
- [x] Implement state transition method.
- [x] Add debug current state field in Inspector.
- [x] Add update loop for active state.

## Bot Sensing

- [x] Add detection range.
- [x] Add attack range.
- [x] Add field of view angle if time permits.
- [x] Add line-of-sight raycast.
- [x] Detect player only when visible.
- [x] Lose player after delay if hidden.

## Patrol State

- [ ] Add patrol points.
- [ ] Choose random patrol destination.
- [ ] Move between patrol points using NavMeshAgent.
- [ ] Wait briefly at patrol point.
- [x] Transition to chase/detect when player is seen.

## Chase State

- [x] Set NavMeshAgent destination to player position.
- [x] Stop chasing if player is lost for too long.
- [x] Transition to attack when in range and line of sight.

## Attack State

- [x] Face player while attacking.
- [x] Shoot or damage player at fixed interval.
- [x] Use raycast line of sight before damaging.
- [x] Add attack cooldown.
- [x] Transition back to chase if player moves out of range.

## Retreat State

- [ ] Trigger retreat at low health if time permits.
- [ ] Pick position away from player.
- [ ] Move away for short duration.
- [ ] Return to chase or attack after retreat.

## Death State

- [x] Stop NavMeshAgent.
- [x] Disable attacks.
- [x] Notify game manager of kill.
- [x] Destroy or pool bot after delay.

## Bot Test

- [x] Bot spawns in scene.
- [x] Bot patrols or idles.
- [x] Bot sees player.
- [x] Bot chases player.
- [x] Bot attacks player.
- [x] Player can kill bot.
- [x] Bot death increases kill count.
- [x] No console errors.

## Definition Of Done

- [x] One bot type works from spawn to death.
- [x] Bot behavior is understandable and tunable.
- [x] NavMesh is functional in the arena.

---

# Milestone 4 - Game Manager And Wave System

Goal: Build the full gameplay loop: start, spawn wave, fight, complete wave, scale difficulty, game over, restart.

## Game State

- [x] Create `GameManager` singleton or scene-level manager.
- [ ] Define game states:
  - [x] MainMenu
  - [x] Playing
  - [ ] WaveComplete
  - [x] Paused
  - [x] GameOver
- [x] Track current wave number.
- [x] Track active bots.
- [x] Track total kills.
- [x] Track score.
- [ ] Track elapsed survival time if needed.
- [x] Add event hook for bot death.
- [x] Add event hook for player death.

## Wave Spawning

- [x] Create `WaveManager`.
- [x] Add list of spawn points.
- [x] Add bot prefab reference.
- [x] Spawn wave 1 with small bot count.
- [x] Increase bot count per wave.
- [x] Increase bot health per wave if needed.
- [x] Increase bot fire rate per wave if needed.
- [x] Increase bot movement speed per wave if needed.
- [x] Prevent spawning directly in front of player if possible.
- [x] Validate spawn position is on NavMesh.
- [x] Start next wave after short delay.

## Difficulty Curve

- [x] Define wave 1 values.
- [x] Define max bot count.
- [x] Define bot health scaling.
- [x] Define bot damage scaling.
- [x] Define bot speed scaling.
- [ ] Keep difficulty fair for testing.
- [ ] Document final tuning values in report notes.

## Score System

- [x] Add score per kill.
- [ ] Add bonus per completed wave if time permits.
- [ ] Add survival time bonus if time permits.
- [x] Update HUD when score changes.

## Game Over And Restart

- [x] Show game over UI.
- [x] Show final wave.
- [x] Show final kills.
- [x] Show final score.
- [x] Add restart button.
- [ ] Add quit to menu button.
- [x] Reset player health/ammo on restart.
- [x] Clear active bots on restart.

## Pause Menu

- [x] Add pause input.
- [x] Pause time scale.
- [x] Show pause UI.
- [x] Resume button.
- [x] Restart button.
- [ ] Quit button if needed.

## Wave System Test

- [x] Wave 1 starts.
- [x] Bots spawn at valid locations.
- [x] Killing all bots completes wave.
- [x] Wave number increases.
- [x] Difficulty increases.
- [x] Player death shows game over.
- [x] Restart works from game over.
- [x] No console errors.

## Definition Of Done

- [x] The game has a complete survival wave loop.
- [x] The player can play until death and restart.
- [x] Score, wave, and kills update correctly.

---

# Milestone 5 - Pickups And Player Support Systems

Goal: Add health and ammo pickups to support longer waves.

## Pickup Architecture

- [x] Create base `Pickup` script.
- [x] Add pickup type.
- [x] Add respawn behavior if needed.
- [x] Add rotate/bob visual effect if time permits.
- [x] Add pickup sound placeholder.
- [ ] Add pickup UI feedback if time permits.

## Health Pickup

- [ ] Create health pickup prefab.
- [x] Restore fixed amount of health.
- [x] Do not exceed max health.
- [x] Destroy or hide after pickup.
- [x] Respawn after delay if needed.

## Ammo Pickup

- [ ] Create ammo pickup prefab.
- [x] Restore ammo for current weapon or all weapons.
- [x] Do not exceed max reserve ammo.
- [x] Destroy or hide after pickup.
- [x] Respawn after delay if needed.

## Pickup Spawning

- [x] Place fixed pickup locations in arena.
- [ ] Add optional pickup manager.
- [x] Tune pickup availability.
- [x] Ensure pickups do not make the game too easy.

## Pickup Test

- [x] Player can collect health pickup.
- [x] Player can collect ammo pickup.
- [x] HUD updates after pickup.
- [x] Pickups respawn if that feature is enabled.
- [x] No console errors.

## Definition Of Done

- [x] Health and ammo pickups work.
- [x] Pickups help survival without breaking difficulty.

---

# Milestone 6 - Second Weapon And Tuning

Goal: Add weapon variety only after the first weapon and game loop work.

## Second Weapon

- [x] Decide second weapon type:
  - [ ] Shotgun
  - [x] Pistol
  - [ ] SMG
- [x] Create second `WeaponData`.
- [x] Create second weapon prefab or placeholder.
- [x] Implement weapon-specific behavior only if needed.
- [x] Add weapon switching support.
- [x] Update HUD correctly.

## Weapon Balance

- [ ] Tune rifle damage.
- [ ] Tune rifle fire rate.
- [ ] Tune rifle magazine size.
- [ ] Tune reload time.
- [x] Tune second weapon damage.
- [x] Tune second weapon fire rate.
- [x] Tune second weapon ammo.
- [x] Confirm both weapons have a reason to exist.

## Definition Of Done

- [x] Player has 2 usable weapons.
- [x] Weapon switching is reliable.
- [x] Both weapons update ammo UI correctly.

---

# Milestone 7 - UI, Menus, And Game Feel

Goal: Make the game understandable and presentable.

## Main Menu

- [x] Create main menu UI.
- [x] Add Play button.
- [x] Add Quit button if needed.
- [x] Add title text.
- [x] Add basic background camera view or static scene.

## HUD Polish

- [ ] Health display is clear.
- [ ] Ammo display is clear.
- [ ] Wave display is clear.
- [ ] Kill count is clear.
- [ ] Score display is clear.
- [ ] Crosshair is centered.
- [x] Hit marker is visible.
- [x] Damage feedback is visible but not distracting.

## Game Over Screen

- [x] Final score displayed.
- [x] Final wave displayed.
- [x] Final kill count displayed.
- [x] Restart button works.
- [x] Main menu button works.

## Audio

- [x] Add weapon fire sound.
- [x] Add reload sound.
- [x] Add weapon switch sound.
- [x] Add bot attack sound.
- [x] Add bot death sound if available.
- [x] Add player death sound.
- [x] Add pickup sound.
- [x] Add movement loop sound for walking/running.
- [x] Add landing sound.
- [x] Add UI button sound if time permits.
- [ ] Add background music only if it does not distract.

## Visual Feedback

- [x] Add muzzle flash.
- [x] Add hit impact effect.
- [ ] Add bot death effect or animation placeholder.
- [ ] Add pickup visual effect if time permits.
- [ ] Add simple material colors for readability.

## Definition Of Done

- [ ] The game can be understood without explanation.
- [ ] UI and feedback make combat readable.
- [ ] Menus and restart flow work.

---

# Milestone 8 - Arena Design And Assets

Goal: Make the arena support combat and look acceptable for submission.

## Arena Layout

- [ ] Define arena size.
- [ ] Add perimeter walls.
- [ ] Add cover objects.
- [ ] Add vertical landmarks if useful.
- [ ] Add pickup locations.
- [ ] Add bot spawn locations.
- [ ] Add enough navigation space for bots.
- [ ] Avoid places where bots get stuck.
- [ ] Avoid places where player can permanently exploit AI.

## Materials And Art

- [x] Import or assign basic floor material.
- [x] Import or assign wall material.
- [ ] Add cover material.
- [ ] Add bot material.
- [ ] Add pickup material.
- [ ] Add weapon placeholder material.
- [x] Import free assets only if needed.
- [x] Confirm imported assets are visual/audio only, not gameplay logic.
- [ ] Track source/credit for every external asset.

## Lighting

- [ ] Add directional light or area lights.
- [ ] Make bots visible at combat distance.
- [ ] Make pickups visible.
- [ ] Avoid overly dark corners.
- [ ] Check final game view.

## Definition Of Done

- [ ] Arena supports combat and movement.
- [ ] Bots can navigate properly.
- [ ] Visuals are simple but readable.

---

# Milestone 9 - Profiling And Optimization

Goal: Collect required profiler metrics and fix obvious performance problems.

## Profiling Setup

- [ ] Open Unity Profiler.
- [ ] Profile in Play Mode.
- [ ] Profile during peak combat.
- [ ] Record target hardware/specs.
- [ ] Capture FPS/frame time.
- [ ] Capture CPU usage.
- [ ] Capture memory allocation.
- [ ] Capture GC allocation if visible.

## Performance Risks To Check

- [ ] Too many bots active.
- [ ] Expensive AI updates every frame.
- [ ] Excessive raycasts.
- [ ] UI allocations.
- [ ] Instantiate/destroy spikes during waves.
- [ ] Particle or audio spam.
- [ ] NavMeshAgent cost with higher bot counts.

## Optimization Tasks

- [ ] Limit max active bots.
- [ ] Use attack/detection timers instead of checking everything every frame where possible.
- [ ] Pool bots if instantiate/destroy spikes are significant.
- [ ] Pool impacts/muzzle flashes if needed.
- [ ] Avoid per-frame string allocations in HUD.
- [ ] Tune bot count for stable performance.

## Profiler Notes For Report

- [ ] Note average FPS during normal combat.
- [ ] Note lowest FPS during peak combat.
- [ ] Note main CPU bottleneck.
- [ ] Note memory allocation observations.
- [ ] Note at least one optimization made.
- [ ] Add screenshots if required by instructor.

## Definition Of Done

- [ ] Profiler data is collected.
- [ ] Major bottlenecks are documented.
- [ ] At least one optimization decision is explained.

---

# Milestone 10 - Build, Report, And Video

Goal: Prepare final deliverables.

## Build

- [x] Add main menu and arena scene to Build Settings.
- [x] Confirm scene order is correct.
- [ ] Make a local test build.
- [ ] Run the build outside the editor.
- [ ] Confirm controls work in build.
- [ ] Confirm restart/menu flow works in build.
- [ ] Confirm no missing assets.
- [ ] Confirm performance is acceptable.

## Source Code Cleanup

- [ ] Remove unused test scripts.
- [ ] Remove unused prefabs if they confuse project structure.
- [ ] Remove console debug spam.
- [ ] Keep useful debug fields in Inspector if helpful.
- [ ] Confirm script names match class names.
- [ ] Confirm no compile errors.
- [ ] Confirm no missing script components.

## Report

- [ ] Explain project concept.
- [ ] Explain FPS controller implementation.
- [ ] Explain weapon system architecture.
- [ ] Explain bot FSM states.
- [ ] Explain NavMesh usage.
- [ ] Explain wave spawning and difficulty curve.
- [ ] Explain pickups and scoring.
- [ ] Explain technical challenges.
- [ ] Include profiler metrics.
- [ ] Include optimization notes.
- [ ] Include asset/audio credits.
- [ ] Include screenshots if required.

## Gameplay Video

- [ ] Record 1-3 minutes of gameplay.
- [ ] Show movement.
- [ ] Show shooting.
- [ ] Show bot chasing/attacking.
- [ ] Show wave progression.
- [ ] Show pickups.
- [ ] Show game over/restart if possible.
- [ ] Keep video concise.

## Final Submission

- [ ] Confirm final git status.
- [ ] Commit final source.
- [ ] Push final branch/main.
- [ ] Confirm repository contains source code.
- [ ] Confirm ignored generated folders are not committed.
- [ ] Package build only if required.
- [ ] Submit report.
- [ ] Submit gameplay video.
- [ ] Submit repository link.

## Definition Of Done

- [ ] Build runs.
- [ ] Source is pushed.
- [ ] Report is complete.
- [ ] Video is complete.
- [ ] Submission requirements are satisfied.

---

# Implementation Order

Use this order unless there is a strong reason to change it:

1. Initial setup and folders.
2. Player movement and camera.
3. Basic arena.
4. Health and damage system.
5. One hitscan weapon.
6. Basic HUD.
7. One NavMesh bot.
8. Bot FSM.
9. Wave manager.
10. Game over and restart.
11. Pickups.
12. Second weapon.
13. UI polish.
14. Arena polish.
15. Profiling.
16. Report.
17. Video.
18. Final build and push.

---

# Session Log

Use this section to record what we completed each work session.

## 2026-05-02

- [x] Created initial development TODO.
- [x] Added initial Unity project folder structure.
- [x] Added original `PlayerController` script.
- [x] Added original `Damageable` health script.
- [x] Created `Arena_Main` scene.
- [x] Built initial arena floor and perimeter walls.
- [x] Wired player controller, character controller, player camera, and health component.
- [x] Verified player movement in Play Mode.
- [x] Decided to use free imported visual/audio assets where useful, while keeping gameplay code original.
- [x] Imported Synty visual assets and confirmed no imported `.cs`, `.asmdef`, `.asmref`, or `.dll` files.
- [x] Added visual cover props with colliders.
- [x] Reworked jump detection around `CollisionFlags.Below` to reduce grounded flicker without allowing flight.
- [x] Temporary jump debug overlay/logging removed before first commit.
- [ ] Next session target: add basic HUD and crosshair.
