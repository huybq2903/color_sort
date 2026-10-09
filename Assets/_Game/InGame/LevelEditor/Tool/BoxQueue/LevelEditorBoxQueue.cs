using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.InGame.Core;
using Falcon.Shared.BaseLevelEditor;
using Falcon.Shared.Common;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Handler hàng chờ hộp: chọn, thêm, xoá, kéo đổi chỗ, đổi màu và cát; undo qua snapshot command.</summary>
    public class LevelEditorBoxQueue : APropertyDataHandler<BoxQueueProperty>, IEditorManager
    {
        public const int ChunkSand = 100; // Generate chia cát mỗi màu thành các hộp cỡ này (hộp cuối lấy phần dư)

        private LevelEditorCommandInvoker _invoker;
        private readonly List<IDisposable> _subs = new();
        private readonly List<string> _selected = new();

        public int NewColorId { get; set; } = 1; // màu của nút + ở mỗi cột
        public BoxQueueProperty Queue => _propertyData;
        public int SelectedCount => _selected.Count;
        public bool IsSelected(string id) => _selected.Contains(id);
        public BoxData Primary => _selected.Count > 0 ? Find(_selected[0]) : null;

        public void Initialized()
        {
            _invoker = LevelEditorManager.Get<LevelEditorCommandInvoker>();
            var input = LevelEditorManager.Get<LevelEditorInputHandler>();
            _subs.Add(input.HotKeyDown(Key.Delete).Subscribe(_ => DeleteSelected()));
            _subs.Add(input.HotKeyDown(Key.Backspace).Subscribe(_ => DeleteSelected()));
            _subs.Add(input.HotKeyDown(Key.Escape).Subscribe(_ => ClearSelection()));
        }

        private void OnDestroy() => _subs.ForEach(s => s.Dispose());

        protected override void OnLoadLevel(OnLoadLevel data)
        {
            base.OnLoadLevel(data);
            _propertyData ??= new BoxQueueProperty(); // chưa có hộp thì chỉ giữ trong editor, lưu vào level khi có hộp đầu tiên
            _propertyData.EnsureIds();
            _selected.Clear();
        }

        private BoxData Find(string id) => _propertyData.list.Find(b => b.id == id);

        // Chọn một hộp: không additive thì click lại hộp đang chọn duy nhất sẽ bỏ chọn; additive thì thêm hoặc bớt
        public void Select(string id, bool additive)
        {
            LevelEditorManager.Get<LevelEditorPicture>()?.ClearSelection(); // hộp và mảnh không chọn cùng lúc
            if (additive)
            {
                if (!_selected.Remove(id)) _selected.Add(id);
                return;
            }
            var same = _selected.Count == 1 && _selected[0] == id;
            _selected.Clear();
            if (!same) _selected.Add(id);
        }

        public void SelectMany(IEnumerable<string> ids, bool additive)
        {
            LevelEditorManager.Get<LevelEditorPicture>()?.ClearSelection();
            if (!additive) _selected.Clear();
            foreach (var id in ids) if (!_selected.Contains(id)) _selected.Add(id);
        }

        public void ClearSelection() => _selected.Clear();

        public List<int> SelectedColorIds() => _selected.Select(Find).Where(b => b != null).Select(b => b.colorId).Distinct().ToList();

        public bool TryGetSelectedValue(out int value)
        {
            var values = _selected.Select(Find).Where(b => b != null).Select(b => b.value).Distinct().ToList();
            value = values.Count == 1 ? values[0] : 0;
            return values.Count == 1;
        }

        // Cát mỗi màu mà các hộp đang có
        public Dictionary<int, int> ColorSand() => _propertyData.list.GroupBy(b => b.colorId).ToDictionary(g => g.Key, g => g.Sum(b => b.value));

        public void Add(int column, int colorId, int value)
        {
            if (column >= BoxQueueProperty.MaxColumns) return;
            Edit(q => q.list.Add(new BoxData { colorId = colorId, value = Mathf.Clamp(value / PieceValue.Step * PieceValue.Step, PieceValue.Min, PieceValue.Max), column = column }));
        }

        public bool CanAdd(int column) => column >= 0 && column < BoxQueueProperty.MaxColumns && column <= _propertyData.ColumnCount;

        public bool Has(string id) => Find(id) != null;

        // Đổi màu và/hoặc cát của một hộp theo id (một bước undo)
        public bool SetBox(string id, int? colorId, int? value)
        {
            if (Find(id) == null) return false;
            Edit(q =>
            {
                var b = q.list.Find(x => x.id == id);
                if (colorId.HasValue) b.colorId = colorId.Value;
                if (value.HasValue) b.value = ClampValue(value.Value);
            });
            return true;
        }

        public bool DeleteBox(string id)
        {
            if (Find(id) == null) return false;
            Edit(q => q.list.RemoveAll(b => b.id == id));
            return true;
        }

        public void SetSelectedColor(int colorId) => EditSelected(b => b.colorId = colorId);

        public void SetSelectedValue(int value) => EditSelected(b => b.value = ClampValue(value));

        public void StepSelectedValue(int steps) => EditSelected(b => b.value = ClampValue(b.value + steps * PieceValue.Step));

        private static int ClampValue(int v) => Mathf.Clamp(v, PieceValue.Min, PieceValue.Max);

        public void DeleteSelected()
        {
            if (_selected.Count == 0) return;
            var ids = new HashSet<string>(_selected);
            Edit(q => q.list.RemoveAll(b => ids.Contains(b.id)));
        }

        // Đặt hộp vào cột column ở hàng row (cột = số cột hiện có nghĩa là mở cột mới)
        public void Move(string id, int column, int row) => Edit(q =>
        {
            if (column >= BoxQueueProperty.MaxColumns) return;
            var box = q.list.Find(b => b.id == id);
            if (box == null) return;
            q.list.Remove(box);
            var col = q.Column(column);
            var at = row < col.Count ? q.list.IndexOf(col[row]) : col.Count > 0 ? q.list.IndexOf(col[^1]) + 1 : q.list.Count;
            box.column = column;
            q.list.Insert(at, box);
        });

        // Chia cát mỗi màu của tranh thành các hộp và rải đều vào các cột (thay toàn bộ hộp hiện có)
        public void Generate(Dictionary<int, int> need, int columns)
        {
            columns = Mathf.Clamp(columns, 1, BoxQueueProperty.MaxColumns);
            var rnd = new System.Random();
            Edit(q =>
            {
                var boxes = new List<BoxData>();
                foreach (var kv in need.OrderBy(k => k.Key))
                    for (var left = kv.Value; left > 0; left -= ChunkSand)
                        boxes.Add(new BoxData { colorId = kv.Key, value = Mathf.Min(ChunkSand, left) });
                for (var i = boxes.Count - 1; i > 0; i--)
                {
                    var j = rnd.Next(i + 1);
                    (boxes[i], boxes[j]) = (boxes[j], boxes[i]);
                }
                for (var i = 0; i < boxes.Count; i++) boxes[i].column = i % columns;
                q.list = boxes.OrderBy(b => b.column).ToList(); // OrderBy ổn định: thứ tự xáo trong từng cột được giữ
                q.columns = columns;
            });
            LevelEditorMainUI.Log($"Đã tạo {_propertyData.list.Count} hộp trong {columns} cột");
        }

        private void EditSelected(Action<BoxData> change)
        {
            var ids = new HashSet<string>(_selected);
            Edit(q => q.list.Where(b => ids.Contains(b.id)).ToList().ForEach(change));
        }

        // Mọi thao tác chạy trên bản sao rồi đẩy vào undo; không đổi gì thì không tạo bước undo
        private void Edit(Action<BoxQueueProperty> change)
        {
            var after = _propertyData.Clone();
            change(after);
            after.EnsureIds();
            var used = after.list.Count == 0 ? 0 : after.list.Max(b => b.column) + 1;
            after.columns = Mathf.Max(used, Mathf.Min(after.columns, BoxQueueProperty.DefaultColumns)); // cột trống cuối vượt mặc định tự bỏ
            if (Same(_propertyData, after)) return;
            _invoker.ExecuteCommand(new BoxQueueSnapshotCommand(Apply, _propertyData, after));
        }

        private static bool Same(BoxQueueProperty a, BoxQueueProperty b) =>
            a.columns == b.columns && a.list.Count == b.list.Count &&
            a.list.Zip(b.list, (x, y) => x.id == y.id && x.colorId == y.colorId && x.value == y.value && x.column == y.column).All(t => t);

        // Áp từ command (thao tác, undo, redo): không còn hộp nào thì gỡ property khỏi level
        private void Apply(BoxQueueProperty p)
        {
            _propertyData = p;
            _selected.RemoveAll(id => Find(id) == null);
            if (p.list.Count == 0) Messenger<OnRemovePropertyData>.Emit(new OnRemovePropertyData { propertyType = "boxQueue" });
            else SavePropertyData();
        }
    }
}
