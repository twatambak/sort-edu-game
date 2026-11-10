# Sorting Game Guide

This guide covers the title flow, scenario content, screens, gameplay feedback and code layout of the Unity scene `Assets/Scenes/Game.unity`.

## Title screen and scenarios

The title screen presents four two-category scenarios:

| Button | Scenario asset | Stable ID |
|---|---|---|
| Animais | `Assets/Data/Scenarios/FarmAndJungleAnimals.asset` | `farm-jungle-animals` |
| Letras e números | `Assets/Data/Scenarios/LettersAndNumbers.asset` | `letters-numbers` |
| Brinquedos e ferramentas | `Assets/Data/Scenarios/ToysAndTools.asset` | `toys-tools` |
| Comidas | `Assets/Data/Scenarios/HealthyAndUnhealthyFood.asset` | `foods` |

Selecting a card saves its scenario ID to `GameSettingsService`; it does not start a match. The play button begins a match with the chosen scenario and game mode.

A scenario is selectable only while it is registered in `GameDefinition` and both item types contain at least one enabled variant with a sprite and a positive weight. Cards of scenarios without playable content are disabled. The highlighted card is always the scenario the next match will use, so a saved scenario that stopped being playable falls back visibly to the first playable one. Add variants through the `ItemTypeDefinition` Inspector. To add or rename a title card, update the stable ID in `TitleScreen` and the matching card in the `AppUI` prefab.

Two game modes exist: Drag and Drop and Swipe (two directions). `GameModes.IsSupported` is the single check for playable modes; values of removed modes found in old settings are sanitized to Drag and Drop.

## Screens

The app flow lives in `Assets/Prefabs/UI/AppUI.prefab`, instanced in the scene. It holds the title, profile, progress, pause and results screens and the gameplay HUD (pause button, timer, error counter), and nests the `OptionsScreen` and `DisclaimerScreen` prefabs. `GameFlowController` on its root drives navigation.

**Tools > Onde Vai > Build App UI** generates a fresh UI from code. The scene now uses a hand-edited `AppUI`, and the generator does not know about those edits (food scenario card, HUD timer, results screen), so rebuilding replaces them. Use it only to start over.

Some widgets depend on data and are created at runtime with `RuntimeUI` instead of being authored in the prefab:

- Results screen: a row of three stars under the title, used when `GameStatusScreen._stars` is empty.
- Progress screen: six headline tiles above the summary text, plus the "Acertos por cenário" and "Itens mais difíceis" sections after the category bars. The new section titles are copies of the authored category title, so restyling that title restyles all of them.

The settings screen contains hold-to-confirm actions for the active child profile. **Apagar progresso** removes that child's match and attempt history but keeps the profile. **Excluir perfil** removes the profile and its history, then switches the next match to guest mode. Hold buttons wobble when tapped briefly to show that they need a hold.

## Gameplay presentation

### Sorting targets

`SortingSource` owns the rules of a target (group, drop, eject). `SortingSourcePresentation` owns every visual reaction. It draws a rounded outline, generated at runtime and sized to the visible part of the category image:

| State | Outline |
|---|---|
| An item is held (drag mode) | Soft white pulse on both targets |
| The held item is over the target | Strong pulse in the category color (`ItemTypeDefinition.Color`) |
| Swipe card leans toward the side | Category color, opacity follows the lean; the other side dims |
| Tutorial points at the target | Yellow pulse |
| Right or wrong answer | Green or red flash, with a matching tint of the image |

### Drag and Drop

`DragAndDrop` moves the item and asks `IDropTarget` candidates under it. The target closest to the item's center wins, and the same lookup feeds both the hover highlight (`IDropHoverTarget`) and the drop, so the highlighted target is always the one that receives the item. `Item.SetLifted` makes the held item larger, draws it on top and shows a drop shadow (`ItemShadow`).

A wrong drop throws the item back toward the table at a fixed speed. Items that leave the camera view plus a margin reappear at their spawn position, so a wave can always be finished.

### Swipe

`SwipeCardInput` reports the gesture; `SwipeCardModeRunner` moves the card. The card follows the finger sideways and tilts. Releasing after 8% of the screen width answers; a shorter gesture springs the card back. `SwipeHintArrows` shows two bobbing arrows beside the card while it rests.

### Feedback effects

`GameFeedbackEffects` listens to `GameController` events and plays recipes from `FeedbackBursts` through the pooled `SpriteBurstPool`:

- Item spawn: sparkles in the category color. Items of a wave pop in one after the other.
- Pick up: an expanding ring, then a star trail while the item moves (`HeldItemTrail`).
- Correct sort: glow, stars, an expanding ring and two small fireworks.
- Streaks: extra fireworks at 3 correct answers in a row and at every multiple of 5.
- Incorrect sort: a soft puff, a restrained camera shake, vibration and blue tears. Tutorial mistakes do not produce tears.
- Idle: after `_idleNudgeSeconds` without interaction one waiting item hops (`IdleNudge`). Disabled during the tutorial.
- Countdown: particles once per second, brighter and with a tick sound in the final five seconds. The timer fill shifts from its rest color through yellow to red.
- Match end: confetti and fireworks on both outcomes; a loss also produces a tear burst. Stars pop in on the results screen and the numbers count up.

All effects use generated sprites (`ProceduralSprites`), so no texture or audio assets are required. Camera shake and colors are tuned on `GameFeedbackEffects`, particle shapes in `FeedbackBursts`, pool capacity and sorting order on `SpriteBurstPool`.

## Match data

`GameController` owns score, errors, tutorial progress and end conditions. `MatchSessionRecorder` collects what is stored about a match and builds the database records; see `DATABASE.md` for the columns. A match is saved when it ends, when it is left or restarted after at least one attempt, and as a replaceable checkpoint when the app goes to the background.

## Scene validation checklist

1. Open `Assets/Scenes/Game.unity`. Confirm `GameController` has a `GameDefinition`, both sorting sources, the question point and spawn points.
2. Confirm the four title cards show their category sprites and that the selected card is highlighted.
3. Drag and Drop: pick an item (both targets pulse), hover a target (category color outline), drop on the right and on the wrong target.
4. Swipe: drag the card slowly (it follows, the side lights up), release early (it returns), swipe to the right and to the wrong side.
5. Play with the tutorial on in both modes and confirm the pointer and the yellow outline.
6. Let the timer reach the last five seconds, finish a match by time and by error limit, and check the stars on the results screen.
7. Restart from the pause screen after a few answers, then open Progress and confirm the interrupted match is listed.
8. Open Progress and Profiles with at least one child that has history. Check the tiles, the scenario and item sections and the summary on each profile row.
9. In a player, check both the first-launch disclaimer flow and the accepted-disclaimer title flow.
