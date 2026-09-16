using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSystem.Services
{
    // 视觉消息解析：格式 "<序号>#<结果>,<原因>" 或 "<序号>#<结果>;<原因>"
    public class VisionResult
    {
        public int Index { get; set; }
        public string Result { get; set; }   // OK / NG
        public string Reason { get; set; }   // 描述
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Reason)) return string.Format("{0}#{1}", Index, Result);
            return string.Format("{0}#{1};{2}", Index, Result, Reason);
        }
    }

    public static class MessageParser
    {
        public static bool TryParseVision(string text, out VisionResult result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            var line = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
            var parts = line.Split(new[] { '#' }, 2);
            if (parts.Length != 2) return false;
            int idx;
            if (!int.TryParse(parts[0], out idx)) return false;
            var rest = parts[1];
            string res = rest;
            string reason = null;
            var sepIndex = rest.IndexOfAny(new[] { ';', ',' });
            if (sepIndex >= 0)
            {
                res = rest.Substring(0, sepIndex);
                reason = rest.Substring(sepIndex + 1);
            }
            result = new VisionResult { Index = idx, Result = res.Trim(), Reason = reason?.Trim() };
            return true;
        }
    }
}
