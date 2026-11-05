// Builds a front-facing walk-cycle spritesheet from ONE static sprite.
//
// This is a cutout rig, not a redraw: the sprite is separated from its
// background, split into a body and two legs, and the legs are rotated about a
// hip pivot with a sinusoid while the body bobs. Because the body is drawn last
// it covers the hip seams, which is what makes a cutout read as a walk at all.
//
//   node Tools/walkcycle/walkcycle.js --src Assets/Art/Character/walk_source.png \
//        --out Assets/Art/Character/walk_front.png [--frames 8] [--debug]
const fs = require('fs');
const path = require('path');
const png = require('./png.js');

function parseArgs(argv) {
  const args = {};
  for (let i = 0; i < argv.length; i++) {
    if (!argv[i].startsWith('--')) continue;
    const key = argv[i].slice(2);
    const next = argv[i + 1];
    if (next === undefined || next.startsWith('--')) args[key] = true;
    else { args[key] = next; i++; }
  }
  return args;
}

const args = parseArgs(process.argv.slice(2));
if (!args.src || !args.out) {
  console.error('usage: node walkcycle.js --src <sprite.png> --out <sheet.png> [--frames 8] [--swing 22] [--lift 3] [--bob 1.5] [--tolerance 20] [--debug]');
  process.exit(2);
}

const FRAMES = Number(args.frames ?? 8);
const SWING = Number(args.swing ?? 22);      // peak leg rotation, degrees
const LIFT = Number(args.lift ?? 3);         // peak foot lift, pixels
const BOB = Number(args.bob ?? 1.5);         // vertical body bob, pixels
const SWAY = Number(args.sway ?? 1);         // body tilt, degrees
const TOLERANCE = Number(args.tolerance ?? 20);

// Fractions of the cropped character's height/width that define the rig.
const LEG_TOP = Number(args.legTop ?? 0.58);   // hips, from the top
const SPLIT_X = Number(args.splitX ?? 0.5);    // left/right leg divide

const source = png.decode(fs.readFileSync(args.src));
console.log(`source ${source.width}x${source.height}`);

// ---------------------------------------------------------------- background

function hasRealAlpha(img) {
  for (let i = 3; i < img.data.length; i += 4) if (img.data[i] < 250) return true;
  return false;
}

// Region-grow inward from the border, absorbing pixels whose colour is within
// `tolerance` of a neighbour already marked as background.
function floodBackground(img, tolerance) {
  const { width, height, data } = img;
  const bg = new Uint8Array(width * height);
  const stack = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    if (bg[y * width + x]) return;
    bg[y * width + x] = 1;
    stack.push(x, y);
  };
  for (let x = 0; x < width; x++) { push(x, 0); push(x, height - 1); }
  for (let y = 0; y < height; y++) { push(0, y); push(width - 1, y); }

  while (stack.length) {
    const y = stack.pop(), x = stack.pop();
    const i = (y * width + x) * 4;
    for (const [nx, ny] of [[x + 1, y], [x - 1, y], [x, y + 1], [x, y - 1]]) {
      if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
      const n = (ny * width + nx) * 4;
      if (bg[ny * width + nx]) continue;
      if (Math.abs(data[n] - data[i]) > tolerance) continue;
      if (Math.abs(data[n + 1] - data[i + 1]) > tolerance) continue;
      if (Math.abs(data[n + 2] - data[i + 2]) > tolerance) continue;
      bg[ny * width + nx] = 1;
      stack.push(nx, ny);
    }
  }
  return bg;
}

// Keep only the largest connected blob of foreground pixels; this is what drops
// a background cat or floor props that survived the flood fill.
function largestBlob(foreground, width, height) {
  const seen = new Uint8Array(width * height);
  let best = null, bestSize = 0;
  const stack = [];
  for (let start = 0; start < foreground.length; start++) {
    if (!foreground[start] || seen[start]) continue;
    const blob = [];
    stack.length = 0;
    stack.push(start);
    seen[start] = 1;
    while (stack.length) {
      const index = stack.pop();
      blob.push(index);
      const x = index % width, y = (index - x) / width;
      for (let dy = -1; dy <= 1; dy++) {
        for (let dx = -1; dx <= 1; dx++) {
          const nx = x + dx, ny = y + dy;
          if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
          const ni = ny * width + nx;
          if (!foreground[ni] || seen[ni]) continue;
          seen[ni] = 1;
          stack.push(ni);
        }
      }
    }
    if (blob.length > bestSize) { bestSize = blob.length; best = blob; }
  }
  const keep = new Uint8Array(width * height);
  if (best) for (const index of best) keep[index] = 1;
  return { keep, size: bestSize };
}

const realAlpha = hasRealAlpha(source);
const foreground = new Uint8Array(source.width * source.height);
if (realAlpha) {
  for (let p = 0; p < foreground.length; p++) foreground[p] = source.data[p * 4 + 3] > 8 ? 1 : 0;
} else {
  const bg = floodBackground(source, TOLERANCE);
  for (let p = 0; p < foreground.length; p++) foreground[p] = bg[p] ? 0 : 1;
  console.log('background removed by flood fill (no alpha channel in source)');
}

const { keep } = largestBlob(foreground, source.width, source.height);
let minX = source.width, minY = source.height, maxX = -1, maxY = -1;
for (let y = 0; y < source.height; y++) {
  for (let x = 0; x < source.width; x++) {
    if (!keep[y * source.width + x]) continue;
    if (x < minX) minX = x;
    if (x > maxX) maxX = x;
    if (y < minY) minY = y;
    if (y > maxY) maxY = y;
  }
}
if (maxX < 0) throw new Error('no foreground found - try a higher --tolerance');
const W = maxX - minX + 1;
const H = maxY - minY + 1;
const keptFraction = (W * H) / (source.width * source.height);
if (keptFraction > 0.92) console.warn('warning: kept almost the whole image, background removal probably failed');

const sprite = png.canvas(W, H);
for (let y = 0; y < H; y++) {
  for (let x = 0; x < W; x++) {
    const sourcePixel = (minY + y) * source.width + (minX + x);
    if (!keep[sourcePixel]) continue;
    const from = sourcePixel * 4;
    const to = (y * W + x) * 4;
    sprite.data[to] = source.data[from];
    sprite.data[to + 1] = source.data[from + 1];
    sprite.data[to + 2] = source.data[from + 2];
    sprite.data[to + 3] = 255;
  }
}
console.log(`character ${W}x${H} (${(keptFraction * 100).toFixed(1)}% of the image)`);

if (args.debug) {
  const debugPath = args.out.replace(/\.png$/, '.mask.png');
  fs.writeFileSync(debugPath, png.encode(sprite));
  console.log(`debug crop -> ${debugPath}`);
}

// ---------------------------------------------------------------- the rig

const legTop = Math.round(H * LEG_TOP);
const splitX = Math.round(W * SPLIT_X);
const hip = { x: splitX, y: legTop };
// A leg of length L swung by θ moves its foot L*sin(θ) sideways, so the cell has
// to grow by that much (plus the lift) or the feet get clipped.
const legLength = Math.max(1, H - legTop);
// Rows of leg the body is allowed to paint over. Without this the rotated legs
// open a triangular hole at the hip.
const hipOverlap = Math.max(2, Math.round(H * 0.03));
const pad = Math.ceil(legLength * Math.sin((Math.abs(SWING) * Math.PI) / 180)) + Math.ceil(LIFT) + 2;
const cellW = W + pad * 2;
const cellH = H + pad * 2;

const inLegs = (x, y) => y >= legTop;
const legMask = (x, y) => (inLegs(x, y) ? (x < splitX ? 1 : 2) : 0);

// Rotate one masked region of the sprite about the hip pivot into `dst`.
function drawPart(dst, angleDeg, offsetX, offsetY, mask) {
  const radians = (-angleDeg * Math.PI) / 180;
  const cos = Math.cos(radians), sin = Math.sin(radians);
  const pivotX = hip.x + pad, pivotY = hip.y + pad;
  for (let y = 0; y < cellH; y++) {
    for (let x = 0; x < cellW; x++) {
      // inverse-rotate the destination pixel back into source space
      const dx = x - pivotX, dy = y - pivotY;
      const srcX = Math.round(cos * dx - sin * dy + pivotX - pad - offsetX);
      const srcY = Math.round(sin * dx + cos * dy + pivotY - pad - offsetY);
      if (srcX < 0 || srcY < 0 || srcX >= W || srcY >= H) continue;
      if (mask(srcX, srcY) === 0) continue;
      const from = (srcY * W + srcX) * 4;
      png.blendPixel(dst, x, y, [
        sprite.data[from], sprite.data[from + 1], sprite.data[from + 2], sprite.data[from + 3],
      ]);
    }
  }
}

function drawBody(dst, angleDeg, offsetX, offsetY) {
  const radians = (-angleDeg * Math.PI) / 180;
  const cos = Math.cos(radians), sin = Math.sin(radians);
  const pivotX = hip.x + pad, pivotY = hip.y + pad;
  for (let y = 0; y < cellH; y++) {
    for (let x = 0; x < cellW; x++) {
      const dx = x - pivotX, dy = y - pivotY;
      const srcX = Math.round(cos * dx - sin * dy + pivotX - pad - offsetX);
      const srcY = Math.round(sin * dx + cos * dy + pivotY - pad - offsetY);
      if (srcX < 0 || srcY < 0 || srcX >= W || srcY >= H) continue;
      if (srcY >= legTop + hipOverlap) continue; // rest of the legs is drawn separately
      const from = (srcY * W + srcX) * 4;
      png.blendPixel(dst, x, y, [
        sprite.data[from], sprite.data[from + 1], sprite.data[from + 2], sprite.data[from + 3],
      ]);
    }
  }
}

const frames = [];
for (let frame = 0; frame < FRAMES; frame++) {
  const phase = (frame / FRAMES) * Math.PI * 2;
  const swing = SWING * Math.sin(phase);
  const canvas = png.canvas(cellW, cellH);

  // legs: opposite phase, each lifts as it travels forward
  const liftA = LIFT * Math.max(0, Math.sin(phase + Math.PI / 2));
  const liftB = LIFT * Math.max(0, Math.sin(phase - Math.PI / 2));

  drawPart(canvas, swing, 0, -liftA, (x, y) => (legMask(x, y) === 1 ? 1 : 0));   // back leg
  drawPart(canvas, -swing, 0, -liftB, (x, y) => (legMask(x, y) === 2 ? 1 : 0));  // front leg

  // body: two bobs per cycle (highest when the legs pass each other), plus a
  // counter-tilt so the torso does not look welded to the hips
  const bob = -BOB * (0.5 - 0.5 * Math.cos(phase * 2));
  const tilt = -SWAY * Math.sin(phase) * 0.5;
  drawBody(canvas, tilt, 0, bob);

  frames.push(canvas);
}

const sheet = png.canvas(cellW * FRAMES, cellH);
for (let i = 0; i < FRAMES; i++) {
  const frame = frames[i];
  for (let y = 0; y < cellH; y++) {
    for (let x = 0; x < cellW; x++) {
      const from = (y * cellW + x) * 4;
      if (frame.data[from + 3] === 0) continue;
      png.setPixel(sheet, i * cellW + x, y, [
        frame.data[from], frame.data[from + 1], frame.data[from + 2], frame.data[from + 3],
      ]);
    }
  }
}

fs.mkdirSync(path.dirname(args.out), { recursive: true });
fs.writeFileSync(args.out, png.encode(sheet));

const manifest = {
  sheet: path.basename(args.out),
  frameWidth: cellW,
  frameHeight: cellH,
  frameCount: FRAMES,
  columns: FRAMES,
  rows: 1,
  // A walk cycle is keyed at ~12-14fps and held by a 60fps renderer; playing
  // all 8 frames at 60fps would look like a 7.5x speed-up.
  suggestedFps: 14,
  loopSeconds: FRAMES / 14,
  pivot: { x: 0.5, y: 0.04 },
  rig: { legTop, splitX, swingDegrees: SWING, liftPixels: LIFT, bobPixels: BOB },
  unity: {
    sliceMode: 'Grid By Cell Size',
    cellSize: [cellW, cellH],
    naming: 'walk_0 .. walk_' + (FRAMES - 1),
    textureSettings: 'Sprite (2D and UI), Point filter, No compression, PPU 100',
  },
};
fs.writeFileSync(args.out.replace(/\.png$/, '.json'), JSON.stringify(manifest, null, 2) + '\n');

console.log(`wrote ${args.out}  ${sheet.width}x${sheet.height}  (${FRAMES} frames of ${cellW}x${cellH})`);
console.log(`wrote ${args.out.replace(/\.png$/, '.json')}`);
