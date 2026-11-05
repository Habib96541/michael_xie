import {BOARD,center} from './geometry.mjs';
const INK='#344f37',SKIN='#8cba9c';
function path(c,points,fill,stroke=INK,w=3){c.beginPath();for(let i=0;i<points.length;i++){const p=points[i];if(i===0)c.moveTo(...p);else c.lineTo(...p);}c.closePath();if(fill){c.fillStyle=fill;c.fill();}if(stroke){c.strokeStyle=stroke;c.lineWidth=w;c.stroke();}}
function ellipse(c,x,y,rx,ry,fill,stroke=INK,w=3,rotation=0){c.beginPath();c.ellipse(x,y,rx,ry,rotation,0,Math.PI*2);if(fill){c.fillStyle=fill;c.fill();}if(stroke){c.strokeStyle=stroke;c.lineWidth=w;c.stroke();}}
function line(c,x1,y1,x2,y2,color=INK,w=3){c.beginPath();c.moveTo(x1,y1);c.lineTo(x2,y2);c.strokeStyle=color;c.lineWidth=w;c.stroke();}
function curve(c,points,color=INK,w=3){c.beginPath();c.moveTo(points[0],points[1]);c.quadraticCurveTo(...points.slice(2));c.strokeStyle=color;c.lineWidth=w;c.stroke();}
function round(c,x,y,w,h,r,fill,stroke=INK,lw=3){c.beginPath();c.roundRect(x,y,w,h,r);if(fill){c.fillStyle=fill;c.fill();}if(stroke){c.strokeStyle=stroke;c.lineWidth=lw;c.stroke();}}
function leaf(c,x,y,angle=0,size=1,color='#82b94e'){c.save();c.translate(x,y);c.rotate(angle);c.scale(size,size);c.beginPath();c.moveTo(0,0);c.bezierCurveTo(-31,-4,-37,-27,-37,-27);c.bezierCurveTo(-10,-32,3,-13,0,0);c.fillStyle=color;c.fill();c.lineWidth=2.7;c.strokeStyle=INK;c.stroke();curve(c,[0,0,-16,-19,-29,-22],'#4c8046',1.7);c.restore();}
function eye(c,x,y,r=3,white=false){if(white)ellipse(c,x,y,r+2,r+4,'#fcf8d9',INK,1.5);ellipse(c,x,y,r,r*1.15,'#263e31',null);ellipse(c,x-.8,y-1,Math.max(.8,r*.27),Math.max(.8,r*.27),'#fffce7',null);}
function smile(c,x,y,size=1){curve(c,[x-6*size,y,x,y+7*size,x+6*size,y],INK,2.4);}
function shadow(c,x,y,rx=38,alpha=.17){ellipse(c,x,y,rx,10,`rgba(35,66,31,${alpha})`,null);}
function seeded(seed){return ()=>{seed=(seed*1664525+1013904223)>>>0;return seed/4294967296;};}

function shootHead(c,x,y,color,highlight,recoil=0){
  c.save();c.translate(x-recoil*7,y);c.rotate(-recoil*.08);
  ellipse(c,-10,0,29,27,color);ellipse(c,-17,-9,15,10,highlight,null,0,-.5);
  c.beginPath();c.moveTo(5,-15);c.quadraticCurveTo(21,-10,37,-17);c.lineTo(38,10);c.quadraticCurveTo(16,7,5,17);c.fillStyle=color;c.fill();c.strokeStyle=INK;c.lineWidth=3;c.stroke();
  ellipse(c,38,-3,10,16,color);ellipse(c,40,-3,5,10,'#294e3b',null);ellipse(c,37,-7,2,4,'#507657',null);
  eye(c,-13,-5,3.2);curve(c,[-23,-14,-17,-19,-10,-15],INK,2.4);ellipse(c,-22,6,5,2.5,'#b4d56c',null);smile(c,-10,9,.6);
  leaf(c,-29,-15,-1.45,.42,color);c.restore();
}
export function drawPlant(c,p,t=0,scale=1,ghost=false,reduced=false){
  c.save();c.translate(p.x,p.y);c.scale(scale,scale);c.globalAlpha=ghost?.55:1;c.lineJoin='round';c.lineCap='round';
  const sway=reduced?0:Math.sin(t*2.6+(p.uid||0)*1.3),age=p.age??10,pop=Math.min(1,age*5);if(age<.2)c.scale(.7+pop*.3,.65+pop*.35);
  if(p.flash>0)c.filter='brightness(1.6) sepia(.3)';
  if(p.id!=='mine'){
    line(c,0,29,sway*3,-15,'#3c6a3d',10);line(c,-1,28,sway*3-1,-15,'#81aa53',5);
    leaf(c,0,29,-.2,1);leaf(c,2,29,2.9,.85,'#92bd5f');
  }
  c.save();c.translate(sway*2,-Math.abs(sway)*1.6);
  if(p.id==='sunbud'){
    c.save();c.translate(0,-22);c.rotate(sway*.055);
    for(let i=0;i<11;i++){const a=i/11*Math.PI*2;ellipse(c,Math.cos(a)*27,Math.sin(a)*27,11,18,i%2?'#f3c454':'#e9ac42','#956e36',2.3,a+Math.PI/2);}
    ellipse(c,0,0,24,25,'#a47943','#674d35',2.7);ellipse(c,-2,-2,19,20,'#f2d47a',null);ellipse(c,-8,7,5,3,'#e8ab57',null);ellipse(c,10,7,5,3,'#e8ab57',null);
    eye(c,-8,-3,2.8);eye(c,8,-3,2.8);smile(c,0,7,1);for(let i=0;i<5;i++)ellipse(c,-12+i*6,-15+(i%2)*2,1,1,'#c7a55c',null);c.restore();
    leaf(c,-2,10,-.4,.6,'#a8bf66');
  }else if(p.id==='sprout'||p.id==='frost'){
    const cold=p.id==='frost';shootHead(c,sway,-22,cold?'#9ad4d3':'#8fbd69',cold?'#d2eeea':'#c0d995',p.recoil||0);
    if(cold){for(let i=0;i<3;i++){let a=-1.7+i*.7;line(c,-20+Math.cos(a)*9,-47,-20+Math.cos(a)*20,-58+Math.sin(a)*10,'#d5f0ea',4);}ellipse(c,-28,-42,4,4,'#bfe9e8','#5b9d9a',1.5);}
  }else if(p.id==='double'){
    line(c,-3,20,-14,-25,'#45734a',8);line(c,1,17,17,-11,'#45734a',8);
    c.save();c.scale(.8,.8);shootHead(c,-15,-44,'#7cab55','#a8d072',p.recoil||0);shootHead(c,15,-3,'#9abd66','#c9db8e',p.recoil||0);c.restore();
  }else if(p.id==='nut'){
    c.beginPath();c.moveTo(-31,-24);c.bezierCurveTo(-50,18,-19,51,0,48);c.bezierCurveTo(29,49,46,16,31,-24);c.closePath();c.fillStyle='#bc8c59';c.fill();c.strokeStyle='#68553a';c.lineWidth=3.2;c.stroke();
    ellipse(c,-15,-4,8,23,'#d2a873',null,0,.2);curve(c,[-22,24,-11,32,-4,27],'#9c734c',2);curve(c,[19,-3,27,14,19,24],'#a5774b',2);
    c.beginPath();c.moveTo(-35,-22);c.quadraticCurveTo(-29,-51,1,-49);c.quadraticCurveTo(32,-49,38,-21);c.quadraticCurveTo(1,-11,-35,-22);c.fillStyle='#7f8050';c.fill();c.strokeStyle='#4c573b';c.lineWidth=3;c.stroke();
    for(let i=-3;i<4;i++)line(c,i*9-4,-34,i*9+4,-24,'#a3a16b',2);for(let i=-2;i<3;i++)line(c,i*10,-43,i*10+8,-34,'#a3a16b',2);
    curve(c,[0,-49,-4,-58,4,-62],'#655b3c',6);leaf(c,3,-49,2,.38,'#91a566');eye(c,-13,-2,3.5,true);eye(c,13,-2,3.5,true);curve(c,[-20,-13,-12,-18,-5,-13],'#68543d',3);curve(c,[6,-13,13,-17,21,-11],'#68543d',3);smile(c,0,17,1.2);round(c,-3,20,7,7,1,'#f5dfad','#8a6a43',1);
    if(p.hp/p.maxHp<.65){path(c,[[-25,-4],[-17,4],[-23,14],[-15,25]],null,'#79573b',2);}
    if(p.hp/p.maxHp<.3)path(c,[[28,1],[16,8],[22,18],[13,29]],null,'#79573b',2);
  }else if(p.id==='cherry'){
    c.save();const pulse=1+Math.sin(age*22)*.045;c.scale(pulse,pulse);
    curve(c,[-19,-18,-12,-57,9,-55],'#5e7c3c',5);curve(c,[22,-15,17,-46,9,-55],'#5e7c3c',5);leaf(c,9,-54,1.5,.6,'#90b95b');
    ellipse(c,-20,1,24,26,'#d87065','#774f40',3);ellipse(c,23,7,24,27,'#c85f55','#774f40',3);ellipse(c,-29,-10,7,10,'#f3aa8e',null,0,.5);ellipse(c,15,-5,6,8,'#e99a7d',null);
    eye(c,-25,0,2.6);eye(c,-11,0,2.6);eye(c,16,7,2.6);eye(c,30,7,2.6);smile(c,-17,11,.8);smile(c,23,18,.8);c.restore();
  }else if(p.id==='mine'){
    ellipse(c,0,27,39,12,'#8e7754','#5f653b',2);for(let i=-2;i<3;i++)ellipse(c,i*14,30+(i%2)*4,7,3,'#b19a66',null);
    if(p.armed||ghost){ellipse(c,0,13,29,22,'#cead76','#685c3a',3);ellipse(c,-9,3,9,6,'#e7ce99',null);eye(c,-9,13,3);eye(c,10,13,3);smile(c,0,23,.8);line(c,0,-9,0,-24,'#8c8b71',5);ellipse(c,0,-25,9,6,Math.sin(t*7)>0?'#e17c60':'#c45d4c','#714b37',2);}
    else{ellipse(c,0,22,17,8,'#c4a16b','#796640',2);line(c,0,15,0,5,'#7d7b5b',4);ellipse(c,0,3,7,4,'#b79b68','#716b4a',1.5);}
  }else if(p.id==='pepper'){
    c.save();c.rotate(-.14+sway*.03);c.beginPath();c.moveTo(-20,-33);c.bezierCurveTo(38,-47,50,3,14,21);c.quadraticCurveTo(-5,33,-30,27);c.quadraticCurveTo(-4,14,-13,-5);c.quadraticCurveTo(-25,-16,-20,-33);c.fillStyle='#d97951';c.fill();c.lineWidth=3;c.strokeStyle='#7b5437';c.stroke();
    curve(c,[-2,-24,25,-23,28,-4],'#efb07a',5);path(c,[[-23,-30],[-30,-39],[-13,-39],[-8,-50],[1,-38],[15,-34],[2,-28],[-7,-32]],'#8bab58','#4d7041',2.5);curve(c,[-10,-40,-8,-54,4,-56],'#4f7742',5);eye(c,1,-10,3);eye(c,19,-8,3);curve(c,[-5,-20,0,-23,6,-19],INK,2.7);curve(c,[14,-18,20,-20,26,-15],INK,2.7);smile(c,13,4,1);c.restore();
  }
  c.restore();c.restore();
}

function shoe(c,x,y,angle=0,scale=1){c.save();c.translate(x,y);c.rotate(angle);c.scale(scale,scale);c.beginPath();c.moveTo(-9,-7);c.lineTo(8,-7);c.quadraticCurveTo(12,1,18,2);c.quadraticCurveTo(25,13,9,14);c.lineTo(-10,12);c.closePath();c.fillStyle='#77745c';c.fill();c.strokeStyle='#424d3a';c.lineWidth=3;c.stroke();line(c,-8,10,16,11,'#c6b88e',2.5);c.restore();}
export function drawZombie(c,z,t=0,scale=1,reduced=false){
  c.save();c.translate(z.x,z.y);c.scale(scale,scale);c.lineCap='round';c.lineJoin='round';
  const a=z.age??t,walk=reduced?0:Math.sin(a*(z.type==='runner'?8:4.5)),lean=z.eating?Math.sin(a*15)*.04:walk*.035,giant=z.type==='giant';if(giant)c.scale(1.35,1.35);c.rotate(lean);
  if(z.flash>0)c.filter='brightness(1.6)';else if(z.slow>0)c.filter='saturate(.65) hue-rotate(35deg)';
  const coat=z.type==='runner'?'#b57d61':giant?'#6b8060':'#89869c',pants=z.type==='runner'?'#87745e':'#666c80';
  // Legs, boots, and arms each move independently of the head and torso.
  c.save();c.translate(-11,7);c.rotate(walk*.19);round(c,-8,0,15,28,5,pants);shoe(c,-1,29,.15);c.restore();
  c.save();c.translate(12,7);c.rotate(-walk*.2);round(c,-7,0,15,30,5,pants);shoe(c,1,30,-.08);c.restore();
  c.save();c.translate(14,-25);c.rotate(-.6-walk*.09);round(c,-6,0,14,33,6,coat);ellipse(c,0,36,9,7,SKIN);c.restore();
  c.beginPath();c.moveTo(-22,-39);c.quadraticCurveTo(0,-49,25,-33);c.lineTo(27,10);c.quadraticCurveTo(0,20,-26,9);c.closePath();c.fillStyle=coat;c.fill();c.strokeStyle=INK;c.lineWidth=3;c.stroke();
  path(c,[[-7,-39],[5,-33],[12,-39],[12,-4],[-8,-3]],'#c8bb8f',INK,2);path(c,[[0,-32],[7,-31],[3,-22],[7,-4],[0,2],[-4,-5],[0,-22]],'#b66e58',INK,1.7);line(c,-16,-26,-10,0,'#a7a2ad',2);round(c,13,-7,10,11,2,'#bcb59b','#646a59',1.5);line(c,15,-5,21,1,'#7d826b',1);
  c.save();c.translate(-20,-30);c.rotate(.9+(z.eating?Math.sin(a*13)*.1:walk*.12));round(c,-7,0,16,28,6,coat);round(c,-7,21,14,18,4,SKIN);ellipse(c,0,40,10,7,SKIN);line(c,-5,40,-6,45,INK,1.7);line(c,0,41,0,46,INK,1.7);c.restore();
  c.save();c.translate(-4,-57+(z.eating?Math.sin(a*15)*2:walk*1.2));c.rotate(-.07);
  line(c,4,20,5,32,SKIN,16);
  c.beginPath();c.moveTo(-22,-21);c.bezierCurveTo(-32,-12,-25,7,-29,13);c.lineTo(-24,28);c.quadraticCurveTo(-9,36,17,25);c.quadraticCurveTo(33,16,27,-12);c.quadraticCurveTo(24,-29,-2,-29);c.closePath();c.fillStyle=giant?'#87a178':SKIN;c.fill();c.strokeStyle=INK;c.lineWidth=3;c.stroke();
  ellipse(c,27,5,7,10,'#8bb296',INK,2);line(c,26,3,28,8,'#56856c',1.5);
  ellipse(c,-17,0,10,12,'#f4edbb','#53785b',2,-.15);ellipse(c,8,-1,10,12,'#f4edbb','#53785b',2,.13);ellipse(c,-20,1,3.5,4.5,'#435640',null);ellipse(c,5,1,3.5,4.5,'#435640',null);
  path(c,[[-8,4],[-15,12],[-5,12]],'#71967b','#4d755b',1.6);curve(c,[-20,-15,-13,-20,-5,-14],'#4d7457',3);curve(c,[0,-15,9,-21,17,-14],'#4d7457',3);
  c.beginPath();c.moveTo(-20,20);c.quadraticCurveTo(-4,z.eating?38:29,13,18);c.quadraticCurveTo(-5,12,-20,20);c.fillStyle='#4d6651';c.fill();c.strokeStyle=INK;c.lineWidth=2;c.stroke();round(c,-13,18,6,7,1,'#e8debb',null);round(c,3,18,5,5,1,'#e8debb',null);line(c,16,12,20,14,'#648d70',2);ellipse(c,18,18,2,1.5,'#648d70',null);
  if(!['cone','bucket'].includes(z.type)){curve(c,[-13,-27,-16,-37,-8,-39],'#435942',3);curve(c,[-3,-28,0,-39,8,-37],'#435942',3);}
  if(z.type==='cone'){
    path(c,[[-28,-26],[-10,-80],[8,-72],[23,-24]],'#d99b5f','#6b6445',3);path(c,[[-22,-45],[15,-44],[11,-55],[-18,-57]],'#eddfb3','#ae9262',1);ellipse(c,-2,-24,32,7,'#d29557','#6b6445',2.5);line(c,-10,-70,-19,-40,'#edb97d',2);
  }else if(z.type==='bucket'){
    c.save();c.rotate(.1);path(c,[[-30,-26],[-27,-61],[24,-61],[28,-24]],'#9caaa0','#4d6553',3);ellipse(c,-1,-60,25,6,'#bdc6ad','#4d6553',2.5);round(c,-32,-29,63,9,3,'#adb5a5','#4d6553',2);curve(c,[-25,-52,-50,-32,-28,-17],'#697e70',3);line(c,-16,-53,-17,-36,'#cad1b9',4);path(c,[[9,-43],[15,-47],[19,-40],[13,-36]],'#b58d68',null);c.restore();
  }else if(z.type==='runner'){
    path(c,[[-26,-17],[-26,-27],[24,-24],[27,-14]],'#cb8c69','#6f6750',2);line(c,-22,-23,19,-19,'#efd4a0',3);path(c,[[24,-21],[41,-27],[38,-16],[29,-12]],'#c68a66',INK,2);
  }else if(giant){
    for(let i=0;i<7;i++)ellipse(c,-22+i*7,-25-Math.sin(i)*5,8,9,i%2?'#6d8f52':'#9eb26a','#526f46',1.5);leaf(c,24,-21,1.5,.5,'#9eb26a');
  }
  c.restore();
  if(z.type==='paper'&&!z.enraged){c.save();c.translate(-30,-11);c.rotate(-.18);path(c,[[-28,-25],[21,-28],[25,21],[-24,25]],'#e3dcc1','#737d5c',2);line(c,-19,-18,16,-20,'#8c9676',3);for(let j=0;j<4;j++){line(c,-19,-9+j*7,-3,-10+j*7,'#a7ad8e',1.5);line(c,3,-10+j*7,17,-11+j*7,'#a7ad8e',1.5);}ellipse(c,-25,11,7,5,SKIN,INK,2);c.restore();}
  c.restore();
}

export function drawSun(c,s,t){c.save();c.translate(s.x,s.y);const pulse=1+Math.sin(t*3+s.uid)*.05;c.scale(pulse,pulse);if(s.collected)c.globalAlpha=Math.max(0,1-s.collectAge*1.5);else if(s.life<3)c.globalAlpha=.5+Math.sin(t*10)*.3;
  const glow=c.createRadialGradient(0,0,8,0,0,49);glow.addColorStop(0,'#ffe38b77');glow.addColorStop(1,'#ffe88b00');ellipse(c,0,0,49,49,glow,null);
  c.save();c.rotate(t*.18);for(let i=0;i<9;i++){const a=i*Math.PI*2/9;path(c,[[Math.cos(a-.12)*23,Math.sin(a-.12)*23],[Math.cos(a)*34,Math.sin(a)*34],[Math.cos(a+.12)*23,Math.sin(a+.12)*23]],'#f8d56e','#b9a04d',1.5);}c.restore();ellipse(c,0,0,22,22,'#f8d471','#b59a42',2.2);ellipse(c,-4,-5,14,12,'#ffec9d',null);curve(c,[-10,4,0,13,10,4],'#c8a444',2);ellipse(c,-7,-2,2,2,'#bca14d',null);ellipse(c,7,-2,2,2,'#bca14d',null);c.restore();}
function daisy(c,x,y,s=1,color='#f2e9c2'){for(let i=0;i<5;i++){const a=i*1.256;ellipse(c,x+Math.cos(a)*4*s,y+Math.sin(a)*4*s,3*s,2*s,color,null,0,a);}ellipse(c,x,y,2*s,2*s,'#e4ba5b',null);}
function grass(c,x,y,s=1,color='#557d38'){curve(c,[x-7*s,y,x-11*s,y-11*s,x-6*s,y-10*s],color,2);curve(c,[x,y,x-2*s,y-18*s,x+3*s,y-19*s],color,2);curve(c,[x+4*s,y,x+8*s,y-11*s,x+12*s,y-10*s],color,2);}
export function mower(c,m){c.save();c.translate(m.x,center(m.row,0).y+14);c.scale(.86,.86);shadow(c,0,25,39);line(c,-22,2,-42,-38,'#647b54',5);line(c,-42,-38,-24,-43,'#647b54',6);ellipse(c,-21,23,10,12,'#4b5841','#344934',3);ellipse(c,24,23,10,12,'#4b5841','#344934',3);ellipse(c,-21,23,4,5,'#a4ad7b',null);ellipse(c,24,23,4,5,'#a4ad7b',null);round(c,-32,-5,66,25,7,'#b96c54','#5d6340',3);round(c,-17,-19,38,22,6,'#cf8d69','#5d6340',2.5);round(c,-7,-25,18,8,3,'#6e7d52','#4b5c3c',2);for(let i=-1;i<3;i++)line(c,19,-3+i*5,29,-3+i*5,'#805e46',1.5);ellipse(c,-17,1,4,4,'#e4ba76',null);c.restore();}

export function background(c,theme){
  const rng=seeded(2741);const night=theme==='night',sunset=theme==='sunset';
  const palette=night?['#8eae8a','#75966b','#6f9163','#c7c6ab','#70866a']:sunset?['#aeb97b','#9cb667','#91aa59','#d8c8a0','#92a16c']:['#a4bd83','#9fbd6e','#91b25e','#d3cfaa','#8da675'];
  c.fillStyle=palette[0];c.fillRect(0,0,1440,700);
  // A hedge of layered, imperfect silhouettes, behind a white picket fence.
  for(let i=0;i<45;i++){const x=i*35;ellipse(c,x,25+rng()*22,40+rng()*20,48,'#728f5b',null);ellipse(c,x+8,20+rng()*18,31,34,'#8ba46c',null);}
  line(c,128,60,1425,60,'#c4ccb0',13);line(c,128,88,1425,88,'#c4ccb0',10);
  for(let x=151;x<1450;x+=38){path(c,[[x-9,95],[x-9,34],[x,22],[x+10,34],[x+10,95]],'#e0dfc1','#9ca782',1.8);line(c,x+5,37,x+5,83,'#eeedd6',2);}
  for(let x=180;x<1400;x+=120){leaf(c,x,99,-.5,.6,'#6e9552');leaf(c,x+20,99,2.6,.5,'#87a961');daisy(c,x+33,88,.9);}
  // House and shaded porch at the left, with a blue door and climbing leaves.
  c.fillStyle='#d6cfad';c.fillRect(0,70,113,548);for(let y=82;y<620;y+=28)line(c,0,y,113,y,'#b9b69a',1.5);
  path(c,[[0,64],[116,51],[137,77],[0,89]],'#9d9e78','#6a7c50',2);round(c,5,231,78,172,4,'#819f9b','#637c6b',4);round(c,13,240,60,57,3,'#adc6b6','#627d6c',2);line(c,43,240,43,297,'#829e89',3);line(c,13,268,73,268,'#829e89',3);round(c,14,312,58,74,2,'#89a69c','#627d6c',2);ellipse(c,65,310,4,4,'#c2a25d','#7f8050',1.5);
  path(c,[[0,405],[89,405],[116,435],[0,435]],'#b8b797','#8e9a77',2);path(c,[[0,436],[116,436],[139,458],[0,458]],'#c9c5a1','#8e9a77',2);
  c.fillStyle='#bdc2a0';c.fillRect(115,94,99,527);for(let y=100;y<625;y+=74){round(c,124+(Math.floor(y/74)%2)*9,y,69,57,9,'#cfd1b0','#aab391',1.5);line(c,137,y+8,173,y+8,'#dadcc0',1.2);}
  for(let x=10;x<140;x+=35){leaf(c,x,126,-.4,.75,'#6d8d50');leaf(c,x+4,117,2,.6,'#8ba968');}
  // Garden bed, and nine by five patches of soft, textured grass.
  round(c,214,97,1032,528,15,'#7d8952','#73814f',4);round(c,221,102,1018,516,10,'#6d8c4c',null);
  for(let r=0;r<5;r++)for(let col=0;col<9;col++){const x=BOARD.x+col*112,y=BOARD.y+r*102;c.fillStyle=palette[1+(r+col)%2];c.fillRect(x,y,112,102);c.fillStyle='#e1e9ab12';c.fillRect(x+3,y+3,106,3);c.fillStyle='#496d3420';c.fillRect(x,y+99,112,3);
    for(let i=0;i<9;i++){let px=x+9+rng()*94,py=y+10+rng()*80;const bright=i%2===0;line(c,px,py,px+3,py-4,bright?'#d2dfa133':'#628e453a',1);if(i%4===0)line(c,px+4,py,px+5,py-3,'#d4e6a033',1);}
  }
  for(let i=0;i<400;i++){const x=224+rng()*1010,y=105+rng()*510;ellipse(c,x,y,.5+rng(),.5,`rgba(69,93,43,${.08+rng()*.1})`,null);}
  // Crooked flagstones and a weathered fence on the zombie approach.
  c.fillStyle=palette[3];c.fillRect(1252,100,188,540);
  for(let row=0;row<7;row++)for(let col=0;col<3;col++){const x=1260+col*67+(row%2)*-15,y=106+row*79;path(c,[[x+8,y+5],[x+57,y],[x+64,y+23],[x+58,y+67],[x+5,y+72],[x-1,y+46]],row%2?'#ded6b4':'#d3cfac','#b7bb96',2);curve(c,[x+15,y+14,x+32,y+9,x+43,y+12],'#eeead0',1.5);if(row%3===0)path(c,[[x+46,y+65],[x+37,y+51],[x+45,y+39]],null,'#b5b990',1.5);}
  for(let y=95;y<660;y+=83){path(c,[[1404,y],[1439,y-8],[1440,y+64],[1402,y+69]],'#a0a37a','#788a61',2);line(c,1414,y+8,1412,y+57,'#b6b690',2);ellipse(c,1422,y+23,3,5,'#8b956c',null);}
  // Foreground meadow, a few pots, and restrained flowers around the edges.
  c.fillStyle=palette[4];c.fillRect(0,639,1440,61);c.fillStyle='#8f995b';c.fillRect(206,623,1048,18);
  for(let i=0;i<75;i++){const x=rng()*1440,y=644+rng()*56;grass(c,x,y,.45+rng()*.4,i%2?'#65834e':'#a6b982');if(i%5===0)daisy(c,x+10,y-9,.8,i%3?'#f3e5bd':'#d4c2a4');}
  for(let row=0;row<5;row++){const y=center(row,0).y;grass(c,1245,y+35,.65,'#708b50');grass(c,215,y+43,.7,'#66854d');}
  for(const y of [173,544]){shadow(c,42,y+26,29);path(c,[[19,y-8],[66,y-8],[60,y+27],[26,y+27]],'#b8946c','#6e7550',2.5);round(c,15,y-13,55,12,3,'#c8a67e','#6e7550',2);for(let i=0;i<4;i++){line(c,39,y-9,23+i*10,y-36-i%2*12,'#537b4a',3);daisy(c,23+i*10,y-36-i%2*12,1.8,i%2?'#efcb75':'#d6a796');}}
  // Hand-painted entry sign and a small red mailbox.
  line(c,1328,42,1328,112,'#8a875b',7);round(c,1298,35,65,39,[18,18,3,3],'#b77860','#6a7550',2.5);round(c,1297,37,31,36,[15,15,3,3],'#d29c78','#6a7550',2);line(c,1347,39,1347,17,'#775b42',3);path(c,[[1347,17],[1368,17],[1368,29],[1347,29]],'#d19864','#775b42',1.5);line(c,1306,58,1317,58,'#8b7450',2);
  if(night){c.fillStyle='#253b6120';c.fillRect(0,0,1440,700);ellipse(c,1136,21,22,22,'#f2e7ae',null);ellipse(c,1145,12,21,21,'#829571',null);for(let i=0;i<20;i++){let x=rng()*1260+130,y=rng()*50;ellipse(c,x,y,1.2,1.2,'#e6e8c4aa',null);}}
  if(sunset){const g=c.createLinearGradient(0,0,1440,700);g.addColorStop(0,'#ffe6a718');g.addColorStop(1,'#c2944f0c');c.fillStyle=g;c.fillRect(0,0,1440,700);}
}

export function drawShovel(c,x=0,y=0,s=1){c.save();c.translate(x,y);c.scale(s,s);c.rotate(.65);line(c,0,-21,0,22,'#92774f',7);round(c,-11,-39,22,19,7,'#bb8c63','#636c48',2.5);round(c,-6,-35,12,10,3,'#f4edd6','#636c48',1.5);path(c,[[-13,14],[13,14],[14,35],[0,46],[-14,35]],'#afc0ad','#586d52',2.5);line(c,0,20,0,38,'#dae0c4',2);c.restore();}
