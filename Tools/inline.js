// Packs the Unity WebGL build into a single self-contained .html file.
// The build must be uncompressed so the payloads can be inlined as base64.
// Usage: node Tools/inline.js [buildDir] [outFile]

const fs = require('fs');
const path = require('path');

const buildRoot = path.resolve(process.argv[2] || 'Build/WebGL');
const outFile = path.resolve(process.argv[3] || 'CrashGame.html');
const buildDir = path.join(buildRoot, 'Build');

if (!fs.existsSync(buildDir)) {
  console.error('No Build/ folder inside ' + buildRoot);
  process.exit(1);
}

const files = fs.readdirSync(buildDir);
const pick = ext => files.find(f => f.endsWith(ext));

const names = {
  loader: pick('.loader.js'),
  data: pick('.data'),
  framework: pick('.framework.js'),
  wasm: pick('.wasm'),
};

for (const [k, v] of Object.entries(names)) {
  if (!v) {
    console.error('Missing ' + k + ' in ' + buildDir + ' (files: ' + files.join(', ') + ')');
    process.exit(1);
  }
}

const b64 = f => fs.readFileSync(path.join(buildDir, f)).toString('base64');
const payload = {
  loader: b64(names.loader),
  data: b64(names.data),
  framework: b64(names.framework),
  wasm: b64(names.wasm),
};

const sizes = Object.entries(payload)
  .map(([k, v]) => k + '=' + (v.length / 1048576).toFixed(1) + 'MB')
  .join(' ');

const html = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8">
<meta name="viewport" content="width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes">
<meta name="theme-color" content="#04040C">
<title>CRASH — cash out before the crash</title>
<style>
  html, body { margin:0; padding:0; height:100%; width:100%; background:#04040C; overflow:hidden;
    font-family:"Segoe UI", Inter, system-ui, -apple-system, "Helvetica Neue", Arial, sans-serif;
    -webkit-tap-highlight-color:transparent; touch-action:manipulation; }
  #stage { position:fixed; inset:0; background:radial-gradient(circle at 50% 42%, #17123a 0%, #0a0820 45%, #04040c 100%); }
  #unity-canvas { width:100%; height:100%; display:block; outline:none; }
  #loader { position:fixed; inset:0; display:flex; flex-direction:column; align-items:center; justify-content:center;
    gap:18px; background:radial-gradient(circle at 50% 45%, #1b1246 0%, #0a0820 48%, #04040c 100%);
    transition:opacity .55s ease; z-index:10; }
  #loader.hide { opacity:0; pointer-events:none; }
  .logo { font-size:clamp(42px,9vw,92px); font-weight:800; letter-spacing:.16em; color:#f5f7ff;
    text-shadow:0 0 34px rgba(57,235,210,.55), 0 0 90px rgba(142,99,255,.45); animation:breathe 2.6s ease-in-out infinite; }
  .logo em { font-style:normal; background:linear-gradient(100deg,#39ebd2,#8e63ff 55%,#ff4fd8);
    -webkit-background-clip:text; background-clip:text; color:transparent; }
  .sub { color:#8f9ac4; font-size:clamp(11px,1.6vw,15px); letter-spacing:.34em; text-transform:uppercase; margin-top:-6px; }
  .bar { width:min(320px,62vw); height:6px; border-radius:99px; background:rgba(255,255,255,.09); overflow:hidden; }
  .fill { height:100%; width:0%; border-radius:99px; background:linear-gradient(90deg,#39ebd2,#8e63ff 60%,#ff4fd8);
    box-shadow:0 0 18px rgba(57,235,210,.65); transition:width .22s ease; }
  .pct { color:#6f7a9e; font-size:12px; letter-spacing:.22em; font-variant-numeric:tabular-nums; }
  .err { display:none; max-width:min(560px,84vw); text-align:center; color:#ffb4c1; font-size:13px; line-height:1.6;
    background:rgba(255,77,109,.08); border:1px solid rgba(255,77,109,.35); border-radius:14px; padding:16px 18px; }
  @keyframes breathe { 0%,100%{transform:translateY(0) scale(1);opacity:.96} 50%{transform:translateY(-4px) scale(1.015);opacity:1} }
</style>
</head>
<body>
<div id="stage"><canvas id="unity-canvas" tabindex="-1" width=1280 height=720></canvas></div>
<div id="loader">
  <div class="logo">CR<em>A</em>SH</div>
  <div class="sub">cash out before the crash</div>
  <div class="bar"><div class="fill" id="fill"></div></div>
  <div class="pct" id="pct">LOADING 0%</div>
  <div class="err" id="err"></div>
</div>
<script>
var B64 = {
  loader: "${payload.loader}",
  data: "${payload.data}",
  framework: "${payload.framework}",
  wasm: "${payload.wasm}"
};
(function () {
  var fill = document.getElementById('fill');
  var pct = document.getElementById('pct');
  var err = document.getElementById('err');
  function onProgress(p) {
    if (typeof p === 'number') {
      var v = Math.round(p * 100);
      if (fill) fill.style.width = v + '%';
      if (pct) pct.textContent = 'LOADING ' + v + '%';
    }
  }
  function fail(msg) {
    if (pct) pct.textContent = 'ERROR';
    if (err) { err.style.display = 'block'; err.textContent = 'The game could not start. ' + (msg || ''); }
    console.error(msg);
  }
  function toBlob(b64, type) {
    var bin = atob(b64), n = bin.length, bytes = new Uint8Array(n);
    for (var i = 0; i < n; i++) bytes[i] = bin.charCodeAt(i);
    return new Blob([bytes], { type: type });
  }
  window.addEventListener('error', function (e) { fail(e.message); });

  var loaderUrl = URL.createObjectURL(toBlob(B64.loader, 'text/javascript'));
  var s = document.createElement('script');
  s.src = loaderUrl;
  s.onerror = function () { fail('loader script failed'); };
  s.onload = function () {
    if (typeof createUnityInstance !== 'function') { fail('createUnityInstance missing'); return; }
    createUnityInstance(document.querySelector('#unity-canvas'), {
      arguments: [],
      dataUrl: URL.createObjectURL(toBlob(B64.data, 'application/octet-stream')),
      frameworkUrl: URL.createObjectURL(toBlob(B64.framework, 'text/javascript')),
      codeUrl: URL.createObjectURL(toBlob(B64.wasm, 'application/wasm')),
      streamingAssetsUrl: 'StreamingAssets',
      companyName: 'Crash Games',
      productName: 'Crash',
      productVersion: '1.0.0',
      devicePixelRatio: Math.min(window.devicePixelRatio || 1, 2)
    }, onProgress).then(function (instance) {
      window.__unity = instance;
      onProgress(1);
      window.__crashReady = true;
      var loader = document.getElementById('loader');
      if (loader) {
        loader.classList.add('hide');
        setTimeout(function () { if (loader.parentNode) loader.parentNode.removeChild(loader); }, 620);
      }
      try { document.querySelector('#unity-canvas').focus(); } catch (e) {}
    }).catch(function (m) { fail(m); });
  };
  document.head.appendChild(s);
})();
</script>
</body>
</html>
`;

fs.writeFileSync(outFile, html);
console.log('Wrote ' + outFile + ' (' + (html.length / 1048576).toFixed(1) + ' MB)  [' + sizes + ']');
