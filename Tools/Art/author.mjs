import { mkdir, readFile, writeFile, access } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const toolDir = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(toolDir, '../..');
const artDir = path.join(root, 'Assets/Art');
const masterDir = path.join(artDir, 'Masters');
const resourcesDir = path.join(root, 'Assets/Resources');
const resourcesArtDir = path.join(resourcesDir, 'Art');
const spriteDir = path.join(resourcesArtDir, 'Sprites');
await Promise.all([mkdir(masterDir, { recursive: true }), mkdir(spriteDir, { recursive: true })]);

const SCALE = 2;
const INK = '#080e19';
const STEEL = `
  <linearGradient id="steel" x1="0" y1="0" x2="0.8" y2="1">
    <stop stop-color="#879aa8"/><stop offset=".26" stop-color="#4b6171"/>
    <stop offset=".53" stop-color="#304454"/><stop offset=".75" stop-color="#4e6270"/>
    <stop offset="1" stop-color="#182938"/>
  </linearGradient>
  <linearGradient id="darkSteel" x1="0" y1="0" x2=".8" y2="1">
    <stop stop-color="#405564"/><stop offset=".48" stop-color="#223441"/>
    <stop offset="1" stop-color="#12212e"/>
  </linearGradient>
  <linearGradient id="edge" x1="0" y1="0" x2="0" y2="1">
    <stop stop-color="#bed0d5"/><stop offset=".45" stop-color="#536d7a"/>
    <stop offset="1" stop-color="#263c4c"/>
  </linearGradient>
  <linearGradient id="teal" x1="0" y1="0" x2="0" y2="1">
    <stop stop-color="#a2f8ed"/><stop offset="1" stop-color="#27b9c4"/>
  </linearGradient>`;
const n = value => Number(value.toFixed(2));
const xy = (x, y) => `${n(x)},${n(y)}`;
const polar = (cx, cy, r, deg) => [cx + Math.cos(deg * Math.PI / 180) * r, cy + Math.sin(deg * Math.PI / 180) * r];
const pt = (cx, cy, r, deg) => polar(cx, cy, r, deg).map(n).join(' ');
const arc = (cx, cy, r, start, end) => `M${pt(cx, cy, r, start)} A${r} ${r} 0 ${Math.abs(end - start) > 180 ? 1 : 0} 1 ${pt(cx, cy, r, end)}`;
const circle = (x, y, r, fill, extra = '') => `<circle cx="${x}" cy="${y}" r="${r}" fill="${fill}" ${extra}/>`;
const rect = (x, y, width, height, fill, extra = '') => `<rect x="${x}" y="${y}" width="${width}" height="${height}" fill="${fill}" ${extra}/>`;
const polygon = (points, fill, extra = '') => `<polygon points="${points}" fill="${fill}" ${extra}/>`;
const p = (d, fill, extra = '') => `<path d="${d}" fill="${fill}" ${extra}/>`;
const inked = 'stroke="#080e19" stroke-width="5"';
const line = (d, color, width, extra = '') => p(d, 'none', `stroke="${color}" stroke-width="${width}" ${extra}`);
const screw = (x, y, r = 4) => `<g>${circle(x, y, r, '#142532', 'stroke="#080e19" stroke-width="1.7"')}${line(`M${x - r * .45} ${y + r * .35} L${x + r * .45} ${y - r * .35}`, '#78929d', 1.4)}</g>`;
const octagon = (x, y, w, h, cut) => `${x + cut},${y} ${x + w - cut},${y} ${x + w},${y + cut} ${x + w},${y + h - cut} ${x + w - cut},${y + h} ${x + cut},${y + h} ${x},${y + h - cut} ${x},${y + cut}`;
function annulus(cx, cy, outer, inner) {
  return `M${cx - outer} ${cy} a${outer} ${outer} 0 1 0 ${outer * 2} 0 a${outer} ${outer} 0 1 0 ${-outer * 2} 0 Z M${cx - inner} ${cy} a${inner} ${inner} 0 1 0 ${inner * 2} 0 a${inner} ${inner} 0 1 0 ${-inner * 2} 0 Z`;
}
function svg(name, width, height, defs, content) {
  return `<?xml version="1.0" encoding="UTF-8"?>\n<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" fill="none" stroke-linecap="round" stroke-linejoin="round">\n<title>${name}</title>\n<desc>Original Bubble Pop arcade-industrial sprite. Editable vector geometry; no embedded raster, fonts, filters, or external resources.</desc>\n<defs>${defs}\n</defs>\n${content}\n</svg>\n`;
}
const assets = [];
function add(name, width, height, category, defs, content, options = {}) {
  assets.push({ name, width, height, category, source: `Assets/Art/Masters/${name}.svg`, file: `Assets/Resources/Art/Sprites/${name}.png`, pivot: [0.5, 0.5], border: [0, 0, 0, 0], ...options, svg: svg(name, width, height, defs, content) });
}

// The shell is shared; each large, hand-built emblem has a different silhouette.
const orbPalettes = {
  fire: { light: '#ffd27c', mid: '#f89528', dark: '#bd431d', face: '#673016', low: '#301b17', facets: ['#ffc064', '#e77924', '#f7a237', '#d75d20'], ink: '#ffb943' },
  void: { light: '#d4b4ff', mid: '#9860e9', dark: '#51289b', face: '#3e245e', low: '#1d1834', facets: ['#bd8df7', '#8c58d4', '#a372eb', '#683eae'], ink: '#c6a0ff' },
  circuit: { light: '#d0f98b', mid: '#78c94b', dark: '#318142', face: '#254c30', low: '#132d25', facets: ['#b8e972', '#68b849', '#8ad451', '#459945'], ink: '#b9f574' },
  storm: { light: '#a3dfff', mid: '#438fe0', dark: '#2756a6', face: '#203c65', low: '#14233e', facets: ['#7dc3f5', '#397bcc', '#58a2e9', '#3264b3'], ink: '#bcecff' },
  solar: { light: '#fff2a4', mid: '#e8c54e', dark: '#ae7529', face: '#59461d', low: '#302819', facets: ['#f9df77', '#d5ac38', '#ecd05e', '#bf9130'], ink: '#ffed9c' },
  ice: { light: '#d3fcff', mid: '#6ed1de', dark: '#2b8dba', face: '#245061', low: '#132e40', facets: ['#b8edf3', '#52b5cc', '#8adce5', '#399cb9'], ink: '#c5faff' }
};
function orbShell(key, emblem) {
  const c = orbPalettes[key];
  const defs = `<linearGradient id="rim" x1=".15" y1="0" x2=".8" y2="1"><stop stop-color="${c.light}"/><stop offset=".42" stop-color="${c.mid}"/><stop offset="1" stop-color="${c.dark}"/></linearGradient>
    <radialGradient id="recess" cx=".38" cy=".3" r=".8"><stop stop-color="${c.face}"/><stop offset="1" stop-color="${c.low}"/></radialGradient>
    <linearGradient id="emblem" x1=".2" y1="0" x2=".7" y2="1"><stop stop-color="${c.light}"/><stop offset="1" stop-color="${c.mid}"/></linearGradient>`;
  let facets = '';
  for (let i = 0; i < 16; i++) {
    const a = -101.25 + i * 22.5;
    const coords = [polar(128, 128, 111.5, a), polar(128, 128, 111.5, a + 22.5), polar(128, 128, 86, a + 28), polar(128, 128, 86, a + 4)].map(v => xy(...v)).join(' ');
    facets += polygon(coords, c.facets[i % 4], 'opacity=".68"');
    facets += line(`M${pt(128, 128, 110, a)} L${pt(128, 128, 87, a + 4)}`, c.light, 1.5, 'opacity=".28"');
  }
  const body = `<g id="ink-silhouette">${circle(128, 128, 118, INK)}</g>
    <g id="faceted-shell">${circle(128, 128, 112, 'url(#rim)')}${facets}
    ${line(arc(128, 128, 109.5, 201, 283), c.light, 3, 'opacity=".8"')}
    ${line(arc(128, 128, 110, 23, 103), c.dark, 3, 'opacity=".7"')}
    ${line(arc(128, 128, 101, 52, 66), INK, 3, 'opacity=".5"')}
    ${line(arc(128, 128, 101, 73, 87), INK, 3, 'opacity=".5"')}</g>
    <g id="recessed-face">${circle(128, 128, 84, 'url(#recess)', 'stroke="#080e19" stroke-width="7"')}
    ${line(arc(128, 128, 79, 198, 294), c.ink, 1.6, 'opacity=".38"')}
    ${line(arc(128, 128, 80, 17, 101), INK, 2, 'opacity=".7"')}</g>
    <g id="element-emblem">${emblem}</g>`;
  add(`orb_${key}`, 256, 256, 'orbs', defs, body, { visibleRadius: 118, gameplaySize: [48, 54] });
}

let fireRays = [];
for (let i = 0; i < 24; i++) {
  const radii = [67, 41, 62, 43, 70, 42, 64, 42];
  fireRays.push(polar(128, 132, radii[i % 8], -90 + i * 15));
}
orbShell('fire', `${polygon(fireRays.map(v => xy(...v)).join(' '), '#ed7023', 'stroke="#281814" stroke-width="4"')}
  ${p('M128 74 C127 96 145 107 148 122 C154 116 156 109 155 102 C177 127 182 147 171 164 C162 182 142 189 123 184 C99 182 84 165 88 147 C91 129 109 119 111 100 C116 110 116 120 122 126 C136 115 134 96 128 74 Z', '#ffce65', inked)}
  ${p('M131 128 C131 143 149 148 145 161 C141 174 123 178 115 166 C106 154 122 145 131 128 Z', '#fff0b1')}
  ${line('M102 152 C100 163 110 174 119 176', '#fff0b1', 3, 'opacity=".65"')}`);

orbShell('void', `${[0, 120, 240].map(angle => `<g transform="rotate(${angle} 128 128)">
  ${p('M128 62 C169 56 200 90 191 126 C180 100 158 94 138 103 C116 112 113 132 123 147 C98 140 87 116 96 94 C103 77 115 67 128 62 Z', 'url(#emblem)', inked)}
  ${line('M121 77 C147 65 174 82 180 100', '#e5ccff', 3, 'opacity=".75"')}
  </g>`).join('')}
  ${circle(128, 128, 23, '#111424', 'stroke="#9670d8" stroke-width="3.5"')}
  ${line(arc(128, 128, 17.5, 185, 284), '#dac2ff', 2.4, 'opacity=".85"')}`);

const circuitTraces = `${arc(128, 128, 54, -88, 63)} ${arc(128, 128, 54, 94, 242)}
  ${arc(128, 128, 35, -47, 120)} ${arc(128, 128, 35, 153, 282)}
  M128 74 V58 M167 91 L180 78 V63 M182 128 H198 M154 176 V194 M89 166 L77 178 H64 M74 128 H59 M92 88 L80 76`;
orbShell('circuit', `${line(circuitTraces, INK, 12)}${line(circuitTraces, '#9fe26c', 6.5)}
  ${[[128, 58], [180, 63], [198, 128], [154, 194], [64, 178], [59, 128], [80, 76]].map(([x, y]) => circle(x, y, 5.5, '#c5f58d', 'stroke="#142d23" stroke-width="3"')).join('')}
  ${circle(128, 128, 15, '#b8ed80', inked)}
  ${circle(124, 124, 4, '#edfaca', 'opacity=".75"')}`);

orbShell('storm', `${p('M141 61 L90 132 H117 L106 195 L170 113 H141 L157 61 Z', '#b7eaff', 'stroke="#080e19" stroke-width="7"')}
  ${polygon('141,68 103,124 127,124 120,151 151,117 132,117', '#f0fcff')}
  ${line('M78 96 L68 113 L80 114', '#4d91dc', 4)}
  ${line('M180 145 L190 144 L180 162', '#4d91dc', 4)}`);

const solarNodes = Array.from({ length: 6 }, (_, i) => polar(128, 128, 56, -90 + i * 60));
const solarConnections = `${solarNodes.map(v => `M128 128 L${v.map(n).join(' ')}`).join(' ')} M${solarNodes.map(v => v.map(n).join(' ')).join(' L')} Z`;
orbShell('solar', `${line(solarConnections, INK, 13)}${line(solarConnections, '#e7bd4f', 6.5)}
  ${solarNodes.map(([x, y]) => circle(n(x), n(y), 10, '#ffe68b', 'stroke="#241e18" stroke-width="4.5"')).join('')}
  ${circle(128, 128, 17, '#ffedaa', 'stroke="#241e18" stroke-width="5"')}
  ${circle(124, 123, 5, '#fff9d5')}`);

orbShell('ice', `<g id="side-crystals">
  ${polygon('84,91 107,119 109,164 74,139', '#9be3ec', inked)}
  ${polygon('84,91 88,132 74,139', '#e0fcff')}
  ${polygon('88,132 109,164 74,139', '#439fb9')}
  ${polygon('172,91 182,139 147,164 149,119', '#8ddde9', inked)}
  ${polygon('172,91 163,133 149,119', '#dcfcff')}
  ${polygon('163,133 182,139 147,164', '#3e99ba')}
  </g><g id="main-crystal">
  ${polygon('128,60 156,125 128,194 100,125', '#b9f4f5', 'stroke="#080e19" stroke-width="6"')}
  ${polygon('128,60 128,125 100,125', '#e1feff')}
  ${polygon('128,60 156,125 128,125', '#84d9e9')}
  ${polygon('100,125 128,125 128,194', '#66c0d5')}
  ${polygon('128,125 156,125 128,194', '#338bac')}
  ${line('M128 64 V190 M103 125 H153', '#17364a', 2.5, 'opacity=".65"')}
  ${polygon('128,99 139,125 128,150 117,125', '#bff7fa', 'stroke="#214d63" stroke-width="3"')}
  </g>`);

const buttonColors = {
  green: ['#d8fbae', '#83cc5b', '#347344', '#74ba51'],
  magenta: ['#ffc1f0', '#d763b6', '#7b316f', '#c84ca5'],
  cyan: ['#c2fbff', '#60c8d9', '#246c85', '#3cb5c7'],
  yellow: ['#fff1ac', '#e5c354', '#927032', '#dbb43d'],
  blue: ['#c0e3ff', '#629bdd', '#305389', '#4c88cf']
};
for (const [key, c] of Object.entries(buttonColors)) {
  const defs = `<linearGradient id="body" x1=".25" y1="0" x2=".65" y2="1"><stop stop-color="${c[0]}"/><stop offset=".35" stop-color="${c[1]}"/><stop offset="1" stop-color="${c[2]}"/></linearGradient>
    <linearGradient id="face" x1=".2" y1="0" x2=".65" y2="1"><stop stop-color="${c[1]}"/><stop offset=".6" stop-color="${c[3]}"/><stop offset="1" stop-color="${c[2]}"/></linearGradient>`;
  add(`button_${key}`, 192, 192, 'buttons', defs, `<g id="outer-rim">
    ${circle(96, 96, 88, INK)}${circle(96, 94, 81, 'url(#body)')}
    ${circle(96, 91, 71, 'url(#face)', 'stroke="#0d1920" stroke-width="4"')}
    ${line(arc(96, 94, 77, 209, 291), c[0], 3, 'opacity=".75"')}
    ${line(arc(96, 94, 77, 35, 139), c[2], 3)}
    </g><g id="restrained-gloss">
    ${p('M41 74 C47 47 68 28 96 27 C122 26 143 42 151 64 C117 55 80 57 41 74 Z', '#ffffff', 'opacity=".18"')}
    ${line('M57 49 C65 41 77 36 89 35', c[0], 4, 'opacity=".65"')}
    ${line('M66 147 C84 155 108 155 126 146', c[1], 2, 'opacity=".65"')}
    </g>`, { blankFace: true });
}

for (const [key, c] of Object.entries(buttonColors)) {
  const defs = `<linearGradient id="body" x1="0" y1="0" x2="0" y2="1"><stop stop-color="${c[0]}"/><stop offset=".4" stop-color="${c[1]}"/><stop offset="1" stop-color="${c[2]}"/></linearGradient>
    <linearGradient id="face" x1="0" y1="0" x2="0" y2="1"><stop stop-color="${c[1]}"/><stop offset=".55" stop-color="${c[3]}"/><stop offset="1" stop-color="${c[2]}"/></linearGradient>`;
  add(`pill_${key}`, 384, 96, 'buttons', defs, `<g id="pill-rim">
    ${rect(3, 3, 378, 90, INK, 'rx="45"')}${rect(9, 7, 366, 78, 'url(#body)', 'rx="39"')}
    ${rect(17, 14, 350, 60, 'url(#face)', 'rx="30" stroke="#0d1920" stroke-width="3.5"')}
    ${line('M48 11 H336', c[0], 2.6, 'opacity=".7"')}
    </g><g id="restrained-gloss">
    ${p('M44 40 C44 27 60 20 88 20 H296 C324 20 340 27 340 40 C290 33 94 33 44 40 Z', '#ffffff', 'opacity=".17"')}
    ${line('M60 82 H324', c[2], 2, 'opacity=".6"')}
    </g>`, { blankFace: true, border: [44, 44, 44, 44] });
}

const baseMount = polygon('105,10 151,10 159,31 97,31', 'url(#darkSteel)', 'stroke="#080e19" stroke-width="5"');
const baseContent = `<g id="mounts">${[0, 90, 180, 270].map(a => `<g transform="rotate(${a} 128 128)">${baseMount}</g>`).join('')}</g>
  <g id="outer-housing">${circle(128, 128, 111, 'url(#darkSteel)', 'stroke="#080e19" stroke-width="7"')}
  ${circle(128, 128, 100, 'url(#steel)', 'stroke="#080e19" stroke-width="4"')}
  ${line(arc(128, 128, 105, 202, 290), '#90a7af', 2.2, 'opacity=".7"')}
  ${circle(128, 128, 83, '#13232e', 'stroke="#080e19" stroke-width="5"')}
  ${[0, 90, 180, 270].map(a => `<g transform="rotate(${a} 128 128)">
    ${line(arc(128, 128, 89, -119, -61), INK, 11)}
    ${line(arc(128, 128, 89, -119, -61), 'url(#teal)', 5)}
    ${polygon('117,25 139,25 143,41 113,41', '#334c5c', 'stroke="#080e19" stroke-width="3"')}
    ${rect(122, 29, 12, 5, '#84e5e4', 'rx="1"')}
    </g>`).join('')}
  ${[45, 135, 225, 315].map(a => { const v = polar(128, 128, 94, a); return screw(n(v[0]), n(v[1]), 4); }).join('')}
  </g><g id="recessed-socket">${circle(128, 128, 67, 'url(#steel)', 'stroke="#080e19" stroke-width="6"')}
  ${circle(128, 128, 55, '#142633', 'stroke="#080e19" stroke-width="4"')}
  ${line(arc(128, 128, 49, 201, 337), '#3f6576', 3)}
  ${line(arc(128, 128, 49, 21, 157), '#091722', 3)}
  ${[0, 90, 180, 270].map(a => `<g transform="rotate(${a} 128 128)">${polygon('119,64 137,64 134,78 122,78', '#627b86', 'stroke="#080e19" stroke-width="3"')}</g>`).join('')}
  </g>`;
add('turret_base', 256, 256, 'hardware', STEEL, baseContent);

add('turret_barrel', 128, 256, 'hardware', STEEL, `<g id="barrel-silhouette">
  ${p('M44 10 H84 L103 30 V79 L92 97 L96 171 L109 185 V225 L92 245 H36 L19 225 V185 L32 171 L36 97 L25 79 V30 Z', 'url(#darkSteel)', 'stroke="#080e19" stroke-width="6"')}
  </g><g id="split-steel-rails">
  ${polygon('42,25 54,25 54,176 41,176 45,95 36,77 36,37', 'url(#steel)', 'stroke="#080e19" stroke-width="3.5"')}
  ${polygon('74,25 86,25 92,37 92,77 83,95 87,176 74,176', 'url(#steel)', 'stroke="#080e19" stroke-width="3.5"')}
  ${rect(57, 33, 14, 143, '#091722', 'rx="3"')}
  ${rect(59, 45, 10, 66, 'url(#teal)', 'rx="2"')}
  ${rect(60, 121, 8, 33, '#247c91', 'rx="2"')}
  ${line('M43 41 V72 L51 90', '#9db6be', 2.5)}
  ${line('M85 40 V72 L77 90', '#5d7b8c', 2.5)}
  </g><g id="muzzle-and-collars">
  ${polygon('40,11 88,11 99,27 93,42 35,42 29,27', 'url(#steel)', 'stroke="#080e19" stroke-width="5"')}
  ${rect(45, 17, 38, 12, '#0a1723', 'rx="3" stroke="#a4bac0" stroke-width="1.4"')}
  ${rect(52, 20, 24, 5, '#58d2db', 'rx="1"')}
  ${polygon('35,163 93,163 99,182 29,182', 'url(#steel)', 'stroke="#080e19" stroke-width="4"')}
  ${rect(43, 168, 42, 5, 'url(#teal)', 'rx="1.5"')}
  ${polygon('37,184 91,184 101,195 101,221 87,237 41,237 27,221 27,195', 'url(#steel)', 'stroke="#080e19" stroke-width="5"')}
  ${polygon('46,191 82,191 90,200 90,219 80,229 48,229 38,219 38,200', '#1c3343', 'stroke="#080e19" stroke-width="3"')}
  ${rect(52, 199, 24, 16, 'url(#teal)', 'rx="3" stroke="#080e19" stroke-width="3"')}
  ${screw(36, 206, 3)}${screw(92, 206, 3)}
  </g>`, { pivot: [0.5, 0], direction: 'up' });

add('chamber_ring', 256, 256, 'hardware', STEEL, `<g id="hollow-steel-ring">
  ${p(annulus(128, 128, 118, 79), INK, 'fill-rule="evenodd"')}
  ${p(annulus(128, 128, 112, 85), 'url(#steel)', 'fill-rule="evenodd"')}
  ${circle(128, 128, 88, 'none', 'stroke="#080e19" stroke-width="4"')}
  ${circle(128, 128, 108, 'none', 'stroke="#9ab2bc" stroke-width="1.5" opacity=".5"')}
  ${[0, 90, 180, 270].map(a => `<g transform="rotate(${a} 128 128)">
    ${line(arc(128, 128, 98, -117, -63), INK, 12)}
    ${line(arc(128, 128, 98, -117, -63), 'url(#teal)', 6)}
    ${line('M128 17 V42', INK, 4)}${screw(128, 28, 3.5)}
    </g>`).join('')}
  </g>`, { transparentCenter: true, holeRadius: 78 });

const cells = {
  full: ['#adf9f4', '#39bfc9', 5],
  empty: ['#d87070', '#803e48', 0],
  green: ['#d4f59b', '#7cbb55', 5],
  yellow: ['#fff0a6', '#dbb44e', 5],
  blue: ['#c0e5ff', '#5b9be2', 5]
};
for (const [key, [light, mid, charge]] of Object.entries(cells)) {
  const defs = `${STEEL}<linearGradient id="cell" x1="0" y1="0" x2=".8" y2="1"><stop stop-color="${light}"/><stop offset="1" stop-color="${mid}"/></linearGradient>`;
  const bars = Array.from({ length: 5 }, (_, i) => rect(23, 36 + 18 * i, 34, 13, charge > i ? 'url(#cell)' : '#192c39', 'rx="2" stroke="#080e19" stroke-width="1.8"')).join('');
  add(`battery_${key}`, 80, 160, 'batteries', defs, `<g id="cell-housing">
    ${rect(27, 4, 26, 17, 'url(#steel)', 'rx="3" stroke="#080e19" stroke-width="4"')}
    ${polygon(octagon(9, 17, 62, 136, 9), 'url(#steel)', 'stroke="#080e19" stroke-width="4.5"')}
    ${polygon(octagon(17, 29, 46, 108, 4), '#101e2b', 'stroke="#080e19" stroke-width="3"')}
    ${bars}
    ${line('M13 37 V129', '#a8c2cb', 2, 'opacity=".6"')}
    ${line('M66 41 V128', '#11212e', 2)}
    ${rect(28, 141, 24, 4, charge ? mid : '#b45560', 'rx="1"')}
    ${line('M22 23 H58', '#a0b5bf', 1.6, 'opacity=".55"')}
    </g>`, { chargeSegments: charge });
}

add('hud_panel', 512, 256, 'frames', `${STEEL}
  <linearGradient id="panel" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#182b38"/><stop offset="1" stop-color="#0f1b28"/></linearGradient>`,
  `<g id="outer-bevel">${rect(5, 5, 502, 246, 'url(#steel)', 'rx="24" stroke="#080e19" stroke-width="7"')}
    ${rect(13, 13, 486, 230, 'none', 'rx="18" stroke="#a1b3bb" stroke-width="1.8" opacity=".75"')}
    ${rect(20, 20, 472, 216, '#0a1722', 'rx="14" stroke="#080e19" stroke-width="4"')}
    </g><g id="double-inset-and-blank-face">
    ${rect(27, 27, 458, 202, 'url(#panel)', 'rx="10" stroke="#4f727f" stroke-width="2"')}
    ${line('M39 29 H473', '#6ba9b5', 1.4, 'opacity=".55"')}
    ${line('M39 227 H473', '#07141e', 2)}
    </g>`, { border: [32, 32, 32, 32], blankFace: true });

let badgeFacets = '';
const badgeOuter = octagon(7, 7, 242, 242, 72).split(' ').map(v => v.split(',').map(Number));
const badgeInner = octagon(31, 31, 194, 194, 58).split(' ').map(v => v.split(',').map(Number));
for (let i = 0; i < 8; i++) {
  badgeFacets += polygon([badgeOuter[i], badgeOuter[(i + 1) % 8], badgeInner[(i + 1) % 8], badgeInner[i]].map(v => v.join(',')).join(' '), ['#8296a2', '#59717f', '#314856', '#263b4b', '#3d5563', '#526976', '#3b5261', '#a3b5bd'][i], 'stroke="#111e2a" stroke-width="2"');
}
add('rank_badge', 256, 256, 'frames', STEEL, `<g id="octagonal-medal">
  ${polygon(octagon(6, 6, 244, 244, 73), INK, 'stroke="#080e19" stroke-width="5"')}${badgeFacets}
  ${polygon(octagon(33, 33, 190, 190, 55), 'url(#darkSteel)', 'stroke="#080e19" stroke-width="5"')}
  ${polygon(octagon(43, 43, 170, 170, 49), '#142533', 'stroke="#5b7c88" stroke-width="2"')}
  ${line('M92 19 H164', '#b8c9cf', 2.5)}
  ${line('M92 237 H164', '#1e3443', 3)}
  ${rect(101, 43, 54, 5, 'url(#teal)', 'rx="2"')}
  ${polygon('104,201 128,211 152,201 145,214 128,222 111,214', '#c8ac67', 'stroke="#080e19" stroke-width="2.5"')}
  ${screw(35, 86, 3.5)}${screw(221, 86, 3.5)}${screw(35, 170, 3.5)}${screw(221, 170, 3.5)}
  </g><g id="blank-central-plaque">
  ${polygon(octagon(49, 77, 158, 104, 13), 'url(#steel)', 'stroke="#080e19" stroke-width="4"')}
  ${polygon(octagon(56, 84, 144, 90, 10), '#122330', 'stroke="#718b95" stroke-width="1.5"')}
  </g>`, { blankFace: true });

const boardOuter = 'M28 4 H612 L636 28 V732 L612 756 H28 L4 732 V28 Z';
const boardInner = 'M34 18 H606 L622 34 V726 L606 742 H34 L18 726 V34 Z';
add('board_frame', 640, 760, 'frames', STEEL, `<g id="hollow-chamfered-frame">
  ${p(`${boardOuter} ${boardInner}`, 'url(#steel)', 'fill-rule="evenodd" stroke="#080e19" stroke-width="3.5"')}
  ${line('M7 29 L29 7 H611 L633 29', '#a6bbc4', 1.7, 'opacity=".75"')}
  ${line('M7 731 L29 753 H611 L633 731', '#172a39', 2.5)}
  ${line('M20 83 V35 L35 20 H112 M528 20 H605 L620 35 V83 M620 677 V725 L605 740 H528 M112 740 H35 L20 725 V677', '#55919c', 1.8)}
  ${[['M10 86 V220', 'M630 86 V220'], ['M10 540 V674', 'M630 540 V674']].flat().map(d => `${line(d, INK, 7)}${line(d, 'url(#teal)', 3.5)}`).join('')}
  ${line('M229 10 H411 M229 750 H411', INK, 7)}
  ${line('M229 10 H411 M229 750 H411', '#68cdd1', 3.5)}
  ${line('M4 259 H17 M623 259 H636 M4 501 H17 M623 501 H636', INK, 2.5)}
  ${screw(23, 23, 3)}${screw(617, 23, 3)}${screw(23, 737, 3)}${screw(617, 737, 3)}
  </g>`, { transparentCenter: true, border: [40, 40, 40, 40] });

let backgroundCircuits = '';
for (let side = 0; side < 2; side++) {
  let traces = '';
  const routes = [
    'M-8 169 H27 L61 203 V307 L87 333 V423',
    'M-9 306 H16 L39 329 V478 L68 507 V591',
    'M-10 639 H29 L49 659 V772 L91 814 V898',
    'M-5 937 H22 L68 983 V1106 L44 1130 V1281',
    'M108 -8 V70 L77 101 V158',
    'M-5 1171 H17 L30 1184 V1281'
  ];
  for (const route of routes) traces += line(route, '#4c7184', 1.5, 'opacity=".15"');
  for (const [x, y] of [[87, 423], [68, 591], [91, 898], [77, 158]]) traces += circle(x, y, 3, 'none', 'stroke="#547a8a" stroke-width="1.3" opacity=".17"');
  backgroundCircuits += `<g${side ? ' transform="translate(720 0) scale(-1 1)"' : ''}>${traces}</g>`;
}
let specks = '';
let seed = 53291;
const random = () => { seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0; return seed / 4294967296; };
for (let i = 0; i < 72; i++) {
  const x = n(12 + random() * 696);
  const y = n(12 + random() * 1256);
  specks += circle(x, y, i % 9 === 0 ? 1.15 : .65, '#9bb1c2', `opacity="${n(.055 + random() * .095)}"`);
}
add('background', 720, 1280, 'backdrops', `<linearGradient id="navy" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#141b29"/><stop offset=".45" stop-color="#101421"/><stop offset="1" stop-color="#0c1320"/></linearGradient>
  <radialGradient id="vignette" cx=".5" cy=".42" r=".72"><stop offset=".35" stop-color="#030712" stop-opacity="0"/><stop offset="1" stop-color="#030712" stop-opacity=".46"/></radialGradient>`,
  `${rect(0, 0, 720, 1280, 'url(#navy)')}<g id="subtle-edge-circuits">${backgroundCircuits}</g><g id="sparse-specks">${specks}</g>${rect(0, 0, 720, 1280, 'url(#vignette)')}`, { opaque: true });

add('board_backdrop', 600, 640, 'backdrops', `<pattern id="grid" width="32" height="32" patternUnits="userSpaceOnUse"><path d="M32 0 H0 V32" fill="none" stroke="#3b5c6e" stroke-width=".8" opacity=".18"/></pattern>
  <pattern id="points" width="64" height="64" patternUnits="userSpaceOnUse"><circle cx="32" cy="32" r=".7" fill="#416374" opacity=".16"/></pattern>
  <radialGradient id="vignette" cx=".5" cy=".42" r=".75"><stop offset=".3" stop-color="#08111b" stop-opacity="0"/><stop offset="1" stop-color="#08111b" stop-opacity=".3"/></radialGradient>`,
  `${rect(0, 0, 600, 640, '#101c27')}${rect(0, 0, 600, 640, 'url(#grid)')}${rect(0, 0, 600, 640, 'url(#points)')}${rect(0, 0, 600, 640, 'url(#vignette)')}`, { opaque: true });

const iconLine = d => `${line(d, INK, 15)}${line(d, '#f4fbff', 9)}`;
const iconShape = d => p(d, '#f4fbff', 'stroke="#080e19" stroke-width="3.5"');
const speaker = 'M17 37 H30 L47 23 V73 L30 59 H17 Z';
const icons = {
  restart: `${iconLine('M71 30 C64 19 49 16 37 22 C22 29 18 46 24 60 C30 75 48 81 62 74 C70 70 75 63 77 56')}${iconShape('M74 15 L77 39 L53 35 Z')}`,
  help: `${iconLine('M32 30 C32 17 62 14 65 31 C68 44 48 44 48 58')}${circle(48, 75, 6, '#f4fbff', 'stroke="#080e19" stroke-width="3"')}`,
  sound: `${iconShape(speaker)}${iconLine('M60 34 C68 42 68 54 60 62')}${iconLine('M72 24 C86 37 86 59 72 72')}`,
  muted: `${iconShape(speaker)}${iconLine('M63 36 L80 60 M80 36 L63 60')}`,
  pause: `${rect(25, 20, 17, 56, '#f4fbff', 'rx="3" stroke="#080e19" stroke-width="3.5"')}${rect(54, 20, 17, 56, '#f4fbff', 'rx="3" stroke="#080e19" stroke-width="3.5"')}`,
  play: iconShape('M31 19 L78 48 L31 77 Z'),
  trophy: `${iconLine('M29 25 H16 V36 C16 46 24 53 34 53 M67 25 H80 V36 C80 46 72 53 62 53')}${iconShape('M29 19 H67 V40 C67 53 58 62 48 62 C38 62 29 53 29 40 Z')}${iconShape('M43 59 H53 V72 H65 V80 H31 V72 H43 Z')}`,
  swap: `${iconLine('M20 32 H76 M76 64 H20')}${iconShape('M64 18 L80 32 L64 46 Z')}${iconShape('M32 50 L16 64 L32 78 Z')}`,
  close: iconLine('M27 27 L69 69 M69 27 L27 69')
};
for (const [name, content] of Object.entries(icons)) add(`icon_${name}`, 96, 96, 'icons', '', `<g id="tintable-symbol">${content}</g>`, { tintable: true });

add('px_white', 8, 8, 'effects', '', rect(0, 0, 8, 8, '#ffffff'), { tintable: true });
add('orb_ghost', 256, 256, 'effects', '', `<g id="tintable-landing-marker">${circle(128, 128, 112, '#ffffff', 'opacity=".16"')}${circle(128, 128, 112, 'none', 'stroke="#ffffff" stroke-width="9" stroke-dasharray="24 15" opacity=".95"')}${circle(128, 128, 5, '#ffffff', 'opacity=".9"')}</g>`, { tintable: true });
add('aim_dot', 32, 32, 'effects', '', `${circle(16, 16, 10, '#81e9f1')}${circle(14.5, 14.5, 4, '#c4ffff', 'opacity=".75"')}`);
add('vfx_ring', 128, 128, 'effects', '', `<g id="tintable-ring">${circle(64, 64, 48, 'none', 'stroke="#ffffff" stroke-width="6"')}${circle(64, 64, 42, 'none', 'stroke="#ffffff" stroke-width="1.5" opacity=".3"')}</g>`, { tintable: true, transparentCenter: true });
add('vfx_spark', 128, 128, 'effects', '', `<g id="tintable-four-point-spark">${p('M64 8 L74 48 L113 64 L75 75 L64 120 L53 77 L15 64 L53 52 Z', '#ffffff')}${polygon('29,17 33,26 43,30 33,34 29,44 25,34 16,30 25,26', '#ffffff', 'opacity=".65"')}</g>`, { tintable: true });
add('vfx_shard', 128, 128, 'effects', '', `<g id="tintable-fracture">${polygon('70,10 99,58 54,119 32,66', '#ffffff', 'opacity=".75"')}${polygon('70,10 63,62 32,66', '#ffffff')}${polygon('63,62 99,58 54,119', '#ffffff', 'opacity=".4"')}</g>`, { tintable: true });

const segments = {
  a: '18,6 46,6 51,11 46,16 18,16 13,11',
  b: '49,14 54,19 54,40 49,45 44,40 44,19',
  c: '49,51 54,56 54,77 49,82 44,77 44,56',
  d: '18,80 46,80 51,85 46,90 18,90 13,85',
  e: '15,51 20,56 20,77 15,82 10,77 10,56',
  f: '15,14 20,19 20,40 15,45 10,40 10,19',
  g: '18,43 46,43 51,48 46,53 18,53 13,48'
};
const digitMap = ['abcdef', 'bc', 'abdeg', 'abcdg', 'bcfg', 'acdfg', 'acdefg', 'abc', 'abcdefg', 'abcdfg'];
const digitTones = { cyan: ['#b9fcff', '#3fc8da'], amber: ['#ffecb0', '#f0a22a'], red: ['#ffc9be', '#ec4843'] };
for (const [tone, [top, bottom]] of Object.entries(digitTones)) {
  const defs = `<linearGradient id="segment" x1="0" y1="0" x2="0" y2="1"><stop stop-color="${top}"/><stop offset="1" stop-color="${bottom}"/></linearGradient>`;
  for (let digit = 0; digit < 10; digit++) {
    let halo = '';
    let content = '';
    for (const [id, points] of Object.entries(segments)) {
      const active = digitMap[digit].includes(id);
      if (active) halo += polygon(points, bottom, `stroke="${bottom}" stroke-width="6" opacity=".22"`);
      content += polygon(points, active ? 'url(#segment)' : '#1b2c38', `id="segment-${id}"${active ? '' : ' opacity=".55"'}`);
    }
    add(`digit_${tone}_${digit}`, 64, 96, 'digits', defs, `<g id="led-glow">${halo}</g><g id="seven-segment-digit">${content}</g>`);
  }
  add(`colon_${tone}`, 32, 96, 'digits', defs, `<g id="led-glow">${rect(10, 24, 12, 12, bottom, `rx="2" stroke="${bottom}" stroke-width="6" opacity=".22"`)}${rect(10, 60, 12, 12, bottom, `rx="2" stroke="${bottom}" stroke-width="6" opacity=".22"`)}</g><g id="colon-dots">${rect(10, 24, 12, 12, 'url(#segment)', 'rx="2"')}${rect(10, 60, 12, 12, 'url(#segment)', 'rx="2"')}</g>`);
}

// World size (in Unity units) of each sprite's full width. The importer PPU is derived from this,
// so scene objects and runtime code can use the sprite at scale 1 with no per-call sizing.
const worldWidths = {
  turret_base: 1.5, turret_barrel: 0.78, chamber_ring: 1.5,
  orb_ghost: 1.04, aim_dot: 1, px_white: 1,
  vfx_ring: 1.3, vfx_spark: 1, vfx_shard: 0.5,
  battery_full: 0.3, battery_empty: 0.3, battery_green: 0.3, battery_yellow: 0.3, battery_blue: 0.3,
  board_frame: 8.64, board_backdrop: 1, background: 1
};
for (const key of Object.keys(orbPalettes)) worldWidths[`orb_${key}`] = 1.04;
for (const asset of assets) asset.worldWidth = worldWidths[asset.name] ?? null;

const hash = value => createHash('sha256').update(`bubble-pop-original-art-v1:${value}`).digest('hex').slice(0, 32);
async function writeIfMissing(file, content) {
  try { await access(file); } catch { await writeFile(file, content); }
}
function textureMeta(asset) {
  const customPivot = asset.pivot[1] === 0;
  return `fileFormatVersion: 2\nguid: ${hash(asset.file)}\nTextureImporter:\n  internalIDToNameTable: []\n  externalObjects: {}\n  serializedVersion: 13\n  mipmaps:\n    mipMapMode: 0\n    enableMipMap: 1\n    sRGBTexture: 1\n    linearTexture: 0\n    fadeOut: 0\n    borderMipMap: 0\n    mipMapsPreserveCoverage: 0\n    alphaTestReferenceValue: 0.5\n    mipMapFadeDistanceStart: 1\n    mipMapFadeDistanceEnd: 3\n  bumpmap:\n    convertToNormalMap: 0\n    externalNormalMap: 0\n    heightScale: 0.25\n    normalMapFilter: 0\n    flipGreenChannel: 0\n  isReadable: 0\n  streamingMipmaps: 0\n  streamingMipmapsPriority: 0\n  vTOnly: 0\n  ignoreMipmapLimit: 0\n  grayScaleToAlpha: 0\n  generateCubemap: 6\n  cubemapConvolution: 0\n  seamlessCubemap: 0\n  textureFormat: 1\n  maxTextureSize: 2048\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 2\n    aniso: 1\n    mipBias: 0\n    wrapU: 1\n    wrapV: 1\n    wrapW: 1\n  nPOTScale: 0\n  lightmap: 0\n  compressionQuality: 100\n  spriteMode: 1\n  spriteExtrude: 1\n  spriteMeshType: 0\n  alignment: ${customPivot ? 9 : 0}\n  spritePivot: {x: ${asset.pivot[0]}, y: ${asset.pivot[1]}}\n  spritePixelsToUnits: ${asset.worldWidth ? Number((asset.width * SCALE / asset.worldWidth).toFixed(4)) : 100 * SCALE}\n  spriteBorder: {x: ${asset.border[0] * SCALE}, y: ${asset.border[1] * SCALE}, z: ${asset.border[2] * SCALE}, w: ${asset.border[3] * SCALE}}\n  spriteGenerateFallbackPhysicsShape: 0\n  alphaUsage: 1\n  alphaIsTransparency: 1\n  spriteTessellationDetail: -1\n  textureType: 8\n  textureShape: 1\n  singleChannelComponent: 0\n  flipbookRows: 1\n  flipbookColumns: 1\n  maxTextureSizeSet: 0\n  compressionQualitySet: 0\n  textureFormatSet: 0\n  ignorePngGamma: 0\n  applyGammaDecoding: 0\n  swizzle: 50462976\n  cookieLightType: 0\n  platformSettings:\n  - serializedVersion: 4\n    buildTarget: DefaultTexturePlatform\n    maxTextureSize: 2048\n    resizeAlgorithm: 0\n    textureFormat: -1\n    textureCompression: 0\n    compressionQuality: 100\n    crunchedCompression: 0\n    allowsAlphaSplitting: 0\n    overridden: 0\n    ignorePlatformSupport: 0\n    androidETC2FallbackOverride: 0\n    forceMaximumCompressionQuality_BC6H_BC7: 0\n  spriteSheet:\n    serializedVersion: 2\n    sprites: []\n    outline: []\n    customData:\n    physicsShape: []\n    bones: []\n    spriteID: ${hash(`sprite:${asset.name}`)}\n    internalID: 0\n    vertices: []\n    indices:\n    edges: []\n    weights: []\n    secondaryTextures: []\n    spriteCustomMetadata:\n      entries: []\n    nameFileIdTable: {}\n  mipmapLimitGroupName:\n  pSDRemoveMatte: 0\n  userData: Original SVG master in Assets/Art/Masters\n  assetBundleName:\n  assetBundleVariant:\n`;
}
function defaultMeta(name, folder = false) {
  return `fileFormatVersion: 2\nguid: ${hash(name)}\n${folder ? 'folderAsset: yes\n' : ''}DefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n`;
}
await writeIfMissing(`${artDir}.meta`, defaultMeta('Assets/Art', true));
await writeIfMissing(`${masterDir}.meta`, defaultMeta('Assets/Art/Masters', true));
await writeIfMissing(`${resourcesArtDir}.meta`, defaultMeta('Assets/Resources/Art', true));
await writeIfMissing(`${spriteDir}.meta`, defaultMeta('Assets/Resources/Art/Sprites', true));
for (const asset of assets) {
  await writeFile(path.join(root, asset.source), asset.svg);
  await writeIfMissing(path.join(root, `${asset.source}.meta`), defaultMeta(asset.source));
  await writeFile(path.join(root, `${asset.file}.meta`), textureMeta(asset));
}
const manifest = {
  schemaVersion: 1,
  style: 'Original inked elemental medallions and chamfered arcade-industrial hardware',
  renderScale: SCALE,
  pixelsPerUnit: 100 * SCALE,
  notes: [
    'All sprites are standalone PNGs in Assets/Resources/Art/Sprites, loaded at runtime with Resources.Load<Sprite>("Art/Sprites/<name>").',
    'All masters are self-contained SVG with editable paths and gradients.',
    'Only background and board_backdrop are intentionally opaque.',
    'UI faces and central rank plaque are blank; all typography belongs in Unity.',
    'hud_panel borders are 32 px; board_frame borders are 40 px (left,bottom,right,top).',
    'turret_barrel points up and has a bottom-center pivot.',
    'PNG and master GUIDs are preserved on rebuild; new assets use deterministic GUIDs.'
  ],
  assets: assets.map(({ svg: source, ...entry }) => entry)
};
await writeFile(path.join(artDir, 'asset-manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`);
console.log(`Authored ${assets.length} SVG masters, PNG import settings, and Assets/Art/asset-manifest.json.`);
