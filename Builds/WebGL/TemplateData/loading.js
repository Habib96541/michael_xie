(() => {
  'use strict';
  const build=window.gardenBuild,canvas=document.getElementById('game'),loading=document.getElementById('loading');
  const meter=document.getElementById('loading-meter'),percentage=document.getElementById('percentage'),caption=document.getElementById('caption');
  const started=performance.now();let target=0,shown=0,engineReady=false,nativeReady=window.bloomDoomSceneReady===true,failed=false,leaving=false,lastPercent=-1,watchdog=0;
  const phrases=['Putting down roots…','Gathering your garden…','Waking the little heroes…','Opening the garden gate…'];let phrase=0;
  function setPhase(index){if(index!==phrase){phrase=index;caption.textContent=phrases[index];}}
  function fail(message){if(!loading.isConnected){console.error(message);return;}if(failed)return;failed=true;clearTimeout(watchdog);caption.textContent='The garden needs another moment.';document.getElementById('failure').textContent=message;document.getElementById('failure').hidden=false;document.getElementById('retry').hidden=false;meter.setAttribute('aria-label','Loading interrupted');}
  document.getElementById('retry').addEventListener('click',()=>location.reload());
  function watchStartup(){clearTimeout(watchdog);if(!engineReady||nativeReady||failed||document.hidden)return;watchdog=setTimeout(()=>{if(!document.hidden&&!nativeReady&&!leaving)fail('The game is taking longer than expected to start. Please try again.');},45000);}
  document.addEventListener('visibilitychange',watchStartup);
  function ready(){nativeReady=true;target=100;setPhase(3);clearTimeout(watchdog);}
  window.addEventListener('bloomdoom-ready',ready,{once:true});
  function frame(now){
    shown+=(target-shown)*.13;if(target===100&&100-shown<.15)shown=100;
    const value=Math.floor(shown);meter.style.setProperty('--progress',shown.toFixed(2)+'%');
    if(value!==lastPercent){lastPercent=value;percentage.textContent=value+'%';meter.setAttribute('aria-valuenow',String(value));}
    if(!failed&&!leaving&&engineReady&&nativeReady&&shown===100&&now-started>=1600){
      leaving=true;clearTimeout(watchdog);document.removeEventListener('visibilitychange',watchStartup);document.body.classList.add('game-ready');canvas.removeAttribute('aria-hidden');loading.classList.add('is-leaving');loading.inert=true;
      window.dispatchEvent(new Event('bloomdoom-play-ready'));
      setTimeout(()=>{loading.textContent='';loading.remove();if(!canvas.inert)canvas.focus({preventScroll:true});},470);
    }
    if(!leaving&&!failed)requestAnimationFrame(frame);
  }
  requestAnimationFrame(frame);
  const config={dataUrl:build.dataUrl,frameworkUrl:build.frameworkUrl,codeUrl:build.codeUrl,streamingAssetsUrl:'StreamingAssets',companyName:'Homegrown Games',productName:'Bloom & Doom',productVersion:build.version,devicePixelRatio:Math.min(window.devicePixelRatio||1,2),showBanner:(message,type)=>{if(type==='error')fail(message);}};
  const script=document.createElement('script');script.src=build.loaderUrl;
  script.onload=()=>{
    try{
      createUnityInstance(canvas,config,p=>{target=Math.max(target,Math.min(92,p*92));setPhase(p<.45?1:2);}).then(instance=>{
        window.gardenGame=instance;engineReady=true;target=Math.max(target,96);setPhase(2);if(window.bloomDoomSceneReady===true)ready();
        watchStartup();
      }).catch(error=>fail(String(error)));
    }catch(error){fail(String(error));}
  };
  script.onerror=()=>fail('The game files could not load. Check your connection, then try again.');
  document.body.appendChild(script);
})();
