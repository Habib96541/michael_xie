const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const root = path.resolve(__dirname, '..');
const html = fs.readFileSync(path.join(root, 'index.html'), 'utf8').replaceAll('\r', '');
const source = html.match(/<script>([\s\S]*?)<\/script>/)?.[1];
if (!source) throw new Error('Bull Ride script was not found in index.html.');

function writeArt(bull, trees, boxes) {
  const chunks = [Buffer.from('RDO1')];
  function integer(value) {
    const bytes = Buffer.alloc(4);
    bytes.writeInt32LE(value);
    chunks.push(bytes);
  }
  function floats(values) {
    const bytes = Buffer.alloc(values.length * 4);
    values.forEach((value, i) => bytes.writeFloatLE(value, i * 4));
    chunks.push(bytes);
  }

  const skin = Buffer.from(bull, 'base64');
  integer(skin.length);
  chunks.push(skin);
  for (const tree of trees) {
    integer(tree.length);
    for (const node of tree) {
      const name = Buffer.from(node.name);
      integer(name.length);
      chunks.push(name);
      integer(node.parent);
      floats(node.p);
      floats(node.e);
      integer(node.meshes.length);
      for (const { mesh, mat } of node.meshes) {
        const vertexCount = mesh.pos.length / 3;
        if (vertexCount > 65535) throw new Error(node.name + ' exceeds 16-bit mesh indices.');
        integer(mat);
        integer(vertexCount);
        integer(mesh.idx.length);
        floats(mesh.pos);
        floats(mesh.nor);
        floats(mesh.col);
        const indices = Buffer.alloc(mesh.idx.length * 2);
        mesh.idx.forEach((value, i) => indices.writeUInt16LE(value, i * 2));
        chunks.push(indices);
      }
    }
  }

  for (const group of boxes) {
    integer(group.length);
    for (const box of group) {
      floats(box.center);
      floats(box.half.map(extent => extent * 2));
    }
  }
  fs.writeFileSync(path.join(root, 'Assets/Resources/BullRideArt.bytes'), Buffer.concat(chunks));
  console.log('Exported %d arena nodes and %d rider nodes.', trees[0].length, trees[1].length);
}

// Run startup so Unity can use the same mesh builders as the standalone page.
const gl = new Proxy({}, { get: (_, name) => name === name.toUpperCase() ? 0 : () => true });
const element = {
  style: {}, value: '',
  addEventListener() {}, removeAttribute() {}, getContext: () => gl
};
const context = {
  document: { getElementById: () => element, querySelectorAll: () => [], addEventListener() {} },
  window: { addEventListener() {} },
  localStorage: { getItem: () => null },
  requestAnimationFrame() {},
  atob,
  writeArt
};

const hook = `
  const names = new Map([[arena, "Arena"], [gate, "Gate"], [rider, "Rider"]]);
  for (const [key, node] of Object.entries(R)) {
    if (node instanceof Node && key !== "root") names.set(node, key);
  }
  for (const key of ["armR", "armL", "legR", "legL"]) {
    for (const part of ["upper", "lower", "hand"]) {
      if (R[key][part]) names.set(R[key][part], key + "_" + part);
    }
  }
  function exportTree(root) {
    const tree = [];
    function walk(node, parent) {
      const index = tree.length;
      tree.push({ name: names.get(node) || node.name || "Part", parent, p: node.p, e: node.e, meshes: node.draws });
      for (const child of node.children) walk(child, index);
    }
    walk(root, -1);
    return tree;
  }
  writeArt(BULL_B64, [exportTree(arena), exportTree(rider)], [arenaColliders, gateColliders]);
`;
const startup = '  resetToMenu();\n  let previousTime';
if (!source.includes(startup)) throw new Error('Bull Ride startup was not found.');
vm.runInNewContext(source
  // Correct the direction of the letters for Unity's left-handed camera.
  .replace('buildArena(mirrorLetters = false)', 'buildArena(mirrorLetters = true)')
  .replace('  function gpu(mesh) {', '  function gpu(mesh) { return mesh; }\n  function uploadMesh(mesh) {')
  .replace(startup, hook + '\n  let previousTime'), context);
