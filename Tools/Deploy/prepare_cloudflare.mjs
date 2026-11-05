import { readdirSync, readFileSync, writeFileSync, statSync, mkdirSync, existsSync, renameSync, openSync, readSync, closeSync, copyFileSync, createReadStream } from 'node:fs';
import { createHash } from 'node:crypto';
import { createGunzip } from 'node:zlib';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const project = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const output = path.resolve(process.argv[2] || path.join(project, 'Logs/CloudflareWebGL'));
if(!output.startsWith(path.join(project,'Logs')+path.sep))throw new Error('Only package a disposable export under the project Logs directory.');
const build = path.join(output, 'Build');
const names = readdirSync(build);
const routeManifest=path.join(project,'Logs/CloudflareStreamedAssets.json');
const routes=existsSync(routeManifest)?JSON.parse(readFileSync(routeManifest,'utf8')):{};
const find = (pattern) => {
  let matches = names.filter(name => pattern.test(name));
  if(!matches.length)matches=Object.keys(routes).map(value=>path.posix.basename(value)).filter(name=>pattern.test(name));
  if (matches.length !== 1) throw new Error(`Expected one ${pattern}; got ${matches}`);
  return `Build/${matches[0]}`;
};
const config = {
  dataUrl: find(/\.data\.unityweb$/),
  frameworkUrl: find(/\.framework\.js\.unityweb$/),
  codeUrl: find(/\.wasm\.unityweb$/),
  streamingAssetsUrl: 'StreamingAssets',
  companyName: 'Quiet Lake Studio',
  productName: 'Little Depths',
  productVersion: '1.1',
  matchWebGLToCanvasSize: true,
};
const deliveryRevision='landscape-controls-4';
for(const key of ['dataUrl','frameworkUrl','codeUrl'])config[key]+='?v='+deliveryRevision;
const loader = find(/\.loader\.js$/);
function writePlayerPage(dataManifest){
writeFileSync(path.join(output, 'index.html'), `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"><title>Little Depths</title>
<meta name="description" content="Cast your line, discover fish, and explore the lake in Little Depths.">
<meta name="theme-color" content="#071f47">
<link rel="manifest" href="game.webmanifest"><link rel="icon" type="image/png" href="app-icon.png"><link rel="apple-touch-icon" href="app-icon.png">
<style>
html,body{margin:0;width:100%;height:100%;overflow:hidden;overscroll-behavior:none;background:#071f47}
#game{position:fixed;inset:0;width:100%;height:100%;height:100dvh;box-sizing:border-box;background:#071f47;padding:env(safe-area-inset-top) env(safe-area-inset-right) env(safe-area-inset-bottom) env(safe-area-inset-left)}
#game:fullscreen{width:100%;height:100%}
#game:-webkit-full-screen{width:100%;height:100%}
canvas{width:100%;height:100%;display:block;touch-action:none;outline:none}
#loading,#rotate{position:absolute;inset:0;display:grid;place-content:center;justify-items:center;gap:16px;background:#071f47;color:white;text-align:center;font:700 20px system-ui,sans-serif}
#rotate,#loading-rotate{display:none}
#rotate strong{font-size:clamp(26px,7vw,38px);color:#ffe26c}
#rotate p,#loading-rotate{margin:0;max-width:32ch;padding:0 16px;font:500 17px/1.5 system-ui,sans-serif;color:#b9ddff}
@media (orientation:portrait){
  canvas{visibility:hidden}
  #rotate{display:grid}
  #loading-rotate{display:block}
}
progress{width:min(70vw,340px);height:14px;accent-color:#16d6ef}
#error{max-width:70vw;font-size:16px;line-height:1.5}
button{min-height:48px;border:0;border-radius:12px;background:#17d5ed;color:#071f47;padding:12px 28px;font:700 17px system-ui;cursor:pointer;touch-action:manipulation}
#fullscreen{position:absolute;z-index:1;top:calc(8px + env(safe-area-inset-top));right:calc(8px + env(safe-area-inset-right));display:grid;place-items:center;width:48px;height:48px;padding:0;border:2px solid #4eeaff;background:#07599a;color:white;box-shadow:0 2px 4px #071f4766}
#fullscreen:active{background:#064277}
#fullscreen svg{width:24px;height:24px;pointer-events:none}
#fullscreen[aria-pressed="false"] .exit,#fullscreen[aria-pressed="true"] .enter{display:none}
button:focus-visible{outline:3px solid #ffe26c;outline-offset:2px}
details{max-width:80vw;font:14px system-ui}
pre{white-space:pre-wrap;overflow-wrap:anywhere;text-align:left;max-height:30vh;overflow:auto}
[hidden]{display:none!important}
</style></head>
<body><main id="game" aria-label="Little Depths">
<canvas id="unity-canvas" tabindex="0" aria-label="Little Depths fishing game"></canvas>
<section id="rotate" aria-label="Landscape mode">
  <strong>Turn your device</strong><p>Rotate to landscape to play Little Depths.</p>
</section>
<div id="loading"><div>Little Depths</div><div id="load-status" role="status">Downloading game…</div><progress id="progress" value="0" max="1"></progress><p id="loading-rotate">Rotate to landscape to play.</p><div id="error" role="alert"></div><button id="retry" hidden>Retry loading</button><details id="error-details" hidden><summary>Loading details</summary><pre id="error-message"></pre></details></div>
<button id="fullscreen" type="button" aria-label="Fullscreen" aria-pressed="false" title="Enter fullscreen" hidden>
  <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
    <path class="enter" d="M9 4H4v5m11-5h5v5M4 15v5h5m6 0h5v-5"/>
    <path class="exit" d="M4 9h5V4m11 5h-5V4M9 20v-5H4m11 5v-5h5"/>
  </svg>
</button>
</main>
<script src="display.${deliveryRevision}.js"></script>
<script src=${JSON.stringify(loader)}></script><script>
window.littleDepthsWebConfig=${JSON.stringify(config)};
window.littleDepthsDataManifest=${JSON.stringify(dataManifest)};
</script><script src="startup.${deliveryRevision}.js"></script></body></html>`);
copyFileSync(path.join(project,'Tools/Deploy/web-startup.js'),path.join(output,'startup.'+deliveryRevision+'.js'));
copyFileSync(path.join(project,'Tools/Deploy/web-display.js'),path.join(output,'display.'+deliveryRevision+'.js'));
const icon=readFileSync(path.join(project,'Assets/LittleDepths/Art/Provided/ref_play.png'));
writeFileSync(path.join(output,'app-icon.png'),icon);
writeFileSync(path.join(output,'game.webmanifest'),JSON.stringify({
  name:'Little Depths',short_name:'Little Depths',start_url:'./',display:'standalone',orientation:'landscape',
  theme_color:'#071f47',background_color:'#071f47',
  icons:[{src:'app-icon.png',sizes:icon.readUInt32BE(16)+'x'+icon.readUInt32BE(20),type:'image/png'}]
}));
}
writeFileSync(path.join(output, '_headers'), `/*
  X-Content-Type-Options: nosniff
  Referrer-Policy: strict-origin-when-cross-origin
/
  Cache-Control: no-store
/index.html
  Cache-Control: no-store
/Build/*
  Cache-Control: public, max-age=31536000, immutable
/Build/*.framework.js.unityweb
  Content-Type: application/javascript
/Build/*.wasm.unityweb
  Content-Type: application/wasm
`);
// A literal 404 page prevents Pages' SPA fallback from returning HTML as WASM.
writeFileSync(path.join(output, '404.html'), '<!doctype html><title>Not found</title><p>Not found</p>');
const archive=path.join(project,'Logs/CloudflareLargeAssets');
for(const name of names){
  const full=path.join(build,name);
  if(!statSync(full).isFile()||statSync(full).size<=25*1024*1024)continue;
  mkdirSync(archive,{recursive:true});
  const original=path.join(archive,existsSync(path.join(archive,name))?`${Date.now()}-${name}`:name);
  const size=statSync(full).size;
  renameSync(full,original);
  const descriptor=openSync(original,'r');
  const buffer=Buffer.alloc(16*1024*1024);
  const parts=[];
  try{
    for(let part=0,offset=0;offset<size;part++){
      const bytes=readSync(descriptor,buffer,0,Math.min(buffer.length,size-offset),offset);
      if(!bytes)throw new Error('Unexpected end of large asset');
      const chunkName=`${name}.part-${String(part).padStart(3,'0')}`;
      writeFileSync(path.join(build,chunkName),buffer.subarray(0,bytes));
      parts.push('/Build/'+chunkName);offset+=bytes;
    }
  }finally{closeSync(descriptor)}
  routes['/Build/'+name]={size,parts,original};
}
// Keep only this build's game data so a repeated packaging pass cannot resolve
// an archived previous build when the original large file has already split.
for(const route of Object.keys(routes))if(route!=='/'+config.dataUrl.split('?')[0])delete routes[route];
writeFileSync(routeManifest,JSON.stringify(routes,null,2));
const dataAsset=routes['/'+config.dataUrl.split('?')[0]];
if(!dataAsset)throw new Error('Missing game-data parts');
let decodedBytes=0;
for await(const chunk of createReadStream(dataAsset.original).pipe(createGunzip()))decodedBytes+=chunk.length;
const dataManifest={revision:deliveryRevision,compressedBytes:dataAsset.size,decodedBytes,parts:dataAsset.parts.map(url=>{
 const full=path.join(output,url.slice(1));return{url,bytes:statSync(full).size,sha256:createHash('sha256').update(readFileSync(full)).digest('hex')};
})};
writePlayerPage(dataManifest);
if(Object.keys(routes).length){
  // Game data uses verified static parts in the browser. Only the smaller
  // executable files need a Worker to set manual native-gzip response headers.
  const publicRoutes={};
  for(const name of names.filter(name=>/\.(wasm|framework\.js)\.unityweb$/.test(name))){
    publicRoutes['/Build/'+name]={size:statSync(path.join(build,name)).size,static:'/Build/'+name,mime:name.includes('.wasm.')?'application/wasm':'application/javascript'};
  }
  writeFileSync(path.join(output,'_worker.js'),`const routes=${JSON.stringify(publicRoutes)};
export default {async fetch(request,env){
 const url=new URL(request.url),asset=routes[url.pathname];
 if(!asset)return env.ASSETS.fetch(request);
 if(request.method!=="GET"&&request.method!=="HEAD")return new Response(null,{status:405});
 const headers={"Content-Type":asset.mime,"Content-Encoding":"gzip","Content-Length":String(asset.size),"Cache-Control":"public, max-age=31536000, immutable, no-transform","X-Content-Type-Options":"nosniff"};
 if(request.method==="HEAD")return new Response(null,{headers,encodeBody:"manual"});
 const response=await env.ASSETS.fetch(new URL(asset.static,url));
 if(!response.ok)return response;
 return new Response(response.body,{headers,encodeBody:"manual"});
}};`);
  writeFileSync(path.join(output,'_routes.json'),JSON.stringify({version:1,include:Object.keys(publicRoutes),exclude:[]},null,2));
}
const files=[];
function visit(directory){for(const name of readdirSync(directory)){const full=path.join(directory,name);if(statSync(full).isDirectory())visit(full);else files.push({path:path.relative(output,full).replaceAll('\\','/'),bytes:statSync(full).size})}}
visit(output);
const oversized=files.filter(file=>file.bytes>25*1024*1024);
if(oversized.length)throw new Error('Cloudflare Pages 25 MiB asset limit: '+JSON.stringify(oversized));
const report={preparedAt:new Date().toISOString(),output,deliveryRevision,streamedAssets:routes,files,totalBytes:files.reduce((total,file)=>total+file.bytes,0)};
writeFileSync(path.join(project,'Logs/CloudflarePackage.json'),JSON.stringify(report,null,2));
console.log(JSON.stringify(report,null,2));
