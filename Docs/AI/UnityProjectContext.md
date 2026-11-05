# Unity project context

- Existing project at D:/30 games/Classic Crossy Roads; original empty-folder
  creation was authorized on 2026-10-31. Current revision 3.0 modifies that project.
- Unity 6000.3.10f1, URP 17.3.0, uGUI/TMP, Input System 1.18.0, WebGL.
- Scene Assets/ChickenRush/ChickenRush.unity; composition root ChickenRushGame.
- RoundRules owns state and integer virtual coins. SeededRisk is a counter hash
  keyed by round seed and hop index: cosmetic traffic, frame rate and waiting
  cannot alter outcomes. No network, purchases, certified RNG or RTP claim.
- Four config assets: EASY, NORMAL (display MEDIUM), HARD, HARDCORE. Each has
  matching chance/multiplier arrays, hop timing and traffic speed. Default lengths
  24/20/16/12; 0.62-second hops. riskVersion3 migrates legacy defaults once.
- Each hop commits once. Land/Hit callbacks must match its outcome. Cash-out is
  allowed only Safe after at least one landing; terminal state guards payout.
- RoadWorld presents staged seeded outcomes. Failed cars physically contact via
  swept AABBs. Successful cars brake or pass clear before a barrier rises.
  Collision presentation is tested independently against sampled sprite bodies.
- Chicken moves +X, traffic ±Y. Step2.4. Pads on lane centers, sparse separators,
  one shared tiled road surface. Twelve recycled lane segments /36 vehicles;
  no growth with progression. Near-miss feedback uses actual positive clearance.
- Raised-view front/rear vehicle/prop sprites supply restrained 2.5D depth.
  Camera remains orthographic and follows X at predictable ~26% viewport width;
  foot placement is around the lower third. View framing adapts independently.
- RushLayout: desktop side panel, landscape phone side controls, portrait/tablet
  bottom panel. CanvasScaler uses logical CSS size, primary targets >=44px,
  safe areas, orientation changes. Next-hop label reformats on resize.
- Animator retains Idle/Walk/Hop/Hit/Celebrate, adds Danger using idle drawings
  and a subtle lean. Root motion off. Hit/Celebrate hold until explicit reset.
- Chicken source54 PNGs unchanged:192px,100PPU,pivot96/18. Source detail remains
  ~130–140px silhouette height. Native new vehicles are roughly270–400px.
- Art/Arcade: original generated vehicles, pads, barriers, foliage, sidewalk and
  quiet painted asphalt. Straight centered pivots, uniform scaling, no grid cuts.
- Three atlases: chicken/shapes ETC2RGBA, new arcade art ETC2RGBA, UI uncompressed
  RGBA32. Bilinear, no mipmaps. UI card128px/border32/multiplier2=16px fixed corners.
- Existing RushEffects pools32 particles; generated audio plus a quiet progress
  pulse. Browser autoplay remains subject to real interaction; mute/pause apply.
- PlayerPrefs existing keys retained; rush.seed adds a stable local seed nonce.
- RushBuilder.Prepare regenerates asset setup; BuildPlayerOnly builds existing
  assets; BuildAll does both. Tools/reference_build.py wraps tests/build.
- No Unity MCP bridge. Installed Editor batch compilation/tests/build plus actual
  Chrome player validation are used. See QA/ReferenceRevision/Report.md.
- QA read-only telemetry enabled by ?qa=1: public chance, state, positions,
  control bounds, pooled cars and impact contact. No future-result oracle.
- Historical QA/Overhaul captures refer to v2's timing-based game and are not
  evidence of current gameplay. Rollback: Backups/PreReferenceRevision/.
