// Cắt bỏ ruột ảnh 9-slice + ghi spriteBorder vào .meta.
// Biên lấy từ bán kính bo góc trong Figma (chuẩn xác), không dò pixel.
const fs = require('fs'), zlib = require('zlib'), path = require('path');
function decode(file) {
  const b = fs.readFileSync(file);
  const w = b.readUInt32BE(16), h = b.readUInt32BE(20), depth = b[24], ct = b[25];
  if (depth !== 8 || ct !== 6) throw new Error(`unsupported depth=${depth} colorType=${ct}`);
  let o = 8, idat = [];
  while (o < b.length) {
    const len = b.readUInt32BE(o), typ = b.toString('ascii', o + 4, o + 8);
    if (typ === 'IDAT') idat.push(b.slice(o + 8, o + 8 + len));
    o += 12 + len;
  }
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const bpp = 4, stride = w * bpp;
  const out = Buffer.alloc(h * stride);
  let p = 0;
  for (let y = 0; y < h; y++) {
    const ft = raw[p++];
    const cur = out.subarray(y * stride, (y + 1) * stride);
    const prev = y ? out.subarray((y - 1) * stride, y * stride) : null;
    for (let x = 0; x < stride; x++) {
      const rv = raw[p + x];
      const a = x >= bpp ? cur[x - bpp] : 0;
      const bb = prev ? prev[x] : 0;
      const c = prev && x >= bpp ? prev[x - bpp] : 0;
      let v;
      switch (ft) {
        case 0: v = rv; break;
        case 1: v = rv + a; break;
        case 2: v = rv + bb; break;
        case 3: v = rv + ((a + bb) >> 1); break;
        case 4: {
          const pa = Math.abs(bb - c), pb = Math.abs(a - c), pc = Math.abs(a + bb - 2 * c);
          v = rv + (pa <= pb && pa <= pc ? a : pb <= pc ? bb : c); break;
        }
        default: throw new Error('filter ' + ft);
      }
      cur[x] = v & 0xff;
    }
    p += stride;
  }
  return { w, h, px: out };
}

const DIR = 'D:/UnityProjects/BusRush/Assets/_Game/OutGame/Events/EDailyLogin/Sprites/';
const KEEP = 4; // px đệm cho vùng kéo giãn

// file -> [L, R, T, B]  (radius Figma + padding do shadow lúc export)
const BORDERS = {
  'panel_outer.png': [124, 124, 124, 138], // radius 120, pad 4/4/4/18
  'panel_inner.png': [110, 110, 110, 110], // radius 104, pad 6
  'list_bg.png':     [53,  53,  53,  53],  // radius 50,  pad 3
};

const crcTable = (() => {
  const t = new Int32Array(256);
  for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; t[n] = c; }
  return t;
})();
const crc32 = buf => { let c = -1; for (const b of buf) c = crcTable[(c ^ b) & 0xff] ^ (c >>> 8); return (c ^ -1) >>> 0; };

function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}

function encode(w, h, rgba) {
  const stride = w * 4, raw = Buffer.alloc(h * (stride + 1));
  for (let y = 0; y < h; y++) { raw[y * (stride + 1)] = 0; rgba.copy(raw, y * (stride + 1) + 1, y * stride, (y + 1) * stride); }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4);
  ihdr[8] = 8; ihdr[9] = 6; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', ihdr),
    chunk('IDAT', zlib.deflateSync(raw, { level: 9 })),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

// giữ [0..L+KEEP) và [w-R-KEEP..w) theo mỗi trục, bỏ phần ruột lặp lại
// biên 0 (pill, ảnh chỉ kéo 1 chiều) hoặc biên chiếm hết ảnh -> giữ nguyên trục đó
function crop(img, [L, R, T, B]) {
  const { w, h, px } = img;
  const xs = [], ys = [];
  const all = (n, arr) => { for (let i = 0; i < n; i++) arr.push(i); };
  if (L + R === 0 || L + R + 2 * KEEP >= w) all(w, xs);
  else { for (let x = 0; x < L + KEEP; x++) xs.push(x); for (let x = w - R - KEEP; x < w; x++) xs.push(x); }
  if (T + B === 0 || T + B + 2 * KEEP >= h) all(h, ys);
  else { for (let y = 0; y < T + KEEP; y++) ys.push(y); for (let y = h - B - KEEP; y < h; y++) ys.push(y); }
  const nw = xs.length, nh = ys.length, out = Buffer.alloc(nw * nh * 4);
  for (let j = 0; j < nh; j++) for (let i = 0; i < nw; i++)
    px.copy(out, (j * nw + i) * 4, (ys[j] * w + xs[i]) * 4, (ys[j] * w + xs[i]) * 4 + 4);
  return { w: nw, h: nh, px: out };
}

for (const [file, b] of Object.entries(BORDERS)) {
  const p = path.join(DIR, file);
  const src = decode(p);
  const dst = crop(src, b);
  fs.writeFileSync(p, encode(dst.w, dst.h, dst.px));

  // Unity spriteBorder: x=left, y=bottom, z=right, w=top
  const meta = p + '.meta';
  let m = fs.readFileSync(meta, 'utf8');
  const val = `spriteBorder: {x: ${b[0]}, y: ${b[3]}, z: ${b[1]}, w: ${b[2]}}`;
  m = /spriteBorder: \{[^}]*\}/.test(m) ? m.replace(/spriteBorder: \{[^}]*\}/g, val)
                                        : m.replace(/(\n\s*)(spriteBorder|spritePixelsToUnits)/, `$1${val}$1$2`);
  fs.writeFileSync(meta, m);

  const before = src.w * src.h, after = dst.w * dst.h;
  console.log(`${file.padEnd(18)} ${src.w}x${src.h} -> ${dst.w}x${dst.h}   border L${b[0]} R${b[1]} T${b[2]} B${b[3]}   -${(100 - after / before * 100).toFixed(1)}% pixel`);
}
