const routes={"/Build/ae988f8f1af503c3150d7ff24656166a.framework.js.unityweb":{"size":88032,"static":"/Build/ae988f8f1af503c3150d7ff24656166a.framework.js.unityweb","mime":"application/javascript"},"/Build/d041587d8ceab0393cc368dc99087d41.wasm.unityweb":{"size":11236746,"static":"/Build/d041587d8ceab0393cc368dc99087d41.wasm.unityweb","mime":"application/wasm"}};
export default {async fetch(request,env){
 const url=new URL(request.url),asset=routes[url.pathname];
 if(!asset)return env.ASSETS.fetch(request);
 if(request.method!=="GET"&&request.method!=="HEAD")return new Response(null,{status:405});
 const headers={"Content-Type":asset.mime,"Content-Encoding":"gzip","Content-Length":String(asset.size),"Cache-Control":"public, max-age=31536000, immutable, no-transform","X-Content-Type-Options":"nosniff"};
 if(request.method==="HEAD")return new Response(null,{headers,encodeBody:"manual"});
 const response=await env.ASSETS.fetch(new URL(asset.static,url));
 if(!response.ok)return response;
 return new Response(response.body,{headers,encodeBody:"manual"});
}};