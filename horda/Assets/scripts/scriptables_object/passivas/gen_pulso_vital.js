const fs = require('fs'), zlib = require('zlib');
const OUT = __dirname;
function crc32(buf){const t=[];for(let i=0;i<256;i++){let c=i;for(let j=0;j<8;j++)c=(c&1)?0xEDB88320^(c>>>1):c>>>1;t[i]=c;}let c=0xFFFFFFFF;for(let i=0;i<buf.length;i++)c=t[(c^buf[i])&0xFF]^(c>>>8);return(c^0xFFFFFFFF)>>>0;}
function chunk(type,data){const l=Buffer.alloc(4);l.writeUInt32BE(data.length);const t=Buffer.from(type);const cr=Buffer.alloc(4);cr.writeUInt32BE(crc32(Buffer.concat([t,data])));return Buffer.concat([l,t,data,cr]);}
function makePNG(px,w,h){const raw=[];for(let y=0;y<h;y++){raw.push(0);for(let x=0;x<w;x++){const i=(y*w+x)*4;raw.push(px[i],px[i+1],px[i+2],px[i+3]);}}const comp=zlib.deflateSync(Buffer.from(raw));const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(w,0);ihdr.writeUInt32BE(h,4);ihdr[8]=8;ihdr[9]=6;return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',comp),chunk('IEND',Buffer.alloc(0))]);}
function newCanvas(w,h,bg=[13,13,26,255]){const px=new Uint8Array(w*h*4);for(let i=0;i<w*h;i++){px[i*4]=bg[0];px[i*4+1]=bg[1];px[i*4+2]=bg[2];px[i*4+3]=bg[3];}return{px,w,h};}
function set(c,x,y,r,g,b,a=255){if(x<0||x>=c.w||y<0||y>=c.h)return;const i=(y*c.w+x)*4;c.px[i]=r;c.px[i+1]=g;c.px[i+2]=b;c.px[i+3]=a;}
function blend(c,x,y,r,g,b,a){if(x<0||x>=c.w||y<0||y>=c.h)return;const i=(y*c.w+x)*4;const al=a/255,bl=1-al;c.px[i]=Math.round(c.px[i]*bl+r*al);c.px[i+1]=Math.round(c.px[i+1]*bl+g*al);c.px[i+2]=Math.round(c.px[i+2]*bl+b*al);c.px[i+3]=Math.min(255,c.px[i+3]+a);}
function circle(c,cx,cy,r,col,fill=false){for(let y=cy-r;y<=cy+r;y++)for(let x=cx-r;x<=cx+r;x++){const d=Math.sqrt((x-cx)**2+(y-cy)**2);if(fill?d<=r:Math.abs(d-r)<=0.7)set(c,x,y,...col);}}
function save(c,name){fs.writeFileSync(require('path').join(OUT,name),makePNG(c.px,c.w,c.h));console.log('Saved:',name);}

// ── PULSO VITAL (drain HP from enemies, heal self) ──
{
    const c = newCanvas(32, 32, [13, 13, 26, 255]);

    // Fundo: brilho carmim
    for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
        const d = Math.sqrt((x-15.5)**2 + (y-15)**2);
        if (d < 15) blend(c, x, y, 160, 10, 20, Math.floor(28 * (1 - d/15)));
    }

    // Anel externo vermelho
    circle(c, 15, 15, 12, [180, 20, 35, 160]);
    circle(c, 15, 15, 11, [140, 15, 25, 100]);

    // Setas apontando para o centro (drenagem)
    // Cima → baixo (apontando p/ centro)
    for (let y = 4; y <= 8; y++) set(c, 15, y, 220, 50, 60);
    set(c, 14, 7, 220, 50, 60); set(c, 16, 7, 220, 50, 60);
    set(c, 13, 8, 220, 50, 60); set(c, 17, 8, 220, 50, 60);
    // Baixo → cima
    for (let y = 22; y <= 26; y++) set(c, 15, y, 220, 50, 60);
    set(c, 14, 23, 220, 50, 60); set(c, 16, 23, 220, 50, 60);
    set(c, 13, 22, 220, 50, 60); set(c, 17, 22, 220, 50, 60);
    // Esquerda → direita
    for (let x = 4; x <= 8; x++) set(c, x, 15, 220, 50, 60);
    set(c, 7, 14, 220, 50, 60); set(c, 7, 16, 220, 50, 60);
    set(c, 8, 13, 220, 50, 60); set(c, 8, 17, 220, 50, 60);
    // Direita → esquerda
    for (let x = 22; x <= 26; x++) set(c, x, 15, 220, 50, 60);
    set(c, 23, 14, 220, 50, 60); set(c, 23, 16, 220, 50, 60);
    set(c, 22, 13, 220, 50, 60); set(c, 22, 17, 220, 50, 60);

    // Coração central vermelho vivo
    const hRows = [
        [11, 11, 13], [11, 17, 19],
        [12,  9, 14], [12, 16, 21],
        [13,  9, 21], [14,  9, 21],
        [15, 10, 20], [16, 11, 19],
        [17, 12, 18], [18, 13, 17],
        [19, 14, 16], [20, 15, 15],
    ];
    for (const [y, x0, x1] of hRows)
        for (let x = x0; x <= x1; x++) set(c, x, y, 230, 40, 55);
    // Highlight
    set(c, 18, 12, 255, 130, 140); set(c, 19, 12, 245, 110, 120);
    // Brilho central (cura)
    set(c, 15, 15, 255, 180, 185); set(c, 15, 16, 255, 160, 170);

    save(c, 'pulso_vital_icon.png');
}
