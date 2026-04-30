# Slot Machine
 
A 3-reel slot machine game built in Unity 6 as part of a technical assignment. The project covers core game mechanics, weighted randomisation, smooth reel animations, a payout system, and a post-process edge detection effect using the Universal Render Pipeline.
 
---
 
## Technical Stack
 
| Item | Detail |
|---|---|
| Engine | Unity 6000.0.47f1 |
| Render Pipeline | Universal Render Pipeline (URP) |
| Input | Unity Input System |
| Language | C# — 79.9% |
| Shaders | ShaderLab / HLSL — 20.1% |
| Platform | PC / WebGL |
 
---
 
## Project Structure
 
```
Slot_Machine/
├── Assets/
│   ├── Animations/          # Reel spin animator controllers and animation clips
│   ├── Materials/           # URP materials including post-process material
│   ├── Prefabs/             # Reel, UI, and machine prefabs
│   ├── Scenes/              # MainScene
│   ├── ScriptableObjects/   # SymbolData assets (SD_Seven, SD_Cherry, SD_Bell, SD_Bar)
│   ├── Scripts/             # All C# source files
│   ├── Settings/            # URP renderer and pipeline settings
│   ├── Shader/              # EdgeDetection.shader
│   ├── Sound/               # Sound effects and background music clips
│   ├── Sprites/             # Machine art, symbol sprites, UI sprites
│   ├── TextMesh Pro/        # TMP font assets and resources
│   ├── DefaultVolumeProfile.asset
│   ├── InputSystem_Actions.inputactions
│   └── UniversalRenderPipelineGlobalSettings.asset
├── Build/
│   └── WebGL/               # Exported WebGL build (index.html + supporting files)
├── Packages/
├── ProjectSettings/
├── .gitignore
├── .vsconfig
└── AGENTS.md
```
 
### Scripts breakdown
 
```
Assets/Scripts/
├── Core/
│   ├── SlotMachineController.cs   # Central orchestrator — state, spin sequence, events
│   ├── ReelController.cs          # Per-reel scroll animation and symbol resolution
│   ├── PayoutTable.cs             # Static evaluator — match detection and payout math
│   └── SymbolData.cs              # ScriptableObject — symbol identity, sprite, payouts, weight
├── UI/
│   ├── UIController.cs            # Button handling, balance display, win banner
│   └── NotificationController.cs  # Slide-in event messages (win, loss, warning)
└── Utilities/
    ├── AudioManager.cs            # Singleton audio handler
    └── WinAnimator.cs             # Scale pulse animation on win popup
```
 
---
 
## Game Rules
 
- The machine has three independent reels.
- Each reel displays one symbol in the result row after stopping.
- A spin costs the selected bet amount, deducted before the reels move.
- Win condition: all three result-row symbols share the same symbol ID.
- Partial win: two adjacent reels (left-center or center-right) match.
- No win: no adjacent pair matches.

### Symbols and Payouts
 
| Symbol | 2-Match Multiplier | 3-Match Multiplier | Weight |
|---|---|---|---|
| Seven | 10x | 50x | 10 |
| Bell | 5x | 20x | 25 |
| Cherry | 5x | 20x | 25 |
| BAR | 3x | 10x | 30 |
 
Weight determines relative probability. All weights are summed and used for proportional random selection. Higher weight means the symbol appears more often.
 
### Bet Options
 
- 10G
- 50G
- 100G

Starting balance is 100G. The spin button is disabled if the selected bet exceeds the current balance.
 
---
 
## How to Run the WebGL Build
 
1. Navigate to the `Build/WebGL/` folder in the repository.
2. Open `index.html` in a browser.
   - Chrome and Edge require a local server due to CORS restrictions on local files. Run one with `npx serve .` inside the WebGL folder, then open `http://localhost:3000`.
   - Firefox can open the file directly without a local server in most configurations.

---
 
## Architecture
 
### Core
 
**`SymbolData`** is a ScriptableObject. Each symbol's ID, sprite, payout multipliers, and weight are defined in a dedicated asset under `Assets/ScriptableObjects/`. No symbol data is hardcoded in scripts.
 
**`ReelController`** manages a single reel. It maintains a scrolling strip of `Image` components inside a masked viewport. On spin, it runs a three-phase coroutine: acceleration, full-speed scrolling with symbol wrapping, and deceleration. The result symbol is pre-determined before the animation begins via weighted random selection, then snapped into the centre row after the strip stops.
 
**`SlotMachineController`** is the central orchestrator. It builds the weighted symbol pool, coordinates staggered reel start and stop timing, waits for all reels to finish via coroutine, then passes the three resolved symbols to the payout evaluator. It exposes C# events (`OnBalanceChanged`, `OnWin`, `OnLoss`, `OnSpinComplete`) that the UI layer subscribes to.
 
**`PayoutTable`** is a static class. It receives the three resolved symbols and the bet amount, evaluates the match condition, computes the payout, and returns a win label string. It has no dependencies on MonoBehaviour or scene state.
 
### UI
 
**`UIController`** handles all button input and reflects game state changes. It subscribes to `SlotMachineController` events via `OnEnable` and unsubscribes in `OnDisable`. Bet button highlight, spin button label, and balance display are all driven by events rather than polling.
 
**`NotificationController`** displays contextual slide-in messages after each player-facing event. It supports four message types — Win, Jackpot, Loss, and Warning — with distinct background sprites and text colours. Each notification runs a smooth-step slide-in coroutine, holds for a configurable duration, then slides out and deactivates.
 
### Utilities
 
**`AudioManager`** is a singleton that persists across scenes. It holds references to all audio clips and exposes named play methods (`PlaySpin`, `PlayWin`, `PlayJackpot`, etc.) rather than exposing clips directly.
 
**`WinAnimator`** drives a repeating scale pulse on the win popup panel using a coroutine. It resets to `Vector3.one` when stopped.
 
### Input
 
The project uses the Unity Input System package. Input bindings are defined in `InputSystem_Actions.inputactions`. Button interactions in the UI go through Unity UI's `Button.onClick` rather than direct polling, keeping input handling consistent with the event-driven architecture.
 
### Post-Processing
 
An edge detection effect is applied via a `FullScreenPassRendererFeature` in the URP 2D Renderer. The shader (`PostProcess/EdgeDetection`) includes `Blit.hlsl` from the URP package, uses the `Vert` function it provides, and writes only a fragment shader. The fragment samples the four cardinal neighbours, computes luminance-weighted differences, and blends the configured edge colour over the original pixel where the difference exceeds the threshold. A `DefaultVolumeProfile` is configured in the scene for any additional post-processing volumes.
 
---
 
## Design Decisions
 
**Pre-determined results.** Each reel resolves its outcome symbol before the spin animation starts. The RNG runs once at spin time, not at stop time. This ensures the result cannot be affected by animation timing and matches how regulated electronic machines behave.
 
**Weighted pool, not weighted random.** The symbol pool is built as a flat list where each symbol appears `weight` times. Selection is then a single `Random.Range` call on the list index. This avoids floating-point probability arithmetic and is straightforward to reason about and tune.
 
**Event-driven UI.** The `UIController` does not poll `SlotMachineController` each frame. All state changes flow through C# events. This keeps the UI layer decoupled from game logic and makes the controller independently testable.
 
**ScriptableObject data.** All tunable values per symbol live in ScriptableObject assets in `Assets/ScriptableObjects/`. Changing a payout multiplier or spawn weight requires editing an asset in the Inspector, not modifying a script.
 
**Static PayoutTable.** The payout evaluator has no instance state and no MonoBehaviour dependencies. It takes inputs and returns outputs. Keeping it static makes the evaluation logic self-contained and straightforward to verify.
 
---
 
## Known Limitations
 
- The WebGL build may not play audio on the first spin due to browser autoplay policy. Audio plays correctly after the first user interaction with the page.
- There is no persistent save system. The balance resets to the starting amount each time the scene loads.
- The edge detection shader uses a four-neighbour cross pattern. A full eight-neighbour Sobel kernel would produce more accurate edges at diagonal boundaries.

---
 
## Repository
 
**Author:** novascream  
**Repository:** https://github.com/novascream/Slot_Machine  
**Unity Version:** 6000.0.47f1
