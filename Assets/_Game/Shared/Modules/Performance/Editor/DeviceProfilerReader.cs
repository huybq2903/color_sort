using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace Falcon.Shared.Performance
{
    /// <summary>Nối Profiler tới player Android qua adb forward rồi đổ số liệu ra Temp/DeviceProfiler.txt.</summary>
    public static class DeviceProfilerReader
    {
        private const string Url = "127.0.0.1:34999";
        private const int MaxFrames = 300;
        private const int TopMarkers = 30;
        private const int MaxThreads = 64;
        private const string OutPath = "Temp/DeviceProfiler.txt";

        private static readonly string[] Counters =
        {
            "SetPass Calls Count", "Draw Calls Count", "Batches Count",
            "Triangles Count", "Vertices Count", "Used Textures Count",
            "GC Used Memory", "System Used Memory",
        };

        [MenuItem("Tools/Device Profiler/1 Connect")]
        public static void Connect()
        {
            // Về Editor trước: DirectURLConnect tới URL đang "connected" bị coi là no-op dù TCP đã chết.
            ProfilerDriver.enabled = false;
            var available = ProfilerDriver.GetAvailableProfilers();
            if (available != null && available.Length > 0) ProfilerDriver.connectedProfiler = available[0];

            ProfilerDriver.ClearAllFrames();
            ProfilerDriver.DirectURLConnect(Url);
            ProfilerDriver.enabled = true;
            Debug.Log($"[DeviceProfiler] connect {Url}");
        }

        /// <summary>Bật/tắt lúc chạy để A/B trong cùng một phiên; sửa URP asset thì phải vào lại màn.</summary>
        [MenuItem("Tools/Device Profiler/6 Toggle SRP Batcher")]
        public static void ToggleSrpBatcher()
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = !GraphicsSettings.useScriptableRenderPipelineBatching;
            Debug.Log($"[SRPBatcher] = {GraphicsSettings.useScriptableRenderPipelineBatching}");
        }

        [MenuItem("Tools/Device Profiler/2 Dump")]
        public static void Dump()
        {
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
            var sb = new StringBuilder();
            sb.AppendLine($"connectedProfiler={ProfilerDriver.connectedProfiler} frames={first}..{last}");

            var frameMs = new List<float>();
            var threadMs = new Dictionary<string, List<float>>();
            var counter = new Dictionary<string, List<long>>();
            var self = new Dictionary<string, float>();
            var calls = new Dictionary<string, int>();
            var used = 0;

            for (var f = Mathf.Max(first, last - MaxFrames + 1); f <= last; f++)
            {
                var counted = false;

                for (var t = 0; t < MaxThreads; t++)
                {
                    using var view = ProfilerDriver.GetRawFrameDataView(f, t);
                    if (view == null || !view.valid) break;

                    if (!counted)
                    {
                        counted = true;
                        used++;
                        frameMs.Add(view.frameTimeMs);
                    }

                    if (view.sampleCount > 0)
                        Add(threadMs, $"{view.threadGroupName}/{view.threadName}", view.GetSampleTimeMs(0));

                    foreach (var name in Counters)
                    {
                        var id = view.GetMarkerId(name);
                        if (id != FrameDataView.invalidMarkerId)
                            Add(counter, name, view.GetCounterValueAsLong(id));
                    }

                    if (t != 0) continue;

                    for (var i = 0; i < view.sampleCount; i++)
                    {
                        var name = view.GetSampleName(i);
                        if (string.IsNullOrEmpty(name)) continue;
                        self.TryGetValue(name, out var acc);
                        self[name] = acc + SelfMs(view, i);
                        calls.TryGetValue(name, out var c);
                        calls[name] = c + 1;
                    }
                }
            }

            sb.AppendLine($"framesRead={used}");
            if (used == 0)
            {
                sb.AppendLine("KHONG CO FRAME — profiler chua nhan duoc du lieu tu player.");
            }
            else
            {
                Percentiles(sb, "CPU frame ms", frameMs);

                sb.AppendLine();
                sb.AppendLine("-- thread (median ms/frame) --");
                foreach (var kv in threadMs.OrderByDescending(k => Median(k.Value)).Take(8))
                    sb.AppendLine($"{Trim(kv.Key, 46),-48}{Median(kv.Value),10:F2}");

                sb.AppendLine();
                sb.AppendLine("-- counter (median/frame) --");
                foreach (var name in Counters)
                    if (counter.TryGetValue(name, out var v) && v.Count > 0)
                        sb.AppendLine($"{name,-48}{MedianL(v),10}");

                sb.AppendLine();
                sb.AppendLine($"{"marker (main thread)",-112}{"self ms",10}{"calls",10}");
                foreach (var kv in self.OrderByDescending(k => k.Value).Take(TopMarkers))
                    sb.AppendLine($"{Trim(kv.Key, 110),-112}{kv.Value / used,10:F3}{calls[kv.Key] / (float)used,10:F1}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutPath) ?? ".");
            File.WriteAllText(OutPath, sb.ToString());
            Debug.Log($"[DeviceProfiler] ghi {OutPath} ({used} frame)");
        }

        /// <summary>Self = thời gian mẫu trừ tổng các con TRỰC TIẾP; con nằm liền sau, mỗi con chiếm recursive+1 ô.</summary>
        private static float SelfMs(RawFrameDataView view, int index)
        {
            var ms = view.GetSampleTimeMs(index);
            var childCount = view.GetSampleChildrenCount(index);
            var c = index + 1;
            for (var n = 0; n < childCount; n++)
            {
                ms -= view.GetSampleTimeMs(c);
                c += view.GetSampleChildrenCountRecursive(c) + 1;
            }
            return ms;
        }

        private static void Add<T>(Dictionary<string, List<T>> map, string key, T value)
        {
            if (!map.TryGetValue(key, out var list)) map[key] = list = new List<T>();
            list.Add(value);
        }

        private static float Median(List<float> v)
        {
            var s = v.OrderBy(x => x).ToList();
            return s.Count == 0 ? 0f : s[s.Count / 2];
        }

        private static long MedianL(List<long> v)
        {
            var s = v.OrderBy(x => x).ToList();
            return s.Count == 0 ? 0L : s[s.Count / 2];
        }

        private static void Percentiles(StringBuilder sb, string label, List<float> values)
        {
            var v = values.Where(x => x > 0f).OrderBy(x => x).ToList();
            if (v.Count == 0) { sb.AppendLine($"{label}: khong co so lieu"); return; }
            sb.AppendLine($"{label}: median={v[v.Count / 2]:F2}  p90={v[(int)(v.Count * 0.9f)]:F2}  " +
                          $"max={v[^1]:F2}  (~{1000f / v[v.Count / 2]:F0} fps)");
        }

        private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
    }
}
