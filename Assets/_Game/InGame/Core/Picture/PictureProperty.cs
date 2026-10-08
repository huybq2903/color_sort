using System.Collections.Generic;
using Falcon.Shared.BaseInGame;
using Newtonsoft.Json;

namespace Falcon.InGame.Core
{
    /// <summary>Tranh mosaic: các mảnh đa giác, mỗi mảnh 1 colorId.</summary>
    [PropertyDataType("picture")]
    public class PictureProperty : PropertyData
    {
        public int width, height;
        public int unit = 1; // toạ độ điểm = số nguyên theo 1/unit của lưới width×height
        public List<RegionData> regions = new();
        [JsonProperty(ItemConverterType = typeof(DeltaIntArrayConverter))]
        public List<int[]> lines = new(); // nét đen trang trí (polyline mở, cùng toạ độ points), không phải mảnh
        public List<int> lineWidths = new(); // độ dày mỗi nét trang trí (đơn vị như points); 0 = dày bằng viền
        public List<int> lineThickness = new(); // độ dày mỗi nét theo % độ dày viền; 0 = mặc định (chấm dùng lineWidths)
        public List<string> lineIds = new(); // id mỗi nét (song song với lines); level cũ chưa có thì EnsureIds tự sinh
        public GenSettings gen = new();

        // Kẹp mọi điểm vào khung chữ nhật của tranh, mảnh không bao giờ tràn ra ngoài
        public void ClampToFrame()
        {
            int w = width * unit, h = height * unit;
            void Clamp(int[] a)
            {
                if (a == null) return;
                for (var k = 0; k + 1 < a.Length; k += 2)
                {
                    a[k] = System.Math.Clamp(a[k], 0, w);
                    a[k + 1] = System.Math.Clamp(a[k + 1], 0, h);
                }
            }
            foreach (var r in regions)
            {
                Clamp(r.points);
                r.holes?.ForEach(Clamp);
            }
            lines?.ForEach(Clamp);
        }

        // Gán id cho mảnh và nét còn thiếu (level cũ, mảnh mới tách); giữ nguyên id đã có
        public void EnsureIds()
        {
            var used = new HashSet<string>();
            var next = 1;
            string Fresh(string prefix)
            {
                string id;
                do id = prefix + next++; while (used.Contains(id));
                used.Add(id);
                return id;
            }
            foreach (var r in regions) if (!string.IsNullOrEmpty(r.id)) used.Add(r.id);
            lineIds ??= new List<string>();
            foreach (var id in lineIds) if (!string.IsNullOrEmpty(id)) used.Add(id);
            foreach (var r in regions) if (string.IsNullOrEmpty(r.id)) r.id = Fresh("r");
            var count = lines?.Count ?? 0;
            while (lineIds.Count < count) lineIds.Add(null);
            if (lineIds.Count > count) lineIds.RemoveRange(count, lineIds.Count - count);
            for (var i = 0; i < lineIds.Count; i++) if (string.IsNullOrEmpty(lineIds[i])) lineIds[i] = Fresh("l");
        }

        public PictureProperty Clone() => new()
        {
            width = width,
            height = height,
            unit = unit,
            gen = gen.Clone(),
            lines = lines?.ConvertAll(l => (int[])l.Clone()),
            lineWidths = lineWidths != null ? new List<int>(lineWidths) : null,
            lineThickness = lineThickness != null ? new List<int>(lineThickness) : null,
            lineIds = lineIds != null ? new List<string>(lineIds) : null,
            regions = regions.ConvertAll(r => new RegionData { id = r.id, points = (int[])r.points?.Clone(), colorId = r.colorId, value = r.value, valueManual = r.valueManual, holes = r.holes?.ConvertAll(h => (int[])h.Clone()), widths = (int[])r.widths?.Clone(), holeWidths = r.holeWidths?.ConvertAll(h => (int[])h.Clone()) }),
        };
    }

    /// <summary>1 mảnh: polygon đơn CCW trên lưới góc pixel.</summary>
    public class RegionData : EntityData
    {
        [JsonConverter(typeof(DeltaIntArrayConverter))]
        public int[] points;
        public int colorId;
        public int value; // giá trị cát của mảnh (50..300, bậc 50), tính từ diện tích
        public bool valueManual; // true = người dùng đặt tay: không bị tính lại theo diện tích
        [JsonProperty(ItemConverterType = typeof(DeltaIntArrayConverter))]
        public List<int[]> holes; // lỗ trong mảnh (polygon đơn mỗi lỗ, cùng toạ độ với points); null = không lỗ
        [JsonConverter(typeof(DeltaIntArrayConverter), 1)]
        public int[] widths; // độ dày nét viền tại mỗi đỉnh (đơn vị như points); null hoặc lệch số đỉnh = dày mặc định, 0 = không đo được
        [JsonProperty(ItemConverterType = typeof(DeltaIntArrayConverter), ItemConverterParameters = new object[] { 1 })]
        public List<int[]> holeWidths; // như widths cho từng lỗ
    }

    /// <summary>Tham số lần generate cuối, lưu kèm level để chỉnh tiếp.</summary>
    public class GenSettings
    {
        public int workSize = 640;
        public int maxColors = ColorPalette.Count;
        public int minArea = 200;
        public int maxArea = 2000;
        public int smooth = 5;
        public int darkThreshold = 60;
        public bool autoDark = true; // tự chọn ngưỡng nét đen cho từng ảnh (chỉ chế độ biên); darkThreshold lưu giá trị đã chọn
        public int mode = 1; // 0 = tách theo màu (cũ), 1 = theo biên SLIC
        public int targetPieces = 45;
        public int paletteSize = 12;
        public bool bgRays = true;
        public int splitMode = 1;
        public int smoothScale = 3;
        public bool snapEdges = true; // nắn đường cắt về cạnh thật của ảnh gốc
        public bool tidy = true; // vector hoá đường viền: biên chung, spline, toạ độ mịn gấp 4
        public float fitTolerance = 1f; // dung sai khớp Bézier (đơn vị lưới); lớn = đường cong dài hơn, 0 = tắt
        public float curveSmooth = 3f; // 0 = tắt làm mượt viền; 1..4 mượt dần
        public int frameW = 100, frameH = 100; // khung tranh (tỉ lệ W:H), đầu ra luôn phủ kín khung
        public bool fitCover = true; // false = ảnh nằm gọn trong khung, phần dư là nền; true = phủ kín khung, cắt phần thừa
        public int bgColorId = -1; // màu nền phần dư/trong suốt; -1 = tự chọn
        public bool inkGaps; // các mảnh cách nhau bằng khe là nét chì: view vẽ nền chì phía sau và viền mảnh
        public string sourceName;

        public GenSettings Clone() => (GenSettings)MemberwiseClone();
    }
}
