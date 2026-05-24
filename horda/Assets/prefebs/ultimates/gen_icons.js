// Generates 6 ultimate ability 32x32 PNG icons without external dependencies
const fs   = require('fs');
const zlib = require('zlib');
const path = require('path');

const OUT = __dirname;

// ── PNG writer ──────────────────────────────────────────────────────────────
function crc32(buf) {
    const t = [];
    for (let i = 0; i < 256; i++) {
        let c = i;
        for (let j = 0; j < 8; j++) c = (c & 1) ? 0xEDB88320 ^ (c >>> 1) : c >>> 1;
        t[i] = c;
    }
    let c = 0xFFFFFFFF;
    for (let i = 0; i < buf.length; i++) c = t[(c ^ buf[i]) & 0xFF] ^ (c >>> 8);
    return (c ^ 0xFFFFFFFF) >>> 0;
}
function chunk(type, data) {
    const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
    const t   = Buffer.from(type);
    const crcBuf = Buffer.alloc(4);
    crcBuf.writeUInt32BE(crc32(Buffer.concat([t, data])));
    return Buffer.concat([len, t, data, crcBuf]);
}
function makePNG(pixels, w, h) {
    // pixels: Uint8Array of RGBA values (w*h*4 bytes)
    const raw = [];
    for (let y = 0; y < h; y++) {
        raw.push(0); // filter type: none
        for (let x = 0; x < w; x++) {
            const i = (y * w + x) * 4;
            raw.push(pixels[i], pixels[i+1], pixels[i+2], pixels[i+3]);
        }
    }
    const compressed = zlib.deflateSync(Buffer.from(raw));
    const ihdr = Buffer.alloc(13);
    ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4);
    ihdr[8]=8; ihdr[9]=6; // 8-bit RGBA
    ihdr[10]=0; ihdr[11]=0; ihdr[12]=0;
    return Buffer.concat([
        Buffer.from([137,80,78,71,13,10,26,10]),
        chunk('IHDR', ihdr),
        chunk('IDAT', compressed),
        chunk('IEND', Buffer.alloc(0))
    ]);
}

// ── Pixel canvas helpers ────────────────────────────────────────────────────
function newCanvas(w, h, bg=[10,10,26,255]) {
    const px = new Uint8Array(w * h * 4);
    for (let i=0;i<w*h;i++) { px[i*4]=bg[0];px[i*4+1]=bg[1];px[i*4+2]=bg[2];px[i*4+3]=bg[3]; }
    return { px, w, h };
}
function set(c, x, y, r,g,b,a=255) {
    if (x<0||x>=c.w||y<0||y>=c.h) return;
    const i=(y*c.w+x)*4; c.px[i]=r;c.px[i+1]=g;c.px[i+2]=b;c.px[i+3]=a;
}
function blend(c, x, y, r,g,b,a) {
    if (x<0||x>=c.w||y<0||y>=c.h) return;
    const i=(y*c.w+x)*4;
    const al=a/255; const bl=1-al;
    c.px[i]  =Math.round(c.px[i]*bl  +r*al);
    c.px[i+1]=Math.round(c.px[i+1]*bl+g*al);
    c.px[i+2]=Math.round(c.px[i+2]*bl+b*al);
    c.px[i+3]=Math.min(255, c.px[i+3]+a);
}
function circle(c, cx,cy, r, col, fill=false) {
    for (let y=cy-r;y<=cy+r;y++) for (let x=cx-r;x<=cx+r;x++) {
        const d=Math.sqrt((x-cx)**2+(y-cy)**2);
        if (fill ? d<=r : Math.abs(d-r)<=0.7) set(c,x,y,...col);
    }
}
function line(c, x0,y0,x1,y1, col, thick=1) {
    const dx=Math.abs(x1-x0), dy=Math.abs(y1-y0);
    const sx=x0<x1?1:-1, sy=y0<y1?1:-1;
    let err=dx-dy, x=x0, y=y0;
    while(true) {
        for(let oy=-Math.floor(thick/2);oy<=Math.floor(thick/2);oy++)
            for(let ox=-Math.floor(thick/2);ox<=Math.floor(thick/2);ox++)
                set(c,x+ox,y+oy,...col);
        if(x===x1&&y===y1) break;
        const e2=2*err;
        if(e2>-dy){err-=dy;x+=sx;}
        if(e2< dx){err+=dx;y+=sy;}
    }
}
function save(c, name) {
    fs.writeFileSync(path.join(OUT, name), makePNG(c.px, c.w, c.h));
    console.log('Saved:', name);
}

// ═══════════════════════════════════════════════════════════════════════════
// 1. TEMPESTADE ELÉTRICA (Lightning Storm)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32);
    // Glow background around bolt
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const d = Math.abs(x-16);
        if(d<8) blend(c,x,y, 255,200,0, Math.floor(18*(1-d/8)));
    }
    // Lightning bolt zigzag path
    const bolt=[
        [16,1],[16,2],[15,3],[15,4],[14,5],[14,6],
        [15,7],[16,8],[17,9],[18,10],[17,11],
        [16,12],[15,13],[14,14],[13,15],[14,16],
        [15,17],[16,18],[17,19],[17,20],[16,21],
        [15,22],[14,23],[15,24],[16,25],[16,26],[15,27],[15,28],[16,29],[16,30]
    ];
    // Outer glow
    for(const [bx,by] of bolt)
        for(let dy=-2;dy<=2;dy++) for(let dx=-2;dx<=2;dx++)
            if(Math.abs(dx)+Math.abs(dy)<=3) blend(c,bx+dx,by+dy, 255,220,0,30);
    // Mid glow
    for(const [bx,by] of bolt)
        for(let dy=-1;dy<=1;dy++) for(let dx=-1;dx<=1;dx++)
            blend(c,bx+dx,by+dy, 255,230,0,120);
    // Core white
    for(const [bx,by] of bolt) { set(c,bx,by, 255,255,255); set(c,bx+1,by, 255,250,180); }
    // Sparks
    const sparks=[[9,9],[23,9],[9,23],[23,23],[16,4],[16,28]];
    for(const [sx,sy] of sparks) {
        set(c,sx,sy, 255,255,0);
        for(const [dx,dy] of [[-1,0],[1,0],[0,-1],[0,1]]) blend(c,sx+dx,sy+dy,255,220,0,180);
    }
    // Corner arcs
    for(const [x,y] of [[2,2],[3,2],[2,3],[29,29],[28,29],[29,28]])
        set(c,x,y, 255,230,100,160);
    save(c,'tempestade_eletrica_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 2. FANTASMA (Ghost)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32);
    // Soft blue inner glow
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const d=Math.sqrt((x-16)**2+(y-16)**2);
        if(d<13) blend(c,x,y, 140,200,255, Math.floor(25*(1-d/13)));
    }
    // Ghost body outline: rounded top (circle head) + wavy bottom
    // Head
    for(let a=0;a<360;a++) {
        const rad=a*Math.PI/180;
        const r=6;
        const gx=Math.round(16+r*Math.cos(rad)), gy=Math.round(11+r*Math.sin(rad));
        blend(c,gx,gy, 180,230,255,200);
    }
    circle(c,16,11, 5, [220,240,255,180], true);
    // Body (trapezoid below head)
    for(let y=11;y<=23;y++) {
        const w=Math.min(6, 5+Math.floor((y-11)/2.5));
        for(let x=16-w;x<=16+w;x++) blend(c,x,y, 200,235,255,160);
    }
    // Wavy bottom edge
    for(let x=10;x<=22;x++) {
        const wave = Math.round(24 + Math.sin((x-10)*Math.PI/2)*1.5);
        set(c,x,wave, 180,220,255,200);
        blend(c,x,wave+1, 180,220,255,100);
    }
    // Eyes (dark)
    for(const [ex,ey] of [[14,10],[18,10]]) {
        set(c,ex,ey, 10,20,50); set(c,ex+1,ey, 10,20,50);
        set(c,ex,ey+1, 10,20,50);
    }
    // Outer ring glow
    circle(c,16,16, 14, [100,180,255,60]);
    circle(c,16,16, 13, [140,200,255,40]);
    save(c,'fantasma_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 3. CHUVA DE METEOROS (Meteor Rain)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32,[8,5,12,255]);
    // Background: faint red-orange sky gradient
    for(let y=0;y<32;y++) for(let x=0;x<32;x++)
        blend(c,x,y, 120,30,0, Math.floor(12*(1-y/32)));
    // Ground impact craters (bottom)
    for(const [cx,cy,r] of [[9,27,3],[20,29,2],[16,26,2]]) {
        for(let a=0;a<360;a++) {
            const rad=a*Math.PI/180;
            blend(c, Math.round(cx+r*Math.cos(rad)), Math.round(cy+r*Math.sin(rad)), 200,80,0,120);
        }
    }
    // Meteors (3 falling at angles)
    const meteors=[
        {x0:24,y0:2,x1:9,y1:27, size:3},
        {x0:20,y0:1,x1:14,y1:20, size:2},
        {x0:29,y0:4,x1:21,y1:22, size:2}
    ];
    for(const m of meteors) {
        const dx=m.x1-m.x0, dy=m.y1-m.y0, len=Math.sqrt(dx*dx+dy*dy);
        // Trail (orange-red fading)
        for(let i=0;i<len;i++) {
            const t=i/len;
            const mx=Math.round(m.x0+dx*t), my=Math.round(m.y0+dy*t);
            const alpha=Math.floor(180*(1-t*0.6));
            const r=255, g=Math.floor(80+t*80), b=0;
            blend(c,mx,my,r,g,b,alpha);
            if(m.size>2) blend(c,mx+1,my,r,g,b,Math.floor(alpha*0.5));
        }
        // Head (bright white core)
        set(c,m.x1,m.y1, 255,255,200);
        for(const [dx2,dy2] of [[-1,0],[1,0],[0,-1],[0,1]])
            blend(c,m.x1+dx2,m.y1+dy2, 255,180,50,200);
    }
    // Impact explosion sparks
    for(const [ex,ey] of [[9,27],[20,29]]) {
        for(const ang of [0,45,90,135,180,225,270,315]) {
            const rad=ang*Math.PI/180;
            blend(c, Math.round(ex+2*Math.cos(rad)), Math.round(ey+2*Math.sin(rad)), 255,150,0,180);
        }
    }
    save(c,'chuva_meteoros_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 4. TSUNAMI (Wave)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32,[5,8,20,255]);
    // Deep water gradient background
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const t=y/32;
        blend(c,x,y, 0,40,120, Math.floor(60*t));
    }
    // Main wave: sinusoidal crest sweeping left→right
    // Wave crest at y≈14 with amplitude
    for(let x=0;x<32;x++) {
        const base = 16 + Math.round(4*Math.sin(x*Math.PI*1.5/32));
        // Water fill below wave
        for(let y=base+1;y<32;y++) blend(c,x,y, 30,100,200, 80);
        // Wave body (3px thick)
        for(let dy=-1;dy<=2;dy++) {
            const a = dy===0?220 : dy===1?160 : dy===-1?120:80;
            const b_val = dy<=0 ? 255 : 200;
            blend(c,x,base+dy, 60,160,b_val, a);
        }
        // Foam/crest (white top)
        set(c,x,base-1, 200,230,255,180);
        set(c,x,base-2, 240,250,255,100);
    }
    // Second smaller wave behind
    for(let x=0;x<32;x++) {
        const base2 = 22 + Math.round(2*Math.sin((x+8)*Math.PI*1.5/32));
        for(let dy=0;dy<=1;dy++) blend(c,x,base2+dy, 40,120,210, 100);
        blend(c,x,base2-1, 180,220,255,80);
    }
    // White foam particles at crest
    for(const [fx,fy] of [[4,12],[10,13],[16,11],[22,13],[28,12],[7,14],[19,12],[25,13]])
        set(c,fx,fy, 240,250,255,200);
    // Spray drops above wave
    for(const [sx,sy] of [[6,9],[12,8],[18,9],[24,8],[15,6],[9,10]])
        blend(c,sx,sy, 180,230,255,150);
    save(c,'tsunami_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 5. CLONE SOMBRA (Shadow Clone)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32,[8,4,14,255]);
    // Purple aura glow
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const d=Math.sqrt((x-16)**2+(y-16)**2);
        if(d<14) blend(c,x,y, 100,0,160, Math.floor(20*(1-d/14)));
    }

    function drawSilhouette(c, cx, cy, alpha, r,g,b) {
        // Head
        circle(c,cx,cy-7, 4, [r,g,b,alpha], true);
        // Body
        for(let y=cy-3;y<=cy+5;y++) {
            const w=Math.min(4,2+Math.floor((y-(cy-3))/2));
            for(let x=cx-w;x<=cx+w;x++) blend(c,x,y, r,g,b,alpha);
        }
        // Legs
        for(let y=cy+5;y<=cy+9;y++) {
            blend(c,cx-2,y, r,g,b,alpha); blend(c,cx-3,y,r,g,b,Math.floor(alpha*0.8));
            blend(c,cx+2,y, r,g,b,alpha); blend(c,cx+3,y,r,g,b,Math.floor(alpha*0.8));
        }
    }

    // Shadow clone (left, darker/more transparent)
    drawSilhouette(c, 12, 17, 160, 80, 0, 130);
    // Main silhouette (right, brighter)
    drawSilhouette(c, 20, 16, 220, 160, 30, 220);

    // Energy rings
    circle(c,16,16, 14, [140,0,200,80]);
    circle(c,16,16, 10, [180,60,255,60]);

    // Connection line (shadow link)
    for(let x=12;x<=20;x++) blend(c,x,16, 160,0,220,80);

    // Sparkles
    for(const [sx,sy] of [[5,5],[27,5],[5,27],[27,27],[16,3],[16,29],[3,16],[29,16]])
        blend(c,sx,sy, 200,100,255,150);
    save(c,'clone_sombra_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 6. VÓRTICE (Vortex/Wind)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32,[4,12,8,255]);
    // Green inner glow
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const d=Math.sqrt((x-16)**2+(y-16)**2);
        if(d<13) blend(c,x,y, 0,180,80, Math.floor(20*(1-d/13)));
    }
    // Spiral arms (3 arms, 120° apart)
    for(let arm=0;arm<3;arm++) {
        const baseAngle = arm * (2*Math.PI/3);
        for(let step=0;step<60;step++) {
            const t = step/60;
            const r = 1 + t*12;
            const angle = baseAngle + t*Math.PI*2.5;
            const px2=Math.round(16+r*Math.cos(angle));
            const py=Math.round(16+r*Math.sin(angle));
            const alpha=Math.floor(220*(1-t*0.4));
            const g=Math.floor(200+t*55), gb=Math.floor(120+t*80);
            set(c,px2,py, 60,g,gb,alpha);
            blend(c,px2+1,py, 40,180,100, Math.floor(alpha*0.5));
            blend(c,px2,py+1, 40,180,100, Math.floor(alpha*0.5));
        }
    }
    // Center bright core
    circle(c,16,16, 3, [180,255,200,220], true);
    set(c,16,16, 255,255,255);
    // Outer particles (8 dots orbiting)
    for(let i=0;i<8;i++) {
        const a=i*Math.PI/4+0.4;
        const px2=Math.round(16+13*Math.cos(a)), py=Math.round(16+13*Math.sin(a));
        blend(c,px2,py, 100,255,160,200);
        blend(c,px2+1,py, 60,220,120,130);
    }
    // Outer ring
    circle(c,16,16, 14, [60,200,100,100]);
    save(c,'vortice_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 7. DRENAGEM DE VIDA (Life Drain)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32,[13,0,16,255]);

    // Inner magenta glow around center
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const d=Math.sqrt((x-16)**2+(y-16)**2);
        if(d<14) blend(c,x,y, 170,0,80, Math.floor(30*(1-d/14)));
    }

    // 4 drain streams flowing inward from cardinal edges with slight curve
    const streamDefs = [
        {dx:0,  dy:-1, ex:16, ey:2 },  // top
        {dx:0,  dy:1,  ex:16, ey:29},  // bottom
        {dx:-1, dy:0,  ex:2,  ey:16},  // left
        {dx:1,  dy:0,  ex:29, ey:16},  // right
    ];
    for(const s of streamDefs) {
        for(let step=0;step<13;step++) {
            const t = step/12;
            // Straight line from edge toward center with slight spiral offset
            const px2 = Math.round(s.ex + (16-s.ex)*t + s.dy*Math.sin(t*Math.PI)*2);
            const py  = Math.round(s.ey + (16-s.ey)*t - s.dx*Math.sin(t*Math.PI)*2);
            const alpha = Math.floor(200*(1-t*0.3));
            const r = Math.floor(153 + t*102);
            blend(c,px2,py, r,0,68, alpha);
            blend(c,px2+s.dy,py-s.dx, r,0,50, Math.floor(alpha*0.4));
        }
        // Source dot at edge (bright)
        set(c,s.ex,s.ey, 255,0,68);
        blend(c,s.ex+s.dy,s.ey-s.dx, 255,0,68,180);
        blend(c,s.ex-s.dy,s.ey+s.dx, 255,0,68,180);
    }

    // Outer scattered aura dots
    for(const [ax,ay] of [[5,5],[27,5],[5,27],[27,27],[10,4],[22,4],[4,10],[28,10],
                           [4,22],[28,22],[10,28],[22,28]])
        blend(c,ax,ay, 170,0,102,160);

    // Center orb glow layers
    circle(c,16,16, 6, [255,34,85,120], true);
    circle(c,16,16, 4, [255,60,100,180], true);
    circle(c,16,16, 3, [255,34,85,230],  true);
    // Core
    circle(c,16,16, 2, [204,0,51,255],   true);
    set(c,16,16, 255,100,130);
    set(c,15,16, 255,80,110);
    set(c,16,15, 255,80,110);

    // Outer ring
    circle(c,16,16, 13, [153,0,68,90]);

    save(c,'drenagem_vida_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 8. RITUAL DO ANCIÃO (Pentagon ritual zone)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32,[8,2,16,255]);

    // Inner dark purple glow
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const d=Math.sqrt((x-16)**2+(y-16)**2);
        if(d<14) blend(c,x,y, 100,30,180, Math.floor(35*(1-d/14)));
    }

    // Pentagon (5 vertices, top-pointing)
    const verts = [];
    for(let i=0;i<5;i++) {
        const ang = (72*i - 90) * Math.PI / 180;
        verts.push([Math.round(16 + 12*Math.cos(ang)), Math.round(16 + 12*Math.sin(ang))]);
    }
    // Draw pentagon edges
    for(let i=0;i<5;i++) {
        const [x0,y0] = verts[i], [x1,y1] = verts[(i+1)%5];
        line(c,x0,y0,x1,y1, [153,51,255,220], 1);
        blend(c,x0,y0, 200,100,255,200);
        blend(c,x1,y1, 200,100,255,200);
    }
    // Draw lines from center to each vertex (pentagram rays)
    for(const [vx,vy] of verts) {
        line(c,16,16,vx,vy, [120,40,200,130], 1);
    }
    // Rune dots at vertices
    for(const [vx,vy] of verts) {
        set(c,vx,vy, 220,140,255);
        for(const [dx,dy] of [[-1,0],[1,0],[0,-1],[0,1]])
            blend(c,vx+dx,vy+dy, 180,80,255,160);
    }

    // Outer red glow ring (energy building up)
    circle(c,16,16, 13, [200,30,60,70]);
    circle(c,16,16, 12, [180,20,80,50]);

    // Center glowing core
    circle(c,16,16, 3, [180,60,255,200], true);
    set(c,16,16, 255,200,255);
    set(c,15,16, 220,160,255);
    set(c,16,15, 220,160,255);

    // Scattered outer energy particles
    for(const [ax,ay] of [[5,4],[27,4],[4,28],[28,28],[16,2],[16,30],[2,16],[30,16]])
        blend(c,ax,ay, 153,51,255,150);

    save(c,'ritual_anciao_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 9. CORRENTES DO INFERNO (Fire Chains)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32,[16,4,0,255]);

    // Orange-red inner glow
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const d=Math.sqrt((x-16)**2+(y-16)**2);
        if(d<14) blend(c,x,y, 200,60,0, Math.floor(40*(1-d/14)));
    }

    // 8 chains radiating outward
    const angulos8 = [0,45,90,135,180,225,270,315];
    for(const ang of angulos8) {
        const rad = ang * Math.PI / 180;
        const dx = Math.cos(rad), dy = Math.sin(rad);
        // Draw segmented chain with sag
        const segs = 8;
        let prev = [16,16];
        for(let i=1;i<=segs;i++) {
            const t = i/segs;
            const nx = Math.round(16 + dx*t*12);
            const ny = Math.round(16 + dy*t*12 + Math.sin(t*Math.PI)*1.5);
            const alpha = Math.floor(230*(1-t*0.3));
            const r = 255, g = Math.floor(120*(1-t*0.5)), b = 0;
            line(c,prev[0],prev[1],nx,ny, [r,g,b,alpha], 1);
            // Chain link dot
            if(i%2===0) set(c,nx,ny, 255,180,40);
            prev=[nx,ny];
        }
        // End glow dot
        const ex=Math.round(16+dx*12), ey=Math.round(16+dy*12);
        blend(c,ex,ey, 255,100,0,200);
    }

    // Center fiery core
    circle(c,16,16, 4, [255,120,20,200], true);
    circle(c,16,16, 2, [255,220,80,240], true);
    set(c,16,16, 255,255,200);

    // Outer ember particles
    for(const [ax,ay] of [[4,4],[28,4],[4,28],[28,28],[16,2],[16,30],[2,16],[30,16]])
        blend(c,ax,ay, 255,80,0,150);

    // Outer ring
    circle(c,16,16, 13, [200,60,0,80]);

    save(c,'correntes_inferno_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 10. BÊNÇÃO DO ANCIÃO (Healing Totem)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32,[5,4,0,255]);

    // Fundo: brilho dourado suave no centro
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const d=Math.sqrt((x-16)**2+(y-16)**2);
        if(d<15) blend(c,x,y, 255,200,40, Math.floor(35*(1-d/15)));
    }

    // Haste do totem (coluna central)
    for(let y=6;y<=25;y++) {
        set(c,15,y, 180,120,20);
        set(c,16,y, 220,160,30);
        set(c,17,y, 180,120,20);
    }

    // Travessa superior
    for(let x=10;x<=22;x++) {
        set(c,x,9,  200,150,30);
        set(c,x,10, 240,190,50);
    }
    // Travessa média
    for(let x=12;x<=20;x++) {
        set(c,x,15, 200,150,30);
        set(c,x,16, 240,190,50);
    }

    // Orbe dourado no topo (brilhante)
    circle(c,16,5, 4, [255,220,60,200], true);
    circle(c,16,5, 3, [255,240,120,230], true);
    circle(c,16,5, 2, [255,255,200,255], true);
    set(c,16,5, 255,255,255);

    // Runas laterais (pequenos pontos dourados)
    for(const [rx,ry] of [[11,12],[21,12],[11,19],[21,19]])
        blend(c,rx,ry, 255,220,60,200);

    // Base do totem (arco oval)
    for(let x=11;x<=21;x++) {
        blend(c,x,26, 200,150,25,180);
    }
    blend(c,12,27, 200,150,25,120);
    blend(c,20,27, 200,150,25,120);

    // Partículas de cura verdes subindo
    const particulasCura = [[13,21],[19,21],[14,17],[18,17],[12,13],[20,13]];
    for(let i=0;i<particulasCura.length;i++) {
        const [px,py] = particulasCura[i];
        const alpha = Math.floor(180*(1-i*0.12));
        blend(c,px,py, 80,255,100, alpha);
    }

    // Anel de cura externo (verde-dourado)
    circle(c,16,16, 14, [120,220,80,60]);
    circle(c,16,16, 13, [200,200,40,40]);

    save(c,'bencao_anciao_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 11. CASULO DE CRISTAL (Crystal Cocoon)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32,[0,6,14,255]);

    // Brilho central ciano/azul
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const d=Math.sqrt((x-16)**2+(y-16)**2);
        if(d<13) blend(c,x,y, 80,200,255, Math.floor(50*(1-d/13)));
    }

    // Hexágono de cristal (6 arestas)
    const hex = [
        [16,5],[26,10],[26,22],[16,27],[6,22],[6,10]
    ];
    for(let i=0;i<hex.length;i++) {
        const [x0,y0] = hex[i];
        const [x1,y1] = hex[(i+1)%hex.length];
        line(c,x0,y0,x1,y1, [130,220,255,220], 1);
    }

    // Linhas internas cruzando (profundidade do cristal)
    line(c,16,5,16,27,  [100,200,255,100], 1);
    line(c,6,10,26,22,  [100,200,255,90],  1);
    line(c,26,10,6,22,  [100,200,255,90],  1);

    // Anel externo maior (brilho)
    const hexExt = [
        [16,2],[29,8],[29,24],[16,30],[3,24],[3,8]
    ];
    for(let i=0;i<hexExt.length;i++) {
        const [x0,y0] = hexExt[i];
        const [x1,y1] = hexExt[(i+1)%hexExt.length];
        line(c,x0,y0,x1,y1, [60,160,220,80], 1);
    }

    // Núcleo brilhante central
    circle(c,16,16, 4, [200,240,255,200], true);
    circle(c,16,16, 2, [240,255,255,240], true);
    set(c,16,16, 255,255,255);

    // Estilhaços/reflexos nos vértices
    for(const [vx,vy] of hex) {
        blend(c,vx,vy, 200,240,255,230);
        blend(c,vx,vy, 255,255,255,120);
    }

    // Brilhinhos espalhados
    for(const [bx,by] of [[10,8],[22,8],[9,20],[23,20],[16,3],[16,29]])
        blend(c,bx,by, 180,230,255,160);

    save(c,'casulo_cristal_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 12. ESCUDO SÔNICO (Sonic Shield)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32,[0,4,12,255]);

    // Brilho central ciano
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const d=Math.sqrt((x-16)**2+(y-16)**2);
        if(d<14) blend(c,x,y, 100,220,255, Math.floor(40*(1-d/14)));
    }

    // Anéis sônicos concêntricos (3 anéis expandindo)
    circle(c,16,16, 5,  [180,240,255,220]);
    circle(c,16,16, 9,  [140,210,255,160]);
    circle(c,16,16, 13, [100,180,255,100]);

    // Distorção sônica nos anéis (pequenos arcos em 8 direções)
    const dirs8 = [[1,0],[-1,0],[0,1],[0,-1],[1,1],[-1,1],[1,-1],[-1,-1]];
    for(const [dx,dy] of dirs8) {
        const x = Math.round(16 + dx*9), y = Math.round(16 + dy*9);
        blend(c,x,y, 220,250,255,200);
        blend(c,x+dx,y+dy, 200,240,255,120);
    }

    // Raios radiais curtos (efeito de impacto)
    for(let i=0;i<8;i++) {
        const ang = (i/8)*Math.PI*2;
        for(let r=10;r<=13;r++) {
            const px=Math.round(16+Math.cos(ang)*r), py=Math.round(16+Math.sin(ang)*r);
            blend(c,px,py, 200,240,255,180);
        }
    }

    // Núcleo central brilhante (escudo/player)
    circle(c,16,16, 3, [180,230,255,200], true);
    circle(c,16,16, 2, [220,250,255,230], true);
    set(c,16,16, 255,255,255);

    // Brilhinhos nas extremidades dos raios
    for(let i=0;i<8;i++) {
        const ang = (i/8)*Math.PI*2;
        const px=Math.round(16+Math.cos(ang)*13), py=Math.round(16+Math.sin(ang)*13);
        blend(c,px,py, 255,255,255,200);
    }

    // Partículas externas (energia sônica)
    for(const [px,py] of [[4,16],[28,16],[16,4],[16,28],[6,6],[26,6],[6,26],[26,26]])
        blend(c,px,py, 150,210,255,130);

    save(c,'escudo_sonico_icon.png');
}

// ═══════════════════════════════════════════════════════════════════════════
// 13. COLHEITA (Passive — XP harvest)
// ═══════════════════════════════════════════════════════════════════════════
{
    const c = newCanvas(32,32,[5,8,3,255]);

    // Brilho dourado suave de fundo
    for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
        const d=Math.sqrt((x-16)**2+(y-16)**2);
        if(d<14) blend(c,x,y, 200,160,0, Math.floor(35*(1-d/14)));
    }

    // Espiga de trigo (caule vertical)
    line(c,16,26, 16,10, [160,120,20,220], 1);

    // Grãos em pares ao longo do caule (alternados esquerda/direita)
    const graos = [[14,10],[18,12],[14,14],[18,16],[14,18],[18,20]];
    for(const [gx,gy] of graos) {
        blend(c,gx,gy,    220,180,40,230);
        blend(c,gx+1,gy,  240,200,60,200);
        blend(c,gx,gy-1,  200,160,20,160);
    }

    // Ponta da espiga (top)
    set(c,16,8,  240,200,50);
    set(c,16,9,  220,180,40);

    // Folhas curtas saindo do caule
    line(c,16,22, 13,25, [80,160,30,180], 1);
    line(c,16,22, 19,25, [80,160,30,180], 1);

    // Orbe de XP flutuando ao lado (símbolo de ganho de XP)
    circle(c,24,10, 4, [255,220,0,200], true);
    circle(c,24,10, 4, [255,255,100,120]);
    // "+" dentro do orbe
    set(c,24,9,  255,255,255);
    set(c,24,10, 255,255,255);
    set(c,24,11, 255,255,255);
    set(c,23,10, 255,255,255);
    set(c,25,10, 255,255,255);

    // Faíscas de XP ao redor
    for(const [sx,sy] of [[8,8],[9,14],[8,22],[24,20],[27,15]])
        blend(c,sx,sy, 255,220,0,160);

    save(c,'colheita_icon.png');
}

console.log('All icons generated!');
