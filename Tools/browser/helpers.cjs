const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const status=page=>page.evaluate(()=>window.hydraulicStatus);
const cachedBounds=new Map();
async function wait(page,predicate,timeout=12000){await page.waitForFunction(predicate,null,{timeout});}
async function tap(page,name,touch=false){
  const buttons=await page.evaluate(()=>window.hydraulicStatus.buttons||[]);
  for(const item of buttons)if(item.interactable)cachedBounds.set(item.name,item);
  const live=buttons.find(item=>item.name===name&&item.interactable);
  if(live)cachedBounds.set(name,live);
  const r=await page.locator('canvas').boundingBox();
  const b=live||cachedBounds.get(name);
  const wide=r.width>=r.height, scale=wide?r.height/720:r.width/720;
  const point=b?{x:r.x+b.x*r.width,y:r.y+b.y*r.height}:name==='Replay'?{x:r.x+r.width/2,y:r.y+r.height/2+143*scale*(wide?.85:1)}:null;
  if(!point)throw Error('Unavailable control: '+name);
  if(touch)await page.touchscreen.tap(point.x,point.y);else await page.mouse.click(point.x,point.y);
  await sleep(120);
}
async function boot(page){
  await page.goto(process.env.HP_URL||'http://127.0.0.1:8088');
  await wait(page,()=>window.hydraulicStatus?.navigation==='MainMenu',90000);await sleep(450);
}
async function play(page,touch=false){await tap(page,'PlayOrResume',touch);await wait(page,()=>window.hydraulicStatus.navigation==='Playing'&&!window.hydraulicStatus.busy);}
module.exports={sleep,status,wait,tap,boot,play};
