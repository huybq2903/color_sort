// Một lệnh: đổi Canvas sang camera -> chụp -> so với ảnh Figma -> ghi diff + crop phóng to -> trả Canvas về Overlay.
// Thay cho chuỗi 4 lượt gọi tool (build / screenshot / re-encode / diff).
//
//   node verify.js <ref.png> [--port 8091] [--zoom x,y,w,h[,scale]]...
//
// ref.png = ảnh get_screenshot của node gốc, phải đúng kích thước canvas (1080x1920).
const path = require('path');
const { decode, encode, at, zoom } = require(path.join(__dirname, 'png.js'));

const args = process.argv.slice(2);
const REF = args[0];
if (!REF) { console.error('thiếu ref.png'); process.exit(1); }
const PORT = (args.includes('--port') ? args[args.indexOf('--port') + 1] : '8091');
const ZOOMS = args.reduce((a, v, i) => (args[i - 1] === '--zoom' ? a.concat([v.split(',').map(Number)]) : a), []);
const BASE = `http://127.0.0.1:${PORT}/skill/`;
const OUT = process.cwd();
const SHOT = 'Assets/Screenshots/_verify.png';

async function call(s, b) {
  const j = await (await fetch(BASE + s, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(b) })).json();
  if (j.status !== 'success') throw new Error(s + ': ' + JSON.stringify(j).slice(0, 300));
  return j;
}
const setProp = (propertyName, rest) => call('component_set_property', { name: '[Canvas]', componentType: 'Canvas', propertyName, ...rest });

(async () => {
  await setProp('renderMode', { value: 'ScreenSpaceCamera' });
  await setProp('worldCamera', { referenceName: '[Main Camera]' });
  await setProp('planeDistance', { value: '10' });
  const ref = decode(REF);
  await call('camera_screenshot', { name: '[Main Camera]', savePath: SHOT, width: ref.w, height: ref.h, maxDimension: 2000 });
  await setProp('renderMode', { value: 'ScreenSpaceOverlay' });
  await setProp('worldCamera', { referenceName: '' });

  const uni = decode(path.join('D:/UnityProjects/BusRush', SHOT));
  const W = Math.min(ref.w, uni.w), H = Math.min(ref.h, uni.h);
  const out = Buffer.alloc(W * H * 4);
  const rows = new Array(H).fill(0), cols = new Array(W).fill(0);
  let bad = 0, tot = 0, sum = 0;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const a = at(ref, x, y), b = at(uni, x, y), i = (y * W + x) * 4;
    if (a[3] < 200) { out[i] = out[i + 1] = out[i + 2] = 30; out[i + 3] = 255; continue; }
    tot++;
    const d = Math.max(Math.abs(a[0] - b[0]), Math.abs(a[1] - b[1]), Math.abs(a[2] - b[2]));
    sum += d;
    if (d > 45) { bad++; rows[y]++; cols[x]++; out[i] = 255; out[i + 3] = 255; }
    else { out[i] = a[0]; out[i + 1] = a[1]; out[i + 2] = a[2]; out[i + 3] = 255; }
  }
  encode(path.join(OUT, 'diff.png'), W, H, out);
  const top = (arr, tag) => arr.map((v, i) => [i, v]).sort((p, q) => q[1] - p[1]).slice(0, 6)
    .filter(p => p[1] > 0).map(p => tag + p[0] + ':' + p[1]).join(' ');
  console.log(`lệch >45: ${bad}/${tot} = ${(100 * bad / tot).toFixed(2)}%   TB ${(sum / tot).toFixed(2)}/255`);
  console.log('hàng lệch nhiều nhất: ' + top(rows, 'y'));
  console.log('cột lệch nhiều nhất: ' + top(cols, 'x'));

  ZOOMS.forEach(([x, y, w, h, s = 2], n) => {
    const a = zoom(ref, x, y, w, h, s), b = zoom(uni, x, y, w, h, s);
    const px = Buffer.alloc(a.w * (a.h * 2 + 12) * 4);
    px.fill(40);
    for (let i = 3; i < px.length; i += 4) px[i] = 255;
    a.px.copy(px, 0);
    b.px.copy(px, a.w * (a.h + 12) * 4);
    encode(path.join(OUT, `zoom${n}.png`), a.w, a.h * 2 + 12, px);
    console.log(`zoom${n}.png  @${x},${y} ${w}x${h} x${s}  (trên = figma, dưới = unity)`);
  });
})();
