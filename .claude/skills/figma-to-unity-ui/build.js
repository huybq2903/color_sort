// Harness dựng UI: sửa phần DATA ở dưới rồi `node build.js`.
// `node build.js --rects` = chỉ set lại RectTransform (dùng khi chỉnh toạ độ, không tạo lại object).
const BASE = "http://127.0.0.1:8090/skill/";

async function call(skill, body) {
  const r = await fetch(BASE + skill, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
  const j = await r.json(), res = j.result || j;
  if (j.status !== "success") { console.log("!! " + skill + " " + JSON.stringify(j).slice(0, 400)); return j; }
  if (res.results) {
    const bad = res.results.filter(x => x.success === false);
    console.log(skill + ": " + res.successCount + "/" + res.totalItems + (bad.length ? " FAILED " + JSON.stringify(bad).slice(0, 800) : ""));
  } else console.log(skill + ": " + JSON.stringify(res).slice(0, 160));
  return j;
}
const batch = (skill, items) => call(skill, { items: JSON.stringify(items) });
const hex = h => { const n = parseInt(h.replace("#", ""), 16); return [(n >> 16 & 255) / 255, (n >> 8 & 255) / 255, (n & 255) / 255].map(v => v.toFixed(4)).join(",") + ",1"; };

const create = [], rects = [], props = [], texts = [];

// x,y = toạ độ Figma (gốc trái-trên) so với PARENT
function rect(name, x, y, w, h) {
  rects.push({ name, anchorMinX: 0, anchorMinY: 1, anchorMaxX: 0, anchorMaxY: 1, pivotX: 0, pivotY: 1, anchoredPosX: x, anchoredPosY: -y, sizeDeltaX: w, sizeDeltaY: h });
}
function stretch(name) {
  rects.push({ name, anchorMinX: 0, anchorMinY: 0, anchorMaxX: 1, anchorMaxY: 1, pivotX: 0.5, pivotY: 0.5, anchoredPosX: 0, anchoredPosY: 0, sizeDeltaX: 0, sizeDeltaY: 0 });
}
function img(name, parent, sprite, x, y, w, h) {
  create.push({ type: "Image", name, parent, spritePath: sprite || undefined, width: w, height: h });
  rect(name, x, y, w, h);
}
function sliced(name, ppuMultiplier) {
  props.push({ name, componentType: "Image", propertyName: "type", value: "Sliced" });
  if (ppuMultiplier) props.push({ name, componentType: "Image", propertyName: "pixelsPerUnitMultiplier", value: String(ppuMultiplier) });
}
function box(name, parent, x, y, w, h) {           // container vô hình (không có type "Empty")
  img(name, parent, null, x, y, w, h);
  props.push({ name, componentType: "Image", propertyName: "color", value: "0,0,0,0" });
  props.push({ name, componentType: "Image", propertyName: "raycastTarget", value: "false" });
}
function txt(name, parent, content, x, y, w, h, size, color) {
  const c = color ? hex(color).split(",").map(Number) : [1, 1, 1];
  create.push({ type: "Text", name, parent, text: content, fontSize: size, r: c[0], g: c[1], b: c[2], width: w, height: h });
  rect(name, x, y, w, h);
  texts.push(name);
}

// ======================= DATA =======================
const FONT = "Assets/Falcon/Fonts/BalooDa2-ExtraBold SDF.asset";
const ROOT = "UIPopupX", F = "Frame";

create.push({ type: "Image", name: ROOT, parent: "[Canvas]", width: 1080, height: 1920 });
stretch(ROOT);
props.push({ name: ROOT, componentType: "Image", propertyName: "color", value: "0,0,0,0" });

create.push({ type: "Image", name: "Dim", parent: ROOT, width: 1080, height: 1920 });
stretch("Dim");
props.push({ name: "Dim", componentType: "Image", propertyName: "color", value: "0,0,0,0.65" });

// Frame = khung 1080x1920 canh giữa; mọi con dùng thẳng toạ độ Figma
create.push({ type: "Image", name: F, parent: ROOT, width: 1080, height: 1920 });
rects.push({ name: F, anchorMinX: 0.5, anchorMinY: 0.5, anchorMaxX: 0.5, anchorMaxY: 0.5, pivotX: 0.5, pivotY: 0.5, anchoredPosX: 0, anchoredPosY: 0, sizeDeltaX: 1080, sizeDeltaY: 1920 });
props.push({ name: F, componentType: "Image", propertyName: "color", value: "0,0,0,0" });
props.push({ name: F, componentType: "Image", propertyName: "raycastTarget", value: "false" });

// ... img("PanelBG", F, "Assets/.../bg.png", 17, 284, 1044, 1565); sliced("PanelBG");
// ... txt("TxtTitle", F, "Hello", 100, 200, 880, 90, 60);
// ScrollView: create.push({type:"ScrollView", name:"Scroll", parent:F, width:936, height:1143, vertical:true});
//   -> con tên "Viewport"/"Content"; nhớ set Image của cả hai về alpha 0, và Content
//      pivot(0.5,1) neo top => toạ độ con = figmaXY - (viewportX, viewportY)
// ====================================================

(async () => {
  const mode = process.argv[2] || "";
  if (!mode) {
    await batch("ui_create_batch", create);
    // ui_create_batch KHÔNG resolve được parent tạo trong cùng batch -> reparent riêng (thứ tự = thứ tự vẽ)
    await batch("gameobject_set_parent_batch", create.filter(c => c.parent).map(c => ({ childName: c.name, parentName: c.parent })));
  }
  await batch("ui_set_rect_transform_batch", rects);   // luôn chạy SAU reparent
  if (mode === "--rects") return;

  const tp = [];
  for (const n of texts) tp.push(
    { name: n, componentType: "TextMeshProUGUI", propertyName: "font", assetPath: FONT },
    { name: n, componentType: "TextMeshProUGUI", propertyName: "alignment", value: "Center" },
    { name: n, componentType: "TextMeshProUGUI", propertyName: "textWrappingMode", value: "NoWrap" },
    { name: n, componentType: "TextMeshProUGUI", propertyName: "raycastTarget", value: "false" },
  );
  await batch("component_set_property_batch", props.concat(tp));
})();
