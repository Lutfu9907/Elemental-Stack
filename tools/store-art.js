// Draws the app icon and the Google Play store art for Elemental Stack.
// Run from the project folder:  node tools/store-art.js
// Writes:
//   Assets/Art/AppIcon.png             1024x1024 icon (Unity default icon, also used by iOS later)
//   Assets/Art/AppIconBackground.png   432x432  Android adaptive icon, background layer
//   Assets/Art/AppIconForeground.png   432x432  Android adaptive icon, foreground layer (transparent)
//   docs/store/icon-512.png            512x512  Play Console "App icon"
//   docs/store/feature-graphic.png     1024x500 Play Console "Feature graphic"
// Everything is drawn in code, so there are no image licences to worry about.
const fs = require("fs");
const path = require("path");
const zlib = require("zlib");

// ------------------------------------------------------------------ colours

const rgb = (r, g, b, a = 1) => ({ r, g, b, a });
const mix = (a, b, t) => rgb(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
const scale = (c, k) => rgb(Math.min(1, c.r * k), Math.min(1, c.g * k), Math.min(1, c.b * k), c.a);
const clamp01 = (x) => Math.max(0, Math.min(1, x));
const smooth = (e0, e1, x) => { const t = clamp01((x - e0) / (e1 - e0)); return t * t * (3 - 2 * t); };

// Same element colours as Block.cs
const FIRE = rgb(1, 0.36, 0.24);
const ICE = rgb(0.25, 0.66, 1);
const WIND = rgb(0.3, 0.85, 0.52);
const ELECTRIC = rgb(1, 0.8, 0.15);
const PLAIN = rgb(0.96, 0.94, 0.9);

// Dusk sky: deep violet at the top, warm sunset at the bottom
function sky(p) {
  const top = rgb(0.13, 0.12, 0.36), mid = rgb(0.47, 0.27, 0.6), low = rgb(1, 0.6, 0.45);
  const y = p.y;
  return y > 0.45 ? mix(mid, top, smooth(0.45, 1, y)) : mix(low, mid, smooth(0, 0.45, y));
}

function hash(n) {
  const s = Math.sin(n * 127.1) * 43758.5453;
  return s - Math.floor(s);
}

// Small twinkling stars in the upper part of the sky
function stars(p, count, aspect) {
  let light = 0;
  for (let i = 0; i < count; i++) {
    const sx = hash(i + 1), sy = 0.55 + hash(i + 50) * 0.45;
    const r = 0.003 + hash(i + 99) * 0.005;
    const dx = (p.x - sx) * aspect, dy = p.y - sy;
    const d = Math.sqrt(dx * dx + dy * dy);
    light = Math.max(light, (1 - smooth(r * 0.4, r, d)) * (0.5 + hash(i + 7) * 0.5));
    // four-point sparkle on the brighter ones
    if (hash(i + 13) > 0.8) {
      const arm = Math.max(0, 1 - Math.abs(dx) / (r * 5)) * (Math.abs(dy) < r * 0.25 ? 1 : 0)
                + Math.max(0, 1 - Math.abs(dy) / (r * 5)) * (Math.abs(dx) < r * 0.25 ? 1 : 0);
      light = Math.max(light, arm * 0.7);
    }
  }
  return light;
}

// Soft round cloud made of a few blobs
function cloud(p, cx, cy, size, aspect) {
  let a = 0;
  const blobs = [[0, 0, 1], [-0.9, -0.15, 0.7], [0.9, -0.12, 0.75], [0.35, 0.35, 0.65], [-0.4, 0.3, 0.55]];
  for (const [bx, by, br] of blobs) {
    const dx = (p.x - (cx + bx * size)) * aspect, dy = p.y - (cy + by * size);
    const d = Math.sqrt(dx * dx + dy * dy) / (br * size);
    a = Math.max(a, 1 - smooth(0.85, 1, d));
  }
  return a;
}

// ------------------------------------------------------------------ isometric blocks

// A block seen from above at an angle: a diamond top face and two side faces.
// cx, ty: centre of the top face; w: half width; t: thickness. Units are the image's 0..1 space (y up).
function block(cx, ty, w, t, color, glow) {
  return { cx, ty, w, h: w * 0.5, t, color, glow };
}

// Returns the colour of a block at p, or null when p is outside it
function shadeBlock(b, p, aspect) {
  const x = (p.x - b.cx) * aspect / b.w;   // -1..1 across the block
  const y = p.y;
  if (Math.abs(x) > 1) return null;
  const edge = b.ty - b.h * (1 - Math.abs(x));         // where the top face meets the side
  const topOuter = b.ty + b.h * (1 - Math.abs(x));
  if (y <= topOuter && y >= edge) {
    // top face, lighter towards the back corner, with a bright rim
    const u = (y - edge) / Math.max(1e-6, topOuter - edge);
    const rim = Math.min(Math.abs(Math.abs(x) - 1) * 6, 1 - Math.abs(u * 2 - 1)) ;
    let c = scale(b.color, 1.12 + 0.08 * (1 - u));
    const rimLight = 1 - smooth(0, 0.1, Math.min(1 - Math.abs(x), Math.min(u, 1 - u) * 2));
    c = mix(c, rgb(1, 1, 1), rimLight * 0.45);
    return c;
  }
  if (y < edge && y >= edge - b.t) {
    // side faces; the right one is darker. A soft highlight runs along the top edge.
    const k = x < 0 ? 0.86 : 0.66;
    let c = scale(b.color, k);
    const along = (edge - y) / b.t;
    c = mix(c, scale(b.color, k * 1.15), 1 - smooth(0, 0.25, along));
    c = mix(c, scale(b.color, k * 0.8), smooth(0.6, 1, along));
    // vertical crease where the two side faces meet
    const crease = 1 - smooth(0, 0.02, Math.abs(x));
    return mix(c, scale(b.color, 0.55), crease * 0.35);
  }
  return null;
}

// Glow halo around the element blocks (adds light)
function glowAt(b, p, aspect) {
  if (!b.glow) return 0;
  const dx = (p.x - b.cx) * aspect / (b.w * 1.9);
  const dy = (p.y - (b.ty - b.t * 0.5)) / (b.w * 1.3);
  const d = Math.sqrt(dx * dx + dy * dy);
  return b.glow * Math.pow(Math.max(0, 1 - d), 2.2);
}

// The stacked tower: a plain base and element blocks on top, each slightly cut, the top one still sliding
function tower(cx, baseY, w, t) {
  const blocks = [];
  const levels = [
    [PLAIN, 0, 1, 0],
    [ICE, -0.03, 0.94, 0.55],
    [WIND, 0.02, 0.88, 0.5],
    [FIRE, -0.02, 0.84, 0.6],
    [ELECTRIC, 0.2, 0.84, 0.95],
  ];
  let y = baseY;
  for (const [color, dx, k, glow] of levels) {
    y += t;
    blocks.push(block(cx + dx * w, y, w * k, t, color, glow));
  }
  return blocks;
}

// Blocks are drawn bottom-up, so higher blocks cover the ones below
function drawBlocks(blocks, p, aspect, under) {
  let c = under;
  let light = 0;
  for (const b of blocks) light += glowAt(b, p, aspect);
  // glow first (behind the blocks), tinted with the top block's colour
  if (light > 0) {
    const tint = blocks[blocks.length - 1].color;
    const add = Math.min(light, 1);
    c = rgb(Math.min(1, c.r + tint.r * add * 0.6), Math.min(1, c.g + tint.g * add * 0.6), Math.min(1, c.b + tint.b * add * 0.6), Math.max(c.a, add * 0.8));
  }
  for (const b of blocks) {
    const s = shadeBlock(b, p, aspect);
    if (s) c = s;
  }
  return c;
}

// Motion streaks behind the sliding top block
function streaks(b, p, aspect) {
  let a = 0;
  for (let i = 0; i < 3; i++) {
    const y = b.ty - b.t * (0.2 + i * 0.3);
    const x0 = b.cx - b.w / aspect * (1.25 + i * 0.12);
    const len = b.w / aspect * (0.55 - i * 0.12);
    if (p.x > x0 - len && p.x < x0 && Math.abs(p.y - y) < b.t * 0.07) {
      a = Math.max(a, (p.x - (x0 - len)) / len * 0.9);
    }
  }
  return a;
}

// ------------------------------------------------------------------ scenes

function iconScene(withBackground, zoom, withTower = true) {
  const w = 0.24 * zoom, t = 0.085 * zoom;
  const blocks = tower(0.5 - 0.02 * zoom, 0.5 - 0.27 * zoom, w, t);
  return (p) => {
    let c = withBackground ? sky(p) : rgb(0, 0, 0, 0);
    if (withBackground) {
      const s = stars(p, 26, 1);
      c = mix(c, rgb(1, 1, 1), s * 0.9);
      // warm sun glow low behind the tower
      const dx = p.x - 0.5, dy = p.y - 0.25;
      c = mix(c, rgb(1, 0.85, 0.6), Math.pow(Math.max(0, 1 - Math.sqrt(dx * dx + dy * dy) / 0.55), 2) * 0.5);
    }
    const top = blocks[blocks.length - 1];
    const sk = streaks(top, p, 1);
    if (sk > 0) c = mix(c, rgb(1, 1, 1, 1), sk * 0.75);
    return withTower ? drawBlocks(blocks, p, 1, c) : c;
  };
}

function featureScene() {
  const aspect = 1024 / 500;   // x is stretched by this much relative to y
  const blocks = tower(0.66, 0.08, 0.2, 0.13);
  return (p) => {
    let c = sky(p);
    c = mix(c, rgb(1, 1, 1), stars(p, 70, aspect) * 0.9);
    const dx = (p.x - 0.66) * aspect, dy = p.y - 0.2;
    c = mix(c, rgb(1, 0.85, 0.6), Math.pow(Math.max(0, 1 - Math.sqrt(dx * dx + dy * dy) / 0.9), 2) * 0.55);
    for (const [cx, cy, size, k] of [[0.14, 0.32, 0.08, 0.85], [0.33, 0.16, 0.06, 0.7], [0.92, 0.42, 0.07, 0.8], [0.06, 0.08, 0.05, 0.6]])
      c = mix(c, rgb(1, 0.93, 0.92), cloud(p, cx, cy, size, aspect) * k);
    const top = blocks[blocks.length - 1];
    const sk = streaks(top, p, aspect);
    if (sk > 0) c = mix(c, rgb(1, 1, 1), sk * 0.75);
    // a few loose blocks tumbling on the left, like cut pieces falling away
    const loose = [block(0.2, 0.72, 0.07, 0.05, ICE, 0.4), block(0.36, 0.55, 0.05, 0.035, FIRE, 0.4), block(0.12, 0.5, 0.04, 0.03, WIND, 0.3)];
    c = drawBlocks(loose, p, aspect, c);
    return drawBlocks(blocks, p, aspect, c);
  };
}

// ------------------------------------------------------------------ rendering and PNG

function render(width, height, scene, samples = 3) {
  const out = Buffer.alloc(width * height * 4);
  for (let py = 0; py < height; py++) {
    for (let px = 0; px < width; px++) {
      let r = 0, g = 0, b = 0, a = 0;
      for (let sy = 0; sy < samples; sy++) {
        for (let sx = 0; sx < samples; sx++) {
          const p = { x: (px + (sx + 0.5) / samples) / width, y: 1 - (py + (sy + 0.5) / samples) / height };
          const c = scene(p);
          r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
        }
      }
      const n = samples * samples;
      const i = (py * width + px) * 4;
      const alpha = a / n;
      out[i] = Math.round(clamp01(alpha > 0 ? r / a : 0) * 255);
      out[i + 1] = Math.round(clamp01(alpha > 0 ? g / a : 0) * 255);
      out[i + 2] = Math.round(clamp01(alpha > 0 ? b / a : 0) * 255);
      out[i + 3] = Math.round(clamp01(alpha) * 255);
    }
  }
  return out;
}

const crcTable = (() => {
  const t = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    t[n] = c >>> 0;
  }
  return t;
})();

function crc32(buf) {
  let c = 0xffffffff;
  for (const byte of buf) c = crcTable[(c ^ byte) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const body = Buffer.concat([Buffer.from(type, "ascii"), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(body));
  return Buffer.concat([len, body, crc]);
}

function png(width, height, rgba, keepAlpha) {
  const channels = keepAlpha ? 4 : 3;
  const raw = Buffer.alloc((width * channels + 1) * height);
  for (let y = 0; y < height; y++) {
    raw[y * (width * channels + 1)] = 0;
    for (let x = 0; x < width; x++) {
      for (let c = 0; c < channels; c++)
        raw[y * (width * channels + 1) + 1 + x * channels + c] = rgba[(y * width + x) * 4 + c];
    }
  }
  const header = Buffer.alloc(13);
  header.writeUInt32BE(width, 0);
  header.writeUInt32BE(height, 4);
  header[8] = 8; header[9] = keepAlpha ? 6 : 2; header[10] = 0; header[11] = 0; header[12] = 0;
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk("IHDR", header),
    chunk("IDAT", zlib.deflateSync(raw, { level: 9 })),
    chunk("IEND", Buffer.alloc(0)),
  ]);
}

function write(file, width, height, scene, keepAlpha = false) {
  const full = path.join(__dirname, "..", file);
  fs.mkdirSync(path.dirname(full), { recursive: true });
  fs.writeFileSync(full, png(width, height, render(width, height, scene), keepAlpha));
  console.log("wrote " + file);
}

const only = process.argv[2];
const jobs = {
  icon: () => write("Assets/Art/AppIcon.png", 1024, 1024, iconScene(true, 1)),
  background: () => write("Assets/Art/AppIconBackground.png", 432, 432, iconScene(true, 1, false)),
  // Android crops adaptive icons to the middle two thirds, so the tower is drawn smaller here
  foreground: () => write("Assets/Art/AppIconForeground.png", 432, 432, iconScene(false, 0.68), true),
  store: () => write("docs/store/icon-512.png", 512, 512, iconScene(true, 1)),
  feature: () => write("docs/store/feature-graphic.png", 1024, 500, featureScene()),
};
for (const [name, job] of Object.entries(jobs))
  if (!only || only === name) job();
