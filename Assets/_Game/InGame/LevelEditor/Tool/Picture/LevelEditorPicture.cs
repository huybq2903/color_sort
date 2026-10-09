using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Falcon.InGame.Core;
using Falcon.Shared.BaseLevelEditor;
using Falcon.Shared.Common;
using R3;
using SFB;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falcon.InGame.LevelEditor
{


    // Công cụ đang hiệu lực, suy ra từ chế độ (và kiểu vẽ khi ở chế độ Vẽ)
    public enum PictureTool { Select, Split, AddLine }

    // Điểm neo: vị trí + tay cầm ra/vào (độ lệch so với điểm); tay cầm 0 = góc nhọn
    internal struct PenAnchor
    {
        public Vector3 pos, outH, inH;
    }

    internal enum PenDrag { None, Anchor, HandleOut, HandleIn }

    /// <summary>Handler tranh mosaic: import, generate, công cụ sửa tay, undo qua snapshot command.</summary>
    public class LevelEditorPicture : APropertyDataHandler<PictureProperty>, IEditorManager
    {
        [SerializeField] private PictureView view;

        private const float CutPointSpacing = 0.03f; // world unit giữa 2 điểm của đường cắt
        private const float SnapFraction = 0.012f, AnchorPickPixels = 10f, DefaultBarPixels = 40f; // hút biên trong 1,2% chiều cao tranh; thanh cong mặc định dài 40px
        public const int MinFrame = 8, MaxFrame = 400;
        private const float EditPickPixels = 8f; // bán kính click trúng một biên để sửa (pixel màn hình)

        private Texture2D _source;
        private RectInt? _sourceCrop;
        private Task<PictureProperty> _job;
        private Stopwatch _jobWatch;
        private Task _bg; // việc nền của thao tác sửa tranh (cắt, chia, gộp, xoá, lỗ)
        private Action _bgDone;
        private string _bgText;
        private Stopwatch _bgWatch;
        private SaveCondition _hasPicture;
        private PictureModel _model;
        private LevelEditorInputHandler _input;
        private LevelEditorCommandInvoker _invoker;
        private LevelEditorCameraController _camera;
        private readonly List<IDisposable> _subs = new();

        private bool _dragging;
        private int _splitIndex = -1;
        private readonly List<Vector3> _cutPath = new(); // đường cắt đang kéo (world)
        private readonly List<int> _selected = new(); // các mảnh đang chọn để chia
        private readonly List<PenAnchor> _pen = new(); // điểm neo đã đặt của đường đang vẽ
        private readonly List<Vector3> _preview = new();
        private readonly List<Vector3> _markers = new();
        private bool _pressing;
        private int _sel = -1, _editLine = -1; // điểm neo đang chọn (hiện tay cầm)
        private EdgeRun _editRun; // đoạn biên đang sửa bằng bút (chế độ Chọn)
        private bool _runDirty; // đoạn biên đang sửa khác với lúc nạp
        private PenState _penBefore, _penOriginal; // bút trước thao tác đang làm; bút lúc nạp đoạn biên
        private readonly List<int> _hiddenOutlines = new(); // mảnh đang bị ẩn viền cũ của đoạn đang sửa
        private bool _tailShown; // đang hiện đoạn xem trước nối từ điểm cuối tới con trỏ (giữ Shift)
        private bool _boxing, _boxShift; // đang kéo khung chọn bằng chuột trái
        private Vector2 _boxStartPic, _boxStartScreen;
        private const float BoxPixels = 6f; // kéo ít hơn thế thì coi là click
        private int _selectedLine = -1; // nét trang trí đang chọn (Delete để xoá)
        private PenDrag _penDrag;

        public event Action Generated; // tranh vừa tạo xong (kết quả Generate đã áp vào level)

        public GenSettings Settings { get; private set; } = new();
        public bool HasSource => _source;
        public Texture2D SourceTexture => _source;
        public bool IsBusy => _job != null || _bg != null;
        public string BusyText => _bg != null ? $"{_bgText} ({_bgWatch.ElapsedMilliseconds / 1000}s)" : $"Đang tạo tranh ({(_jobWatch?.ElapsedMilliseconds ?? 0) / 1000}s)";
        public PictureProperty Picture => _propertyData;
        public bool ShowColor { get; private set; } = true;
        public bool ShowGlass { get; private set; } = true;
        public bool ShowValues { get; private set; } = true;
        public float SourceAlpha { get; private set; }
        private bool _splitting;
        public bool Splitting // đang đặt điểm cắt cho mảnh đang chọn (Shift+click khi chọn đúng 1 mảnh)
        {
            get => _splitting;
            private set
            {
                _splitting = value;
                SyncCutBand();
            }
        }
        public int SelectedLine => _selectedLine;
        public int InspectedLine => _selectedLine >= 0 ? _selectedLine : _editLine; // nét đang chọn, hoặc nét đang nạp vào bút để sửa
        public int NewLineThickness { get; set; } // độ dày nét sắp vẽ theo % viền; 0 = mặc định
        public bool IsLineTool => Tool == PictureTool.AddLine;
        public bool IsCutTool => Tool == PictureTool.Split;
        private bool _cutAsHole;
        public bool CutAsHole // đường cắt khép kín khoét thành lỗ trống thay vì tách mảnh con
        {
            get => _cutAsHole;
            set
            {
                _cutAsHole = value;
                SyncCutBand();
            }
        }

        private int _cutThickness = 3;
        public int CutThickness // tranh có khe nét: bề rộng khe của đường cắt (ô lưới)
        {
            get => _cutThickness;
            set
            {
                if (_cutThickness == value) return;
                _cutThickness = value;
                SyncCutBand();
            }
        }

        // Dải khe xem trước chỉ có khi đang tách trên tranh có khe
        private void SyncCutBand() => view.SetCutBand(_splitting && !_cutAsHole && _propertyData != null && _propertyData.gen.inkGaps ? _cutThickness : 0f);
        public PictureTool Tool => Splitting ? PictureTool.Split : PictureTool.Select;
        public int PenAnchorCount => _pen.Count;
        public int SelectedCount => _selected.Count;
        public bool IsSelected(int index) => _selected.Contains(index);
        public int SplitPattern { get; set; } // kiểu chia mảnh (SubdividePattern); 0 = tự động
        public int SplitCount { get; set; } = 2; // số mảnh sau khi chia mảnh đang chọn (mặc định = gợi ý theo diện tích)
        public int SelectedIndex => _selected.Count == 1 ? _selected[0] : -1;
        public int PrimarySelected => _selected.Count > 0 ? _selected[0] : -1; // mảnh chọn đầu tiên (nhiều mảnh: đại diện cho nhóm)
        public int CurrentColorId { get; set; } = 1;

        public void Initialized()
        {
            _hasPicture = new SaveCondition("Chưa có tranh: Import Image rồi Generate",
                () => _propertyData == null || _propertyData.regions.Count == 0);
            LevelEditorManager.Get<LevelEditorSaveLoad>().RegisterSaveCondition(_hasPicture);

            _invoker = LevelEditorManager.Get<LevelEditorCommandInvoker>();
            _camera = LevelEditorManager.Get<LevelEditorCameraController>();
            _input = LevelEditorManager.Get<LevelEditorInputHandler>();
            _subs.Add(_input.PointerDown.Subscribe(OnPointerDown));
            _subs.Add(_input.PointerMove.Subscribe(OnPointerMove));
            _subs.Add(_input.PointerUp.Subscribe(OnPointerUp));
            _subs.Add(_input.HotKeyDown(Key.LeftBracket).Subscribe(_ => StepSelectedValue(-1)));
            _subs.Add(_input.HotKeyDown(Key.RightBracket).Subscribe(_ => StepSelectedValue(1)));
            _subs.Add(_input.HotKeyDown(Key.M).Subscribe(_ => MergeSelected()));
            _subs.Add(_input.HotKeyDown(Key.K).Subscribe(_ => SubdivideSelected()));
            _subs.Add(_input.HotKeyDown(Key.Enter).Subscribe(_ => FinishPen()));
            _subs.Add(_input.HotKeyDown(Key.Escape).Subscribe(_ => OnEscape()));
            _subs.Add(_input.HotKeyDown(Key.Backspace).Subscribe(_ => OnDelete()));
            _subs.Add(_input.HotKeyDown(Key.Delete).Subscribe(_ => OnDelete()));
        }

        protected override void OnLoadLevel(OnLoadLevel data)
        {
            base.OnLoadLevel(data);
            _propertyData?.EnsureIds();
            _job = null; // bỏ kết quả Generate của level cũ
            Settings = _propertyData != null ? _propertyData.gen.Clone() : new GenSettings();
            Settings.splitMode = 1; // luôn cắt cong
            Settings.maxColors = ColorPalette.Count; // luôn dùng đủ bảng màu
            Settings.bgRays = true; // nền cắt tia và vector hoá viền luôn bật, không còn tuỳ chọn
            Settings.tidy = true;
            var d = new GenSettings();
            Settings.curveSmooth = d.curveSmooth; // độ cong mịn và độ dài đường cong luôn ở mức mạnh nhất, không cho chỉnh
            Settings.fitTolerance = d.fitTolerance;
            if (Settings.maxArea < 800 || Settings.maxArea > 10000) Settings.maxArea = new GenSettings().maxArea; // level cũ chưa có giới hạn này
            if (_source) Destroy(_source); // level khác → phải import ảnh lại
            _source = null;
            ShowColor = true;
            SourceAlpha = 0f;
            Splitting = false;
            EndBox();
            CancelPen(); // level khác: bỏ đoạn biên đang sửa của level cũ
            RebuildView();
            FitView();
        }

        public bool ImportImage()
        {
            var ext = new[] { new ExtensionFilter("Image", "png", "jpg", "jpeg") };
            var paths = StandaloneFileBrowser.OpenFilePanel("Import Image", "", ext, false);
            return paths is { Length: > 0 } && !string.IsNullOrEmpty(paths[0]) && ImportImage(paths[0]);
        }

        public bool ImportImage(string path)
        {
            if (IsBusy) return false;
            if (_propertyData != null)
            {
                LevelEditorMainUI.Warn("Đang có tranh: bấm Clear trước khi nạp ảnh mới");
                return false;
            }
            var tex = new Texture2D(2, 2);
            try
            {
                if (!File.Exists(path) || !tex.LoadImage(File.ReadAllBytes(path))) throw new IOException("không giải mã được ảnh");
            }
            catch (Exception e) when (e is IOException or ArgumentException)
            {
                Destroy(tex);
                LevelEditorMainUI.Warn($"Không đọc được ảnh: {path} ({e.Message})");
                return false;
            }
            if (_source) Destroy(_source);
            _source = tex;
            _sourceCrop = PictureGenerator.OpaqueBounds(tex);
            Settings.sourceName = Path.GetFileName(path);
            FitFrameToImage();
            LevelEditorMainUI.Log($"Đã nạp {Settings.sourceName} ({tex.width}x{tex.height})");
            return true;
        }

        // Chạy thuật toán ở luồng nền (chỉ C# thuần, không đụng đối tượng Unity); Update nhận kết quả
        public void Generate()
        {
            if (!_source || IsBusy) return;
            EndDrag();
            var settings = Settings.Clone();
            _jobWatch = Stopwatch.StartNew();
            var snap = new PictureGenerator.SourceImage(_source); // đọc điểm ảnh ở luồng chính; ghép khung và thuật toán chạy nền để popup hiện ngay
            var crop = _sourceCrop;
            _job = Task.Run(() =>
            {
                var px = PictureGenerator.Compose(snap, settings, out var w, out var h, out var bgId, crop);
                return PictureGenerator.Generate(px, w, h, settings, bgId);
            });
        }

        // Chạy work ở luồng nền (chỉ C# thuần), xong thì done chạy ở luồng chính; trong lúc chạy IsBusy = true nên popup chờ hiện và thao tác khác bị chặn
        private void RunBusy<T>(string text, Func<T> work, Action<T> done)
        {
            if (IsBusy) return;
            var baseData = _propertyData;
            var t = Task.Run(work);
            _bg = t;
            _bgText = text;
            _bgWatch = Stopwatch.StartNew();
            _bgDone = () =>
            {
                if (_propertyData != baseData) // vd undo giữa chừng: kết quả không còn khớp dữ liệu hiện tại
                {
                    LevelEditorMainUI.Warn("Dữ liệu đã đổi trong lúc xử lý, bỏ kết quả");
                    return;
                }
                done(t.Result);
            };
        }

        private void FinishBusy()
        {
            var t = _bg;
            var done = _bgDone;
            _bg = null;
            _bgDone = null;
            if (t.IsFaulted)
            {
                UnityEngine.Debug.LogError(t.Exception);
                LevelEditorMainUI.Warn("Lỗi khi xử lý, xem Console");
                return;
            }
            done();
        }

        private void Update()
        {
            if (_bg != null && _bg.IsCompleted) FinishBusy();
            if (_job == null || !_job.IsCompleted) return;
            var job = _job;
            _job = null;
            _jobWatch.Stop();
            if (job.IsFaulted)
            {
                UnityEngine.Debug.LogError(job.Exception);
                LevelEditorMainUI.Warn("Lỗi khi tạo tranh, xem Console");
                return;
            }
            var result = job.Result;
            if (result == null)
            {
                LevelEditorMainUI.Warn("Ảnh không có pixel đặc nào để chia mảnh");
                return;
            }
            ShowColor = true;
            SourceAlpha = 0f; // đã có tranh: ẩn ảnh gốc, kéo slider để đè so sánh
            Execute(result);
            FitView();
            Generated?.Invoke();
            var colors = result.regions.Select(r => r.colorId).Distinct().Count();
            LevelEditorMainUI.Log($"{result.regions.Count} mảnh · {colors} màu · {_jobWatch.ElapsedMilliseconds / 1000}s");
        }

        // Xoá tranh hiện tại (undo được) và ảnh nguồn, để nạp ảnh mới
        public void Clear()
        {
            if (IsBusy) return;
            EndDrag();
            view.SetHighlight(-1);
            if (_source) Destroy(_source);
            _source = null;
            SourceAlpha = 0f;
            Settings.sourceName = null;
            if (_propertyData != null) Execute(null);
            else RefreshSource();
            LevelEditorMainUI.Log("Đã xoá tranh");
        }

        public void SetShowColor(bool on)
        {
            ShowColor = on;
            view.SetAllPainted(on);
        }

        public void SetShowValues(bool on)
        {
            ShowValues = on;
            view.SetShowValues(on);
        }

        public void SetShowGlass(bool on)
        {
            ShowGlass = on;
            view.SetGlass(on);
        }

        public void SetSourceAlpha(float alpha)
        {
            SourceAlpha = alpha;
            RefreshSource();
        }

        private const string CutHint = "Cắt: Shift+click đặt điểm (điểm đầu trên biên hoặc trong mảnh; từ điểm thứ 3, bấm gần điểm đầu để khép kín), kéo đầu thanh cong để uốn · Enter cắt · Esc huỷ · Del xoá điểm";
        private const float ClosePixels = 18f; // từ điểm thứ 3: điểm đặt cách điểm đầu chừng này (pixel màn hình) thì hút vào điểm đầu

        // Điểm cắt đầu tiên: chỉ khi chọn đúng 1 mảnh; bút giữ các điểm cho tới khi Enter hoặc bỏ chọn mảnh
        private void StartCut(LevelEditorPointerEvent e, Vector2 pic)
        {
            Splitting = true;
            _splitIndex = SelectedIndex;
            PressPath(e, pic, -1);
            if (_pen.Count == 0)
            {
                Splitting = false;
                _splitIndex = -1;
                return;
            }
            LevelEditorMainUI.Log(CutHint);
        }

        private Vector3 SnapToStart(Vector3 pos) =>
            Splitting && _pen.Count >= 2 && (Screen2(pos) - Screen2(_pen[0].pos)).magnitude <= ClosePixels ? _pen[0].pos : pos;

        // Đổi trạng thái: bỏ mọi thao tác đang dở (bút, khung chọn, uốn mềm) và nét đang chọn
        private void ResetInteraction()
        {
            EndBox();
            CancelPen();
            _selectedLine = -1;
            view.SetHighlight(-1);
            view.SetLineHighlight(-1);
        }

        public void FitView()
        {
            if (_propertyData != null) _camera?.FitTo(view.WorldBounds);
            else if (_source) _camera?.FitTo(view.FrameBounds);
        }

        // Tổng số cát của các mảnh theo từng màu
        public Dictionary<int, int> ColorSand() =>
            _propertyData == null ? new Dictionary<int, int>() :
            _propertyData.regions.GroupBy(r => r.colorId).ToDictionary(g => g.Key, g => g.Sum(r => r.value));

        private bool IsPathTool => Tool is PictureTool.Split or PictureTool.AddLine || _editRun != null;

        private void OnPointerDown(LevelEditorPointerEvent e)
        {
            if (_model == null || IsBusy) return;
            var pic = view.WorldToPicture(e.WorldPosition);
            var hit = _model.HitTest(pic);
            if (e.Button != 0) return;
            if (Tool == PictureTool.Select) { PressSelect(e, pic); return; }
            if (Tool == PictureTool.Split && (e.Modifiers & KeyModifiers.Shift) == 0 && !OnPenControl(e)) { PressSelect(e, pic); return; }

            switch (Tool)
            {
                case PictureTool.Split:
                case PictureTool.AddLine:
                    PressPath(e, pic, hit);
                    break;
            }
        }

        // Chế độ Chọn: kéo tiếp neo hoặc tay cầm đang sửa; click gần biên của mảnh đã chọn = sửa biên; còn lại là chọn (click hoặc kéo khung, chốt khi thả)
        // Con trỏ đang trên điểm neo hoặc đầu tay cầm của bút?
        private bool OnPenControl(LevelEditorPointerEvent e) =>
            _sel >= 0 && _sel < _pen.Count && HandleHit(e.ScreenPosition) != PenDrag.None || NearestAnchor(e.ScreenPosition) >= 0;

        private void PressSelect(LevelEditorPointerEvent e, Vector2 pic)
        {
            if (_editRun != null)
            {
                if (PressPath(e, pic, -1)) return;
                if (_runDirty) FinishPen();
                else CancelPen();
            }
            var shift = (e.Modifiers & KeyModifiers.Shift) != 0;
            var ctrl = (e.Modifiers & KeyModifiers.Ctrl) != 0;
            if (shift && !ctrl && SelectedIndex >= 0) { StartCut(e, pic); return; } // chọn đúng 1 mảnh: Shift+click đặt điểm cắt
            if (!shift && !ctrl && !Splitting && SelectedIndex >= 0 && BeginEditRun(pic)) return; // Ctrl+click (hoặc Shift khi chưa chọn đúng 1 mảnh) là chọn thêm/bớt
            _boxing = true;
            _boxShift = shift || ctrl;
            _boxStartPic = pic;
            _boxStartScreen = e.ScreenPosition;
            _input.SetPointerCaptured(true);
        }

        // Nhấn trong bút: đầu thanh cong hoặc điểm có sẵn thì kéo nó; Shift+click lên đường thì chèn điểm; Shift+click chỗ trống thì đặt điểm mới; click thường chỗ trống chỉ bỏ chọn điểm
        private bool PressPath(LevelEditorPointerEvent e, Vector2 pic, int hit)
        {
            var shift = (e.Modifiers & KeyModifiers.Shift) != 0;
            _penBefore = PenSnapshot();
            _penDrag = PenDrag.None;
            if (_sel >= 0 && _sel < _pen.Count)
            {
                var h = HandleHit(e.ScreenPosition);
                if (h != PenDrag.None) _penDrag = h;
            }
            if (_penDrag == PenDrag.None)
            {
                var near = NearestAnchor(e.ScreenPosition);
                if (Splitting && shift && near == 0 && _pen.Count >= 2)
                {
                    _pen.Add(new PenAnchor { pos = _pen[0].pos }); // khép kín: điểm cuối trùng điểm đầu
                    _sel = _pen.Count - 1;
                    PushPenChange(_penBefore);
                    return true;
                }
                if (near >= 0) { _sel = near; _penDrag = PenDrag.Anchor; }
                else if (shift && _pen.Count > 1 && InsertOnCurve(e.ScreenPosition)) _penDrag = PenDrag.Anchor;
            }
            if (_penDrag != PenDrag.None)
            {
                _pressing = true;
                _dragging = true;
                _input.SetPointerCaptured(true);
                RefreshPen(null);
                return true;
            }
            if (_editRun != null) return false; // đang sửa biên: không đặt điểm mới ngoài đường
            if (!shift)
            {
                if (_pen.Count == 0 && Tool == PictureTool.AddLine)
                {
                    var li = _model.NearestLine(pic);
                    if (li >= 0) { BeginEditLine(li); return true; } // click thường vào nét đã lưu: nạp để sửa
                }
                _sel = -1;
                RefreshPen(null);
                return true;
            }
            if (_pen.Count == 0 && Tool == PictureTool.Split)
            {
                if (_splitIndex < 0) _splitIndex = SelectedIndex; // xoá hết điểm làm CancelPen reset _splitIndex: lấy lại mảnh đang chọn
                if (_splitIndex < 0) return true;
            }
            var pos = SnapToStart(Place(e, out var onEdge));
            if (Splitting && _pen.Count == 0 && !onEdge && _model.HitTest(view.WorldToPicture(pos)) != _splitIndex)
            {
                LevelEditorMainUI.Warn("Điểm đầu phải nằm trong hoặc trên biên của mảnh đang chọn");
                return true;
            }
            _pen.Add(new PenAnchor { pos = pos });
            _sel = _pen.Count - 1;
            PushPenChange(_penBefore);
            return true;
        }

        private PenState PenSnapshot() => new(_pen, _sel);

        // Ghi một bước của bút vào lịch sử undo/redo nếu các điểm đã đổi so với before
        private void PushPenChange(PenState before)
        {
            var after = PenSnapshot();
            if (before == null || _invoker == null || before.SameAnchors(after))
            {
                RefreshPen(null);
                return;
            }
            _invoker.ExecuteCommand(new PenStateCommand(ApplyPen, before, after));
        }

        // Áp trạng thái bút từ lệnh (thao tác, undo, redo)
        private void ApplyPen(PenState s)
        {
            _pen.Clear();
            _pen.AddRange(s.anchors);
            _sel = s.sel < _pen.Count ? s.sel : -1;
            _pressing = false;
            _penDrag = PenDrag.None;
            if (_editRun != null && _penOriginal != null) _runDirty = !s.SameAnchors(_penOriginal);
            RefreshPen(null);
        }

        // Thanh cong của điểm i: hai đầu (ra, vào); điểm còn thẳng thì hiện thanh ngắn dọc theo hướng hai điểm kề
        private void HandleEnds(int i, out Vector3 outEnd, out Vector3 inEnd)
        {
            var a = _pen[i];
            Vector3 dir;
            if (a.outH != Vector3.zero) dir = a.outH;
            else if (a.inH != Vector3.zero) dir = -a.inH;
            else
            {
                var prev = i > 0 ? _pen[i - 1].pos : a.pos;
                var next = i < _pen.Count - 1 ? _pen[i + 1].pos : a.pos;
                var d = next - prev;
                d.z = 0f;
                dir = d.sqrMagnitude < 1e-10f ? Vector3.right : d.normalized;
                dir *= PixelsToWorld(a.pos, DefaultBarPixels);
            }
            outEnd = a.pos + (a.outH != Vector3.zero ? a.outH : dir);
            inEnd = a.pos + (a.inH != Vector3.zero ? a.inH : (a.outH != Vector3.zero ? -a.outH : -dir));
        }

        private static float PixelsToWorld(Vector3 at, float px)
        {
            var cam = Camera.main;
            if (!cam) return 0.1f;
            var s = cam.WorldToScreenPoint(at);
            return (cam.ScreenToWorldPoint(new Vector3(s.x + px, s.y, s.z)) - at).magnitude;
        }

        private Vector2 Screen2(Vector3 world) => Camera.main ? (Vector2)Camera.main.WorldToScreenPoint(world) : Vector2.zero;

        // Số pixel màn hình của 1 ô lưới tại điểm pic
        private float PixelsPerCell(Vector2 pic)
        {
            var a = Screen2(view.PictureToWorld(pic));
            var b = Screen2(view.PictureToWorld(pic + Vector2.right));
            return (b - a).magnitude;
        }

        private int NearestAnchor(Vector2 screen)
        {
            var best = -1;
            var bestD = AnchorPickPixels * AnchorPickPixels;
            for (var i = 0; i < _pen.Count; i++)
            {
                var d = (Screen2(_pen[i].pos) - screen).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        // Đầu tay cầm của điểm đang chọn trúng con trỏ?
        private PenDrag HandleHit(Vector2 screen)
        {
            HandleEnds(_sel, out var outEnd, out var inEnd);
            var lim = AnchorPickPixels * AnchorPickPixels;
            if ((Screen2(outEnd) - screen).sqrMagnitude < lim) return PenDrag.HandleOut;
            if ((Screen2(inEnd) - screen).sqrMagnitude < lim) return PenDrag.HandleIn;
            return PenDrag.None;
        }

        // Nhấn lên đường đang vẽ: chèn điểm neo mới đúng chỗ đó mà không đổi dáng đường (tách Bézier tại t)
        private bool InsertOnCurve(Vector2 screen)
        {
            var bestD = AnchorPickPixels * AnchorPickPixels;
            int seg = -1;
            var bestT = 0f;
            for (var i = 0; i + 1 < _pen.Count; i++)
            {
                for (var k = 1; k < 24; k++)
                {
                    var t = k / 24f;
                    var d = (Screen2(Bezier(i, t)) - screen).sqrMagnitude;
                    if (d < bestD) { bestD = d; seg = i; bestT = t; }
                }
            }
            if (seg < 0) return false;
            Vector3 p0 = _pen[seg].pos, p1 = p0 + _pen[seg].outH, p3 = _pen[seg + 1].pos, p2 = p3 + _pen[seg + 1].inH;
            var a = Vector3.Lerp(p0, p1, bestT); var b = Vector3.Lerp(p1, p2, bestT); var c = Vector3.Lerp(p2, p3, bestT);
            var d1 = Vector3.Lerp(a, b, bestT); var e1 = Vector3.Lerp(b, c, bestT);
            var f = Vector3.Lerp(d1, e1, bestT);
            var left = _pen[seg]; left.outH = a - p0;
            var right = _pen[seg + 1]; right.inH = c - p3;
            _pen[seg] = left;
            _pen[seg + 1] = right;
            _pen.Insert(seg + 1, new PenAnchor { pos = f, inH = d1 - f, outH = e1 - f });
            _sel = seg + 1;
            return true;
        }

        private Vector3 Bezier(int seg, float t)
        {
            Vector3 p0 = _pen[seg].pos, p1 = p0 + _pen[seg].outH, p3 = _pen[seg + 1].pos, p2 = p3 + _pen[seg + 1].inH;
            var u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }

        // Nạp nét đã có vào đường đang vẽ để sửa; bản cũ ẩn cho tới khi chốt (Esc thì giữ nguyên)
        private void BeginEditLine(int li)
        {
            var l = _propertyData.lines[li];
            var unit = Mathf.Max(1, _propertyData.unit);
            _pen.Clear();
            for (var k = 0; k + 1 < l.Length; k += 2)
            {
                var w = view.PictureToWorld(new Vector2(l[k], l[k + 1]) / unit);
                w.z = 0f;
                _pen.Add(new PenAnchor { pos = w });
            }
            var dot = li < _propertyData.lineWidths.Count && _propertyData.lineWidths[li] > 0 && _pen.Count == 2;
            if (dot) _pen.RemoveAt(1);
            _sel = _pen.Count - 1;
            _editLine = li;
            view.SetLineVisible(li, false);
            view.SetLineHighlight(-1);
            RefreshPen(null);
        }

        // Nạp đoạn biên của mảnh đang chọn vào bút: neo dựng lại bằng Bézier, hai đầu khoá; false nếu click không gần biên nào
        private bool BeginEditRun(Vector2 pic)
        {
            var unit = Mathf.Max(1, _propertyData.unit);
            var radius = EditPickPixels / Mathf.Max(0.01f, PixelsPerCell(pic)); // bán kính chọn tính theo pixel màn hình, không lấn mất việc chọn mảnh
            var hole = EdgeSkeleton.NearestHole(_propertyData, SelectedIndex, pic, radius);
            if (hole >= 0) return StartRunEdit(EdgeSkeleton.HoleRun(_propertyData, SelectedIndex, hole));
            if (!EdgeSkeleton.NearRing(_propertyData, SelectedIndex, pic, radius)) return false; // click xa biên: khỏi tính RunsOf nặng
            var run = EdgeSkeleton.Nearest(EdgeSkeleton.RunsOf(_propertyData, SelectedIndex), pic, unit, radius, _propertyData.gen.inkGaps);
            return StartRunEdit(run);
        }

        // Nạp một đoạn biên (hoặc cả vòng lỗ) vào bút để sửa
        private bool StartRunEdit(EdgeRun run)
        {
            if (run == null) return false;
            var unit = Mathf.Max(1, _propertyData.unit);
            var cubics = EdgeSkeleton.Fit(run, unit);
            if (cubics.Count == 0) return false;
            Vector3 W(Vector2 p)
            {
                var w = view.PictureToWorld(p);
                w.z = 0f;
                return w;
            }
            _pen.Clear();
            for (var i = 0; i <= cubics.Count; i++)
            {
                var a = new PenAnchor { pos = W(i < cubics.Count ? cubics[i].p0 : cubics[i - 1].p3) };
                if (i < cubics.Count) a.outH = W(cubics[i].p1) - a.pos;
                if (i > 0) a.inH = W(cubics[i - 1].p2) - a.pos;
                _pen.Add(a);
            }
            _sel = -1;
            _editRun = run;
            _runDirty = false;
            _penOriginal = PenSnapshot();
            HideRunOutline(run);
            RefreshPen(null);
            LevelEditorMainUI.Log(run.hole >= 0 ? "Sửa lỗ: kéo điểm hoặc đầu thanh cong, Shift+click biên lỗ để thêm điểm, Del xoá điểm · Enter chốt · Esc huỷ" : "Sửa biên: kéo điểm hoặc đầu thanh cong, Shift+click biên để thêm điểm, Del xoá điểm · Enter chốt · Esc huỷ");
            return true;
        }

        // Ẩn viền cũ của đoạn đang sửa ở cả hai mảnh, cho đỡ rối với đường xem trước; CancelPen hiện lại
        private void HideRunOutline(EdgeRun run)
        {
            if (run.hole >= 0) return; // vòng lỗ không nằm trong vòng ngoài của mảnh
            int Find(int[] pts, Vector2Int v)
            {
                for (var i = 0; i + 1 < pts.Length; i += 2) if (pts[i] == v.x && pts[i + 1] == v.y) return i / 2;
                return -1;
            }
            var first = run.pts[0];
            var last = run.pts[run.pts.Count - 1];
            view.HideOutlineRun(run.a, run.lo, run.hi);
            _hiddenOutlines.Add(run.a);
            if (_propertyData.gen.inkGaps) return; // tranh có khe: chỉ sửa biên của chính mảnh này
            var b = _propertyData.regions[run.b].points;
            view.HideOutlineRun(run.b, Find(b, last), Find(b, first)); // vòng của mảnh kề chạy ngược chiều
            _hiddenOutlines.Add(run.b);
        }

        // Vị trí đặt hoặc kéo điểm: hút vào đường biên gần (trừ vẽ nét và sửa biên)
        private Vector3 Place(LevelEditorPointerEvent e, out bool snapped)
        {
            snapped = false;
            var w = e.WorldPosition;
            if (Tool != PictureTool.AddLine && _editRun == null && !(Splitting && _cutAsHole) && _model != null && _propertyData != null
                && _model.NearestBoundaryPoint(view.WorldToPicture(w), SnapFraction * _propertyData.height, out var q, Splitting ? (_splitIndex >= 0 ? _splitIndex : SelectedIndex) : -1))
            {
                snapped = true;
                var v = view.PictureToWorld(q);
                v.z = 0f;
                return v;
            }
            return w;
        }

        private void OnPointerMove(LevelEditorPointerEvent e)
        {
            if (_dragging && Mouse.current != null && !Mouse.current.leftButton.isPressed) EndDrag(); // thả ngoài Game view: mất sự kiện up
            if (_model == null) return;
            var pic = view.WorldToPicture(e.WorldPosition);
            if (_boxing)
            {
                if (Mouse.current != null && !Mouse.current.leftButton.isPressed) EndBox();
                else if ((e.ScreenPosition - _boxStartScreen).sqrMagnitude > BoxPixels * BoxPixels) ShowBox(pic);
                return;
            }
            var hit = _model.HitTest(pic);
            view.SetHighlight(hit);
            if (IsPathTool) { MovePath(e, pic); return; }
            if (!_dragging) return;

        }

        // Vòng lỗ khép kín: điểm đầu và điểm cuối trùng nhau, kéo một bên thì bên kia theo (cả tay cầm)
        private void SyncClosedEnds(int i)
        {
            var last = _pen.Count - 1;
            if (last < 2) return;
            if (i == 0) { var b = _pen[0]; _pen[last] = b; }
            else if (i == last) { var b = _pen[last]; _pen[0] = b; }
        }

        private void MovePath(LevelEditorPointerEvent e, Vector2 pic)
        {
            if (Tool == PictureTool.AddLine && _pen.Count == 0 && !_pressing) view.SetLineHighlight(_model.NearestLine(pic)); // nét có thể nhấn để sửa
            else view.SetLineHighlight(-1);

            if (_pressing && _penDrag != PenDrag.None)
            {
                var a = _pen[_sel];
                if (_penDrag == PenDrag.Anchor)
                {
                    var locked = _editRun != null && _editRun.hole < 0 && (_sel == 0 || _sel == _pen.Count - 1); // điểm nối không dịch (vòng lỗ thì không có điểm nối)
                    if (!locked) { a.pos = Place(e, out _); _runDirty = true; }
                }
                else
                {
                    var off = e.WorldPosition - a.pos; // kéo một đầu thanh cong: đầu kia đối xứng
                    a.outH = _penDrag == PenDrag.HandleOut ? off : -off;
                    a.inH = -a.outH;
                    _runDirty = true;
                }
                _pen[_sel] = a;
                if (_editRun != null && _editRun.hole >= 0) SyncClosedEnds(_sel);
                RefreshPen(null);
                return;
            }
            if (_editRun == null && (e.Modifiers & KeyModifiers.Shift) != 0)
            {
                var p = Place(e, out var snapped); // giữ Shift: sắp đặt điểm, cho thấy chỗ hút vào biên và đường nối tiếp từ điểm cuối
                var closed = SnapToStart(p);
                if (closed != p) { p = closed; snapped = true; }
                view.SetSnapMarker(snapped, p);
                if (_pen.Count > 0)
                {
                    RefreshPen(new PenAnchor { pos = p });
                    _tailShown = true;
                }
            }
            else
            {
                view.SetSnapMarker(false);
                if (_tailShown)
                {
                    _tailShown = false;
                    RefreshPen(null);
                }
            }
        }

        // Vẽ lại bản xem trước (đường cong qua các điểm neo + đoạn tới con trỏ), chấm neo và tay cầm của điểm đang chọn
        private void RefreshPen(PenAnchor? tail)
        {
            _markers.Clear();
            foreach (var a in _pen) _markers.Add(a.pos);
            var showTail = tail.HasValue && _pressing && _penDrag == PenDrag.None;
            if (showTail) _markers.Add(tail.Value.pos);
            view.SetAnchorMarkers(_markers, showTail ? _markers.Count - 1 : _sel);
            var hs = new List<Vector3>();
            if (showTail)
            {
                if (tail.Value.outH != Vector3.zero) { hs.Add(tail.Value.pos); hs.Add(tail.Value.pos + tail.Value.outH); }
                if (tail.Value.inH != Vector3.zero) { hs.Add(tail.Value.pos); hs.Add(tail.Value.pos + tail.Value.inH); }
            }
            else if (_sel >= 0 && _sel < _pen.Count)
            {
                HandleEnds(_sel, out var outEnd, out var inEnd); // điểm đang chọn luôn có thanh cong
                hs.Add(_pen[_sel].pos);
                hs.Add(outEnd);
                hs.Add(_pen[_sel].pos);
                hs.Add(inEnd);
            }
            view.SetHandles(hs);
            var list = new List<PenAnchor>(_pen);
            if (tail.HasValue) list.Add(tail.Value);
            if (list.Count < 2) { view.SetCutPreview(false); return; }
            _preview.Clear();
            Flatten(list, _preview);
            view.SetCutPreview(true, _preview);
        }

        // Đường cong Bézier bậc 3 giữa các điểm neo (tay cầm 0 = đoạn thẳng) thành chuỗi điểm world
        private static void Flatten(IReadOnlyList<PenAnchor> a, List<Vector3> o)
        {
            o.Add(a[0].pos);
            for (var i = 0; i + 1 < a.Count; i++)
            {
                Vector3 p0 = a[i].pos, p3 = a[i + 1].pos, p1 = p0 + a[i].outH, p2 = p3 + a[i + 1].inH;
                var curved = a[i].outH != Vector3.zero || a[i + 1].inH != Vector3.zero;
                var n = curved ? Mathf.Clamp(Mathf.CeilToInt(((p3 - p0).magnitude + a[i].outH.magnitude + a[i + 1].inH.magnitude) / CutPointSpacing), 4, 80) : 1;
                for (var k = 1; k <= n; k++)
                {
                    var t = k / (float)n;
                    var u = 1f - t;
                    o.Add(u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3);
                }
            }
        }

        private void OnPointerUp(LevelEditorPointerEvent e)
        {
            if (e.Button != 0) return;
            if (_boxing) { FinishBox(e); return; }
            if (IsPathTool && _pressing)
            {
                _pressing = false;
                _dragging = false;
                _input.SetPointerCaptured(false);
                _penDrag = PenDrag.None;
                PushPenChange(_penBefore); // chèn hoặc kéo điểm, kéo thanh cong: một bước undo
                return;
            }
            if (_dragging) EndDrag();
        }

        // Enter / chuột phải / nhấp đúp: chốt đường đang vẽ
        private void FinishPen()
        {
            if (_pen.Count == 0) return;
            var path = new List<Vector3>();
            if (_pen.Count == 1) path.Add(_pen[0].pos); else Flatten(_pen, path);
            var split = _splitIndex;
            var splitting = Splitting;
            var closed = PenClosed;
            var edit = _editLine;
            var run = _editRun;
            var dirty = _runDirty;
            CancelPen();
            if (run != null)
            {
                if (dirty) EditRunAlong(path, run); // chưa sửa gì thì không ghi dữ liệu
                return;
            }
            CommitPath(path, split, edit, splitting, closed);
        }

        // Esc: bỏ đường đang vẽ (nét đang sửa giữ nguyên bản cũ)
        private void CancelPen()
        {
            Splitting = false;
            if (_editLine >= 0) view.SetLineVisible(_editLine, true);
            _editLine = -1;
            _editRun = null;
            _runDirty = false;
            _penBefore = null;
            _penOriginal = null;
            _tailShown = false;
            foreach (var r in _hiddenOutlines) view.ShowOutline(r);
            _hiddenOutlines.Clear();
            _invoker?.RemoveWhere(c => c is PenStateCommand); // hết phiên vẽ: các bước của bút không còn ý nghĩa trong lịch sử
            _pen.Clear();
            _pressing = false;
            _penDrag = PenDrag.None;
            _sel = -1;
            EndDrag();
            view.SetSnapMarker(false);
            view.SetAnchorMarkers(null);
            view.SetHandles(null);
        }

        // Backspace / Delete: đang vẽ hoặc sửa thì bỏ điểm neo, không thì xoá nét đang chọn
        private void OnDelete()
        {
            if (_pen.Count > 0) DeleteAnchor();
            else if (_selectedLine >= 0) DeleteSelectedLine();
            else DeleteSelectedPieces();
        }

        // Bỏ điểm neo đang chọn (mặc định điểm cuối)
        private void DeleteAnchor()
        {
            if (_pen.Count == 0) return;
            var i = _sel >= 0 && _sel < _pen.Count ? _sel : _pen.Count - 1;
            if (_editRun != null && (i == 0 || i == _pen.Count - 1)) return; // điểm nối giữ nguyên
            var before = PenSnapshot();
            _pen.RemoveAt(i);
            if (_pen.Count == 0) { CancelPen(); return; }
            _sel = Mathf.Min(i, _pen.Count - 1);
            PushPenChange(before);
        }

        private void CommitPath(IReadOnlyList<Vector3> path, int splitIndex, int editLine, bool splitting, bool closed)
        {
            if (_propertyData == null) return;
            if (splitting && IsValid(splitIndex) && _cutAsHole)
            {
                if (closed) AddHoleAlong(path, splitIndex);
                else LevelEditorMainUI.Warn("Khoét lỗ cần đường khép kín: bấm gần điểm đầu để khép");
            }
            else if (splitting && IsValid(splitIndex)) SplitAlong(path, splitIndex);
            else if (Tool == PictureTool.AddLine) AddLineAlong(path, editLine);
        }

        private void BeginDrag()
        {
            _dragging = true;
            _input.SetPointerCaptured(true);
        }

        private void EndDrag()
        {
            _dragging = false;
            _splitIndex = -1;
            _cutPath.Clear();
            _input?.SetPointerCaptured(false);
            view.SetCutPreview(false);
        }

        // Nét trang trí từ đường vẽ; 1 điểm = chấm
        private void AddLineAlong(IReadOnlyList<Vector3> world, int replace)
        {
            var pts = world.Select(w => view.WorldToPicture(w)).ToList();
            var work = new PictureModel(_propertyData.Clone());
            if (work.AddLine(pts, replace, NewLineThickness)) Execute(work.Picture);
        }

        // Độ dày (% viền) của nét đang chọn; 0 = mặc định hoặc không có nét chọn
        public int SelectedLineThickness => _propertyData?.lineThickness != null && InspectedLine >= 0 && InspectedLine < _propertyData.lineThickness.Count ? _propertyData.lineThickness[InspectedLine] : 0;

        public void SetSelectedLineThickness(int percent)
        {
            if (_propertyData == null || InspectedLine < 0) return;
            var li = InspectedLine;
            var editing = _selectedLine < 0;
            var work = new PictureModel(_propertyData.Clone());
            if (!work.SetLineThickness(li, percent)) return;
            Execute(work.Picture);
            if (!editing) SelectLine(li); // dựng lại view làm mất chọn
            else { view.SetLineVisible(li, false); RefreshPen(null); } // đang sửa nét: bản cũ vẫn ẩn, đường xem trước giữ nguyên
        }

        // Chốt đường biên đã sửa: hai đầu ghim lại đúng điểm nối rồi thay vào cả hai mảnh
        // Điểm world của bút thành vòng lỗ theo toạ độ lưu (bỏ điểm lặp, bỏ điểm cuối trùng điểm đầu)
        private List<Vector2Int> WorldToRing(IReadOnlyList<Vector3> world)
        {
            var unit = Mathf.Max(1, _propertyData.unit);
            var ring = new List<Vector2Int>();
            foreach (var w in world)
            {
                var q = Vector2Int.RoundToInt(view.WorldToPicture(w) * unit);
                if (ring.Count == 0 || ring[ring.Count - 1] != q) ring.Add(q);
            }
            if (ring.Count > 1 && ring[0] == ring[ring.Count - 1]) ring.RemoveAt(ring.Count - 1);
            return ring;
        }

        private void FinishHoleEdit(PictureModel work, int index, string message)
        {
            Execute(work.Picture);
            SelectRegions(new[] { index }, false); // giữ mảnh đang chọn sau khi tranh dựng lại
            LevelEditorMainUI.Log(message);
        }

        private void EditHoleAlong(IReadOnlyList<Vector3> world, EdgeRun run)
        {
            var work = new PictureModel(_propertyData.Clone());
            var ring = WorldToRing(world);
            RunBusy("Đang sửa lỗ", () =>
            {
                var ok = work.ReplaceHole(run.a, run.hole, ring, out var err);
                return (ok, err);
            }, r =>
            {
                if (!r.ok) LevelEditorMainUI.Warn(r.err);
                else FinishHoleEdit(work, run.a, "Đã sửa lỗ");
            });
        }

        private void AddHoleAlong(IReadOnlyList<Vector3> world, int index)
        {
            var work = new PictureModel(_propertyData.Clone());
            var ring = WorldToRing(world);
            RunBusy("Đang khoét lỗ", () =>
            {
                var ok = work.AddHole(index, ring, out var err);
                return (ok, err);
            }, r =>
            {
                if (!r.ok) LevelEditorMainUI.Warn(r.err);
                else FinishHoleEdit(work, index, "Đã khoét lỗ");
            });
        }

        // Xoá lỗ h của mảnh đang chọn: phần lỗ thuộc lại về mảnh
        public void DeleteHole(int h)
        {
            var index = SelectedIndex;
            if (index < 0 || _propertyData == null || IsBusy) return;
            var work = new PictureModel(_propertyData.Clone());
            RunBusy("Đang xoá lỗ", () =>
            {
                var ok = work.RemoveHole(index, h, out var err);
                return (ok, err);
            }, r =>
            {
                if (!r.ok)
                {
                    if (r.err != null) LevelEditorMainUI.Warn(r.err);
                    return;
                }
                FinishHoleEdit(work, index, "Đã xoá lỗ");
            });
        }

        public int EditingHole => _editRun != null ? _editRun.hole : -1;

        // Mảnh nằm trong lỗ h của mảnh đang chọn, dạng "#3, #5" (rỗng nếu không có)
        public string HoleContains(int h)
        {
            if (SelectedIndex < 0 || _model == null) return "";
            return string.Join(", ", _model.PiecesInHole(SelectedIndex, h).Select(r => "#" + (r + 1)));
        }

        // Đường cắt đang đặt đã khép kín (điểm cuối trùng điểm đầu)
        public bool PenClosed => _pen.Count >= 4 && _editRun == null && (_pen[0].pos - _pen[_pen.Count - 1].pos).sqrMagnitude < 1e-8f;

        private void EditRunAlong(IReadOnlyList<Vector3> world, EdgeRun run)
        {
            if (run.hole >= 0) { EditHoleAlong(world, run); return; }
            var unit = Mathf.Max(1, _propertyData.unit);
            var chain = new List<Vector2Int>();
            foreach (var w in world)
            {
                var q = Vector2Int.RoundToInt(view.WorldToPicture(w) * unit);
                if (chain.Count == 0 || chain[chain.Count - 1] != q) chain.Add(q);
            }
            if (chain.Count < 2) { LevelEditorMainUI.Warn("Biên quá ngắn"); return; }
            chain[0] = run.pts[0];
            chain[chain.Count - 1] = run.pts[run.pts.Count - 1];
            var work = new PictureModel(_propertyData.Clone());
            RunBusy("Đang sửa biên", () =>
            {
                var ok = work.ReplaceRun(run, chain, out var err);
                return (ok, err);
            }, r =>
            {
                if (!r.ok)
                {
                    LevelEditorMainUI.Warn(r.err);
                    return;
                }
                Execute(work.Picture);
                LevelEditorMainUI.Log("Đã sửa biên");
            });
        }

        // Xoá các mảnh đang chọn (một bước undo); chỗ mảnh để lại là khoảng trống, mảnh nằm trong lỗ thì lỗ trống ra
        public void DeleteSelectedPieces()
        {
            if (_propertyData == null || _selected.Count == 0 || IsBusy) return;
            var work = new PictureModel(_propertyData.Clone());
            var picked = new List<int>(_selected);
            RunBusy("Đang xoá mảnh", () => work.RemovePieces(picked), n =>
            {
                if (n == 0) return;
                Execute(work.Picture);
                LevelEditorMainUI.Log($"Đã xoá {n} mảnh");
            });
        }

        // Xoá nét trang trí đang chọn (Delete)
        public void DeleteSelectedLine()
        {
            if (_propertyData == null || _selectedLine < 0) return;
            var work = new PictureModel(_propertyData.Clone());
            if (!work.RemoveLineAt(_selectedLine)) return;
            Execute(work.Picture);
            LevelEditorMainUI.Log("Đã xoá nét");
        }

        private void SplitAlong(IReadOnlyList<Vector3> world, int index)
        {
            var path = world.Select(w => view.WorldToPicture(w)).ToList();
            var work = new PictureModel(_propertyData.Clone());
            var gap = _propertyData.gen.inkGaps ? CutThickness : 0;
            RunBusy("Đang tách mảnh", () =>
            {
                var n = work.SplitPath(index, path, out var err, gap);
                return (n, err);
            }, r =>
            {
                if (r.n == 0)
                {
                    LevelEditorMainUI.Warn(r.err);
                    return;
                }
                Execute(work.Picture);
                LevelEditorMainUI.Log($"Tách thành {r.n + 1} mảnh");
            });
        }

        // Chọn mọi mảnh cùng màu (bấm màu trong inspector tranh)
        public void SelectByColor(int colorId)
        {
            if (_propertyData == null) return;
            var ids = new List<int>();
            for (var i = 0; i < _propertyData.regions.Count; i++) if (_propertyData.regions[i].colorId == colorId) ids.Add(i);
            if (ids.Count == 0) return;
            SelectLine(-1);
            SelectRegions(ids, false);
            LevelEditorMainUI.Log($"Chọn {ids.Count} mảnh màu #{colorId}");
        }

        // Chọn các mảnh theo chỉ số (cho trợ lý chỉ chỗ đề xuất)
        public void SelectPieces(IEnumerable<int> indices)
        {
            SelectLine(-1);
            SelectRegions(indices, false);
        }

        // Gộp ngay (không chạy nền, để nằm trong một nhóm undo): false kèm lý do nếu các mảnh không gộp được
        public bool MergePiecesNow(IList<int> indices, out string error)
        {
            error = null;
            if (_propertyData == null || IsBusy) { error = "Tranh đang bận"; return false; }
            var merged = PictureModel.MergeMany(_propertyData, indices.ToList(), out error);
            if (merged == null) return false;
            Execute(merged);
            LevelEditorMainUI.Log($"Gộp {indices.Count} mảnh thành 1");
            return true;
        }

        // Cặp mảnh kề nhau của tranh hiện tại (cho trợ lý)
        public HashSet<(int, int)> AdjacentPairs() => _propertyData == null ? new HashSet<(int, int)>() : new PictureModel(_propertyData.Clone()).AdjacentPairs();

        // Thử gộp trên bản sao, không đổi tranh: trả tranh sau gộp, null kèm lý do nếu không gộp được
        public PictureProperty TryMerge(IList<int> indices, out string error)
        {
            error = null;
            if (_propertyData == null) { error = "Chưa có tranh"; return null; }
            return PictureModel.MergeMany(_propertyData, indices.ToList(), out error);
        }

        // Chọn ids; add = thêm vào lựa chọn hiện có, không thì thay thế
        private void SelectRegions(IEnumerable<int> ids, bool add)
        {
            if (!add) _selected.Clear();
            foreach (var i in ids) if (IsValid(i) && !_selected.Contains(i)) _selected.Add(i);
            AfterSelectionChanged();
        }

        // Shift+click: thêm hoặc bớt 1 mảnh
        private void ToggleSelect(int i)
        {
            if (!IsValid(i)) return;
            if (!_selected.Remove(i)) _selected.Add(i);
            AfterSelectionChanged();
        }

        public void ClearSelection()
        {
            _selected.Clear();
            SelectLine(-1);
            AfterSelectionChanged();
        }

        // Chọn một nét trang trí (bỏ chọn mảnh); -1 = không chọn nét
        private void SelectLine(int li)
        {
            _selectedLine = li;
            view.SetLineHighlight(li);
            if (li < 0) return;
            _selected.Clear();
            AfterSelectionChanged();
        }

        private void AfterSelectionChanged()
        {
            if (Splitting && (_selected.Count != 1 || _selected[0] != _splitIndex)) CancelPen(); // bỏ chọn hoặc đổi mảnh: huỷ cắt và các điểm đã đặt
            view.SetSelection(_selected);
            if (_selected.Count > 0) LevelEditorManager.Get<LevelEditorBoxQueue>()?.ClearSelection(); // mảnh và hộp không chọn cùng lúc
            if (_selected.Count == 0 || _propertyData == null) return;
            CurrentColorId = _propertyData.regions[_selected[_selected.Count - 1]].colorId; // bảng màu focus theo mảnh chọn gần nhất
            SplitCount = _model != null ? _model.RecommendSplit(_selected[0]) : 2;
        }

        private void OnEscape()
        {
            if (_boxing) { EndBox(); return; }
            if (_pen.Count > 0 || _dragging) CancelPen();
            else ClearSelection();
        }

        private void EndBox()
        {
            if (!_boxing) return;
            _boxing = false;
            _input?.SetPointerCaptured(false);
            view.SetCutPreview(false);
        }

        // Khung chọn xem trước: hình chữ nhật từ điểm nhấn tới con trỏ
        private void ShowBox(Vector2 b)
        {
            var a = _boxStartPic;
            Vector3 W(float x, float y)
            {
                var w = view.PictureToWorld(new Vector2(x, y));
                w.z = 0f;
                return w;
            }
            view.SetCutPreview(true, new List<Vector3> { W(a.x, a.y), W(b.x, a.y), W(b.x, b.y), W(a.x, b.y), W(a.x, a.y) });
        }

        // Thả chuột: kéo dài = chọn mọi mảnh chạm khung (Shift = thêm vào); click = chọn nét gần nhất, không thì mảnh (Shift = thêm/bớt), nền trống = bỏ chọn
        private void FinishBox(LevelEditorPointerEvent e)
        {
            var shift = _boxShift;
            var moved = (e.ScreenPosition - _boxStartScreen).sqrMagnitude > BoxPixels * BoxPixels;
            EndBox();
            if (_model == null) return;
            if (moved)
            {
                if (!shift) SelectLine(-1);
                SelectRegions(_model.RegionsInRect(_boxStartPic, view.WorldToPicture(e.WorldPosition)), shift);
                return;
            }
            var line = _model.NearestLine(_boxStartPic);
            if (line >= 0) { SelectLine(line); return; }
            var hit = _model.HitTest(_boxStartPic);
            SelectLine(-1);
            if (hit < 0) { if (!shift) ClearSelection(); return; }
            if (shift) ToggleSelect(hit);
            else if (_selected.Count == 1 && _selected[0] == hit) ClearSelection(); // click lại mảnh đang chọn: bỏ chọn
            else SelectRegions(new[] { hit }, false);
        }

        // Chọn màu trên bảng: có mảnh đang chọn thì đổi màu cả nhóm trong một bước undo (giữ nguyên lựa chọn)
        public void PickPaletteColor(int colorId)
        {
            CurrentColorId = colorId;
            if (_selected.Count == 0 || _propertyData == null) return;
            var keep = new List<int>(_selected);
            var after = _propertyData.Clone();
            var n = 0;
            foreach (var i in keep)
            {
                if (!IsValid(i) || after.regions[i].colorId == colorId) continue;
                after.regions[i].colorId = colorId;
                n++;
            }
            if (n > 0) Execute(after);
            _selected.Clear();
            foreach (var i in keep) if (IsValid(i)) _selected.Add(i);
            view.SetSelection(_selected);
        }

        // Số cát chung của các mảnh đang chọn; false nếu khác nhau hoặc chưa chọn gì
        public bool TryGetSelectedValue(out int value)
        {
            value = 0;
            if (_propertyData == null || _selected.Count == 0) return false;
            value = _propertyData.regions[_selected[0]].value;
            foreach (var i in _selected) if (_propertyData.regions[i].value != value) return false;
            return true;
        }

        // Số cát gợi ý theo diện tích của mảnh i
        public int SuggestedValue(int i) => IsValid(i) ? PieceValue.Suggest(_propertyData, _propertyData.regions[i]) : 0;

        // Đặt tay số cát cho cả vùng chọn (một bước undo)
        public void SetSelectedValue(int value)
        {
            value = PieceValue.Clamp(value);
            ChangeSelectedValues(r => { if (r.value == value && r.valueManual) return false; r.value = value; r.valueManual = true; return true; });
        }

        // Đổi số cát từng mảnh đang chọn theo bậc 50 (phím [ ]), mảnh nào đổi thì thành đặt tay
        public void StepSelectedValue(int steps) =>
            ChangeSelectedValues(r =>
            {
                var v = PieceValue.Clamp(r.value + steps * PieceValue.Step);
                if (v == r.value) return false;
                r.value = v;
                r.valueManual = true;
                return true;
            });

        // Áp thay đổi lên các mảnh đang chọn trong một bước undo, giữ nguyên lựa chọn; change trả true nếu mảnh có đổi
        private void ChangeSelectedValues(Func<RegionData, bool> change)
        {
            if (_propertyData == null || _selected.Count == 0 || IsBusy) return;
            var after = _propertyData.Clone();
            var n = 0;
            foreach (var i in _selected) if (change(after.regions[i])) n++;
            if (n == 0) return;
            var keep = new List<int>(_selected);
            Execute(after);
            _selected.Clear();
            foreach (var i in keep) if (IsValid(i)) _selected.Add(i);
            view.SetSelection(_selected);
        }

        // Gộp các mảnh đang chọn thành 1 (một bước undo)
        public void MergeSelected()
        {
            if (_propertyData == null || _selected.Count < 2 || IsBusy) return;
            var source = _propertyData;
            var picked = new List<int>(_selected);
            RunBusy("Đang gộp mảnh", () =>
            {
                var merged = PictureModel.MergeMany(source, picked, out var err);
                return (merged, err);
            }, r =>
            {
                if (r.merged == null)
                {
                    LevelEditorMainUI.Warn(r.err);
                    return;
                }
                Execute(r.merged);
                LevelEditorMainUI.Log($"Gộp {picked.Count} mảnh thành 1");
            });
        }

        // Chia các mảnh đang chọn thành nhiều ô ôm theo hình thể; undo được như các thao tác khác
        public void SubdivideSelected()
        {
            if (_propertyData == null || _selected.Count == 0 || IsBusy) return;
            var work = new PictureModel(_propertyData.Clone());
            var picked = new List<int>(_selected);
            var parts = SplitCount;
            var pattern = (SubdividePattern)SplitPattern;
            var gap = _propertyData.gen.inkGaps ? CutThickness : 0; // tranh có khe: các mảnh con cách nhau bằng khe như lúc tạo
            RunBusy("Đang chia mảnh", () =>
            {
                var n = work.SubdivideRegions(picked, parts, out var err, gap, pattern);
                return (n, err);
            }, r =>
            {
                if (r.n == 0)
                {
                    LevelEditorMainUI.Warn(r.err);
                    return;
                }
                Execute(work.Picture);
                LevelEditorMainUI.Log($"Chia {picked.Count} mảnh thành {picked.Count + r.n} mảnh");
            });
        }

        private bool IsValid(int index) => _propertyData != null && index >= 0 && index < _propertyData.regions.Count;

        private void Execute(PictureProperty after) =>
            _invoker.ExecuteCommand(new PictureSnapshotCommand(ApplyPicture, _propertyData, after));

        // Áp tranh từ command (thao tác, undo, redo): cập nhật level data, model, view
        private void ApplyPicture(PictureProperty p)
        {
            if (_editRun != null) CancelPen(); // dữ liệu đổi giữa lúc sửa biên: bỏ đoạn cũ; không đụng kéo Tô/Gộp đang chạy
            _propertyData = p;
            p?.EnsureIds();
            p?.ClampToFrame();
            PieceValue.Assign(p); // giá trị cát luôn khớp diện tích hiện tại (sau gộp, tách, vẽ biên)
            if (p == null) Messenger<OnRemovePropertyData>.Emit(new OnRemovePropertyData { propertyType = "picture" });
            else SavePropertyData();
            RebuildView();
        }

        private void RebuildView()
        {
            CancelPen(); // dữ liệu tranh đổi: bỏ điểm cắt đang đặt
            _selected.Clear();
            _selectedLine = -1;
            _model = _propertyData != null ? new PictureModel(_propertyData) : null;
            view.Build(_propertyData);
            view.SetLineHighlight(-1);
            view.SetAllPainted(ShowColor);
            view.SetGlass(ShowGlass);
            RefreshSource();
        }

        // Chưa có tranh: xem ảnh rõ 100%; có tranh: đè theo độ đậm để so
        private void RefreshSource()
        {
            if (!_source)
            {
                view.SetSource(null, false, 0f, default, default, 1f);
                return;
            }
            var crop = _sourceCrop ?? new RectInt(0, 0, _source.width, _source.height);
            var place = FrameLayout.Place(crop.width, crop.height, Settings.frameW, Settings.frameH, Settings.fitCover, out var uv);
            view.SetSource(_source, _propertyData == null || SourceAlpha > 0.01f, _propertyData == null ? 1f : SourceAlpha,
                FrameLayout.SourceRect(crop, uv), place, Settings.frameW / (float)Settings.frameH);
        }

        // Đổi khung/nền khi chưa có tranh: cập nhật xem trước
        public void RefreshPreview()
        {
            Settings.frameW = Mathf.Clamp(Settings.frameW, MinFrame, MaxFrame);
            Settings.frameH = Mathf.Clamp(Settings.frameH, MinFrame, MaxFrame);
            RefreshSource();
            FitView();
        }

        // Giữ chiều rộng khung, chỉnh chiều cao theo tỉ lệ ảnh
        public void FitFrameToImage()
        {
            if (!_source) return;
            var c = _sourceCrop ?? new RectInt(0, 0, _source.width, _source.height);
            Settings.frameH = Mathf.Clamp(Mathf.RoundToInt(Settings.frameW * c.height / (float)c.width), MinFrame, MaxFrame);
            RefreshPreview();
        }

        private void OnDestroy()
        {
            foreach (var s in _subs) s.Dispose();
            if (_hasPicture != null && LevelEditorManager.TryGet<LevelEditorSaveLoad>(out var saveLoad))
                saveLoad.UnregisterSaveCondition(_hasPicture);
            if (_source) Destroy(_source);
        }
    }
}
