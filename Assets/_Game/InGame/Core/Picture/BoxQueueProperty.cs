using System.Collections.Generic;
using System.Linq;
using Falcon.Shared.BaseInGame;

namespace Falcon.InGame.Core
{
    /// <summary>Hàng chờ hộp: các cột hộp xếp dọc, hộp đầu cột (thứ tự trong list) ra trước.</summary>
    [PropertyDataType("boxQueue")]
    public class BoxQueueProperty : MultipleEntityDataProperty<BoxData>
    {
        public const int DefaultColumns = 3, MaxColumns = 5;
        public int columns = DefaultColumns;

        // Số cột hiển thị: đủ chứa mọi hộp, tối thiểu bằng số cột đã đặt
        public int ColumnCount => System.Math.Max(columns, list.Count == 0 ? 1 : list.Max(b => b.column) + 1);

        public List<BoxData> Column(int c) => list.Where(b => b.column == c).ToList();

        // Gán id cho hộp còn thiếu, giữ nguyên id đã có
        public void EnsureIds()
        {
            var used = new HashSet<string>(list.Where(b => !string.IsNullOrEmpty(b.id)).Select(b => b.id));
            var next = 1;
            foreach (var b in list)
            {
                if (!string.IsNullOrEmpty(b.id)) continue;
                string id;
                do id = "b" + next++; while (used.Contains(id));
                used.Add(id);
                b.id = id;
            }
        }

        public BoxQueueProperty Clone() => new()
        {
            columns = columns,
            list = list.ConvertAll(b => new BoxData { id = b.id, colorId = b.colorId, value = b.value, column = b.column }),
        };
    }

    /// <summary>1 hộp: màu, sức chứa cát (50..300, bậc 50), cột chứa nó.</summary>
    public class BoxData : EntityData
    {
        public int colorId, value, column;
    }
}
