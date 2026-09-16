using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoSystem.Services;
//namespace AutoSystem
//{
//    public partial class ConfigForm : Form
//    {
//        public ConfigForm()
//        {
//            InitializeComponent();
//        }
//    }
//}

namespace AutoSystem
{
    public partial class ConfigForm : Form
    {
        readonly string iniPath;
        IniFile ini;

        public ConfigForm(string iniFilePath = null)
        {
            InitializeComponent();
            iniPath = string.IsNullOrEmpty(iniFilePath)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini")
                : iniFilePath;
            // 统一在构造函数绑定事件，避免在 Designer/外部重复绑定导致多次触发
            btnSave.Click -= BtnSave_Click;
            btnSave.Click += BtnSave_Click;
            btnCancel.Click -= BtnCancel_Click;
            btnCancel.Click += BtnCancel_Click;

            // 立即加载配置到控件（在构造中加载，避免 Load 事件重复）
            LoadConfig();
        }
        // 帮助方法：从 ini 读取并在空时返回默认值
        private string GetIniValueOrDefault(string section, string key, string defaultValue)
        {
            if (ini == null) return defaultValue;
            var v = ini.GetValue(section, key);
            return string.IsNullOrWhiteSpace(v) ? defaultValue : v;
        }
        // 将加载逻辑抽出为方法，构造函数和（若需要）Load 事件都可调用
        private void LoadConfig()
        {
            try
            {
                ini = new IniFile(iniPath);
                // PLC
                //txtIP_PLC1.Text = GetIniValueOrDefault("PLC", "PLC1_IP", "192.168.30.180");
                txtIP_PLC1.Text = ini.GetValue("PLC", "PLC1_IP") ?? ini.GetValue("PLC", "PLC1_IP") ?? "192.168.30.180";
                txtPort_PLC1.Text = ini.GetValue("PLC", "PLC1_PORT") ?? ini.GetValue("PLC", "PLC1_PORT") ?? "1088";
                txtIP_PLC2.Text = ini.GetValue("PLC", "PLC2_IP") ?? "192.168.30.181";
                txtPort_PLC2.Text = ini.GetValue("PLC", "PLC2_PORT") ?? txtPort_PLC1.Text;
                txtIP_PLC3.Text = ini.GetValue("PLC", "PLC3_IP") ?? "192.168.30.182";
                txtPort_PLC3.Text = ini.GetValue("PLC", "PLC3_PORT") ?? txtPort_PLC1.Text;

                // 读码器与视觉（DEVICE）
                txtIP_Reader1.Text = ini.GetValue("DEVICE", "BARCODE1_IP") ?? "192.168.30.70";
                txtRevPort_Reader1.Text = ini.GetValue("DEVICE", "BARCODE1_RevPORT") ?? ini.GetValue("DEVICE", "BARCODE1_RevPORT") ?? "1088";
                txtSendPort_Reader1.Text = ini.GetValue("DEVICE", "BARCODE1_SendPORT") ?? ini.GetValue("DEVICE", "BARCODE1_SendPORT") ?? "1088";
                txtIP_Reader2.Text = ini.GetValue("DEVICE", "BARCODE2_IP") ?? "192.168.30.71";
                txtRevPort_Reader2.Text = ini.GetValue("DEVICE", "BARCODE2_RevPORT") ?? ini.GetValue("DEVICE", "BARCODE2_RevPORT") ?? "1088";
                txtSendPort_Reader2.Text = ini.GetValue("DEVICE", "BARCODE2_SendPORT") ?? ini.GetValue("DEVICE", "BARCODE2_SendPORT") ?? "1088";
                txtIP_Reader3.Text = ini.GetValue("DEVICE", "BARCODE3_IP") ?? "192.168.30.72";
                txtRevPort_Reader3.Text = ini.GetValue("DEVICE", "BARCODE3_RevPORT") ?? ini.GetValue("DEVICE", "BARCODE3_RevPORT") ?? "1088";
                txtSendPort_Reader3.Text = ini.GetValue("DEVICE", "BARCODE3_SendPORT") ?? ini.GetValue("DEVICE", "BARCODE3_SendPORT") ?? "1088";

                txtIP_CameraDeal.Text = ini.GetValue("DEVICE", "VISION_IP") ?? "192.168.30.170";
                txtPort_CameraDeal.Text = ini.GetValue("DEVICE", "VISION_PORT") ?? "1088";

                // 打印机与模板
                txtName_Printer1.Text = ini.GetValue("PRINTER", "PRINTER1_NAME") ?? "Printer1";
                txtName_Printer2.Text = ini.GetValue("PRINTER", "PRINTER2_NAME") ?? "Printer2";
                txtFileDirectory_Label1.Text = ini.GetValue("TEMPLATE", "TEMPLATE1_DIR") ?? "";
                txtFileDirectory_Label2.Text = ini.GetValue("TEMPLATE", "TEMPLATE2_DIR") ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载配置失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            // 仅关闭，不触发其它逻辑
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (ini == null) ini = new IniFile(iniPath);

                // PLC
                ini.SetValue("PLC", "PLC1_IP", txtIP_PLC1.Text.Trim());
                ini.SetValue("PLC", "PLC1_PORT", txtPort_PLC1.Text.Trim());
                ini.SetValue("PLC", "PLC2_IP", txtIP_PLC2.Text.Trim());
                ini.SetValue("PLC", "PLC2_PORT", txtPort_PLC2.Text.Trim());
                ini.SetValue("PLC", "PLC3_IP", txtIP_PLC3.Text.Trim());
                ini.SetValue("PLC", "PLC3_PORT", txtPort_PLC3.Text.Trim());

                // DEVICE (读码器与视觉)
                ini.SetValue("DEVICE", "BARCODE1_IP", txtIP_Reader1.Text.Trim());
                ini.SetValue("DEVICE", "BARCODE1_RevPORT", txtRevPort_Reader1.Text.Trim());
                ini.SetValue("DEVICE", "BARCODE1_SendPORT", txtSendPort_Reader1.Text.Trim());
                ini.SetValue("DEVICE", "BARCODE2_IP", txtIP_Reader2.Text.Trim());
                ini.SetValue("DEVICE", "BARCODE2_RevPORT", txtRevPort_Reader2.Text.Trim());
                ini.SetValue("DEVICE", "BARCODE2_SendPORT", txtSendPort_Reader2.Text.Trim());
                ini.SetValue("DEVICE", "BARCODE3_IP", txtIP_Reader3.Text.Trim());
                ini.SetValue("DEVICE", "BARCODE3_RevPORT", txtRevPort_Reader3.Text.Trim());
                ini.SetValue("DEVICE", "BARCODE3_SendPORT", txtSendPort_Reader3.Text.Trim());

                ini.SetValue("DEVICE", "VISION_IP", txtIP_CameraDeal.Text.Trim());
                ini.SetValue("DEVICE", "VISION_PORT", txtPort_CameraDeal.Text.Trim());

                // PRINTER
                ini.SetValue("PRINTER", "PRINTER1_NAME", txtName_Printer1.Text.Trim());
                ini.SetValue("PRINTER", "PRINTER2_NAME", txtName_Printer2.Text.Trim());

                // TEMPLATE
                ini.SetValue("TEMPLATE", "TEMPLATE1_DIR", txtFileDirectory_Label1.Text.Trim());
                ini.SetValue("TEMPLATE", "TEMPLATE2_DIR", txtFileDirectory_Label2.Text.Trim());

                ini.Save();
                MessageBox.Show("配置已保存。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存配置失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
