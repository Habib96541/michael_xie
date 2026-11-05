# Runtime design

`GameManager` composes concrete systems once, owns the session state, and drives their lifetimes. The saved startup scene contains the bootstrap object, orthographic camera and global URP volume. The arena, pooled actors and responsive Canvas are authored from reproducible assets at startup, so there are no per-scene manual references to repair.

`GameConfig` is the editable ScriptableObject for timing, speeds, initial lives, ball cap, power durations and life bonus. `GameRules` and `ScoreManager` isolate deterministic calculations. `SaveManager` namespaces local best/preferences with a versioned key prefix.

## Time and state

Menu -> Countdown -> Playing -> Results. Pause retains the previous gameplay/countdown state. The initial countdown is 3 seconds; a life respawn countdown is 1.2 seconds. Session time uses unscaled elapsed frame time while Playing. Simulation slow motion affects ball steps only, so achievements cannot extend the timer. Power durations and drop movement stop on pause. Browser blur, backgrounding and touch cancellation pause the session.

The last lost ball causes one life decrement after all balls have stepped. All balls are pooled; a shared combo resets on any paddle return. The 0.55-second wave transition retains power timers, rehomes balls below the new field and continues the session. Wave completion is recorded even if time expires during that transition.

## Collision control

Balls have explicit positions and normalized direction vectors. At 120 simulation ticks/second, each step sweeps its circle through real BoxCollider2D brick shapes with a preallocated hit buffer. Wall planes and a paddle top-plane test participate in the same nearest-hit calculation. The unconsumed distance is simulated after a reflection, with bounded iterations and a small separation epsilon. Direction limits preserve both vertical and horizontal travel, and paddle offset controls the return angle.

Brick death disables its collider and marks it dead before score, effects, or recursive explosions run. This makes simultaneous-ball damage and adjacent explosive chains idempotent. A brief sprite death animation continues after collider removal. Fireball can pierce destructible bricks, including armor, while steel remains reflective.

## Bounded work

- 81 reusable brick slots, 5 balls, 12 falling powers and 16 laser bolts.
- 224 pooled fragment/spark sprites, 24 floating scores and 12 impact rings.
- One central VFX tick; no Instantiate/Destroy during ordinary gameplay.
- Shared unlit materials, fixed sprite sizes, short trails, capped DPI and a lightweight arena shader.
- 14 reusable SFX voices and one music source; repetitive brick sounds are rate-limited.
- HUD values refresh at 20 Hz; animation and input run each rendered frame.

## Responsive presentation

The arena always keeps a 9.9:14.6 aspect ratio. uGUI calculates a safe-area viewport for the camera, with editorial side panels on wide desktop displays, a central portrait column on phones, and two-column menus on landscape phones. `UIManager.Responsive.cs` enlarges phone touch targets and restores each page's original geometry when changing orientation. Mobile presentation uses Unity's simulated device API plus a WebGL touch-capability hint, so a landscape phone does not accidentally select the desktop layout.

Resolution, safe area, Canvas scale, root size and native render-target changes trigger layout. The camera viewport is reconciled with the arena every LateUpdate using a reused corner buffer, including when simulator orientation and the Canvas settle on different frames. The HTML wrapper applies CSS safe-area insets, blocks page scrolling, and positions mobile fullscreen controls away from Pause. Touch can move the paddle from the strip below the arena.

## Verification surface

Only a URL containing `?qa=1` enables `QACommand` and the `window.brickRushState` snapshots. Normal play never enables automated steering or test commands. The `inspect` command publishes a layout snapshot only when requested; hierarchy inspection does not run in the gameplay loop. Browser tests use real pointer/keyboard/touch interaction for UI and input; diagnostic commands arrange powers, final-ball loss, wave clear, late-session and timer-expiry scenarios. The separate natural-session test lets the entire 90-second clock run without acceleration. This is a local test interface, not a network service.
