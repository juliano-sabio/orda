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
function save(c,name){fs.writeFileSync(require('path').join(OUT,name),makePNG(c.px,c.w,c.h));console.log('Saved:',name);}

// Desenha figura humanóide simplificada (correndo para direita)
function figura(c, cx, cy, r, g, b, a) {
    // cabeça
    set(c, cx,   cy,   r, g, b, a);
    set(c, cx+1, cy,   r, g, b, a);
    set(c, cx,   cy+1, r, g, b, a);
    set(c, cx+1, cy+1, r, g, b, a);
    // ombros / torso
    set(c, cx-1, cy+3, r, g, b, a);
    set(c, cx,   cy+3, r, g, b, a);
    set(c, cx+1, cy+3, r, g, b, a);
    set(c, cx+2, cy+3, r, g, b, a);
    set(c, cx,   cy+4, r, g, b, a);
    set(c, cx+1, cy+4, r, g, b, a);
    set(c, cx,   cy+5, r, g, b, a);
    set(c, cx+1, cy+5, r, g, b, a);
    // pernas (posição corrida: abertas)
    set(c, cx-1, cy+7, r, g, b, a);
    set(c, cx,   cy+7, r, g, b, a);
    set(c, cx+2, cy+6, r, g, b, a);
    set(c, cx+3, cy+6, r, g, b, a);
    // pés
    set(c, cx-2, cy+8, r, g, b, a);
    set(c, cx-1, cy+8, r, g, b, a);
    set(c, cx+3, cy+7, r, g, b, a);
    set(c, cx+4, cy+7, r, g, b, a);
}

// ── SOMBRA VELOZ (on-kill speed stacks + trail) ──
{
    const c = newCanvas(32, 32, [13, 13, 26, 255]);

    // Fundo: brilho roxo/azul suave (velocidade)
    for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
        const d = Math.sqrt((x-18)**2 + (y-15)**2);
        if (d < 16) blend(c, x, y, 60, 20, 160, Math.floor(22 * (1 - d/16)));
    }

    // Sombra 2 (mais à esquerda, mais apagada) — roxo escuro
    figura(c, 4, 10, 80, 40, 180, 70);

    // Sombra 1 (meio, semitransparente) — roxo médio
    figura(c, 11, 10, 130, 70, 230, 150);

    // Figura principal (direita, brilhante) — branco-azulado
    figura(c, 18, 10, 220, 210, 255, 255);
    // highlight na cabeça
    set(c, 19, 10, 255, 255, 255);
    set(c, 19, 11, 250, 248, 255);

    // Linhas de velocidade (streaks à direita da figura)
    const streaks = [
        { y: 12, x0: 24, len: 6 },
        { y: 14, x0: 23, len: 7 },
        { y: 16, x0: 24, len: 5 },
        { y: 19, x0: 23, len: 6 },
    ];
    for (const { y, x0, len } of streaks) {
        for (let i = 0; i < len; i++) {
            const alpha = Math.round(220 * (i / (len - 1)));
            blend(c, x0 + i, y, 180, 140, 255, alpha);
        }
    }

    // Pequenas faíscas / estrelinhas no rastro
    for (const [sx, sy, sa] of [
        [7, 9, 120], [13, 9, 160], [9, 20, 100],
        [15, 20, 140], [25, 11, 200], [28, 15, 170],
    ]) blend(c, sx, sy, 200, 160, 255, sa);

    // Brilhinhos nos cantos
    for (const [sx,sy] of [[1,1],[30,1],[1,30],[30,30]])
        blend(c, sx, sy, 160, 100, 255, 90);

    save(c, 'sombra_veloz_icon.png');
}
