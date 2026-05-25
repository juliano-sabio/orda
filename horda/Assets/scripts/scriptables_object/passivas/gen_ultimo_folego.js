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
function save(c,name){fs.writeFileSync(require('path').join(OUT,name),makePNG(c.px,c.w,c.h));console.log('Saved:',name);}

// ── ÚLTIMO FÔLEGO (reactive passive: death prevention) ──
{
    const c = newCanvas(32, 32, [13, 13, 26, 255]);

    // Fundo: brilho vermelho/carmim suave (perigo, quase morte)
    for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
        const d = Math.sqrt((x-15.5)**2 + (y-15)**2);
        if (d < 15) blend(c, x, y, 160, 20, 20, Math.floor(30 * (1 - d/15)));
    }

    // Halo dourado externo (passiva ativando)
    circle(c, 15, 15, 13, [200, 160, 40, 130]);
    circle(c, 15, 15, 12, [230, 190, 70,  80]);

    // === CORAÇÃO (corpo) ===
    // shape: bump esquerdo e direito no topo, afina em ponta na base
    const heartRows = [
        [8,  10, 12], [8, 17, 19],      // dois topos dos bumps
        [9,   9, 13], [9, 16, 20],      // bumps mais largos (gap no meio)
        [10,  8, 21],                   // linha cheia
        [11,  8, 21],
        [12,  9, 20],
        [13, 10, 19],
        [14, 11, 18],
        [15, 12, 17],
        [16, 13, 16],
        [17, 14, 15],
        [18, 15, 15],  // ponta
    ];

    for (const seg of heartRows) {
        if (seg.length === 3) {
            const [y, x0, x1] = seg;
            for (let x = x0; x <= x1; x++) set(c, x, y, 210, 35, 45);
        }
    }

    // Highlight (canto superior do bump direito)
    set(c, 18,  9, 255, 130, 140);
    set(c, 17,  9, 245, 110, 120);
    set(c, 18, 10, 240, 105, 115);
    set(c, 19, 10, 230,  95, 105);

    // Sombra na base do coração
    for (const [y, x0, x1] of [[15,12,17],[16,13,16],[17,14,15],[18,15,15]])
        for (let x = x0; x <= x1; x++) blend(c, x, y, 100, 10, 15, 120);

    // Contorno escuro do coração
    for (const seg of heartRows) {
        if (seg.length === 3) {
            const [y, x0, x1] = seg;
            set(c, x0, y, 80, 10, 15);
            set(c, x1, y, 80, 10, 15);
        }
    }
    // topo dos bumps
    for (let x = 10; x <= 12; x++) set(c, x, 8, 80, 10, 15);
    for (let x = 17; x <= 19; x++) set(c, x, 8, 80, 10, 15);

    // === RACHA / CRACK (simboliza quase morte) ===
    // Linha diagonal do topo-centro descendo para baixo-esquerda
    const crack = [
        [9, 14], [10, 14], [10, 15],
        [11, 15], [11, 14], [12, 14],
        [13, 15], [14, 15], [14, 16],
    ];
    for (const [y, x] of crack) set(c, x, y, 60, 5, 8);
    // rachinha secundária saindo para direita
    set(c, 15, 11, 60, 5, 8);
    set(c, 16, 12, 60, 5, 8);

    // === BRILHO DOURADO (passiva salva) — pequenas faíscas ===
    // Faíscas em volta do coração
    for (const [fx, fy, alpha] of [
        [7, 9, 160], [22, 9, 160], [6, 14, 120], [23, 14, 120],
        [9, 20, 100], [21, 20, 100], [14, 4, 140], [17, 4, 140],
    ]) blend(c, fx, fy, 255, 210, 60, alpha);

    // Brilhinhos nos cantos
    for (const [sx, sy] of [[1,1],[30,1],[1,30],[30,30]])
        blend(c, sx, sy, 220, 170, 50, 100);

    save(c, 'ultimo_folego_icon.png');
}
