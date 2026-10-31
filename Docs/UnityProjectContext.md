# Brick Rush

New local project, created from Universal 2D with Unity 6000.3.10f1. URP 17.3, uGUI + TextMeshPro, Input System. Single offline 90-second arcade scene; no monetization, accounts or backend. Build target WebGL, desktop and mobile browsers.

Runtime composition is explicit in GameManager. Session rules are plain C#; presentation uses pooled custom sprites, bounded VFX, synthesized original audio and a responsive Canvas. Physics uses swept Physics2D circle casts against brick colliders, with controlled wall and paddle reflection; no accumulated forces. The timer uses unscaled time while active and stops for pause/countdown. Any ball returning to the paddle resets the shared multiball combo. One life is lost only when the final active ball leaves the arena.

Build entry: BrickRush.Editor.BuildGame.BuildWebGL. Output: Builds/BrickRush_WebGL. All scene, font and configuration assets are generated through Unity APIs by BuildGame.Setup. Art/audio are reproducible with Tools/generate_assets.py. See README.md and Docs/QA.md for delivery and evidence.
