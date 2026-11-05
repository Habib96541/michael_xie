// Download complete static parts before handing the data archive to Unity.
// This avoids relying on a single long response from a Pages Function.
(async function () {
  'use strict';
  const config=window.littleDepthsWebConfig;
  const manifest=window.littleDepthsDataManifest;
  const canvas=document.getElementById('unity-canvas');
  const progress=document.getElementById('progress');
  const status=document.getElementById('load-status');
  document.getElementById('retry').onclick=()=>location.reload();
  let objectUrl;
  const hex=buffer=>Array.from(new Uint8Array(buffer),value=>value.toString(16).padStart(2,'0')).join('');
  async function downloadPart(part,index) {
    let failure;
    for(let attempt=0;attempt<3;attempt++){
      try{
        const url=new URL(part.url,location.href);
        url.searchParams.set('v',manifest.revision);
        if(attempt)url.searchParams.set('retry',String(attempt));
        const response=await fetch(url,{cache:attempt?'reload':'default'});
        if(!response.ok)throw new Error('Game part '+(index+1)+' returned HTTP '+response.status);
        const bytes=await response.arrayBuffer();
        if(bytes.byteLength!==part.bytes)throw new Error('Incomplete game part '+(index+1));
        if(hex(await crypto.subtle.digest('SHA-256',bytes))!==part.sha256)throw new Error('Game part '+(index+1)+' failed its integrity check');
        return bytes;
      }catch(error){failure=error;status.textContent='Retrying game download…';}
    }
    throw failure;
  }
  try{
    const parts=[];
    for(let i=0;i<manifest.parts.length;i++){
      status.textContent='Downloading game '+(i+1)+' / '+manifest.parts.length;
      parts.push(await downloadPart(manifest.parts[i],i));
      progress.value=.65*(i+1)/manifest.parts.length;
    }
    const compressedBytes=parts.reduce((size,part)=>size+part.byteLength,0);
    if(compressedBytes!==manifest.compressedBytes)throw new Error('Incomplete game archive');
    status.textContent='Preparing game…';
    let data;
    if(typeof DecompressionStream==='function'){
      // Feed verified bytes directly without reading a large temporary Blob.
      const input=new ReadableStream({pull(controller){
        if(parts.length)controller.enqueue(new Uint8Array(parts.shift()));
        else controller.close();
      }});
      data=await new Response(input.pipeThrough(new DecompressionStream('gzip'))).blob();
      if(data.size!==manifest.decodedBytes)throw new Error('The game archive did not decompress completely');
    }else data=new Blob(parts,{type:'application/octet-stream'});
    objectUrl=URL.createObjectURL(data);
    config.dataUrl=objectUrl;
    // The verified local blob must never reuse an earlier incomplete cache entry.
    config.cacheControl=()=> 'no-store';
    config.devicePixelRatio=Math.min(window.devicePixelRatio||1,2);
    status.textContent='Starting game…';
    window.littleDepthsInstance=await createUnityInstance(canvas,config,value=>{progress.value=.65+.35*value});
    document.getElementById('loading').remove();canvas.focus();
  }catch(error){
    const message=String(error?.message||error||'Unknown startup error');
    document.getElementById('error').textContent=/memory|out of bounds|allocation/i.test(message)?'The browser could not allocate enough memory. Close other games and retry.':/webgl|graphics|gpu|context/i.test(message)?'The browser could not start the game graphics. Please retry.':/download|fetch|network|game part|archive/i.test(message)?'A game file did not finish downloading. Please retry.':'The game could not start. Please retry.';
    document.getElementById('retry').hidden=false;document.getElementById('error-details').hidden=false;
    document.getElementById('error-message').textContent=message;console.error(error);
  }finally{if(objectUrl)URL.revokeObjectURL(objectUrl)}
})();
