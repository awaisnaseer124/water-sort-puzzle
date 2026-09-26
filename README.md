# Water Sort Puzzle

A water-sort puzzle for Android, built with Unity 6. Pour coloured liquid between bottles until every bottle holds a
single colour.

The interesting part is under the hood: the game rules are plain C# with no engine dependencies, every level is
proven solvable by an optimal solver, new levels are generated along a difficulty curve, and the whole thing is
covered by 200+ EditMode tests.

[![Gameplay video](https://img.youtube.com/vi/OA0k-wRADjo/hqdefault.jpg)](https://youtu.be/OA0k-wRADjo)

*Gameplay video (opens YouTube)*

## Features

- **Three modes:** Classic (star rating by move count), Time, and Hard (moves and a time limit).
- **50 levels**, each validated by the solver. Star limits and time limits are derived from the optimal move count
  instead of being hand-tuned.
- **Undo and hints.** A hint shows the next move on a shortest solution; if the position is a dead end, the game says
  so before asking the player to watch an ad.
- **Liquid-like pouring:** the surface stays level while the bottle tilts, the stream lands on the rising liquid with
  a splash, and the liquid sloshes when it settles.
- **Cosmetics:** backgrounds and four bottle designs (tube, conical, round and angled flasks), bought with coins.
- **Monetisation:** rewarded ads (extra bottle, double reward, hints, free coins), frequency-capped interstitials and a
  remove-ads purchase, all behind interfaces with simulated versions for the editor.
- **Crash-safe saves:** a versioned JSON file written atomically with a backup, and migration/repair on load.

## Architecture

The game code lives in `Assets/_Game`, split into assemblies so that dependencies only point one way.

```
Core      Pure C# (no UnityEngine): board rules, game session, solver, level generator, player profile, store rules
Services  Interfaces and engine-light implementations: save files, ad policy, purchases, analytics, haptics
Game      Unity presentation: board and bottle views, level controller, screens, game flow, data assets
Platform  SDK adapters: Unity Ads, Unity IAP 5, Nice Vibrations
App       Composition root: builds the services once and hands them to the scene
Editor    Level validation, level generator window, save-data tools, import settings
```

A few decisions worth pointing out:

- **Rules don't know about rendering.** [`BoardState`](Assets/_Game/Core/Board/BoardState.cs) and
  [`GameSession`](Assets/_Game/Core/Session/GameSession.cs) own every rule: pours, undo, win and lose conditions, time
  limits. The board view only animates what the session reports, so it can't put the game in an invalid state.
- **No singletons in game code.** [`GameBootstrap`](Assets/_Game/App/GameBootstrap.cs) builds the services before the
  first scene loads, and [`SceneInstaller`](Assets/_Game/App/SceneInstaller.cs) passes them to the scene's components.
  [`GameFlow`](Assets/_Game/Game/Flow/GameFlow.cs) handles navigation; screens only report button presses.
- **Levels are data.** A `LevelDefinition` asset holds the starting bottles. Balancing comes from the solver, so
  changing a level never means re-tuning numbers by hand.
- **Money is handled defensively.** Purchases are granted before the store confirms them and recorded in a
  transaction ledger in the same save write, so a purchase the store re-delivers after a crash is never granted twice
  ([`PurchaseFulfillment`](Assets/_Game/Core/Store/StoreCatalog.cs)).
- **SDKs stay at the edge.** Game code talks to `IAdService` and `IPurchaseService`; only the `Platform` assembly
  references Unity Ads or Unity IAP.

## Solver and level generation

[`Solver`](Assets/_Game/Core/Solving/Solver.cs) is an A* search over board states. The heuristic counts the colour
boundaries inside bottles; one pour removes at most one boundary, so the heuristic is consistent and the first
solution found is optimal. Board states that differ only in the order of identical bottles are merged, which keeps
the search small: the hardest shipped level is proven optimal after about 35,000 states. The tests compare it
against a plain breadth-first search on random boards.

The solver is used in three places:

- **ColorSort > Validate Levels** checks every level (solvable, no duplicates, no look-alike colours) and stores its
  optimal move count. The output is [`Docs/LevelReport.md`](Docs/LevelReport.md).
- **ColorSort > Level Generator** creates levels along a difficulty curve from a seed, keeping only candidates the
  solver proves solvable within the target range. Levels 12–50 were generated this way.
- **In-game hints** run the solver on a background thread from the player's current position.

## Tests

EditMode tests cover the board rules, session, solver, generator, hints, player profile and save migration, purchase
fulfilment, ad policy, the save file store, board layout and the liquid geometry used while pouring. Run them from
**Window > General > Test Runner**.

## Running the project

1. Open the project in **Unity 6000.6.2f1** (Unity 6.6).
2. Open `Assets/Scenes/ColorFillPuzzle.unity` and press Play.

In the editor, ads and purchases are simulated, so every reward and purchase flow works without any account or SDK
setup. **ColorSort > Save Data** can reveal the save file, reset it, or grant coins while playing.

## Project layout

```
Assets/_Game/     All game code, data, prefabs and generated art (see Architecture)
Assets/Scenes/    ColorFillPuzzle, the only scene
Assets/UI/        UI sprites
Assets/Sounds/    Music and sound effects
Docs/             Generated level report
```

## Credits

Third-party assets are included so the project opens and runs as-is. They remain the property of their authors and
are covered by their own licenses, not by this repository.

| Asset | Author | Used for |
|---|---|---|
| [DOTween (HOTween v2)](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) ([GitHub](https://github.com/demigiant/dotween)) | Demigiant | Tweening |
| [Nice Vibrations](https://nice-vibrations.moremountains.com/) | More Mountains | Haptics on iOS and Android |
| [Epic Victory Effects](https://assetstore.unity.com/packages/vfx/particles/epic-victory-effects-88115) | Mixture Art | Level-complete firework effect |
| Calibri Light font | Microsoft | Bundled with Epic Victory Effects |
| Unity Ads, Unity IAP | Unity Technologies | Ads and in-app purchases |
| Mobile Dependency Resolver | Unity Technologies | Android dependency resolution |

## License

The code and project files are released under the [MIT License](LICENSE). Third-party assets listed under Credits
are not covered by it and remain under their own licenses.
