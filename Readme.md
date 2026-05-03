# Slot Machine 

A 3-reel slot machine game built in Unity 6. The project implements weighted RNG, staggered reel animation with pixel-perfect alignment, a personality-driven croupier commentary system, and a custom URP post-process shader combining palette quantization, ordered dithering, and Sobel edge detection.

**Playable WebGL build:** https://novascream.github.io/Slot_Machine/Build/

Made as an intership assignment for underpin services.
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
│   ├── Materials/           # URP materials and post-process material
│   ├── Prefabs/             # Reel, UI, and machine prefabs
│   ├── Scenes/              # MainScene
│   ├── ScriptableObjects/   # SymbolData assets per symbol
│   ├── Scripts/             # All C# source files
│   ├── Settings/            # URP renderer and pipeline settings
│   ├── Shader/              # PixelNoir.shader
│   ├── Sound/               # SFX clips and background music
│   ├── Sprites/             # Machine art, symbol sprites, UI sprites
│   ├── TextMesh Pro/        # TMP font assets and resources
│   ├── DefaultVolumeProfile.asset
│   ├── InputSystem_Actions.inputactions
│   └── UniversalRenderPipelineGlobalSettings.asset
├── Build/
│   └── WebGL/               # Exported WebGL build
├── Packages/
├── ProjectSettings/
├── .gitignore
├── .vsconfig
└── AGENTS.md
```

### Scripts

```
Assets/Scripts/
├── SlotMachineManager.cs    # Spin orchestration, weighted RNG, win evaluation
├── ReelController.cs        # Per-reel animation, wrapping, pixel-perfect snap
├── BettingManager.cs        # Balance, bet input, handle visuals, scene reset
├── CroupierAI.cs            # Typewriter commentary, streak tracking
└── SymbolData.cs            # ScriptableObject — symbol identity and payout data
```

Namespaces: `SlotGame.Core` covers `SlotMachineManager`, `ReelController`, and `CroupierAI`. `SlotGame.Data` covers `SymbolData`. `BettingManager` sits in the global namespace.

---

## How to Play

1. Open the WebGL build at the link above.
2. Select a bet amount from the betting panel.
3. The handle toggles down to signal a spin has started.
4. All three reels spin, stop one by one, and are evaluated.
5. If all three reels show the same symbol, winnings are added to the wallet.
6. The croupier log at the bottom displays a contextual message after every outcome.
7. Press Exit to reset the session and reload the scene.

---

## How to Run Locally

**WebGL (from the repository):**

1. Navigate to `Build/WebGL/` in the repository.
2. Chrome and Edge require a local server due to CORS restrictions.
   Run `npx serve .` inside the folder, then open `http://localhost:3000`.
3. Firefox can open `index.html` directly without a local server.

**Unity Editor:**

1. Open the project in Unity 6000.0.47f1.
2. Open `Assets/Scenes/MainScene.unity`.
3. Press Play.

---

## Game Rules

- The machine has three independent reels, each displaying one symbol after stopping.
- The bet is deducted from the wallet before the reels begin spinning.
- Win condition: all three reels resolve to the same symbol ID.
- There is no partial win for two matching reels. A win requires all three to match.
- On a win, the wallet receives the flat `PayoutValue` defined on that symbol's ScriptableObject, not a multiplier of the bet.

### Symbol Data

Each symbol is a ScriptableObject (`SymbolData`) with four fields:

| Field | Type | Purpose |
|---|---|---|
| `SymbolID` | int | Unique identifier used in win evaluation |
| `SymbolSprite` | Sprite | Visual displayed on the reel |
| `PayoutValue` | int | Flat coin reward on a 3-of-a-kind match |
| `winWeight` | int | Relative probability weight for RNG selection |

### Starting Balance

The player starts with **1000G**.

---

## Architecture

### SlotMachineManager

The central orchestrator. On `RequestSpin()`, it runs a coroutine that:

1. Pre-determines one weighted random `SymbolData` result per reel before any animation begins.
2. Starts each reel in sequence with a `staggeredStartDelay` (default 0.15s) between them.
3. Waits for `minimumSpinDuration` (default 2.0s) at full speed.
4. Stops each reel in sequence with a `staggeredStopDelay` (default 0.5s), injecting the pre-determined result into each `ReelController`.
5. Evaluates the results for a win or loss.
6. Signals `BettingManager.OnSpinComplete()` to re-enable the betting panel.

**Weighted RNG** uses `SymbolData.winWeight`. Total weight is summed, a random number is drawn in that range, and symbols are walked through until the cumulative weight exceeds the roll. This is a standard weighted selection without floating-point probability tables.

**Win evaluation** compares all resolved `SymbolID` values against the first reel's result. If any differs, the spin is a loss.

### ReelController

Manages one reel. On start, it instantiates six `Image` components spaced by `symbolHeight + spacing` (96px + 10px = 106px per slot), centred on Y=0 so symbols are distributed equally above and below the visible window.

**Spin loop** moves every symbol downward by `spinSpeed * Time.deltaTime` per frame. When a symbol passes `thresholdY` (-2 * step), it is teleported to the top of the stack and assigned a new random sprite, creating the illusion of continuous scrolling.

**Finalize position** runs when `StopSpin(result)` is called. It finds the symbol currently closest to Y=0, injects the pre-determined result sprite into that slot, then shifts the entire stack so that symbol lands exactly at Y=0. All Y values are rounded to the nearest integer to keep pixel art crisp on the UI grid. A cleanup pass then wraps any symbols that drifted outside bounds during the shift.

### BettingManager

Owns the player wallet and all bet-facing UI. Key behaviours:

- `PlaceBet(int amount)` checks the balance, deducts the bet, hides the betting panel, toggles the handle visual, and calls `SlotMachineManager.RequestSpin()`.
- `AddWinnings(int amount)` receives the payout from `SlotMachineManager` and updates the balance display.
- `ExitAndResetGame()` reloads the active scene by build index, resetting all state.
- `PlayClickSound()` randomises the `AudioSource` pitch between 0.9 and 1.1 on every button press to prevent listener fatigue from a repeated identical click.
- The handle uses two separate GameObjects (`handleUpObject`, `handleDownObject`) toggled active to simulate the pull animation, with a 0.5s `Invoke` to reset.

### CroupierAI

A typewriter-style commentary system that gives the machine personality. It tracks four session statistics: current win streak, current loss streak, total gold won, total gold spent.

It maintains five phrase banks:

| Bank | Trigger |
|---|---|
| Welcome phrases | On scene start |
| Standard win phrases | Win, streak below 3 |
| Win streak phrases | Win streak reaches 3 or more |
| Standard loss phrases | Loss, streak below 4 |
| Loss streak phrases | Loss streak reaches 4 or more |

Every message is displayed character by character via a coroutine at a configurable `textTypingSpeed` (default 0.03s per character). A new message stops the current coroutine and starts fresh, preventing overlap. Win streak count and total gold won are appended to every outcome message.

### Post-Process Shader: PixelNoir

Registered as `PostProcess/PixelNoir`. Applied via a `FullScreenPassRendererFeature` in the URP 2D Renderer.

The vertex function uses `GetFullScreenTriangleVertexPosition` and `GetFullScreenTriangleTexCoord` from the URP Core library, which is the correct Unity 6 approach for full-screen passes. No custom `Attributes` struct or vertex buffer is needed.

The fragment shader applies six effects in order:

**1. Pixelation.** UVs are snapped to a grid defined by `_PixelSize`, effectively reducing the sample resolution before any other processing. This makes all subsequent effects operate on blocky pixel-sized regions.

**2. Saturation boost.** Colours are pushed away from greyscale by `_SaturationBoost` before quantization, so the reduced palette retains visible colour variation rather than collapsing to near-grey.

**3. Full Sobel edge detection.** All eight neighbours are sampled to compute horizontal (`gx`) and vertical (`gy`) gradient vectors in RGB space. The average gradient magnitude across the three channels produces a scalar edge strength per pixel.

**4. 8x8 Bayer ordered dithering.** The screen pixel position modulo 8 selects a threshold from a hardcoded 64-entry Bayer matrix. The colour is offset by `(threshold - 0.5) * localDither / levels`, then rounded to the nearest step in a palette of `_PaletteSize` levels. The dither strength is attenuated near detected edges by `_EdgeProtect`, which preserves the legibility of text and UI elements that sit on hard colour boundaries.

**5. Scanlines.** A very subtle darkening is applied to every other screen row by `_ScanlineStrength` (default 0.08, maximum 0.3). The effect is intentionally faint — enough to suggest a CRT without obscuring the art.

**6. Vignette.** A radial falloff from centre darkens the screen corners by `_VignetteAmount`, drawing focus toward the machine in the middle of the frame.

All shader properties are exposed to the Inspector through the Material, so every parameter can be adjusted at runtime without recompiling.

---

## Design Decisions

**Pre-determined results before animation.** The RNG runs once when `RequestSpin()` is called, before any reel moves. The animation serves only to reveal a decision that was already made. This prevents any possibility of the result being influenced by frame rate, coroutine timing, or user input during the spin.

**Flat payout value, not a bet multiplier.** `SymbolData.PayoutValue` is a fixed coin amount per symbol. This makes each symbol's reward immediately readable in the Inspector without needing to cross-reference a bet amount. The tradeoff is that the reward is the same regardless of how much was wagered.

**Scene reload on exit.** `ExitAndResetGame()` calls `SceneManager.LoadScene` on the active scene's build index. This guarantees a completely clean state reset with no residual coroutines, cached values, or stale references, at the cost of a brief load.

**CroupierAI as a separate component.** The croupier is not embedded in `SlotMachineManager` or `BettingManager`. It receives two calls — `ReportBetPlaced` and `ReportSpinOutcome` — and manages all of its own state independently. This means it can be removed, replaced, or muted without touching any game logic.

**Pitch randomisation on SFX.** A `Random.Range(0.9f, 1.1f)` pitch offset on every button click means no two clicks sound identical, which reduces the perception of a looping sample over extended play.

**Edge protection in the dither pass.** Dithering applied to hard edges in the scene — particularly UI text — produces noise that makes text harder to read. The Sobel pass runs on the original colour before quantization and feeds back as a mask that suppresses dithering at those boundaries. Flat colour regions get full dithering; edges and text get progressively less.

---

## Known Limitations

- There is no persistent save system. The wallet balance resets to 1000G on every scene load.
- The win condition requires all three reels to match. There is no partial payout for two matching reels.
- The payout is a flat value per symbol, not a multiplier of the wager, so bet size does not affect winnings.
- The WebGL build may delay audio on the first spin due to browser autoplay policy. Audio plays normally after the first user interaction with the page.

---


