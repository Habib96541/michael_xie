// Local QA only: run the shipped Unity page with an iPhone browser identifier.
// No emulation flag or browser override is included in the deployed game.
import http from 'node:http';
import {readFile} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=fileURLToPath(new URL('../Builds/WebGL/',import.meta.url));
const types={'.html':'text/html','.js':'application/javascript','.css':'text/css','.png':'image/png','.ttf':'font/ttf','.webmanifest':'application/manifest+json'};
http.createServer(async(req,res)=>{
  try{
    const url=new URL(req.url,'http://localhost'),relative=decodeURIComponent(url.pathname).replace(/^\/+/,''),target=path.resolve(root,relative||'index.html');
    if(!target.startsWith(root)){res.writeHead(403).end();return;}
    let data=await readFile(target);
    if(!relative&&url.searchParams.has('iphone'))data=Buffer.from(data.toString().replace('<head>','<head><script>Object.defineProperty(navigator,"userAgent",{get:()=>"Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1"});Object.defineProperty(document,"fullscreenEnabled",{get:()=>false});Object.defineProperty(document,"webkitFullscreenEnabled",{get:()=>false});</script>'));
    res.writeHead(200,{'Content-Type':types[path.extname(target)]||'application/octet-stream','Cache-Control':'no-store','Cross-Origin-Opener-Policy':'same-origin','Cross-Origin-Embedder-Policy':'require-corp'}).end(data);
  }catch{res.writeHead(404).end();}
}).listen(8082,'127.0.0.1',()=>console.log('Client iPhone QA: http://127.0.0.1:8082/?iphone'));
