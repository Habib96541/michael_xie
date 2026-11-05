export const BOARD=Object.freeze({x:226,y:105,cols:9,rows:5,cw:112,ch:102,width:1440,height:700});
export function center(row,col){return {x:BOARD.x+(col+.5)*112,y:BOARD.y+(row+.5)*102};}
