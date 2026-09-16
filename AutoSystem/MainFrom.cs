using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoSystem.Controllers;
using AutoSystem.Services;

namespace AutoSystem
{
    public partial class MainFrom : Form
    {
        StationManager manager;
        const int MaxLogItems = 1000;
        private readonly string iniPath;
        bool configOpenGuard = false; // 防重入标志

        public MainFrom()
        {
            InitializeComponent();
            iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            btnStop.Enabled = false;
            // 连接在 OnShown 启动
        }

        // 在窗体显示后自动初始化并连接（自动尝试连接）
        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await InitializeAsync();
        }

        // 配置按钮：在 UI 线程以模态方式打开配置窗体，防重入并在保存后重新初始化
        private async void btnConfig_Click(object sender, EventArgs e)
        {
            if (configOpenGuard) return;
            configOpenGuard = true;
            btnConfig.Enabled = false;
            try
            {
                using (var cfg = new ConfigForm(iniPath))
                {
                    var dr = cfg.ShowDialog(this); // 在 UI 线程以模态方式打开
                    if (dr == DialogResult.OK)
                    {
                        LogToUi("配置已保存，正在重新加载配置并重建连接...");
                        await InitializeAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogToUi("打开配置失败: " + ex.Message);
            }
            finally
            {
                try { btnConfig.Enabled = true; } catch { }
                configOpenGuard = false;
            }
        }

        private async Task InitializeAsync()
        {
            try// 先停止并释放已有 manager（如果有）
            {
                try
                {
                    manager?.Stop();
                    manager?.Dispose();
                }
                catch { }
                var ini = new IniFile(iniPath);

                // 读取上次保存的厂商与生产日期并应用到 UI（若有）
                try
                {
                    var lastVendor = ini.GetValue("APP", "LAST_VENDOR");
                    if (!string.IsNullOrWhiteSpace(lastVendor))
                    {
                        try
                        {
                            if (Cbxvendor.InvokeRequired)
                            {
                                Cbxvendor.Invoke(new Action(() =>
                                {
                                    try
                                    {
                                        // 尝试把值设为 SelectedItem（若在列表中），否则设 Text
                                        if (Cbxvendor.Items.Contains(lastVendor)) Cbxvendor.SelectedItem = lastVendor;
                                        else Cbxvendor.Text = lastVendor;
                                    }
                                    catch { Cbxvendor.Text = lastVendor; }
                                }));
                            }
                            else
                            {
                                if (Cbxvendor.Items.Contains(lastVendor)) Cbxvendor.SelectedItem = lastVendor;
                                else Cbxvendor.Text = lastVendor;
                            }
                        }
                        catch { /* 忽略 UI 赋值异常 */ }
                    }
                    var lastDateStr = ini.GetValue("APP", "LAST_DATE");
                    if (!string.IsNullOrWhiteSpace(lastDateStr))
                    {
                        DateTime lastDate;
                        if (DateTime.TryParseExact(lastDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out lastDate))
                        {
                            try
                            {
                                if (DtPdateStr.InvokeRequired)
                                {
                                    DtPdateStr.Invoke(new Action(() => { try { DtPdateStr.Value = lastDate; } catch { } }));
                                }
                                else
                                {
                                    DtPdateStr.Value = lastDate;
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { /* 读取/赋值非关键，失败也继续初始化 */ }
                // 默认 IP/端口（可在 config.ini 中覆盖）
                var plc1Ip = ini.GetValue("PLC", "PLC1_IP") ?? "192.168.30.180";
                var plc2Ip = ini.GetValue("PLC", "PLC2_IP") ?? "192.168.30.181";
                var plc3Ip = ini.GetValue("PLC", "PLC3_IP") ?? "192.168.30.182";
                int plc1port = int.TryParse(ini.GetValue("PLC", "PLC1_PORT"), out plc1port) ? plc1port : 1088;
                int plc2port = int.TryParse(ini.GetValue("PLC", "PLC2_PORT"), out plc2port) ? plc2port : 1088;
                int plc3port = int.TryParse(ini.GetValue("PLC", "PLC3_PORT"), out plc3port) ? plc3port : 1088;

                var barcode1Ip = ini.GetValue("DEVICE", "BARCODE1_IP") ?? "192.168.30.70";
                var barcode2Ip = ini.GetValue("DEVICE", "BARCODE2_IP") ?? "192.168.30.71";
                var barcode3Ip = ini.GetValue("DEVICE", "BARCODE3_IP") ?? "192.168.30.72";
                var visionIp = ini.GetValue("DEVICE", "VISION_IP") ?? "192.168.30.170";
                int barcode1Port = int.TryParse(ini.GetValue("DEVICE", "BARCODE1_RevPORT"), out barcode1Port) ? barcode1Port : 1088;
                int barcode2port = int.TryParse(ini.GetValue("DEVICE", "BARCODE2_RevPORT"), out barcode2port) ? barcode2port : 1088;
                int barcode3port = int.TryParse(ini.GetValue("DEVICE", "BARCODE3_RevPORT"), out barcode3port) ? barcode3port : 1088;
                int visionport = int.TryParse(ini.GetValue("DEVICE", "VISION_PORT"), out visionport) ? visionport : 1088;

                // 创建 PLC 客户端（若无法连接会在 PLC 层抛出或返回状态）（但不在构造器中同步连接）
                var plcClient1 = new PlcMelsecClient(plc1Ip, plc1port);
                var plcClient2 = new PlcMelsecClient(plc2Ip, plc2port);
                var plcClient3 = new PlcMelsecClient(plc3Ip, plc3port);

                // 创建设备 TCP 客户端
                var bc1 = new TcpDeviceClient(barcode1Ip, barcode1Port);
                var bc2 = new TcpDeviceClient(barcode2Ip, barcode2port);
                var bc3 = new TcpDeviceClient(barcode3Ip, barcode3port);
                var vision = new TcpDeviceClient(visionIp, visionport);

                // 尝试连接（使用默认超时与回退日志）
                try { await bc1.ConnectAsync(3000); LogToUi("已成功连接读码器1"); } catch { LogToUi("连接读码器1 失败"); }
                try { await bc2.ConnectAsync(3000); LogToUi("已成功连接读码器2"); } catch { LogToUi("连接读码器2 失败"); }
                try { await bc3.ConnectAsync(3000); LogToUi("已成功连接读码器3"); } catch { LogToUi("连接读码器3 失败"); }
                try { await vision.ConnectAsync(3000); LogToUi("已成功连接视觉服务器"); } catch { LogToUi("连接视觉服务器 失败"); }

                // 并行尝试连接 PLC（短超时，不会长时间阻塞 UI）
                var plcConnectTimeout = 2000; // ms，可按需调整
                var p1 = plcClient1.ConnectAsync(plcConnectTimeout).ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion) LogToUi("已成功连接 PLC1");
                    else LogToUi("连接 PLC1 失败: " + (t.Exception?.GetBaseException().Message ?? "未知"));
                });
                var p2 = plcClient2.ConnectAsync(plcConnectTimeout).ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion) LogToUi("已成功连接 PLC2");
                    else LogToUi("连接 PLC2 失败: " + (t.Exception?.GetBaseException().Message ?? "未知"));
                });
                var p3 = plcClient3.ConnectAsync(plcConnectTimeout).ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion) LogToUi("已成功连接 PLC3");
                    else LogToUi("连接 PLC3 失败: " + (t.Exception?.GetBaseException().Message ?? "未知"));
                });

                // 等待所有连接尝试完成（每项仅等待短超时）
                await Task.WhenAll(p1, p2, p3);

                var uidFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uid.state");
                var idGen = new UniqueIdGenerator(uidFile);
                // 提供从 UI 安全读取厂商与日期的委托（在需要时会回到 UI 线程读取控件）
                Func<string> vendorProvider = () =>
                {
                    try
                    {
                        if (Cbxvendor.InvokeRequired)
                        {
                            return (string)Cbxvendor.Invoke(new Func<string>(() =>
                                (Cbxvendor.SelectedItem != null ? Cbxvendor.SelectedItem.ToString() : Cbxvendor.Text) ?? "DEFAULT"));
                        }
                        return (Cbxvendor.SelectedItem != null ? Cbxvendor.SelectedItem.ToString() : Cbxvendor.Text) ?? "DEFAULT";
                    }
                    catch { return "DEFAULT"; }
                };

                Func<DateTime> dateProvider = () =>
                {
                    try
                    {
                        if (DtPdateStr.InvokeRequired)
                        {
                            return (DateTime)DtPdateStr.Invoke(new Func<DateTime>(() => DtPdateStr.Value.Date));
                        }
                        return DtPdateStr.Value.Date;
                    }
                    catch { return DateTime.Now.Date; }
                };
                manager = new StationManager(plcClient1, plcClient2, plcClient3, bc1, bc2, bc3, vision, idGen, ini, vendorProvider, dateProvider);

                // 默认仅启用站1用于调试，可在 Config 中添加开关后读取
                manager.EnableStation1 = true;
                manager.EnableStation2 = true;
                manager.EnableStation3 = true;
                manager.EnableStation4 = true;
                // 订阅站1的实时 UI 事件（先取消同一方法的订阅以防重复）
                manager.OnStation1Signal -= UpdateStation1Signal;
                manager.OnStation1Signal += UpdateStation1Signal;
                manager.OnStation1ReadBarcodeFlag -= UpdateStation1ReadBarcodeFlag;
                manager.OnStation1ReadBarcodeFlag += UpdateStation1ReadBarcodeFlag;
                manager.OnStation1CodeChanged -= UpdateStn1CodeChanged;
                manager.OnStation1CodeChanged += UpdateStn1CodeChanged;
                // 订阅日志（避免重复订阅）
                manager.OnLog -= LogToUi; // 确保不重复
                manager.OnLog += LogToUi;
                manager.OnError -= (s => LogToUi("错误: " + s));
                manager.OnError += s => LogToUi("错误: " + s);

                // 新增每站条码实时显示：订阅寄存器变更事件并映射到 UI 控件（确保先移除旧订阅）
                manager.OnStation2Signal -= UpdateStation2Signal;
                manager.OnStation2Signal += UpdateStation2Signal;
                manager.OnStation2CodeChanged -= UpdateStn2CodeChanged;
                manager.OnStation2CodeChanged += UpdateStn2CodeChanged;

                manager.OnStation3Signal -= UpdateStation3Signal;
                manager.OnStation3Signal += UpdateStation3Signal;
                manager.OnStation3CodeChanged -= UpdateStn3CodeChanged;
                manager.OnStation3CodeChanged += UpdateStn3CodeChanged;

                manager.OnStation4ASignal -= UpdateStation4ASignal;
                manager.OnStation4ASignal += UpdateStation4ASignal;
                manager.OnStation4ACodeChanged -= UpdateStn4ACodeChanged;
                manager.OnStation4ACodeChanged += UpdateStn4ACodeChanged;
                manager.OnStation4APrintChanged -= UpdateStn4APrintChanged;
                manager.OnStation4APrintChanged += UpdateStn4APrintChanged;
                manager.OnStation4APackChanged -= UpdateStn4APackChanged;
                manager.OnStation4APackChanged += UpdateStn4APackChanged;
                manager.OnStation4AReadFlagChanged -= UpdateStn4AReadFlagChanged;
                manager.OnStation4AReadFlagChanged += UpdateStn4AReadFlagChanged;

                manager.OnStation4BSignal -= UpdateStation4BSignal;
                manager.OnStation4BSignal += UpdateStation4BSignal;
                manager.OnStation4BCodeChanged -= UpdateStn4BCodeChanged;
                manager.OnStation4BCodeChanged += UpdateStn4BCodeChanged;

                manager.OnStation4BPrintChanged -= UpdateStn4BPrintChanged;
                manager.OnStation4BPrintChanged += UpdateStn4BPrintChanged;
                manager.OnStation4BPackChanged -= UpdateStn4BPackChanged;
                manager.OnStation4BPackChanged += UpdateStn4BPackChanged;
                manager.OnStation4BReadFlagChanged -= UpdateStn4BReadFlagChanged;
                manager.OnStation4BReadFlagChanged += UpdateStn4BReadFlagChanged;

                this.Invoke((Action)(() =>
                {
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                    LogToUi("系统初始化并尝试连接完成（使用默认值或配置文件中配置）。");
                }));
            }
            catch (Exception ex)
            {
                LogToUi("初始化异常: " + ex.Message);
            }
            finally
            {
                // 无论成功或失败，确保配置按钮可用并清除 guard（防止界面被锁住）
                try
                {
                    this.Invoke((Action)(() =>
                    {
                        btnConfig.Enabled = true;
                    }));
                }
                catch { }
                configOpenGuard = false;
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager == null)
                {
                    LogToUi("未完成初始化，无法启动。");
                    return;
                }
                manager.Start();
                LogToUi("系统启动命令已发送。");
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            catch (Exception ex)
            {
                LogToUi("启动失败: " + ex.Message);
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager == null)
                {
                    LogToUi("未初始化，无需停止。");
                    return;
                }
                manager.Stop();
                LogToUi("系统停止命令已发送。");
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
            catch (Exception ex)
            {
                LogToUi("停止失败: " + ex.Message);
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            try
            {
                this.Invoke((Action)(() =>
                {
                    lstLog.Items.Clear();
                    lblStation1Status.Text = "空闲";
                    lblStation2Status.Text = "空闲";
                    lblStation3Status.Text = "空闲";
                    lblStation4Status.Text = "空闲";
                    lblSignalStatus_1.Text = "D50000: 0/1";
                    lblReadbarcode_1.Text = "D50001: 1/2";
                    lblbarcode_1.Text = "D50010:";
                    lblSignalStatus_2.Text = "D50101: 0/1";
                    lblbarcode_2.Text = "D50110:";
                    lblSignalStatus_3A.Text = "D50201: 1/2/3/4";
                    lblbarcode_3A.Text = "D50210:";
                    lblSignalStatus_4A.Text = "D50301: 0/1";
                    lblPrint_4A.Text = "贴标D50302: 0/1";
                    lblPackageLayer_4A.Text = "层数D50303: 1/2";
                    lblReadbarcode_4A.Text = "读码D50304: 0/1";
                    lblbarcode_4A.Text = "D50310: ";
                    lblSignalStatus_4B.Text = "D50401: 0/1";

                    lblPrint_4B.Text = "贴标D50402: 0/1";
                    lblPackageLayer_4B.Text = "层数D50403: 1/2";
                    lblReadbarcode_4B.Text = "读码D50404: 0/1";
                    lblbarcode_4B.Text = "D50410: ";
                }));
                LogToUi("已重置界面显示。");
            }
            catch (Exception ex)
            {
                LogToUi("重置失败: " + ex.Message);
            }
        }

        private void LogToUi(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return;
            try
            {
                this.Invoke((Action)(() =>
                {
                    if (lstLog != null)
                    {
                        lstLog.Items.Insert(0, s);
                        while (lstLog.Items.Count > MaxLogItems) lstLog.Items.RemoveAt(lstLog.Items.Count - 1);
                    }

                    try
                    {
                        if (s.Contains("站1")) lblStation1Status.Text = TruncateStatus(s);
                        if (s.Contains("站2")) lblStation2Status.Text = TruncateStatus(s);
                        if (s.Contains("站3") || s.Contains("面1") || s.Contains("面2") || s.Contains("面3") || s.Contains("面4")) lblStation3Status.Text = TruncateStatus(s);
                        if (s.Contains("PLC2") || s.Contains("站4")) lblStation4Status.Text = TruncateStatus(s);
                    }
                    catch { }
                }));
            }
            catch { }
        }

        private string TruncateStatus(string s, int maxLen = 60)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length <= maxLen) return s;
            return s.Substring(0, maxLen - 3) + "...";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // 保存当前厂商与生产日期到 ini，供下次启动使用
                try
                {
                    var ini = new IniFile(iniPath);
                    string lastVendor = "DEFAULT";
                    try
                    {
                        if (Cbxvendor != null)
                        {
                            if (Cbxvendor.InvokeRequired)
                            {
                                lastVendor = (string)Cbxvendor.Invoke(new Func<string>(() =>
                                    (Cbxvendor.SelectedItem != null ? Cbxvendor.SelectedItem.ToString() : Cbxvendor.Text) ?? "DEFAULT"));
                            }
                            else
                            {
                                lastVendor = (Cbxvendor.SelectedItem != null ? Cbxvendor.SelectedItem.ToString() : Cbxvendor.Text) ?? "DEFAULT";
                            }
                        }
                    }
                    catch { lastVendor = "DEFAULT"; }

                    if (string.IsNullOrWhiteSpace(lastVendor)) lastVendor = "DEFAULT";
                    ini.SetValue("APP", "LAST_VENDOR", lastVendor);

                    DateTime dateVal = DateTime.Now.Date;
                    try
                    {
                        if (DtPdateStr != null)
                        {
                            if (DtPdateStr.InvokeRequired)
                            {
                                dateVal = (DateTime)DtPdateStr.Invoke(new Func<DateTime>(() => DtPdateStr.Value.Date));
                            }
                            else
                            {
                                dateVal = DtPdateStr.Value.Date;
                            }
                        }
                    }
                    catch { dateVal = DateTime.Now.Date; }

                    ini.SetValue("APP", "LAST_DATE", dateVal.ToString("yyyy-MM-dd"));
                    ini.Save();
                    LogToUi("已保存上次厂商与生产日期。");
                }
                catch (Exception ex)
                {
                    LogToUi("保存上次厂商/日期失败: " + ex.Message);
                }

                try { manager?.Dispose(); } catch { }
            }
            catch { }
            base.OnFormClosing(e);
        }
        private void UpdateStation1Signal(string text)
        {
            if (lblSignalStatus_1.InvokeRequired)
                lblSignalStatus_1.Invoke(new Action(() => lblSignalStatus_1.Text = text));
            else
                lblSignalStatus_1.Text = text;
        }

        private void UpdateStation1ReadBarcodeFlag(string text)
        {
            if (lblReadbarcode_1.InvokeRequired)
                lblReadbarcode_1.Invoke(new Action(() => lblReadbarcode_1.Text = text));
            else
                lblReadbarcode_1.Text = text;
        }// 以下为新加入的 UI 更新方法，用于接收 StationManager 推送并在 UI 线程安全赋值
        private void UpdateStn1CodeChanged(string text)
        {
            if (lblbarcode_1.InvokeRequired)
                lblbarcode_1.Invoke(new Action(() => lblbarcode_1.Text = text));
            else
                lblbarcode_1.Text = text;
        }
        private void UpdateStation2Signal(string text)
        {
            if (lblSignalStatus_2.InvokeRequired)
                lblSignalStatus_2.Invoke(new Action(() => lblSignalStatus_2.Text = text));
            else
                lblSignalStatus_2.Text = text;
        }
        private void UpdateStn2CodeChanged(string text)
        {
            if (lblbarcode_2.InvokeRequired)
                lblbarcode_2.Invoke(new Action(() => lblbarcode_2.Text = text));
            else
                lblbarcode_2.Text = text;
        }
        private void UpdateStation3Signal(string text)
        {
            if (lblSignalStatus_3A.InvokeRequired)
                lblSignalStatus_3A.Invoke(new Action(() => lblSignalStatus_3A.Text = text));
            else
                lblSignalStatus_3A.Text = text;
        }
        private void UpdateStn3CodeChanged(string text)
        {
            if (lblbarcode_3A.InvokeRequired)
                lblbarcode_3A.Invoke(new Action(() => lblbarcode_3A.Text = text));
            else
                lblbarcode_3A.Text = text;
        }
        private void UpdateStation4ASignal(string text)
        {
            if (lblSignalStatus_4A.InvokeRequired)
                lblSignalStatus_4A.Invoke(new Action(() => lblSignalStatus_4A.Text = text));
            else
                lblSignalStatus_4A.Text = text;
        }
        private void UpdateStn4ACodeChanged(string text)
        {
            if (lblbarcode_4A.InvokeRequired)
                lblbarcode_4A.Invoke(new Action(() => lblbarcode_4A.Text = text));
            else
                lblbarcode_4A.Text = text;
        }
        private void UpdateStn4APrintChanged(string text)
        {
            if (lblPrint_4A.InvokeRequired)
                lblPrint_4A.Invoke(new Action(() => lblPrint_4A.Text = text));
            else
                lblPrint_4A.Text = text;
        }
        private void UpdateStn4APackChanged(string text)
        {
            if (lblPackageLayer_4A.InvokeRequired)
                lblPackageLayer_4A.Invoke(new Action(() => lblPackageLayer_4A.Text = text));
            else
                lblPackageLayer_4A.Text = text;
        }
        private void UpdateStn4AReadFlagChanged(string text)
        {
            if (lblReadbarcode_4A.InvokeRequired)
                lblReadbarcode_4A.Invoke(new Action(() => lblReadbarcode_4A.Text = text));
            else
                lblReadbarcode_4A.Text = text;
        }
        private void UpdateStation4BSignal(string text)
        {
            if (lblSignalStatus_4B.InvokeRequired)
                lblSignalStatus_4B.Invoke(new Action(() => lblSignalStatus_4B.Text = text));
            else
                lblSignalStatus_4B.Text = text;
        }
        private void UpdateStn4BCodeChanged(string text)
        {
            if (lblbarcode_4B.InvokeRequired)
                lblbarcode_4B.Invoke(new Action(() => lblbarcode_4B.Text = text));
            else
                lblbarcode_4B.Text = text;
        }
        private void UpdateStn4BPrintChanged(string text)
        {
            if (lblPrint_4B.InvokeRequired)
                lblPrint_4B.Invoke(new Action(() => lblPrint_4B.Text = text));
            else
                lblPrint_4B.Text = text;
        }
        private void UpdateStn4BPackChanged(string text)
        {
            if (lblPackageLayer_4B.InvokeRequired)
                lblPackageLayer_4B.Invoke(new Action(() => lblPackageLayer_4B.Text = text));
            else
                lblPackageLayer_4B.Text = text;
        }
        private void UpdateStn4BReadFlagChanged(string text)
        {
            if (lblReadbarcode_4B.InvokeRequired)
                lblReadbarcode_4B.Invoke(new Action(() => lblReadbarcode_4B.Text = text));
            else
                lblReadbarcode_4B.Text = text;
        }








        private void UpdateD301Changed(string text)
        {
            if (lblbarcode_3A.InvokeRequired)
                lblbarcode_3A.Invoke(new Action(() => lblbarcode_3A.Text = text));
            else
                lblbarcode_3A.Text = text;
        }

        private void UpdateD302Changed(string text)
        {
            if (lblbarcode_3B.InvokeRequired)
                lblbarcode_3B.Invoke(new Action(() => lblbarcode_3B.Text = text));
            else
                lblbarcode_3B.Text = text;
        }

        private void UpdateD303Changed(string text)
        {
            if (lblbarcode_3C.InvokeRequired)
                lblbarcode_3C.Invoke(new Action(() => lblbarcode_3C.Text = text));
            else
                lblbarcode_3C.Text = text;
        }

        private void UpdateD304Changed(string text)
        {
            if (lblbarcode_3D.InvokeRequired)
                lblbarcode_3D.Invoke(new Action(() => lblbarcode_3D.Text = text));
            else
                lblbarcode_3D.Text = text;
        }

        private void UpdateD401Changed(string text)
        {
            if (lblbarcode_4A.InvokeRequired)
                lblbarcode_4A.Invoke(new Action(() => lblbarcode_4A.Text = text));
            else
                lblbarcode_4A.Text = text;
        }

        private void UpdateD501Changed(string text)
        {
            if (lblbarcode_4B.InvokeRequired)
                lblbarcode_4B.Invoke(new Action(() => lblbarcode_4B.Text = text));
            else
                lblbarcode_4B.Text = text;
        }
    }
}