using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSystem.Services
{
    // 内部流水标签码：厂商号A01-4位流水-日期 (示例: A01-0000-20260717)
    // 持久化到文件：每行 "vendor|yyyyMMdd|lastSeq"
    public class UniqueIdGenerator
    {
        readonly string stateFile;
        readonly object sync = new object();

        public UniqueIdGenerator(string stateFilePath)
        {
            stateFile = stateFilePath;
        }

        // 生成内部流水标签码，例如: "A01-0000-20260717"
        // 流水从 0000 开始；相同厂商+日期时流水继续，否则从 0000 开始
        public string Next(string vendorCode, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(vendorCode)) throw new ArgumentException("厂商号 不能为空", nameof(vendorCode));
            var vendor = vendorCode.Trim().ToUpperInvariant();
            var dateStr = date.ToString("yyyyMMdd");
            string key = vendor + "|" + dateStr;

            lock (sync)
            {
                // 读现有状态
                var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(stateFile))
                {
                    var lines = File.ReadAllLines(stateFile);
                    foreach (var line in lines)
                    {
                        var raw = (line ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(raw)) continue;
                        var parts = raw.Split('|');
                        if (parts.Length != 3) continue;
                        var k = parts[0].Trim() + "|" + parts[1].Trim();
                        if (int.TryParse(parts[2].Trim(), out int last)) map[k] = last;
                    }
                }

                int nextSeq;
                if (map.TryGetValue(key, out int lastSeq))
                {
                    nextSeq = lastSeq + 1;
                }
                else
                {
                    nextSeq = 0; // 首次为 0000
                }

                // 更新并写回
                map[key] = nextSeq;
                var outLines = map.Select(kv =>
                {
                    var parts = kv.Key.Split('|');
                    return string.Format("{0}|{1}|{2}", parts[0], parts[1], kv.Value);
                }).ToArray();

                // 尝试以原子方式写入（简单实现，文件替换在锁内）
                File.WriteAllLines(stateFile, outLines);

                return string.Format("{0}-{1:D4}-{2}", vendor, nextSeq, dateStr);
            }
        }

        // 从内部标签码得到 print_label_code 的格式 "0000_20260717"
        // 如果输入格式不对则抛出
        public static string GetPrintLabelCode(string internalLabel)
        {
            if (string.IsNullOrWhiteSpace(internalLabel)) throw new ArgumentException("内部流水标签码 不能为空", nameof(internalLabel));
            var parts = internalLabel.Split('-');
            if (parts.Length != 3) throw new FormatException("厂商号 格式应为 'VENDOR-XXXX-YYYYMMDD'");
            var seq = parts[1];
            var date = parts[2];
            return string.Format("{0}_{1}", seq, date);
        }

        // 可选工具：解析内部标签码为 (vendor, seq, date)
        public static void ParseInternalLabel(string internalLabel, out string vendor, out int seq, out string date)
        {
            vendor = null;
            seq = 0;
            date = null;
            if (string.IsNullOrWhiteSpace(internalLabel)) throw new ArgumentException("internalLabel 不能为空", nameof(internalLabel));
            var parts = internalLabel.Split('-');
            if (parts.Length != 3) throw new FormatException("内部流水标签码 格式应为 'VENDOR-XXXX-YYYYMMDD'");
            vendor = parts[0];
            date = parts[2];
            if (!int.TryParse(parts[1], out seq)) throw new FormatException("序号部分解析失败");
        }
    }
}
