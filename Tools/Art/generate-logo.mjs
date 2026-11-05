import {createCanvas,GlobalFonts} from '@napi-rs/canvas';
import {writeFile,mkdir} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import {drawPlant,drawZombie} from './art.mjs';

// The wordmark is drawn from the same illustration source as the game's characters.
GlobalFonts.registerFromPath(fileURLToPath(new URL('../../Assets/Resources/BloomDoom/Fonts/LilitaOne-Regular.ttf',import.meta.url)),'GardenTitle');
const canvas=createCanvas(1800,800),c=canvas.getContext('2d');c.lineCap='round';c.lineJoin='round';
const ink='#344f37';
function ellipse(x,y,rx,ry,fill,stroke=null,width=3){c.beginPath();c.ellipse(x,y,rx,ry,0,0,Math.PI*2);c.fillStyle=fill;c.fill();if(stroke){c.lineWidth=width;c.strokeStyle=stroke;c.stroke();}}
function line(points,color,width=4){c.beginPath();c.moveTo(...points[0]);for(const point of points.slice(1))c.lineTo(...point);c.strokeStyle=color;c.lineWidth=width;c.stroke();}
function leaf(x,y,angle,size=1,fill='#9dbd62'){c.save();c.translate(x,y);c.rotate(angle);c.scale(size,size);c.beginPath();c.moveTo(0,0);c.bezierCurveTo(-10,-31,-40,-36,-57,-24);c.bezierCurveTo(-50,3,-23,13,0,0);c.fillStyle=fill;c.fill();c.lineWidth=4;c.strokeStyle=ink;c.stroke();line([[0,0],[-20,-12],[-43,-22]],'#5e8748',2.5);c.restore();}
function flower(x,y,size=1){for(let i=0;i<9;i++){let a=i*Math.PI*2/9;ellipse(x+Math.cos(a)*20*size,y+Math.sin(a)*20*size,10*size,12*size,'#ecc464','#8a783f',2.5);}ellipse(x,y,16*size,17*size,'#e8d897','#7b7140',2.5);ellipse(x-5*size,y-2*size,2*size,3*size,ink);ellipse(x+5*size,y-2*size,2*size,3*size,ink);c.beginPath();c.arc(x,y+2*size,6*size,.15,Math.PI-.15);c.strokeStyle=ink;c.lineWidth=2;c.stroke();}

ellipse(918,694,690,27,'#3e562b10');
// Curling stems connect the botanical lettering without becoming a background plate.
c.beginPath();c.moveTo(350,370);c.bezierCurveTo(480,390,442,107,565,126);c.bezierCurveTo(594,131,569,170,551,153);c.strokeStyle='#506e3c';c.lineWidth=9;c.stroke();
leaf(408,332,-.4,.95);leaf(427,257,1.2,.8,'#c4d78d');

function word(text,baseline,style){
  c.font='260px GardenTitle';c.textBaseline='alphabetic';
  const widths=[...text].map(ch=>c.measureText(ch).width),gap=6,total=widths.reduce((a,b)=>a+b,0)+gap*(text.length-1);let x=940-total/2;
  const bounds=[];
  [...text].forEach((ch,i)=>{
    const w=widths[i],tilt=(style==='plant'?[-.025,.018,-.015,.018,-.015]:[-.02,.016,-.015,.022])[i]||0;
    c.save();c.translate(x,baseline+(i%2?-5:0));c.rotate(tilt);c.font='260px GardenTitle';
    c.lineWidth=22;c.strokeStyle=ink;c.fillStyle=style==='plant'?'#517c3d':'#64796d';c.strokeText(ch,0,12);c.fillText(ch,0,12);
    const gradient=c.createLinearGradient(0,-196,0,2);if(style==='plant'){gradient.addColorStop(0,'#d4e69b');gradient.addColorStop(.45,'#a7c46d');gradient.addColorStop(1,'#7da34d');}else{gradient.addColorStop(0,'#cad1c4');gradient.addColorStop(.5,'#a7b2a8');gradient.addColorStop(1,'#8a9c96');}
    c.strokeStyle=ink;c.lineWidth=18;c.strokeText(ch,0,0);c.fillStyle=gradient;c.fillText(ch,0,0);
    c.save();c.globalCompositeOperation='source-atop';
    if(style==='plant'){line([[w*.25,-164],[w*.19,-125]],'#e7edbd88',7);line([[w*.16,-113],[w*.16,-95]],'#e7edbd88',5);}
    else{line([[w*.62,-178],[w*.52,-153],[w*.64,-140],[w*.48,-121]],'#687f76',4);line([[w*.48,-121],[w*.36,-113]],'#687f76',3);line([[w*.2,-53],[w*.3,-42],[w*.24,-25]],'#788c7e',3);}
    c.restore();
    if(style==='plant'&&ch==='O'&&i===2)flower(w*.52,-102,.7);
    if(style==='zombie'&&ch==='O'){
      const eyeY=-100;ellipse(w*.48,eyeY,19,23,'#eee8ba','#617760',2.5);ellipse(w*.49-5,eyeY+2,5.5,7,ink);
      ellipse(w*.48-6,eyeY-6,2,2,'#fff8db');
    }
    c.restore();bounds.push({x,w,baseline});x+=w+gap;
  });return bounds;
}
const bloom=word('BLOOM',319,'plant');
const doom=word('DOOM',648,'zombie');
leaf(bloom[1].x+18,132,-.4,.92);leaf(bloom[1].x+24,129,1.8,.72,'#bed782');
leaf(bloom[4].x+bloom[4].w*.58,118,.5,.85);leaf(bloom[4].x+bloom[4].w*.61,116,2.3,.8,'#bdd580');
leaf(bloom[0].x+8,306,-1.6,.65,'#c1d484');leaf(bloom[3].x+bloom[3].w*.85,327,2.5,.65);
flower(bloom[3].x+bloom[3].w*.54,117,.83);

// A sunny ampersand between the two halves of the title.
const ampX=938,ampY=396;
for(let i=0;i<10;i++){const a=i*Math.PI/5;ellipse(ampX+Math.cos(a)*47,ampY+Math.sin(a)*47,12,14,'#e6bd58','#8e7b40',2.5);}
ellipse(ampX,ampY,45,46,'#f1d784','#7c7740',4);c.font='68px GardenTitle';c.fillStyle='#667b3f';c.textAlign='center';c.fillText('&',ampX,ampY+23);c.textAlign='left';

// Stone chips, bandages and tiny moss leaves finish the undead lettering.
const last=doom[3];c.save();c.translate(last.x+last.w*.77,585);c.rotate(-.24);c.fillStyle='#e3d9b7';c.strokeStyle='#687c69';c.lineWidth=3;c.beginPath();c.roundRect(-25,-12,50,24,6);c.fill();c.stroke();for(let i=-1;i<2;i++)line([[i*10,-6],[i*10+2,6]],'#a39d7e',2.5);c.restore();
leaf(doom[0].x+21,480,-.5,.53,'#9ead72');leaf(last.x+last.w-9,640,2.8,.6,'#9ead72');
for(const letter of doom.slice(1,3)){c.fillStyle='#f1e5bc';c.strokeStyle='#5e7564';c.lineWidth=2;c.beginPath();c.roundRect(letter.x+letter.w*.38,letter.baseline-20,15,17,3);c.fill();c.stroke();}

drawPlant(c,{id:'sunbud',x:231,y:515,age:10,uid:1,hp:100,maxHp:100},0,3.18,false,true);
drawPlant(c,{id:'sprout',x:384,y:655,age:10,uid:2,hp:100,maxHp:100},0,1.75,false,true);
drawZombie(c,{type:'cone',x:1538,y:525,age:.3,hp:100,maxHp:100},0,2.7,true);
ellipse(476,603,12,12,'#d5e89c','#678849',3);ellipse(472,599,4,3,'#f3f1c0');

c.textAlign='center';c.font='27px GardenTitle';c.fillStyle='#7f8b5d';c.fillText('A  B A C K Y A R D  B A T T L E',938,742);
leaf(669,734,.25,.38,'#a7b778');leaf(1203,737,2.9,.38,'#a7b778');
const destination=new URL('../../Assets/Resources/BloomDoom/Art/title_logo.png',import.meta.url);
await mkdir(fileURLToPath(new URL('.',destination)),{recursive:true});await writeFile(destination,canvas.toBuffer('image/png'));
console.log(fileURLToPath(destination));
