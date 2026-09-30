// Đọc/ghi PNG không cần thư viện ngoài. Đọc được cả colorType 2 (RGB) lẫn 6 (RGBA)
// — screenshot của Unity ra RGB, ảnh Figma ra RGBA.
const fs = require('fs'), zlib = require('zlib');

function decode(file) {
  const b = fs.readFileSync(file);
  const w = b.readUInt32BE(16), h = b.readUInt32BE(20), depth = b[24], ct = b[25];
  if (depth !== 8 || (ct !== 2 && ct !== 6)) throw new Error(`${file}: depth=${depth} colorType=${ct} chưa hỗ trợ`);
  const bpp = ct === 6 ? 4 : 3;
  let o = 8, idat = [];
  while (o < b.length) {
    const len = b.readUInt32BE(o), typ = b.toString('ascii', o + 4, o + 8);
    if (typ === 'IDAT') idat.push(b.subarray(o + 8, o + 8 + len));
    o += 12 + len;
  }
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const stride = w * bpp, lines = Buffer.alloc(h * stride);
  let p = 0;
  for (let y = 0; y < h; y++) {
    const ft = raw[p++], cur = lines.subarray(y * stride, (y + 1) * stride);
    const prev = y ? lines.subarray((y - 1) * stride, y * stride) : null;
    for (let x = 0; x < stride; x++) {
      const rv = raw[p + x], a = x >= bpp ? cur[x - bpp] : 0, bb = prev ? prev[x] : 0;
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
  // chuẩn hoá về RGBA để chỗ gọi khỏi phải phân nhánh
  if (bpp === 4) return { w, h, px: lines };
  const px = Buffer.alloc(w * h * 4);
  for (let i = 0, j = 0; i < w * h; i++, j += 3) {
    px[i * 4] = lines[j]; px[i * 4 + 1] = lines[j + 1]; px[i * 4 + 2] = lines[j + 2]; px[i * 4 + 3] = 255;
  }
  return { w, h, px };
}

const at = (im, x, y) => { const i = (y * im.w + x) * 4; return [im.px[i], im.px[i + 1], im.px[i + 2], im.px[i + 3]]; };

function crc(buf) { let c = ~0; for (const b of buf) { c ^= b; for (let k = 0; k < 8; k++) c = (c >>> 1) ^ (0xEDB88320 & -(c & 1)); } return ~c >>> 0; }
function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type), data]);
  const c = Buffer.alloc(4); c.writeUInt32BE(crc(td));
  return Buffer.concat([len, td, c]);
}
function encode(file, w, h, px) {
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  const raw = Buffer.alloc(h * (w * 4 + 1));
  for (let y = 0; y < h; y++) { raw[y * (w * 4 + 1)] = 0; px.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4); }
  fs.writeFileSync(file, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw, { level: 9 })), chunk('IEND', Buffer.alloc(0))]));
}

// phóng to nguyên khối (nearest), dùng để soi lệch vài px
function zoom(im, x0, y0, w, h, s) {
  const W = w * s, H = h * s, out = Buffer.alloc(W * H * 4);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const sx = x0 + Math.floor(x / s), sy = y0 + Math.floor(y / s);
    const o = (y * W + x) * 4;
    if (sx < 0 || sy < 0 || sx >= im.w || sy >= im.h) { out[o + 3] = 255; continue; }
    const c = at(im, sx, sy);
    out[o] = c[0]; out[o + 1] = c[1]; out[o + 2] = c[2]; out[o + 3] = 255;
  }
  return { w: W, h: H, px: out };
}

module.exports = { decode, encode, at, zoom };
