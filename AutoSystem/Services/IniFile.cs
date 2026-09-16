using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSystem.Services
{
    // 简单 INI 读写实现（与 PLCTest 保持一致）
    public class IniFile
    {
        private readonly string path;
        private readonly Dictionary<string, Dictionary<string, string>> data;

        public IniFile(string path)
        {
            this.path = path;
            data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Load();
        }

        private void Load()
        {
            data.Clear();
            if (!File.Exists(path)) return;
            string currentSection = "";
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    if (!data.ContainsKey(currentSection)) data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    continue;
                }
                var idx = line.IndexOf('=');
                if (idx <= 0) continue;
                var k = line.Substring(0, idx).Trim();
                var v = line.Substring(idx + 1).Trim();
                if (!data.ContainsKey(currentSection)) data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                data[currentSection][k] = v;
            }
        }

        public string GetValue(string section, string key)
        {
            if (section == null) section = "";
            if (data.TryGetValue(section, out var sec))
            {
                if (sec.TryGetValue(key, out var v)) return v;
            }
            return null;
        }

        public void SetValue(string section, string key, string value)
        {
            if (section == null) section = "";
            if (!data.ContainsKey(section)) data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            data[section][key] = value ?? string.Empty;
        }

        public void Save()
        {
            using (var sw = new StreamWriter(path, false, System.Text.Encoding.UTF8))
            {
                foreach (var sec in data)
                {
                    sw.WriteLine("[" + sec.Key + "]");
                    foreach (var kv in sec.Value)
                    {
                        sw.WriteLine(kv.Key + " = " + kv.Value);
                    }
                    sw.WriteLine();
                }
            }
        }
    }
}
