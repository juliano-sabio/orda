const fs = require('fs'), zlib = require('zlib');
const OUT = __dirname;
function crc32(buf){const t=[];for(let i=0;i<256;i++){let c=i;for(let j=0;j<8;j++)c=(c&1)?0xEDB88320^(c>>>1):c>>>1;t[i]=c;}let c=0xFFFFFFFF;for(let i=0;i<buf.length;i++)c=t[(c^buf[i])&0xFF]^(c>>>8);return(c^0xFFFFFFFF)>>>0;}
function chunk(type,data){const l=Buffer.alloc(4);l.writeUInt32BE(data.length);const t=Buffer.from(type);const cr=Buffer.alloc(4);cr.writeUInt32BE(crc32(Buffer.concat([t,data])));return Buffer.concat([l,t,data,cr]);}
function makePNG(px,w,h){const raw=[];for(let y=0;y<h;y++){raw.push(0);for(let x=0;x<w;x++){const i=(y*w+x)*4;raw.push(px[i],px[i+1],px[i+2],px[i+3]);}}const comp=zlib.deflateSync(Buffer.from(raw));const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(w,0);ihdr.writeUInt32BE(h,4);ihdr[8]=8;ihdr[9]=6;return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',comp),chunk('IEND',Buffer.alloc(0))]);}
function newCanvas(w,h,bg=[13,13,26,255]){const px=new Uint8Array(w*h*4);for(let i=0;i<w*h;i++){px[i*4]=bg[0];px[i*4+1]=bg[1];px[i*4+2]=bg[2];px[i*4+3]=bg[3];}return{px,w,h};}
function set(c,x,y,r,g,b,a=255){if(x<0||x>=c.w||y<0||y>=c.h)return;const i=(y*c.w+x)*4;c.px[i]=r;c.px[i+1]=g;c.px[i+2]=b;c.px[i+3]=a;}
function blend(c,x,y,r,g,b,a){if(x<0||x>=c.w||y<0||y>=c.h)return;const i=(y*c.w+x)*4;const al=a/255,bl=1-al;c.px[i]=Math.round(c.px[i]*bl+r*al);c.px[i+1]=Math.round(c.px[i+1]*bl+g*al);c.px[i+2]=Math.round(c.px[i+2]*bl+b*al);c.px[i+3]=Math.min(255,c.px[i+3]+a);}
function line(c,x0,y0,x1,y1,col){const dx=Math.abs(x1-x0),dy=Math.abs(y1-y0),sx=x0<x1?1:-1,sy=y0<y1?1:-1;let err=dx-dy,x=x0,y=y0;while(true){set(c,x,y,...col);if(x===x1&&y===y1)break;const e2=2*err;if(e2>-dy){err-=dy;x+=sx;}if(e2<dx){err+=dx;y+=sy;}}}
function save(c,name){fs.writeFileSync(require('path').join(OUT,name),makePNG(c.px,c.w,c.h));console.log('Saved:',name);}

// ── IMPOSIÇÃO (aura reduz ATK dos inimigos próximos) ──
{
    const c = newCanvas(32, 32, [13, 13, 26, 255]);

    // Fundo: brilho roxo-dourado
    for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
        const d = Math.sqrt((x-15.5)**2 + (y-16)**2);
        if (d < 15) blend(c, x, y, 80, 30, 140, Math.floor(25 * (1 - d/15)));
    }

    // Linhas radiantes (8 direções, saindo do centro)
    const centro = [15, 15];
    for (let i = 0; i < 8; i++) {
        const ang = i / 8 * Math.PI * 2;
        const x1 = Math.round(15 + Math.cos(ang) * 13);
        const y1 = Math.round(15 + Math.sin(ang) * 13);
        line(c, 15, 15, x1, y1, [100, 40, 180, 100]);
    }

    // Anel externo roxo
    for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
        const d = Math.sqrt((x-15.5)**2 + (y-15)**2);
        if (Math.abs(d - 12) < 0.9) blend(c, x, y, 130, 60, 210, 140);
    }

    // Coroa dourada — base retangular
    for (let x = 9; x <= 22; x++) {
        set(c, x, 18, 200, 158, 40);
        set(c, x, 19, 200, 158, 40);
        set(c, x, 20, 180, 138, 30);
    }
    // Coroa — 5 pontas (3 altas, 2 baixas nas bordas)
    // Ponta esq externa (baixa)
    for (let y = 14; y <= 18; y++) { set(c, 9, y, 200, 158, 40); set(c, 10, y, 200, 158, 40); }
    // Ponta esq interna (alta)
    for (let y = 10; y <= 18; y++) { set(c, 12, y, 200, 158, 40); set(c, 13, y, 200, 158, 40); }
    // Ponta central (mais alta)
    for (let y = 8; y <= 18; y++) { set(c, 15, y, 210, 168, 50); set(c, 16, y, 210, 168, 50); }
    // Ponta dir interna
    for (let y = 10; y <= 18; y++) { set(c, 18, y, 200, 158, 40); set(c, 19, y, 200, 158, 40); }
    // Ponta dir externa
    for (let y = 14; y <= 18; y++) { set(c, 21, y, 200, 158, 40); set(c, 22, y, 200, 158, 40); }

    // Highlights na coroa
    set(c, 15, 8, 255, 230, 120); set(c, 16, 8, 255, 230, 120);
    set(c, 12, 10, 240, 210, 100); set(c, 9, 14, 240, 210, 100);
    set(c, 21, 14, 240, 210, 100); set(c, 18, 10, 240, 210, 100);

    // Gema no centro da coroa
    set(c, 15, 15, 180, 80, 255); set(c, 16, 15, 180, 80, 255);
    set(c, 15, 16, 160, 60, 230); set(c, 16, 16, 160, 60, 230);

    save(c, 'imposicao_icon.png');
}
