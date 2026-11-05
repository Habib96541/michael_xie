import { createReadStream, readFileSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { createGunzip } from 'node:zlib';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const base=process.argv[2]||'http://127.0.0.1:8788';
const routes=JSON.parse(readFileSync(path.join(root,'Logs/CloudflareStreamedAssets.json'),'utf8'));
const packageInfo=JSON.parse(readFileSync(path.join(root,'Logs/CloudflarePackage.json'),'utf8'));
const revision=packageInfo.deliveryRevision;
const checks=[];
const require=(value,message)=>{if(!value)throw new Error(message)};
const home=await fetch(base);
require(home.ok,'Home page does not return success');
const html=await home.text();require(html.includes('littleDepthsDataManifest')&&html.includes('unity-canvas'),'Home page contains the verified Unity player');
checks.push('Unity player HTML: HTTP 200');
const manifestMatch=html.match(/window\.littleDepthsDataManifest=(\{[^\n]*\});/);
require(manifestMatch,'The player includes its data-part manifest');
const manifest=JSON.parse(manifestMatch[1]);
const asset=Object.values(routes).find(asset=>asset.size===manifest.compressedBytes&&asset.parts.length===manifest.parts.length&&asset.parts.every((url,index)=>url===manifest.parts[index].url));
require(asset,'Published data parts correspond to the local build');
{
  const original=createHash('sha256');
  let expectedSize=0;
  for await(const bytes of createReadStream(asset.original).pipe(createGunzip())){original.update(bytes);expectedSize+=bytes.length}
  require(expectedSize===manifest.decodedBytes,'Decoded game length matches the build');
  const expectedCompressed=createHash('sha256');for await(const bytes of createReadStream(asset.original))expectedCompressed.update(bytes);
  const downloaded=createHash('sha256');let size=0;
  for(const part of manifest.parts){
    require(part.url.startsWith('/Build/')&&!part.url.includes('..'),'Part URL stays in the game build');
    const response=await fetch(new URL(part.url+'?v='+revision,base));
    require(response.ok,'Static game part returned '+response.status);
    const bytes=Buffer.from(await response.arrayBuffer());
    require(bytes.length===part.bytes,'Static game part has its full expected length');
    require(createHash('sha256').update(bytes).digest('hex')===part.sha256,'Static game part passes its published checksum');
    downloaded.update(bytes);size+=bytes.length;
  }
  require(size===asset.size&&downloaded.digest('hex')===expectedCompressed.digest('hex'),'Assembled static game parts exactly match the original archive');
  checks.push(`${manifest.parts.length} static game parts: ${size} bytes, SHA256 verified against the build`);
}
const startup=await fetch(new URL('startup.'+revision+'.js',base));
require(startup.ok,'Verified startup script is available');
require(await startup.text()===readFileSync(path.join(root,'Tools/Deploy/web-startup.js'),'utf8'),'Published startup script matches the tested source');
checks.push('Verified startup script matches tested source');
const displayScript='display.'+revision+'.js';
require(html.includes(displayScript)&&html.includes('id="rotate"'),'Home page includes landscape and fullscreen controls');
const display=await fetch(new URL(displayScript,base));
require(display.ok&&await display.text()===readFileSync(path.join(root,'Tools/Deploy/web-display.js'),'utf8'),'Published display script matches the tested source');
checks.push('Landscape and fullscreen controls match tested source');
const output=packageInfo.output;
for(const file of packageInfo.files.filter(file=>/\.(wasm|framework\.js)\.unityweb$/.test(file.path))){
  const expected=createHash('sha256');for await(const bytes of createReadStream(path.join(output,file.path)).pipe(createGunzip()))expected.update(bytes);
  const response=await fetch(new URL(file.path+'?v='+revision,base));
  require(response.ok&&response.headers.get('content-encoding')==='gzip',file.path+' must use native gzip');
  const type=file.path.includes('.wasm.')?'application/wasm':'application/javascript';
  require(response.headers.get('content-type')?.split(';')[0]===type,file.path+' has the correct MIME type');
  const actual=createHash('sha256');for await(const bytes of response.body)actual.update(bytes);
  require(actual.digest('hex')===expected.digest('hex'),file.path+' decoded bytes match the build');
  checks.push(file.path+': native gzip, correct MIME type, SHA256 verified');
}
const missing=await fetch(new URL('/__missing_asset__.wasm',base));
require(missing.status===404,'Missing assets must return 404 instead of the game HTML');
checks.push('Missing asset: HTTP 404');
const report={url:base,verifiedAt:new Date().toISOString(),checks};
writeFileSync(path.join(root,'Logs',base.startsWith('http://127.')?'Cloudflare-Local-HTTP.json':'Cloudflare-Live-HTTP.json'),JSON.stringify(report,null,2));
console.log(JSON.stringify(report,null,2));
