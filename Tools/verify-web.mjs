import { readFile, readdir, stat, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { brotliDecompressSync } from 'node:zlib';
import vm from 'node:vm';

const project = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const root = join(project, 'Builds', 'WebGL');
const html = await readFile(join(root, 'index.html'), 'utf8');
if (html.includes('{{{')) throw new Error('Unprocessed Unity template variables.');
const references = [...new Set(html.match(/Build\/[a-zA-Z0-9_.-]+/g))];
if (references.length < 4) throw new Error('Missing Unity bundle references.');
for (const file of references) await stat(join(root, file));
const inlineScripts = [...html.matchAll(/<script>([\s\S]*?)<\/script>/g)];
for (const [, source] of inlineScripts) new vm.Script(source);
const files = [];
async function walk(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const file = join(directory, entry.name);
    if (entry.isDirectory()) await walk(file);
    else files.push({ file, bytes: (await stat(file)).size });
  }
}
await walk(root);
if (files.some(file => file.bytes > 25 * 1024 * 1024)) throw new Error('An asset exceeds the Cloudflare Pages file limit.');
const wasmFile = files.find(file => file.file.endsWith('.wasm.br'));
if (!wasmFile) throw new Error('Missing WebAssembly bundle.');
const wasm = brotliDecompressSync(await readFile(wasmFile.file));
if (!WebAssembly.validate(wasm)) throw new Error('WebAssembly binary did not validate.');
for (const bundle of files.filter(file => file.file.endsWith('.js.br'))) {
  new vm.Script(brotliDecompressSync(await readFile(bundle.file)).toString('utf8'));
}
const headers = await readFile(join(root, '_headers'), 'utf8');
if (!headers.includes('Content-Type: application/wasm') || !headers.includes('Content-Encoding: br')) throw new Error('Missing compressed WebAssembly response headers.');
const result = {
  templateResolved: true, bundleReferencesExist: true, javaScriptParses: true,
  wasmValid: true, cloudflareFileSizesPass: true, compressionHeadersPresent: true,
  files: files.length, totalBytes: files.reduce((sum, file) => sum + file.bytes, 0),
  largestAssetBytes: Math.max(...files.map(file => file.bytes)), wasmBytes: wasm.length
};
await writeFile(join(project, 'Artifacts', 'web-export-checks.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify(result));
