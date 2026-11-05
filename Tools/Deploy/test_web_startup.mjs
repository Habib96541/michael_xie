import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { createHash, webcrypto } from 'node:crypto';
import { gzipSync, gunzipSync } from 'node:zlib';
import vm from 'node:vm';

const source=readFileSync(new URL('./web-startup.js',import.meta.url),'utf8');
const payload=Buffer.alloc(16384);
for(let i=0;i<payload.length;i++)payload[i]=(i*73+Math.floor(i/17))%256;
const archive=gzipSync(payload);
const split=Math.floor(archive.length/2);
const chunks=[archive.subarray(0,split),archive.subarray(split)];
const digest=bytes=>createHash('sha256').update(bytes).digest('hex');
async function check(mode,nativeDecoder=true){
  const attempts=[0,0];let started=0;
  const elements=new Map();
  const element=id=>{
    if(!elements.has(id))elements.set(id,{hidden:true,textContent:'',value:0,remove(){this.removed=true},focus(){this.focused=true}});
    return elements.get(id);
  };
  const manifest={revision:'test',compressedBytes:archive.length,decodedBytes:payload.length,parts:chunks.map((bytes,index)=>({url:'/Build/part-'+index,bytes:bytes.length,sha256:digest(bytes)}))};
  const context={
    window:{littleDepthsWebConfig:{},littleDepthsDataManifest:manifest,devicePixelRatio:1},
    document:{getElementById:element},location:{href:'https://game.example/',reload(){}},
    Blob,Response,ReadableStream,URL,crypto:webcrypto,Uint8Array,console:{error(){}},
    DecompressionStream:nativeDecoder?DecompressionStream:undefined,
    async fetch(input){
      const url=new URL(input);const index=Number(url.pathname.slice('/Build/part-'.length));
      assert.ok(index===0||index===1,'Only known static parts are requested');
      attempts[index]++;
      let bytes=Buffer.from(chunks[index]);
      if(index===0&&(mode==='permanently-truncated'||(mode==='truncated-once'&&attempts[index]===1)))bytes=bytes.subarray(0,bytes.length-1);
      if(index===0&&mode==='corrupted-once'&&attempts[index]===1)bytes[5]^=1;
      return new Response(bytes);
    },
    async createUnityInstance(canvas,config,onProgress){
      started++;
      const response=await fetch(config.dataUrl);
      let bytes=Buffer.from(await response.arrayBuffer());
      if(!nativeDecoder)bytes=gunzipSync(bytes);
      assert.deepEqual(bytes,payload,'Unity receives the complete original decoded data');
      assert.equal(config.cacheControl(),'no-store','An incomplete cached response cannot replace verified data');
      onProgress(1);return{};
    },
  };
  await vm.runInNewContext(source,context,{filename:'web-startup.js'});
  if(mode==='permanently-truncated'){
    assert.equal(started,0,'Unity must not start with truncated data');
    assert.equal(attempts[0],3,'Retry count is bounded');
    assert.equal(element('retry').hidden,false,'A retry control is provided');
  }else{
    assert.equal(started,1);assert.equal(element('loading').removed,true);
    assert.equal(attempts[0],mode==='complete'?1:2);
  }
  console.log('PASS: '+mode+(nativeDecoder?' with native decompression':' with Unity fallback'));
}
await check('complete');
await check('truncated-once');
await check('corrupted-once');
await check('permanently-truncated');
await check('complete',false);
