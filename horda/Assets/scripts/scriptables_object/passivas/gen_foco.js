const fs = require('fs'), zlib = require('zlib');
const OUT = __dirname;
function crc32(buf){const t=[];for(let i=0;i<256;i++){let c=i;for(let j=0;j<8;j++)c=(c&1)?0xEDB88320^(c>>>1):c>>>1;t[i]=c;}let c=0xFFFFFFFF;for(let i=0;i<buf.length;i++)c=t[(c^buf[i])&0xFF]^(c>>>8);return(c^0xFFFFFFFF)>>>0;}
function chunk(type,data){const l=Buffer.alloc(4);l.writeUInt32BE(data.length);const t=Buffer.from(type);const cr=Buffer.alloc(4);cr.writeUInt32BE(crc32(Buffer.concat([t,data])));return Buffer.concat([l,t,data,cr]);}
function makePNG(px,w,h){const raw=[];for(let y=0;y<h;y++){raw.push(0);for(let x=0;x<w;x++){const i=(y*w+x)*4;raw.push(px[i],px[i+1],px[i+2],px[i+3]);}}const comp=zlib.deflateSync(Buffer.from(raw));const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(w,0);ihdr.writeUInt32BE(h,4);ihdr[8]=8;ihdr[9]=6;return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',comp),chunk('IEND',Buffer.alloc(0))]);}
function newCanvas(w,h,bg=[13,13,26,255]){const px=new Uint8Array(w*h*4);for(let i=0;i<w*h;i++){px[i*4]=bg[0];px[i*4+1]=bg[1];px[i*4+2]=bg[2];px[i*4+3]=bg[3];}return{px,w,h};}
function set(c,x,y,r,g,b,a=255){if(x<0||x>=c.w||y<0||y>=c.h)return;const i=(y*c.w+x)*4;c.px[i]=r;c.px[i+1]=g;c.px[i+2]=b;c.px[i+3]=a;}
function blend(c,x,y,r,g,b,a){if(x<0||x>=c.w||y<0||y>=c.h)return;const i=(y*c.w+x)*4;const al=a/255,bl=1-al;c.px[i]=Math.round(c.px[i]*bl+r*al);c.px[i+1]=Math.round(c.px[i+1]*bl+g*al);c.px[i+2]=Math.round(c.px[i+2]*bl+b*al);c.px[i+3]=Math.min(255,c.px[i+3]+a);}
function save(c,name){fs.writeFileSync(require('path').join(OUT,name),makePNG(c.px,c.w,c.h));console.log('Saved:',name);}

// ── FOCO (próximos ataques são críticos garantidos) ──
{
    const c = newCanvas(32, 32, [13, 13, 26, 255]);

    // Fundo: brilho ciano
    for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
        const d = Math.sqrt((x-15.5)**2 + (y-15.5)**2);
        if (d < 15) blend(c, x, y, 10, 160, 200, Math.floor(25 * (1 - d/15)));
    }

    // Círculo externo (mira)
    for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
        const d = Math.sqrt((x-15.5)**2 + (y-15.5)**2);
        if (Math.abs(d - 11) < 0.85) blend(c, x, y, 40, 210, 240, 180);
        if (Math.abs(d - 8)  < 0.85) blend(c, x, y, 40, 210, 240, 100);
    }

    // Crosshair — linhas finas com gap central
    const COR = [60, 230, 255, 220];
    // Horizontal
    for (let x = 3;  x <= 11; x++) set(c, x, 15, ...COR);
    for (let x = 20; x <= 28; x++) set(c, x, 15, ...COR);
    // Vertical
    for (let y = 3;  y <= 11; y++) set(c, 15, y, ...COR);
    for (let y = 20; y <= 28; y++) set(c, 15, y, ...COR);

    // Marcadores de canto (colchetes)
    // Superior esquerdo
    for (let x = 4; x <= 7; x++) set(c, x,  4, ...COR);
    for (let y = 4; y <= 7; y++) set(c,  4, y, ...COR);
    // Superior direito
    for (let x = 24; x <= 27; x++) set(c, x, 4, ...COR);
    for (let y = 4; y <= 7; y++) set(c, 27, y, ...COR);
    // Inferior esquerdo
    for (let x = 4; x <= 7; x++) set(c, x, 27, ...COR);
    for (let y = 24; y <= 27; y++) set(c, 4, y, ...COR);
    // Inferior direito
    for (let x = 24; x <= 27; x++) set(c, x, 27, ...COR);
    for (let y = 24; y <= 27; y++) set(c, 27, y, ...COR);

    // Ponto central brilhante
    for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++)
        set(c, 15+dx, 15+dy, 255, 255, 255);
    // Brilho amarelo (crítico)
    set(c, 15, 15, 255, 240, 100);

    // Faíscas diagonais (símbolo de crítico)
    set(c, 12, 12, 255, 220, 80); set(c, 11, 11, 220, 190, 60);
    set(c, 19, 12, 255, 220, 80); set(c, 20, 11, 220, 190, 60);
    set(c, 12, 18, 255, 220, 80); set(c, 11, 19, 220, 190, 60);
    set(c, 19, 18, 255, 220, 80); set(c, 20, 19, 220, 190, 60);

    save(c, 'foco_icon.png');
}
