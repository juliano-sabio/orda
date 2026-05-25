const fs = require('fs'), zlib = require('zlib');
const OUT = __dirname;

function crc32(buf) {
    const t = []; for (let i=0;i<256;i++){let c=i;for(let j=0;j<8;j++)c=(c&1)?0xEDB88320^(c>>>1):c>>>1;t[i]=c;}
    let c=0xFFFFFFFF; for(let i=0;i<buf.length;i++)c=t[(c^buf[i])&0xFF]^(c>>>8); return (c^0xFFFFFFFF)>>>0;
}
function chunk(type,data){const l=Buffer.alloc(4);l.writeUInt32BE(data.length);const t=Buffer.from(type);const cr=Buffer.alloc(4);cr.writeUInt32BE(crc32(Buffer.concat([t,data])));return Buffer.concat([l,t,data,cr]);}
function makePNG(px,w,h){const raw=[];for(let y=0;y<h;y++){raw.push(0);for(let x=0;x<w;x++){const i=(y*w+x)*4;raw.push(px[i],px[i+1],px[i+2],px[i+3]);}}const comp=zlib.deflateSync(Buffer.from(raw));const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(w,0);ihdr.writeUInt32BE(h,4);ihdr[8]=8;ihdr[9]=6;return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',comp),chunk('IEND',Buffer.alloc(0))]);}
function newCanvas(w,h,bg=[13,13,26,255]){const px=new Uint8Array(w*h*4);for(let i=0;i<w*h;i++){px[i*4]=bg[0];px[i*4+1]=bg[1];px[i*4+2]=bg[2];px[i*4+3]=bg[3];}return{px,w,h};}
function set(c,x,y,r,g,b,a=255){if(x<0||x>=c.w||y<0||y>=c.h)return;const i=(y*c.w+x)*4;c.px[i]=r;c.px[i+1]=g;c.px[i+2]=b;c.px[i+3]=a;}
function blend(c,x,y,r,g,b,a){if(x<0||x>=c.w||y<0||y>=c.h)return;const i=(y*c.w+x)*4;const al=a/255,bl=1-al;c.px[i]=Math.round(c.px[i]*bl+r*al);c.px[i+1]=Math.round(c.px[i+1]*bl+g*al);c.px[i+2]=Math.round(c.px[i+2]*bl+b*al);c.px[i+3]=Math.min(255,c.px[i+3]+a);}
function circle(c,cx,cy,r,col,fill=false){for(let y=cy-r;y<=cy+r;y++)for(let x=cx-r;x<=cx+r;x++){const d=Math.sqrt((x-cx)**2+(y-cy)**2);if(fill?d<=r:Math.abs(d-r)<=0.7)set(c,x,y,...col);}}
function line(c,x0,y0,x1,y1,col){const dx=Math.abs(x1-x0),dy=Math.abs(y1-y0),sx=x0<x1?1:-1,sy=y0<y1?1:-1;let err=dx-dy,x=x0,y=y0;while(true){set(c,x,y,...col);if(x===x1&&y===y1)break;const e2=2*err;if(e2>-dy){err-=dy;x+=sx;}if(e2<dx){err+=dx;y+=sy;}}}
function save(c,name){fs.writeFileSync(require('path').join(OUT,name),makePNG(c.px,c.w,c.h));console.log('Saved:',name);}

// ── ASCETA (balanced passive: ATK + DEF + regen + XP) ──
{
    const c = newCanvas(32, 32, [13, 13, 26, 255]);

    // Fundo: brilho dourado suave
    for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
        const d = Math.sqrt((x-15.5)**2 + (y-14)**2);
        if (d < 15) blend(c, x, y, 200, 155, 30, Math.floor(28 * (1 - d/15)));
    }

    // === ESCUDO (corpo principal) ===
    // Forma clássica: topo reto, lados verticais, base em ponta
    const shieldRows = [
        [7,  11, 20],
        [8,  10, 21],
        [9,   9, 22],
        [10,  9, 22],
        [11,  9, 22],
        [12,  9, 22],
        [13,  9, 22],
        [14, 10, 21],
        [15, 11, 20],
        [16, 12, 19],
        [17, 13, 18],
        [18, 14, 17],
        [19, 15, 16],
        [20, 15, 15], // ponta
    ];

    // Corpo dourado
    for (const [y, x0, x1] of shieldRows)
        for (let x = x0; x <= x1; x++) set(c, x, y, 200, 158, 45);

    // Highlight (canto superior esquerdo)
    for (const [y, x0] of [[7,11],[8,10],[9,9],[10,9],[11,9]]) {
        set(c, x0, y, 240, 215, 110); set(c, x0+1, y, 235, 205, 95);
    }

    // Sombra (canto inferior direito)
    for (const [y, x1] of [[9,22],[10,22],[11,22],[12,22],[13,22]]) {
        set(c, x1, y, 120, 90, 15); set(c, x1-1, y, 150, 112, 22);
    }

    // Contorno dourado brilhante
    for (const [y, x0, x1] of shieldRows) {
        set(c, x0, y, 232, 190, 78); set(c, x1, y, 232, 190, 78);
    }
    for (let x = 11; x <= 20; x++) set(c, x, 7, 232, 190, 78); // topo
    set(c, 15, 20, 255, 225, 100); // ponta

    // Cruz branca no escudo
    for (let x = 11; x <= 20; x++) set(c, x, 13, 255, 255, 255); // horizontal
    for (let y = 9; y <= 18; y++) set(c, 15, y, 255, 255, 255);  // vertical
    // Suaviza interseção
    blend(c, 15, 13, 255, 255, 200, 200);

    // === ESPADA (canto superior esquerdo, diagonal) ===
    // Lâmina vermelha-alaranjada, small e diagonal
    const swordPts = [[5,3],[4,4],[3,5],[2,6]];
    const swordPts2 = [[6,3],[5,4],[4,5],[3,6]];
    for (const [x,y] of swordPts)  set(c,x,y, 220, 70, 70);
    for (const [x,y] of swordPts2) set(c,x,y, 180, 50, 50);
    set(c, 6, 2, 255, 150, 150); // ponta
    // Guarda
    set(c, 2, 7, 180, 130, 60); set(c, 1, 7, 180, 130, 60);
    set(c, 3, 7, 180, 130, 60);

    // === ORB (canto superior direito, azul) ===
    circle(c, 26, 6, 4, [68, 136, 204, 230], true);
    circle(c, 26, 6, 4, [130, 190, 255, 150]);
    set(c, 25, 5, 200, 235, 255); // highlight
    set(c, 24, 6, 180, 220, 255);

    // === GEMA (canto inferior, verde) ===
    // Diamante em x=15, y=24
    set(c, 15, 23, 130, 255, 170); // topo (brilho)
    set(c, 14, 24, 51, 204, 102);
    set(c, 15, 24, 100, 240, 140);
    set(c, 16, 24, 51, 204, 102);
    set(c, 13, 25, 40, 170, 80);
    set(c, 14, 25, 51, 204, 102);
    set(c, 15, 25, 51, 204, 102);
    set(c, 16, 25, 51, 204, 102);
    set(c, 17, 25, 40, 170, 80);
    set(c, 14, 26, 30, 140, 60);
    set(c, 15, 26, 40, 160, 70);
    set(c, 16, 26, 30, 140, 60);
    set(c, 15, 27, 20, 110, 50); // ponta inferior

    // Brilhinhos nos cantos
    for (const [sx,sy] of [[1,1],[30,1],[1,30],[30,30]])
        blend(c, sx, sy, 255, 220, 100, 110);

    save(c, 'asceta_icon.png');
}
