# CODEX TASK: Build a Polished 2D Coin Flip Hypercasual Game in Unity

## PROJECT OBJECTIVE

Create a complete, playable, visually polished **2D Coin Flip game** in Unity 6, inspired by the core gameplay of Stake Flip, but with a completely redesigned, modern, intuitive, and visually appealing interface.

**Reference:** https://www.youtube.com/watch?v=KELB3q93qpo

The game must be simple, satisfying, fast-paced, responsive, and optimized for WebGL.

**Important:** Do NOT replicate Stake's cluttered casino interface. Do NOT create a 3D game. Everything must be implemented in 2D, including the coin, animations, VFX, and UI.

The final deliverable must be a working WebGL build that plays smoothly on mobile browsers, laptops, and desktop computers.

---

## 1. CORE GAMEPLAY

The game revolves around predicting whether a coin will land on Heads or Tails.

### Gameplay Flow

1. The player starts with 10 virtual coins.
2. A beautifully designed 2D coin appears in the center.
3. The player chooses Heads or Tails.
4. The player presses the Flip button.
5. The coin launches into a smooth, satisfying flip animation.
6. The coin lands and reveals the result.
7. Correct prediction = Win.
8. Incorrect prediction = Lose.
9. Display animated feedback.
10. Allow the player to immediately play another round.

The entire gameplay loop should be quick and addictive.

### Game Economy

- Starting balance: 10 virtual coins.
- Default wager: 1 coin.
- Allow changing the wager using simple + and − buttons.
- Correct prediction: award 2× the wager as total return (net profit equals the wager).
- Incorrect prediction: lose the wager.
- Balance must never become negative.
- Disable flipping when the wager exceeds the available balance.
- Provide a clear restart/reset option if the balance reaches zero.
- Use virtual currency only. No real-money gambling, payment processing, or withdrawals.

### Result Generation

- Exactly 50% probability for Heads and 50% for Tails.
- Generate the outcome once, at the beginning of each valid flip.
- The animation must visually land on that predetermined outcome.
- Never change the result during the animation.
- Never manipulate the outcome based on the player's choice, balance, or history.

---

## 2. VISUAL DESIGN — VERY IMPORTANT

Create a premium-looking 2D hypercasual game with a unified art direction.

### Art Style

- Modern flat/vector-inspired 2D graphics.
- Clean shapes and carefully selected colors.
- Smooth gradients and subtle highlights.
- Soft shadows and layered visual depth.
- Rounded buttons and panels.
- Consistent iconography.
- Excellent typography.
- No realistic materials.
- No 3D models.
- No excessive glow or distracting visual effects.

### Suggested Color Palette

- Background: Deep navy (#101827).
- Secondary surface: Slate blue (#202D42).
- Primary accent: Warm gold (#F5B942).
- Heads: Gold/yellow.
- Tails: Cyan/blue.
- Success: Soft emerald green.
- Failure: Soft coral red.
- Text: Off-white.

Use this palette consistently throughout the game.

The interface must look like a professionally designed mobile arcade game, not a Unity prototype.

---

## 3. COIN DESIGN AND ANIMATION — HIGHEST PRIORITY

The coin is the central visual element of the entire game.

It must look polished, attractive, and feel dynamic.

### Coin Artwork

Create a custom 2D coin with:

- Distinct Heads and Tails faces.
- Clear symbols or illustrations for each face.
- Beautiful circular silhouette.
- Decorative outer ring.
- Subtle internal gradients.
- Clean highlights.
- Consistent 2D visual language.

Do not use placeholder circles, default Unity sprites, or plain text to represent the coin.

### Coin Flip Animation

Implement a smooth 2D flip illusion using sprite swapping, horizontal squash/stretch, rotation, easing, and controlled motion.

The coin should:

1. Anticipate the flip with a subtle squash.
2. Launch upward with an elastic movement.
3. Spin rapidly around its vertical axis using 2D scale animation.
4. Alternate between Heads and Tails artwork at the correct visual moments.
5. Gradually decelerate.
6. Fall toward its resting position.
7. Bounce slightly on landing.
8. Settle naturally on the predetermined result.

The animation should feel physically believable while remaining stylized.

### Animation Requirements

- Smooth interpolation with no jitter.
- Proper easing curves.
- Dynamic acceleration and deceleration.
- No abrupt sprite transitions.
- No incorrect face display.
- No clipping or distortion.
- No animation desynchronization.
- Coin remains visually centered throughout.
- The final result must always match the generated outcome.

Target animation duration: approximately 1.2–1.8 seconds.

Use DOTween if available, otherwise implement a lightweight custom tweening system.

Do not use actual 3D rotation. Simulate depth entirely through 2D transformations and artwork.

---

## 4. JUICY VFX AND GAME FEEL

The game must feel satisfying every time the player flips the coin.

### Flip Effects

- Small anticipation pulse.
- Soft motion trails during spinning.
- Subtle radial speed lines.
- Animated coin shadow.
- Tiny sparkle particles.
- Light impact pulse when the coin lands.

### Winning Effects

- Coin briefly shines.
- Gold particles burst outward.
- Small celebratory confetti.
- Balance increases with an animated number counter.
- Subtle screen pulse.
- Positive result icon appears.

### Losing Effects

- Brief soft red impact pulse.
- Small downward coin movement.
- Balance decreases smoothly.
- Subtle negative feedback animation.
- No excessive screen shake.

### VFX Requirements

- Use lightweight 2D particles or UI-based effects.
- Avoid expensive shaders and post-processing.
- Pool frequently reused effects.
- Keep particle counts controlled.
- Avoid excessive visual noise.
- Effects must remain smooth on low-end mobile devices.

Every effect should reinforce the gameplay rather than distract from it.

---

## 5. SOUND DESIGN

Implement a satisfying, lightweight audio system.

### Required SFX

- Button hover (desktop).
- Button press.
- Heads/Tails selection.
- Coin launch.
- Rapid flipping sound.
- Coin deceleration.
- Coin landing.
- Winning sound.
- Losing sound.
- Balance update.
- UI transitions.

### Audio Behavior

- Synchronize sounds with animation events.
- Slight pitch variation on repeated effects.
- Avoid overlapping identical sounds excessively.
- Smooth volume transitions.
- Optional subtle background music.
- Separate music and SFX volume controls.
- Mute/unmute button.

Create or integrate suitable royalty-free audio assets. Do not leave missing audio references.

Respect browser audio restrictions: initialize or resume audio after a valid user interaction.

---

## 6. UI/UX DESIGN

The interface must be minimal, visually attractive, and extremely easy to understand.

### Main Screen Layout

**Top Section**
- Current virtual coin balance.
- Compact sound/settings button.
- Small game title or logo.

**Center Section**
- Large animated coin.
- Subtle decorative background.
- Heads/Tails result indicator.

**Bottom Section**
- Heads selection button.
- Tails selection button.
- Wager controls.
- Large primary Flip button.

**Secondary Information**
- Last few results shown as small circular icons.
- Optional win streak counter.
- Animated win/loss feedback.

### Interaction Design

- Clearly highlight the selected side.
- Heads and Tails must have distinct colors/icons.
- Primary Flip button must be visually dominant.
- Buttons should have smooth press animations.
- Prevent interaction while the coin is flipping.
- Provide immediate feedback after each action.
- Avoid unnecessary instructions or paragraphs.
- Use icons wherever the meaning is obvious.
- Keep all information readable.

### UI Restrictions

DO NOT include:

- Stake branding.
- Cryptocurrency indicators.
- Complex betting dashboards.
- Manual/Auto tabs.
- Unnecessary statistics panels.
- Large empty UI areas.
- Tiny unreadable labels.
- Generic Unity default buttons.
- Unnecessary popups.

The game should be understandable within five seconds of opening it.

---

## 7. MOBILE RESPONSIVE WEBGL — CRITICAL

The final game must work properly in mobile and desktop browsers.

### Supported Devices

- Android phones.
- iPhones.
- Tablets.
- Windows laptops.
- Desktop computers.
- Modern Chromium, Safari, and Firefox browsers where Unity WebGL is supported.

### Responsive Layout

Implement a fully responsive UI.

**Mobile Portrait**
- Primary target.
- Coin centered.
- Large touch-friendly buttons.
- Compact layout.
- No overlapping elements.
- No horizontal scrolling.

**Mobile Landscape**
- Automatically reorganize layout.
- Maintain comfortable button sizes.
- Avoid stretching or cropping the coin.

**Desktop**
- Center the gameplay within a polished layout.
- Use available screen space intelligently.
- Avoid excessively large UI.
- Support mouse interaction.

### Technical Requirements

- Unity Canvas Scaler with appropriate reference resolution.
- Proper anchors and layout groups.
- Safe area support for notches and rounded screens.
- Correct handling of browser resize and orientation changes.
- Preserve coin aspect ratio.
- No stretched sprites.
- No clipped buttons.
- No overlapping text.
- No off-screen interactive elements.
- Touch and mouse controls.
- Prevent accidental double input.
- No dependency on keyboard input.
- Handle browser focus loss and restoration.

Test common resolutions, including 360×640, 390×844, 414×896, 768×1024, 1920×1080, and 1366×768.

The UI must adapt to the actual browser viewport, not just the Unity Game view.

---

## 8. GAME ARCHITECTURE

Implement a clean, modular architecture.

Suggested systems:

- GameManager
- CoinFlipController
- CoinAnimationController
- UIManager
- CurrencyManager
- VFXManager
- AudioManager
- GameSettings
- ResultHistoryManager
- SaveManager

Use event-driven communication where appropriate.

### Code Quality

- Clean C# scripts.
- Serialized configuration values.
- No unnecessary Update loops.
- No hardcoded scene references when avoidable.
- No duplicate systems.
- No missing references.
- No deprecated APIs.
- No unnecessary packages.
- No excessive allocations during gameplay.
- Proper cleanup of tweens, coroutines, and event subscriptions.

Use a clear state machine:

Idle → SideSelected → Flipping → Result → Ready.

Validate the wager and available balance before starting the flip. Deduct the wager exactly once, then award the total return exactly once on a win.

The result must be resolved only once per flip, even if buttons are pressed rapidly.

---

## 9. SAVE SYSTEM

Use lightweight local persistence.

Save:

- Current virtual coin balance.
- Sound settings.
- Music settings.
- Recent flip history.
- Best winning streak.

Ensure saving works in WebGL using supported Unity persistence mechanisms.

Handle missing or corrupted save data gracefully.

---

## 10. PERFORMANCE OPTIMIZATION

This is a small hypercasual game. It must be extremely lightweight.

### Performance Targets

- Target 60 FPS on supported devices.
- Remain responsive on mid-range and lower-end phones.
- Minimize draw calls.
- Use sprite atlases where useful.
- Avoid unnecessary texture memory.
- Compress audio appropriately.
- Avoid large asset dependencies.
- Avoid expensive post-processing.
- Avoid unnecessary physics calculations.
- Minimize garbage collection allocations.
- Keep WebGL loading time and build size reasonably small.

Prioritize stable frame pacing over excessive visual effects.

---

## 11. EDGE CASES AND BUG PREVENTION

Explicitly handle:

- Player clicking Flip repeatedly.
- Changing selection during animation.
- Wager exceeding balance.
- Zero or invalid wager.
- Balance reaching zero.
- Browser resizing during animation.
- Mobile device rotation.
- Browser losing focus.
- Rapid taps on buttons.
- Reloading the page.
- Corrupted local save data.
- Missing audio initialization.
- Animation interruption.
- Coin landing on the wrong side.
- Duplicate win/loss payouts.
- UI elements becoming unresponsive.

All critical gameplay logic must remain deterministic after the result has been generated.

Do not claim the game is bug-free without actually testing it.

---

## 12. WEBGL BUILD AND TESTING

Configure the project for Unity 6 WebGL.

### Build Requirements

- Create a complete WebGL build.
- Include all required build files.
- Ensure correct browser scaling.
- Enable appropriate compression compatible with the deployment server.
- Configure a responsive WebGL hosting template.
- Provide a mobile-friendly loading screen.
- Ensure the game initializes correctly.
- Avoid browser console errors.
- Ensure touch input works correctly.
- Ensure audio works after user interaction.

### Verification

Before completing:

1. Open the Unity project and check for compilation errors.
2. Verify all assets and scene references.
3. Test Heads and Tails outcomes.
4. Test repeated flips and payouts.
5. Test the zero-balance state.
6. Test rapid clicking.
7. Test animation synchronization.
8. Build WebGL.
9. Serve the build through a local HTTP server.
10. Test it in a real browser.
11. Test desktop and mobile viewport sizes.
12. Check browser console logs.
13. Fix discovered issues and rebuild.

Where physical mobile testing is unavailable, clearly report that limitation instead of claiming it was tested.

---

## 13. FINAL DELIVERABLES

Deliver:

1. Complete Unity project with organized source code.
2. Polished custom 2D coin artwork.
3. Responsive UI and functional gameplay.
4. Smooth coin flip animations.
5. Complete VFX and SFX integration.
6. Persistent virtual currency and settings.
7. Optimized WebGL build.
8. Brief README containing build and hosting instructions.
9. Testing summary with any known limitations.

**The WebGL build is mandatory. Do not stop after writing scripts or creating the scene.**

## FINAL QUALITY EXPECTATIONS

This game must feel like a finished, commercially polished hypercasual browser game.

The main focus is not complexity. It is the quality of every interaction.

The coin should be enjoyable to watch flip repeatedly. Buttons should feel responsive. Winning should feel rewarding. Losing should feel clear but not frustrating.

Use thoughtful animation timing, clean 2D artwork, coordinated sound effects, and polished visual feedback.

**No 3D. No clutter. No placeholder graphics. No unnecessary text. No broken interactions.**

Start by inspecting the existing Unity project, then implement the complete game, validate it, and produce the final playable WebGL build.