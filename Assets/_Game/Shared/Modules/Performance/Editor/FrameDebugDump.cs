using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Falcon.Shared.Performance
{
    /// <summary>Đổ danh sách event của Frame Debugger ra file. Gọi qua reflection vì API là internal.</summary>
    public static class FrameDebugDump
    {
        private const string OutPath = "Temp/FrameDebug.txt";
        private const string Ns = "UnityEditorInternal.FrameDebuggerInternal.";
        private const string Asm = ", UnityEditor.CoreModule";
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Static | BindingFlags.Instance;

        private static readonly Type Util = Type.GetType($"{Ns}FrameDebuggerUtility{Asm}");
        private static readonly Type DataType = Type.GetType($"{Ns}FrameDebuggerEventData{Asm}");

        [MenuItem("Tools/Device Profiler/3 FrameDebug Start")]
        public static void Start()
        {
            if (!Ready()) return;

            // Ép 0 = Editor: Frame Debugger không đi chung kết nối với Profiler, để -1 là bắt được 0 event.
            Util.GetProperty("limit", Any)!.SetValue(null, int.MaxValue);
            Util.GetMethod("SetEnabled", Any)!.Invoke(null, new object[] { true, 0 });
            Debug.Log("[FrameDebug] bật trên Editor. Chờ vài giây rồi bấm '4 FrameDebug Dump'.");
        }

        // Đổi limit xong phải chờ một nhịp repaint mới đọc được, nên mỗi event tốn 2 tick.
        private static int _i, _count, _read;
        private static bool _armed;
        private static object _data;
        private static string[] _causes;
        private static Dictionary<string, int[]> _passes;
        private static Dictionary<string, int> _breaks, _targets;

        [MenuItem("Tools/Device Profiler/4 FrameDebug Dump")]
        public static void Dump()
        {
            if (!Ready()) return;

            _count = (int)Util.GetProperty("count", Any)!.GetValue(null);
            if (_count <= 0) { Debug.LogError("[FrameDebug] count=0 — bật Frame Debugger trước."); return; }

            _i = 0;
            _read = 0;
            _armed = false;
            _data = Activator.CreateInstance(DataType);
            _causes = (string[])Util.GetMethod("GetBatchBreakCauseStrings", Any)!.Invoke(null, null);
            _passes = new Dictionary<string, int[]>();
            _breaks = new Dictionary<string, int>();
            _targets = new Dictionary<string, int>();

            EditorApplication.update -= Step;
            EditorApplication.update += Step;
            Debug.Log($"[FrameDebug] quét {_count} event, ~{_count * 2} tick...");
        }

        private static void Step()
        {
            if (_i >= _count) { EditorApplication.update -= Step; Finish(); return; }

            if (!_armed)
            {
                Util.GetProperty("limit", Any)!.SetValue(null, _i + 1);
                _armed = true;
                return;
            }

            _armed = false;

            if ((bool)Util.GetMethod("GetFrameEventData", Any)!.Invoke(null, new[] { _i, _data }))
            {
                _read++;

                var shader = Str(_data, "m_RealShaderName");
                if (string.IsNullOrEmpty(shader)) shader = Str(_data, "m_OriginalShaderName");
                var key = $"{shader} :: {Str(_data, "m_PassName")} [{Str(_data, "m_PassLightMode")}]";

                if (!_passes.TryGetValue(key, out var acc)) _passes[key] = acc = new int[4];
                acc[0]++;
                acc[1] += Int(_data, "m_DrawCallCount");
                acc[2] += Int(_data, "m_InstanceCount");
                acc[3] += Int(_data, "m_VertexCount");

                var cause = Int(_data, "m_BatchBreakCause");
                var causeName = _causes != null && cause >= 0 && cause < _causes.Length
                    ? _causes[cause] : $"#{cause}";
                _breaks.TryGetValue(causeName, out var c);
                _breaks[causeName] = c + 1;

                var rt = Str(_data, "m_RenderTargetName");
                if (!string.IsNullOrEmpty(rt)) { _targets.TryGetValue(rt, out var t); _targets[rt] = t + 1; }
            }

            _i++;
        }

        private static void Finish()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"events={_count} read={_read}");

            // Không phụ thuộc limit — luôn đọc được kiểu event và object của TOÀN BỘ frame.
            var events = Util.GetMethod("GetFrameEvents", Any)!.Invoke(null, null) as Array;
            if (events != null)
            {
                var evType = events.GetType().GetElementType()!;
                var fType = evType.GetField("m_Type", Any);
                var fObj = evType.GetField("m_Obj", Any);
                var types = new Dictionary<string, int>();
                var objs = new Dictionary<string, int>();
                var names = new Dictionary<string, int>();
                var infoName = Util.GetMethod("GetFrameEventInfoName", Any);

                for (var i = 0; i < events.Length; i++)
                {
                    var e = events.GetValue(i);
                    var tn = fType?.GetValue(e)?.ToString() ?? "?";
                    types.TryGetValue(tn, out var tc);
                    types[tn] = tc + 1;

                    var on = (fObj?.GetValue(e) as UnityEngine.Object)?.name ?? "(no object)";
                    objs.TryGetValue(on, out var oc);
                    objs[on] = oc + 1;

                    // Không cần limit — tên event đọc được cho mọi index.
                    var nm = infoName?.Invoke(null, new object[] { i }) as string;
                    if (string.IsNullOrEmpty(nm)) nm = "(no name)";
                    // Tên event là đường dẫn dài, phần phân biệt nằm ở ĐUÔI — cắt đầu chứ không cắt đuôi.
                    var slash = nm.LastIndexOf('/');
                    if (slash >= 0 && slash < nm.Length - 1) nm = nm[(slash + 1)..];
                    if (on == "(no object)") nm = $"{nm}   <no object>";
                    names.TryGetValue(nm, out var nc);
                    names[nm] = nc + 1;
                }

                sb.AppendLine();
                sb.AppendLine("-- kiểu event --");
                foreach (var kv in types.OrderByDescending(k => k.Value))
                    sb.AppendLine($"{Trim(kv.Key, 70),-72}{kv.Value,8}");

                sb.AppendLine();
                sb.AppendLine("-- tên event (top 30) --");
                foreach (var kv in names.OrderByDescending(k => k.Value).Take(30))
                    sb.AppendLine($"{Trim(kv.Key, 70),-72}{kv.Value,8}");

                sb.AppendLine();
                sb.AppendLine("-- object vẽ nhiều nhất (top 25) --");
                foreach (var kv in objs.OrderByDescending(k => k.Value).Take(25))
                    sb.AppendLine($"{Trim(kv.Key, 70),-72}{kv.Value,8}");
            }

            sb.AppendLine();
            sb.AppendLine($"{"shader :: pass [lightmode]",-72}{"events",8}{"draws",8}{"inst",8}{"verts",10}");
            foreach (var kv in _passes.OrderByDescending(k => k.Value[0]))
                sb.AppendLine($"{Trim(kv.Key, 70),-72}{kv.Value[0],8}{kv.Value[1],8}{kv.Value[2],8}{kv.Value[3],10}");

            sb.AppendLine();
            sb.AppendLine("-- lý do vỡ batch --");
            foreach (var kv in _breaks.OrderByDescending(k => k.Value))
                sb.AppendLine($"{Trim(kv.Key, 70),-72}{kv.Value,8}");

            sb.AppendLine();
            sb.AppendLine("-- render target --");
            foreach (var kv in _targets.OrderByDescending(k => k.Value))
                sb.AppendLine($"{Trim(kv.Key, 70),-72}{kv.Value,8}");

            Directory.CreateDirectory(Path.GetDirectoryName(OutPath) ?? ".");
            File.WriteAllText(OutPath, sb.ToString());
            Debug.Log($"[FrameDebug] xong, ghi {OutPath} ({_read}/{_count} event)");
        }

        private static bool Ready()
        {
            if (Util != null && DataType != null) return true;
            Debug.LogError("[FrameDebug] Không tìm thấy API internal của Frame Debugger — Unity đổi tên rồi.");
            return false;
        }

        private static string Str(object data, string field) =>
            DataType.GetField(field, Any)?.GetValue(data) as string ?? "";

        private static int Int(object data, string field) =>
            DataType.GetField(field, Any)?.GetValue(data) is int v ? v : 0;

        private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
    }
}

