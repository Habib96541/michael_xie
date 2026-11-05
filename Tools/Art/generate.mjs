import {createCanvas} from '@napi-rs/canvas';
import {writeFile,mkdir} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import path from 'node:path';
const out=fileURLToPath(new URL('../../Assets/Resources/BloomDoom/Art/',import.meta.url));
await mkdir(out,{recursive:true});
const art=await import('./art.mjs');
const save=async(name,canvas)=>writeFile(path.join(out,name+'.png'),canvas.toBuffer('image/png'));
for(const theme of ['day','night','sunset']){const c=createCanvas(1440,700);art.background(c.getContext('2d'),theme);await save('garden_'+theme,c);}
const plants=['sunbud','sprout','nut','frost','cherry','mine','double','pepper','nut_damaged','nut_critical','mine_unarmed'];
for(const id of plants){const c=createCanvas(4096,256),ctx=c.getContext('2d');for(let i=0;i<16;i++){ctx.save();ctx.beginPath();ctx.rect(i*256,0,256,256);ctx.clip();ctx.translate(i*256,0);art.drawPlant(ctx,{id:id.startsWith('nut_')?'nut':id==='mine_unarmed'?'mine':id,x:128,y:128,uid:1,age:10,hp:id==='nut_damaged'?60:id==='nut_critical'?20:100,maxHp:100,armed:id!=='mine_unarmed'},i/16*Math.PI*2/2.6,1,false,false);ctx.restore();}await save('plant_'+id,c);if(!id.includes('_')){const icon=createCanvas(128,128);const ic=icon.getContext('2d');ic.translate(64,72);ic.scale(128/115,128/115);art.drawPlant(ic,{id,x:0,y:0,age:10,hp:100,maxHp:100,armed:true},0,1,false,true);await save('icon_'+id,icon);}}
for(const type of ['basic','cone','runner','bucket','paper','giant'])for(const state of ['walk','eat']){const c=createCanvas(4096,256),ctx=c.getContext('2d');for(let i=0;i<16;i++){ctx.save();ctx.beginPath();ctx.rect(i*256,0,256,256);ctx.clip();ctx.translate(i*256,0);art.drawZombie(ctx,{type,x:128,y:170,age:i/16*Math.PI*2/(type==='runner'?8:4.5),eating:state==='eat'},0,1,false);ctx.restore();}await save('zombie_'+type+'_'+state,c);}
{const c=createCanvas(128,128);art.drawSun(c.getContext('2d'),{x:64,y:64,uid:1,life:10},0);await save('sun',c);}
{const c=createCanvas(128,128),ctx=c.getContext('2d');ctx.translate(64-170,64-156-14);art.mower(ctx,{row:0,x:170});await save('mower',c);}
{const c=createCanvas(128,128);art.drawShovel(c.getContext('2d'),64,64,1);await save('shovel',c);}
{const c=createCanvas(32,32),ctx=c.getContext('2d');ctx.beginPath();ctx.arc(16,16,14,0,Math.PI*2);ctx.fillStyle='#cae98d';ctx.fill();ctx.strokeStyle='#628c4c';ctx.lineWidth=3;ctx.stroke();ctx.beginPath();ctx.ellipse(11,10,5,3,-.4,0,Math.PI*2);ctx.fillStyle='#f6fad6';ctx.fill();await save('pea',c);}
await import('./generate-logo.mjs');
console.log('Generated the Unity garden, character atlases, icons, effects and title logo in '+out);
