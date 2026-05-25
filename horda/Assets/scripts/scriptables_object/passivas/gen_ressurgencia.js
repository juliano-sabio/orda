const fs = require('fs'), zlib = require('zlib');
const OUT = __dirname;
function crc32(buf){const t=[];for(let i=0;i<256;i++){let c=i;for(let j=0;j<8;j++)c=(c&1)?0xEDB88320^(c>>>1):c>>>1;t[i]=c;}let c=0xFFFFFFFF;for(let i=0;i<buf.length;i++)c=t[(c^buf[i])&0xFF]^(c>>>8);return(c^0xFFFFFFFF)>>>0;}
function chunk(type,data){const l=Buffer.alloc(4);l.writeUInt32BE(data.length);const t=Buffer.from(type);const cr=Buffer.alloc(4);cr.writeUInt32BE(crc32(Buffer.concat([t,data])));return Buffer.concat([l,t,data,cr]);}
function makePNG(px,w,h){const raw=[];for(let y=0;y<h;y++){raw.push(0);for(let x=0;x<w;x++){const i=(y*w+x)*4;raw.push(px[i],px[i+1],px[i+2],px[i+3]);}}const comp=zlib.deflateSync(Buffer.from(raw));const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(w,0);ihdr.writeUInt32BE(h,4);ihdr[8]=8;ihdr[9]=6;return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',comp),chunk('IEND',Buffer.alloc(0))]);}
function newCanvas(w,h,bg=[13,13,26,255]){const px=new Uint8Array(w*h*4);for(let i=0;i<w*h;i++){px[i*4]=bg[0];px[i*4+1]=bg[1];px[i*4+2]=bg[2];px[i*4+3]=bg[3];}return{px,w,h};}
function set(c,x,y,r,g,b,a=255){if(x<0||x>=c.w||y<0||y>=c.h)return;const i=(y*c.w+x)*4;c.px[i]=r;c.px[i+1]=g;c.px[i+2]=b;c.px[i+3]=a;}
function blend(c,x,y,r,g,b,a){if(x<0||x>=c.w||y<0||y>=c.h)return;const i=(y*c.w+x)*4;const al=a/255,bl=1-al;c.px[i]=Math.round(c.px[i]*bl+r*al);c.px[i+1]=Math.round(c.px[i+1]*bl+g*al);c.px[i+2]=Math.round(c.px[i+2]*bl+b*al);c.px[i+3]=Math.min(255,c.px[i+3]+a);}
function save(c,name){fs.writeFileSync(require('path').join(OUT,name),makePNG(c.px,c.w,c.h));console.log('Saved:',name);}

// ── RESSURGÊNCIA (on-kill: cura + burst de velocidade) ──
{
    const c = newCanvas(32, 32, [13, 13, 26, 255]);

    // Fundo: brilho verde-dourado
    for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
        const d = Math.sqrt((x-15.5)**2 + (y-15.5)**2);
        if (d < 15) blend(c, x, y, 20, 140, 60, Math.floor(22 * (1 - d/15)));
    }

    // Anéis verdes (pulso de energia)
    for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
        const d = Math.sqrt((x-15.5)**2 + (y-15.5)**2);
        if (Math.abs(d - 12) < 0.85) blend(c, x, y, 40, 200, 100, 130);
        if (Math.abs(d - 9)  < 0.85) blend(c, x, y, 40, 200, 100,  70);
    }

    // Seta para cima (ressurgir) — dourada
    // Cabeça da seta (triângulo)
    set(c, 15, 6, 230, 190, 50); set(c, 16, 6, 230, 190, 50);
    for (let i = 0; i <= 3; i++) {
        for (let x = 14-i; x <= 17+i; x++) set(c, x, 7+i, 210, 170, 40);
    }
    // Shaft
    for (let y = 11; y <= 22; y++) {
        set(c, 13, y, 200, 158, 35);
        set(c, 14, y, 210, 168, 45);
        set(c, 15, y, 220, 178, 55);
        set(c, 16, y, 220, 178, 55);
        set(c, 17, y, 210, 168, 45);
        set(c, 18, y, 200, 158, 35);
    }
    // Highlight no eixo da seta
    for (let y = 11; y <= 22; y++) set(c, 15, y, 245, 210, 100);
    set(c, 15, 6, 255, 235, 120);

    // Sombra/base da seta
    for (let x = 13; x <= 18; x++) set(c, x, 22, 160, 120, 20);

    // Faíscas verdes nos lados
    for (const [fx, fy, fa] of [
        [9, 9, 160], [22, 9, 160], [8, 17, 120], [23, 17, 120],
        [10, 24, 100], [21, 24, 100],
    ]) blend(c, fx, fy, 60, 230, 110, fa);

    save(c, 'ressurgencia_icon.png');
}
