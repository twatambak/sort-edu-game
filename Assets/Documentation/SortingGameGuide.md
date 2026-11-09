# Sorting Game Guide

This guide covers the title flow, scenario content, reusable screens, and gameplay feedback in the Unity scene `Assets/Scenes/Game.unity`.

## Title screen and scenarios

The title screen presents three two-category scenarios:

| Button | Scenario asset | Stable ID |
|---|---|---|
| Animais | `Assets/Data/Scenarios/FarmAndJungleAnimals.asset` | `farm-jungle-animals` |
| Letras e números | `Assets/Data/Scenarios/LettersAndNumbers.asset` | `letters-numbers` |
| Brinquedos e ferramentas | `Assets/Data/Scenarios/ToysAndTools.asset` | `toys-tools` |

Selecting a card saves its scenario ID to `GameSettingsService`; it does not start a match. The play button begins a match with the chosen scenario and game mode. Cards show the first playable sprite from each category as a visual cue and use the category names for clarity.

A scenario is selectable only while it is registered in `GameDefinition` and both item types contain at least one enabled variant with a sprite and a positive weight. Add variants through the `ItemTypeDefinition` Inspector. To add or rename a title card, update the stable ID and label mapping in `TitleScreen` and the matching card in `OndeVaiSceneBuilder.Screens.cs`.

The title also exposes Drag and Drop, Question, Swipe Card, and Four-way Swipe modes. Four-way mode requires a playable four-category scenario. Choosing one of the three two-category cards while Four-way is selected switches the mode to Drag and Drop, so the selected scenario is never silently replaced when the match starts.

## Reusable UI screens

Use **Tools > Onde Vai > Build App UI** with `Game.unity` open to create or rebuild the app flow. The builder wires the disclaimer, title, settings, profile, progress, pause, result, and gameplay HUD screens to the scene's `GameController`.

The parental settings screen and privacy disclaimer are authored as reusable prefab instances:

- `Assets/Prefabs/UI/OptionsScreen.prefab`
- `Assets/Prefabs/UI/DisclaimerScreen.prefab`

Both use TextMesh Pro UGUI text with the project's LondrinaSolid SDF font. The disclaimer is shown until accepted; the settings screen is reachable from the title through the existing hold-to-confirm parental gate. Settings are persisted locally through `GameSettingsService`.

The settings screen also contains hold-to-confirm actions for the active child profile. **Apagar progresso** removes that child's match and attempt history but keeps the profile. **Excluir perfil** removes the active profile and its history, then switches the next match to guest mode. Both actions are hidden when no child profile is active, and database failures are shown in the screen.

The profile editor can also delete a selected profile; the progress screen can clear history for its selected profile. These existing actions keep the same database behavior and now show an error instead of closing or refreshing as if a failed delete succeeded.

The builder disables the old root-level `TitleScreen` when it installs `AppUI`, preventing two title canvases from displaying at once. Rebuilding replaces generated UI and systems; manually authored changes to those generated objects are not preserved. Changes to the reusable screens should be made in the builder and then rebuilt, rather than editing only a scene instance.

## Gameplay feedback

`GameFeedbackEffects` listens to `GameController` events and uses the pooled `SpriteBurstPool` rather than allocating Unity `ParticleSystem` objects:

- Item spawn: small category-colored sparkles.
- Correct sort: stars, an expanding ring, and two small firework bursts. The storage's existing `Tweenimation.Jelly` animation adds squash and rotation.
- Incorrect sort: a soft puff, a restrained camera position/rotation shake, and blue tear particles. Tutorial mistakes remain forgiving and do not produce tears.
- Match end: a confetti and firework celebration on both outcomes; a loss also produces a short tear burst.
- Countdown: small particles are emitted once per elapsed second, with a brighter star accent during the final five seconds.

`GameTimer.SecondElapsed` emits the remaining whole seconds. `OndeVaiSceneBuilder` connects the scene timer to `GameFeedbackEffects`; keep that reference wired if the scene is modified outside the builder. `SpriteBurstPool` prewarms particles and creates additional pooled renderers only when an effect exceeds the current pool.

All effects use generated sprites, so no additional texture or audio assets are required. The camera shake and particle settings can be tuned on `GameFeedbackEffects`; pool capacity and sorting order can be tuned on `SpriteBurstPool`.

Swipe-card mode clears its answer lock whenever it spawns a card. This is required for input to work after the first accepted item; the input component also resets stale pointer gestures when configured or disabled.

## Scene validation checklist

1. Open `Assets/Scenes/Game.unity` and run **Tools > Onde Vai > Build App UI**.
2. Confirm `GameController` has a `GameDefinition`, two storage references, and spawn points.
3. Confirm the three title cards show their category sprites and labels.
4. Select each scenario, press Play, and sort one item into each category.
5. Test each two-category scenario with Drag and Drop, Question, and Swipe Card. Four-way mode uses its own four-category scenario.
6. Confirm the options and disclaimer objects are prefab instances and their TMP text renders with the intended SDF font.
7. Test a correct answer, an incorrect answer, the low-time warning, and both match-ending paths.
8. Save the scene after building. In a player, check both first-launch disclaimer flow and the accepted-disclaimer title flow.
