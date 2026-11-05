import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import sharp from 'sharp';

const toolDir = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(toolDir, '../..');
const manifest = JSON.parse(await readFile(path.join(root, 'Assets/Art/asset-manifest.json'), 'utf8'));
const scale = manifest.renderScale;

let count = 0;
for (const asset of manifest.assets) {
  const svg = await readFile(path.join(root, asset.source));
  const density = 72 * scale;
  let image = sharp(svg, { density }).resize(asset.width * scale, asset.height * scale);
  if (asset.opaque) image = image.flatten({ background: '#000000' });
  await image.png({ compressionLevel: 9 }).toFile(path.join(root, asset.file));
  count++;
}
console.log(`Rendered ${count} PNG sprites at ${scale}x.`);
