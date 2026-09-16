using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using AutoSystem.Services;
using HslCommunication.Core.IMessage;
using MyCodeSoftPrint;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using static MyCodeSoftPrint.MyCodeSoftPrintLib;


namespace AutoSystem.Controllers
{
    // 管理四个站点的流程与 PLC / 设备通讯（严格读码器映射，支持 3 台 PLC）
    public class StationManager : IDisposable
    {
        readonly IPlcClient plc1; // 管理站1、站2、站3 （IP: 192.168.30.180）
        readonly IPlcClient plc2; // 打印站对应 PLC（打印机1，IP:192.168.30.181）
        readonly IPlcClient plc3; // 打印站对应 PLC（打印机2，IP:192.168.30.182）
        private MyCodeSoftPrintLib printer1;
        readonly TcpDeviceClient barcode1; // 只服务站1 (192.168.30.70)
        readonly TcpDeviceClient barcode2; // 打印站/2号读码器 (192.168.30.71)
        readonly TcpDeviceClient barcode3; // 打印站/3号读码器 (192.168.30.72)
        readonly TcpDeviceClient visionClient; // 视觉服务器 (192.168.30.170)

        readonly UniqueIdGenerator idGen;
        readonly IniFile ini;
        CancellationTokenSource cts;
        Task mainTask;

        readonly ConcurrentDictionary<int, TaskCompletionSource<VisionResult>> visionTcs = new ConcurrentDictionary<int, TaskCompletionSource<VisionResult>>();
        readonly ConcurrentDictionary<string, TaskCompletionSource<string>> barcodeTcs = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        public event Action<string> OnLog;
        public event Action<string> OnError;

        //实时 UI 更新事件（站1）
        public event Action<string> OnStation1Signal; // 格式示例: "到位信号：D50000=1"
        public event Action<string> OnStation1ReadBarcodeFlag; // 格式示例: "读码标志：D50001=1需要读码"
        //PLC 寄存器变更事件，供 UI 订阅（格式："D50010:***" / "D50110:***" 等）
        public event Action<string> OnStation1CodeChanged;

        public event Action<string> OnStation2Signal;
        public event Action<string> OnStation2CodeChanged;

        public event Action<string> OnStation3Signal;
        public event Action<string> OnStation3CodeChanged;

        public event Action<string> OnStation4ASignal;
        public event Action<string> OnStation4ACodeChanged;
        public event Action<string> OnStation4APrintChanged;
        public event Action<string> OnStation4APackChanged;
        public event Action<string> OnStation4AReadFlagChanged;

        public event Action<string> OnStation4BSignal;
        public event Action<string> OnStation4BCodeChanged;
        public event Action<string> OnStation4BPrintChanged;
        public event Action<string> OnStation4BPackChanged;
        public event Action<string> OnStation4BReadFlagChanged;
        // 可用性开关（调试时可只启用站1）
        public bool EnableStation1 { get; set; } = false;
        public bool EnableStation2 { get; set; } = false;
        public bool EnableStation3 { get; set; } = false;
        public bool EnableStation4 { get; set; } = false;
        // 数据库记录器
        readonly InspectionRecorder recorder;
        // 从 UI 获取厂商与日期的回调（避免直接依赖控件）
        readonly Func<string> vendorProvider;
        readonly Func<DateTime> dateProvider;
        public StationManager(IPlcClient plc1, IPlcClient plc2, IPlcClient plc3,
                              TcpDeviceClient barcode1, TcpDeviceClient barcode2, TcpDeviceClient barcode3,
                              TcpDeviceClient visionClient, UniqueIdGenerator idGen, IniFile ini,
                              Func<string> vendorProvider, Func<DateTime> dateProvider)
        {
            this.plc1 = plc1;
            this.plc2 = plc2;
            this.plc3 = plc3;
            this.barcode1 = barcode1;
            this.barcode2 = barcode2;
            this.barcode3 = barcode3;
            this.visionClient = visionClient;
            this.idGen = idGen;
            this.ini = ini;

            this.vendorProvider = vendorProvider ?? (() => "DEFAULT");
            this.dateProvider = dateProvider ?? (() => DateTime.Now.Date);
            try
            {
                recorder = new InspectionRecorder(ini);
                Log("读取数据库记配置完成. ");
            }
            catch (Exception ex)
            {
                Err("初始化数据库记录器失败: " + ex.Message);
            }
            if (visionClient != null) visionClient.OnTextReceived += VisionReceived;
            if (barcode1 != null) barcode1.OnTextReceived += Barcode1Received;
            if (barcode2 != null) barcode2.OnTextReceived += Barcode2Received;
            if (barcode3 != null) barcode3.OnTextReceived += Barcode3Received;
            printer1 = new MyCodeSoftPrintLib();
        }

        void Log(string s) => OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {s}");
        void Err(string s) => OnError?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {s}");

        public void Start()
        {
            if (cts != null) return;

            Log($"启动命令：启用站点 -> 站1:{EnableStation1}, 站2:{EnableStation2}, 站3:{EnableStation3}, 站4:{EnableStation4}");

            cts = new CancellationTokenSource();
            mainTask = Task.Run(() => RunAsync(cts.Token));
        }

        public void Stop()
        {
            try { cts?.Cancel(); } catch { }
        }

        async Task RunAsync(CancellationToken token)
        {
            Log("StationManager 启动。");

            var tasks = new List<Task>();
            if (EnableStation1) tasks.Add(Task.Run(() => MonitorStation1(token), token)); else Log("站1 已被禁用（跳过）。");
            if (EnableStation2) tasks.Add(Task.Run(() => MonitorStation2(token), token)); else Log("站2 已被禁用（跳过）。");
            if (EnableStation3) tasks.Add(Task.Run(() => MonitorStation3(token), token)); else Log("站3 已被禁用（跳过）。");
            if (EnableStation4) tasks.Add(Task.Run(() => MonitorStation4(token), token)); else Log("站4 已被禁用（跳过）。");
            if (EnableStation4) tasks.Add(Task.Run(() => MonitorRePrint(token), token)); else Log("站点重新贴标站 已被禁用（跳过）。");
            if (tasks.Count == 0)
            {
                Log("未启用任何站点，StationManager 空循环中...");
                try { await Task.Delay(-1, token); } catch (TaskCanceledException) { }
                Log("StationManager 停止（无任务）。");
                return;
            }

            await Task.WhenAll(tasks);
            Log("StationManager 停止。");
        }
        // 解析视觉结果：返回 (result, ngCode)
        Tuple<string, string> ParseVision(VisionResult vr)
        {
            try
            {
                var s = vr?.ToString() ?? string.Empty;
                var result = s.IndexOf("NG", StringComparison.OrdinalIgnoreCase) >= 0 ? "NG" : "OK";
                string ng = null;
                var idx = s.IndexOf(';');
                if (idx >= 0 && idx + 1 < s.Length)
                {
                    ng = s.Substring(idx + 1).Trim();
                }
                return Tuple.Create(result, ng);
            }
            catch { return Tuple.Create("OK", (string)null); }
        }

        // ---------- 第一站：卡扣监测站（使用 plc1、barcode1、vision） ----------
        // 到位信号：D50000=1
        // 读码标志：D50001=1/2(不读码时PLC带过来的是内部流水标签码,读码时PLC带过来的是箱体SN(BodySn))
        //卡扣标志(是否使用视觉)： 需要卡扣(D50002=1),不需要卡扣(D50002=2)
        // 写条码到 D50010（条码字符串）；写工位完成 D50003；
        // 后等待视觉结果 190s（190000ms），超时后重新等待读码
        private volatile bool stn1Processing = false;
        async Task MonitorStation1(CancellationToken token)
        {
            Log("站点1 监控已启动（使用 PLC1、读码器1、视觉）。");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var Stn1Signal = await plc1.ReadintAsync("D50000");//Stn1Signal
                    try { OnStation1Signal?.Invoke($"到位信号：D50000={Stn1Signal}"); } catch { }// 触发 UI 实时显示到位信号
                    if (Stn1Signal == 1)
                    {
                        if (stn1Processing)
                        {
                            Log("站1: 当前产品仍在处理，跳过重复触发。");
                            await Task.Delay(120, token); continue;
                        }
                        stn1Processing = true;// 标记为正在处理，确保只处理一次
                        try
                        {
                            var ModelName = await plc1.ReadStringAsync("D50510", 10); //机种名称 ModelName = ModelName.Split('\0')[0].Trim();
                            var ModelNum = await plc1.ReadintAsync("D50500");//机种号
                            var VisionFlag = await plc1.ReadintAsync("D50002");//是否需要检测卡扣
                            Log("站1 读取到 50500: " + ModelNum);
                            Log("站1 到位 (D50000=1)。正在读取是否需要读码(D50001)= ?");
                            var dm = await plc1.ReadintAsync("D50001"); // 1需要读码，2不需要
                            try { var suffix = dm == 1 ? "需要读码" : "不需要读码"; OnStation1ReadBarcodeFlag?.Invoke($"读码标志：D50001 = {dm}{suffix}"); } catch { } //UI 实时显示读码标志
                            if (dm == 1)// 需要读码：等待 barcode1（只有在当前到位时所接收条码才有效）
                            {
                                var tcs = new TaskCompletionSource<string>();
                                barcodeTcs["S1"] = tcs;
                                //Reader1Trigger(2);
                                TriggerBarcodeSend(1); // 触发读码器1，触发字符串T
                                var wait = await Task.WhenAny(tcs.Task, Task.Delay(8000, token));
                                string code = null;
                                var received = false;
                                if (wait == tcs.Task)
                                {
                                    try { code = tcs.Task.Result; received = true; } catch { received = false; }
                                }// 如果收到 NoRead 或者第一次超时 -> 再次触发一次
                                if (!received || string.Equals(code, "NoRead", StringComparison.OrdinalIgnoreCase))
                                {
                                    Log("站1: 首次读码 未收到有效条码或收到 NoRead，执行第二次触发。");
                                    // 清理旧 tcs（如果还在字典中）
                                    barcodeTcs.TryRemove("S1", out _);
                                    var tcs2 = new TaskCompletionSource<string>();
                                    barcodeTcs["S1"] = tcs2;
                                    TriggerBarcodeSend(1); // 第二次触发
                                    var wait2 = await Task.WhenAny(tcs2.Task, Task.Delay(8000, token));
                                    if (wait2 == tcs2.Task)
                                    {
                                        try { code = tcs2.Task.Result; received = true; } catch { received = false; }
                                    }
                                    else { received = false; }// 无论成功或失败，都尝试移除 S1 的等待项
                                    barcodeTcs.TryRemove("S1", out _);

                                    //// 第二次仍然超时或返回 NoRead -> 写 PLC 处理完成并跳过本次流程
                                    //if (!received || string.Equals(code, "NoRead", StringComparison.OrdinalIgnoreCase))
                                    //{
                                    //    Log("站1: 第二次读码仍失败（超时或 NoRead），写入 D50003=2（处理NG），并继续等待下一个到位。");
                                    //    try
                                    //    {
                                    //        await plc1.WriteintAsync("D50003", 2); // D50003 处理完成（告知 PLC 本次不继续等待）
                                    //    }
                                    //    catch (Exception ex) { Err("站1: 写入 D50003 失败: " + ex.Message); }
                                    //    // 跳到下一循环，继续轮询到位信号
                                    //    await Task.Delay(120, token);
                                    //    continue;
                                    //}
                                    // 第二次仍然超时或返回 NoRead -> 将 code 设为 "NG" 
                                    if (!received || string.Equals(code, "NoRead", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Log("站1: 第二次读码仍失败（超时或 NoRead），将条码赋值为 NG,续检测处理。");
                                        code = "NG";
                                        received = true;
                                    }
                                }
                                else
                                {
                                    barcodeTcs.TryRemove("S1", out _); // 第一次成功，清理 S1（Barcode1Received 会 TryRemove，但确保无残留）
                                }
                                Log($"站1 收到条码: {code}。");//code 是有效的非 NoRead 条码
                                                         //await plc1.WriteStringAsync("D50010", code);// 写字符串到 PLC
                                                         //try { OnStation1CodeChanged?.Invoke($"D50010:{code}"); } catch { }// 通知 UI 更新 lblbarcode_1
                                if (VisionFlag == 1) //需要检测卡扣
                                {
                                    // 更新界面状态
                                    Log("站1: 读码完成, 等待视觉结果");
                                    // 等视觉结果，最长 190s
                                    var vtcs = new TaskCompletionSource<VisionResult>();
                                    visionTcs[1] = vtcs;
                                    BeginTrigger(1, "CAM1," + ModelNum + "\r\n");
                                    var vdone = await Task.WhenAny(vtcs.Task, Task.Delay(190000, token));
                                    if (vdone == vtcs.Task)
                                    {
                                        var vr = vtcs.Task.Result;
                                        Log("站1 视觉结果: " + vr);

                                        // 记录到数据库：内部流水标签码，若需要读码则 body_sn=D2
                                        try
                                        {//读取厂商与日期（已在 MainFrom 中处理了 UI 线程访问）
                                            var vendor = vendorProvider?.Invoke() ?? "DEFAULT";
                                            var selectedDate = dateProvider?.Invoke() ?? DateTime.Now.Date;
                                            var internalLabel = idGen.Next(vendor, selectedDate);
                                            await plc1.WriteStringAsync("D50010", internalLabel);// 写字符串到 PLC
                                            try { OnStation1CodeChanged?.Invoke($"D50010:{internalLabel}"); } catch { }// 通知 UI 更新 lblbarcode_1
                                                                                                                       //string plccode = await plc1.ReadStringAsync("D50010", 50);// 通知 UI（再次读取 D50010 时更新）
                                                                                                                       //try { OnStation1CodeChanged?.Invoke($"D50010:{plccode}"); } catch { }
                                                                                                                       //var bodySn = !string.IsNullOrWhiteSpace(plccode) ? plccode : null;
                                            var printLabel = UniqueIdGenerator.GetPrintLabelCode(internalLabel);// 打印标签码
                                            var bodySn = !string.IsNullOrWhiteSpace(code) ? code : null;
                                            var parsed = ParseVision(vr);
                                            var buckleResult = parsed.Item1;
                                            var buckleNg = parsed.Item1 == "NG" ? parsed.Item2 : null;
                                            var Station1Result = bodySn != null && bodySn != "NG" && buckleResult == "OK" ? "OK" : "NG";
                                            var rec = new InspectionRecord
                                            {
                                                InternalLabelCode = internalLabel,
                                                PrintLabelCode = printLabel,
                                                CoverSn = null,
                                                BodySn = bodySn,
                                                BuckleResult = buckleResult,
                                                BuckleNgCode = buckleNg,
                                                FeedTime = DateTime.Now,
                                                Station1Result = Station1Result,
                                                TopCoverResult = null,
                                                TopCoverNgCode = null,
                                                TopDetectionTime = null,
                                                Side1Result = null,
                                                Side1NgCode = null,
                                                Side2Result = null,
                                                Side2NgCode = null,
                                                Side3Result = null,
                                                Side3NgCode = null,
                                                Side4Result = null,
                                                Side4NgCode = null,
                                                Side4DetectionTime = null,
                                                PrintTime = null
                                            };
                                            await recorder?.InsertAsync(rec);
                                            Log("站1: 已将检验记录写入数据库 (internal=" + internalLabel + ").");
                                            //await plc1.WriteintAsync("D50003", ParseVision(vr).Item1 == "OK" ? 1 : 2); // D50003 处理完成
                                            //Log("站1: 已写 D50003（" + ParseVision(vr).Item1 + "），产品发送至第二站。");
                                            await plc1.WriteintAsync("D50003", 1); // D50003 处理完成
                                            Log("站1: 已写 D50003（=1），产品发送至第二站。");
                                        }
                                        catch (Exception ex)
                                        {
                                            Err("站1: 写入数据库失败: " + ex.Message);
                                        }
                                    }
                                    else
                                    {
                                        Log("站1: 视觉处理超时（190s），将重新等待读码器条码。");
                                        // 清除 D50003 以便 PLC 及流程恢复
                                        // await plc1.WriteintAsync("D50003", 0);
                                    }
                                }
                                else//不需要检测卡扣
                                {// 更新界面状态
                                    Log("站1: 读码完成");
                                    // 记录到数据库：内部流水标签码，若需要读码则 body_sn=D2
                                    try
                                    {
                                        var vendor = vendorProvider?.Invoke() ?? "DEFAULT";
                                        var selectedDate = dateProvider?.Invoke() ?? DateTime.Now.Date;
                                        var internalLabel = idGen.Next(vendor, selectedDate);
                                        await plc1.WriteStringAsync("D50010", internalLabel);// 写字符串到 PLC
                                        try { OnStation1CodeChanged?.Invoke($"D50010:{internalLabel}"); } catch { }// 通知 UI 更新 lblbarcode_1
                                        var printLabel = UniqueIdGenerator.GetPrintLabelCode(internalLabel);// 打印标签码
                                        var bodySn = !string.IsNullOrWhiteSpace(code) ? code : null;
                                        var Station1Result = bodySn != null && bodySn != "NG" ? "OK" : "NG";
                                        //string plccode = await plc1.ReadStringAsync("D50010", 50); // 通知 UI（再次读取 D50010 时更新）
                                        //try { OnStation1CodeChanged?.Invoke($"D50010:{plccode}"); } catch { }
                                        //var bodySn = !string.IsNullOrWhiteSpace(plccode) ? plccode : null;
                                        var rec = new InspectionRecord
                                        {
                                            InternalLabelCode = internalLabel,
                                            PrintLabelCode = printLabel,
                                            CoverSn = null,
                                            BodySn = bodySn,
                                            BuckleResult = null,
                                            BuckleNgCode = null,
                                            FeedTime = DateTime.Now,
                                            Station1Result = Station1Result,
                                            TopCoverResult = null,
                                            TopCoverNgCode = null,
                                            TopDetectionTime = null,
                                            Side1Result = null,
                                            Side1NgCode = null,
                                            Side2Result = null,
                                            Side2NgCode = null,
                                            Side3Result = null,
                                            Side3NgCode = null,
                                            Side4Result = null,
                                            Side4NgCode = null,
                                            Side4DetectionTime = null,
                                            PrintTime = null
                                        };
                                        await recorder?.InsertAsync(rec);
                                        Log("站1: 已将检验记录写入数据库 (internal=" + internalLabel + ").");
                                        await plc1.WriteintAsync("D50003", 1); // D50003 处理完成
                                        Log("站1: 已写 D50003=1（处理完成），产品发送至第二站。");
                                    }
                                    catch (Exception ex)
                                    {
                                        Err("站1: 写入数据库失败: " + ex.Message);
                                    }
                                }
                                //}
                                //else //{  Log("站1 读码等待超时(8s)，继续轮询到位信号。");}
                                //barcodeTcs.TryRemove("S1", out _);
                            }
                            else//不需要读码直接触发视觉并发送处理结果或者直接写入流水号
                            {
                                if (VisionFlag == 1)
                                {
                                    BeginTrigger(1, "CAM1," + ModelNum + "\r\n");
                                    Log("站1 配置为不需要读码：等待视觉结果并生成唯一 ID。");
                                    var vtcs = new TaskCompletionSource<VisionResult>();
                                    visionTcs[1] = vtcs;
                                    var vdone = await Task.WhenAny(vtcs.Task, Task.Delay(8000, token));
                                    if (vdone == vtcs.Task)
                                    {
                                        var vr = vtcs.Task.Result;
                                        Log("站1 视觉结果: " + vr);
                                        var vendor = vendorProvider?.Invoke() ?? "DEFAULT";
                                        var selectedDate = dateProvider?.Invoke() ?? DateTime.Now.Date;
                                        var internalLabel = idGen.Next(vendor, selectedDate); //uid = internalLabel;
                                        await plc1.WriteStringAsync("D50010", internalLabel); // 通知 UI 更新 lblbarcode_1
                                        try { OnStation1CodeChanged?.Invoke($"D50010:{internalLabel}"); } catch { }
                                        try // 写库：body_sn=null
                                        {
                                            var printLabel = UniqueIdGenerator.GetPrintLabelCode(internalLabel);
                                            var parsed = ParseVision(vr);
                                            var buckleResult = parsed.Item1;
                                            var buckleNg = parsed.Item1 == "NG" ? parsed.Item2 : null;
                                            var Station1Result = buckleResult == "OK" ? "OK" : "NG";
                                            var rec = new InspectionRecord
                                            {
                                                InternalLabelCode = internalLabel,
                                                PrintLabelCode = printLabel,
                                                CoverSn = null,
                                                BodySn = null,
                                                BuckleResult = buckleResult,
                                                BuckleNgCode = buckleNg,
                                                FeedTime = DateTime.Now,
                                                Station1Result = Station1Result,
                                                TopCoverResult = null,
                                                TopCoverNgCode = null,
                                                TopDetectionTime = null,
                                                Side1Result = null,
                                                Side1NgCode = null,
                                                Side2Result = null,
                                                Side2NgCode = null,
                                                Side3Result = null,
                                                Side3NgCode = null,
                                                Side4Result = null,
                                                Side4NgCode = null,
                                                Side4DetectionTime = null,
                                                PrintTime = null
                                            };
                                            await recorder?.InsertAsync(rec);
                                            Log("站1(无读码): 已将检验记录写入数据库 (internal=" + internalLabel + ").");
                                            //await plc1.WriteintAsync("D50003", ParseVision(vr).Item1 == "OK" ? 1 : 2); // D50003 处理完成
                                            //Log("站1: 生成流水码并写入到 D50010，并写 D50003=" + (ParseVision(vr).Item1 == "OK" ? 1 : 2) + "，产品发送至第二站。");
                                            await plc1.WriteintAsync("D50003", 1); // D50003 处理完成
                                            Log("站1: 生成流水码并写入到 D50010，已写 D50003=1（处理完成），产品发送至第二站。");
                                        }
                                        catch (Exception ex)
                                        {
                                            Err("站1(无读码): 写入数据库失败: " + ex.Message);
                                        }
                                    }
                                    else
                                    {
                                        Log("站1 在不需读码情况下视觉等待超时。");
                                    }
                                    visionTcs.TryRemove(1, out _);
                                }
                                else//直接记录流水号到数据库
                                {
                                    var vendor = vendorProvider?.Invoke() ?? "DEFAULT";
                                    var selectedDate = dateProvider?.Invoke() ?? DateTime.Now.Date;
                                    var internalLabel = idGen.Next(vendor, selectedDate);
                                    await plc1.WriteStringAsync("D50010", internalLabel);
                                    try { OnStation1CodeChanged?.Invoke($"D50010:{internalLabel}"); } catch { }// 通知 UI 更新 lblbarcode_1

                                    try// 写库：body_sn=null
                                    {
                                        var printLabel = UniqueIdGenerator.GetPrintLabelCode(internalLabel);
                                        var rec = new InspectionRecord
                                        {
                                            InternalLabelCode = internalLabel,
                                            PrintLabelCode = printLabel,
                                            CoverSn = null,
                                            BodySn = null,
                                            BuckleResult = null,
                                            BuckleNgCode = null,
                                            FeedTime = DateTime.Now,
                                            Station1Result = "OK",
                                            TopCoverResult = null,
                                            TopCoverNgCode = null,
                                            TopDetectionTime = null,
                                            Side1Result = null,
                                            Side1NgCode = null,
                                            Side2Result = null,
                                            Side2NgCode = null,
                                            Side3Result = null,
                                            Side3NgCode = null,
                                            Side4Result = null,
                                            Side4NgCode = null,
                                            Side4DetectionTime = null,
                                            PrintTime = null
                                        };
                                        await recorder?.InsertAsync(rec);
                                        Log("站1(无读码无视觉): 已将检验记录写入数据库 (internal=" + internalLabel + ").");
                                        await plc1.WriteintAsync("D50003", 1); // D50003 处理完成
                                        Log("站1: 生成流水码并写入到 D50010，并写 D50003=1，产品发送至第二站。");
                                    }
                                    catch (Exception ex)
                                    {
                                        Err("站1(无读码): 写入数据库失败: " + ex.Message);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Err("站1 异常: " + ex.Message);
                        }
                        finally
                        {
                            // 不管处理成功或异常，等待当前产品离开（D50000 != 1）再允许下一次处理
                            await WaitForStn1LeaveAsync(token);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Err("站1 异常: " + ex.Message);
                }
                await Task.Delay(120, token);
            }
        }
        async Task WaitForStn1LeaveAsync(CancellationToken token)
        {
            try
            {
                // 等待 PLC 的到位信号 D50000 从 1 变为非 1，表示完成
                while (!token.IsCancellationRequested)
                {
                    int sig = await plc1.ReadintAsync("D50000");
                    if (sig != 1) break;
                    try { await Task.Delay(120, token); } catch (TaskCanceledException) { break; }
                }
            }
            catch (Exception ex)
            {
                Err("WaitForStn1LeaveAsync 异常: " + ex.Message);
            }
            finally
            {
                stn1Processing = false;
            }
        }
        public void BeginTrigger(int T, string TriggerStr)
        {
            // 触发字符串 CAM1,****
            if (T <= 0) return;
            if (TriggerStr == null) TriggerStr = string.Empty;
            // 确保以 CRLF 结束
            var Tr = TriggerStr.EndsWith("\r\n") ? TriggerStr : (TriggerStr + "\r\n");
            var token = cts?.Token ?? CancellationToken.None;
            Task.Run(async () =>
            {
                for (int i = 0; i < T && !token.IsCancellationRequested; i++)
                {
                    try
                    {
                        if (visionClient != null)
                        {
                            await visionClient.SendAsync(Tr);
                            Log($"已向视觉服务器发送触发字符 ({i + 1}/{T}): {TriggerStr}");
                        }
                        else
                        {
                            Log("视觉客户端未配置或未连接，跳过触发发送。");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Err("BeginTrigger 发送触发失败: " + ex.Message);
                    }
                    // 触发间隔：默认 100000ms
                    try { await Task.Delay(100000, token); } catch (TaskCanceledException) { break; }
                }
            }, token);
        }

        /// <summary>
        /// 向读码器发送触发字符串。
        /// 配置节点 section = "BARCODE"，键名按：BARCODE1_IP / BARCODE1_SendPORT
        /// </summary>
        /// <param name="barcodeIndex">读码器编号：1/2/3</param>
        /// <param name="times">发送次数</param>
        /// <param name="triggerStr">触发字符串（不强制以 CRLF 结尾，方法内部会确保）</param>
        void TriggerBarcodeSend(int barcodeIndex, int times = 1, string triggerStr = "T")
        {
            if (barcodeIndex <= 0 || barcodeIndex > 3) return;
            if (times <= 0) return;
            if (triggerStr == null) triggerStr = string.Empty;
            var Tr = triggerStr.Trim() == "" ? "T" : triggerStr;
            var token = cts?.Token ?? CancellationToken.None;

            Task.Run(async () =>
            {
                try
                {
                    var ipKey = $"BARCODE{barcodeIndex}_IP";
                    var portKey = $"BARCODE{barcodeIndex}_SendPORT";
                    string ip = null;
                    string portStr = null;
                    try
                    {
                        // 配置文件在 "DEVICE" 节
                        ip = ini.GetValue("DEVICE", ipKey);
                        portStr = ini.GetValue("DEVICE", portKey);
                    }
                    catch { }

                    if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(portStr) || !int.TryParse(portStr, out int port))
                    {
                        Err($"TriggerBarcode: 无法读取 BARCODE{barcodeIndex} 的 IP/SendPORT 配置 (键: {ipKey}/{portKey})。");
                        return;
                    }
                    using (var client = new TcpDeviceClient(ip, port))
                    {
                        try
                        {
                            await client.ConnectAsync(5000);
                        }
                        catch (Exception ex)
                        {
                            Err($"TriggerBarcode: 连接到 {ip}:{port} 失败: {ex.Message}");
                            return;
                        }

                        for (int i = 0; i < times && !token.IsCancellationRequested; i++)
                        {
                            try
                            {
                                await client.SendAsync(Tr);
                                Log($"已向读码器{barcodeIndex} 发送触发 ({i + 1}/{times}) -> {ip}:{port} : {triggerStr}");
                            }
                            catch (Exception ex)
                            {
                                Err($"TriggerBarcode 发送失败 (读码器{barcodeIndex}): " + ex.Message);
                            }

                            try { await Task.Delay(100, token); } catch (TaskCanceledException) { break; }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Err("TriggerBarcode 异常: " + ex.Message);
                }
            }, token);
        }
        // ---------- 第二站：上盖检测站（plc1） ----------
        // 到位：D50101=1，视觉触发由 PLC IO 发起，PC 读取 D50110 的产品 ID
        // 视觉OK/NG 50105
        async Task MonitorStation2(CancellationToken token)
        {
            Log("站点2 监控已启动（PLC1）。");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var Stn2Signal = await plc1.ReadintAsync("D50101");
                    try { OnStation2Signal?.Invoke($"到位信号：D50101={Stn2Signal}"); } catch { }
                    if (Stn2Signal == 1)
                    {
                        Log("站2 到位 (D50101=1)。读取产品 ID (D50110)。");
                        var idVal = await plc1.ReadStringAsync("D50110", 20);
                        Log("站2 读取到 D50110: " + idVal);
                        var ModelNum = await plc1.ReadintAsync("D50500");//机种号
                        Log("站2 读取到 50500 机种号: " + ModelNum);
                        try { OnStation2CodeChanged?.Invoke($"D50110:{idVal}"); } catch { }// 通知 UI 更新 lblbarcode_2
                        var vtcs = new TaskCompletionSource<VisionResult>();
                        visionTcs[2] = vtcs;
                        BeginTrigger(1, "CAM2," + ModelNum + "\r\n");
                        var vdone = await Task.WhenAny(vtcs.Task, Task.Delay(8000, token));
                        if (vdone == vtcs.Task)
                        {
                            var vr = vtcs.Task.Result;
                            Log("站2 视觉结果: " + vr);
                            try
                            {
                                var parsed = ParseVision(vr);
                                var topResult = parsed.Item1;
                                var topNg = parsed.Item1 == "NG" ? parsed.Item2 : null;

                                if (!string.IsNullOrWhiteSpace(idVal))
                                {
                                    await recorder?.UpdateTopCoverAsync(idVal, topResult, topNg);
                                    Log("站2: 已在数据库更新上盖结果 (internal=" + idVal + ").");
                                    //await plc1.WriteintAsync("D50105", ParseVision(vr).Item1 == "OK" ? 1 : 2); // 50105 视觉OK/NG
                                    //Log("站2: 已写 50105=" + (ParseVision(vr).Item1 == "OK" ? 1 : 2) + "（视觉完成）。");
                                    await plc1.WriteintAsync("D50105", 1); // D50105 处理完成
                                    Log("站2: 已写 D50105=1（处理完成），产品发送至第三站。");
                                }
                                else
                                {
                                    Log("站2: D50110 为空，无法更新数据库。");
                                }
                            }
                            catch (Exception ex)
                            {
                                Err("站2: 更新数据库失败: " + ex.Message);
                            }
                        }
                        else
                        {
                            Log("站2 视觉等待超时。");
                        }
                        visionTcs.TryRemove(2, out _);
                    }
                }
                catch (Exception ex) { Err("站2 异常: " + ex.Message); }
                await Task.Delay(120, token);
            }
        }

        // ---------- 第三站：四面检测站（plc1） ----------
        // 到位信号 D50201=1/2/3/4；条码寄存器 D50210；完成信号 D50205=1/2/3/4；四面总结果 D50206=1OK/2NG
        // 存储站3 四面的临时视觉结果，key = internalLabel()，value = string[4] 面结果（"OK"/"NG"）
        readonly ConcurrentDictionary<string, string[]> station3FaceResults = new ConcurrentDictionary<string, string[]>();
        async Task MonitorStation3(CancellationToken token)
        {
            Log("站点3 监控已启动（PLC1，四面检测）。");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var Stn3Signal = await plc1.ReadintAsync("D50201");
                    try { OnStation3Signal?.Invoke($"到位信号：D50201={Stn3Signal}"); } catch { }
                    switch (Stn3Signal)
                    {
                        case 1: // 面1
                            var ModelNum = await plc1.ReadintAsync("D50500");//机种号
                            Log("站3 读取到 50500 机种号: " + ModelNum);
                            Log("站3 面1 到位 (D50201=1)，等待视觉 。");
                            var vtcs = new TaskCompletionSource<VisionResult>(); visionTcs[3] = vtcs;
                            BeginTrigger(1, "CAM3A," + ModelNum + "\r\n");
                            var vdone = await Task.WhenAny(vtcs.Task, Task.Delay(8000, token));
                            if (vdone == vtcs.Task)
                            {
                                var vr = vtcs.Task.Result;
                                Log("站3 面1 视觉结果: " + vr);
                                // 读取对应唯一码 D50210
                                var code = await plc1.ReadStringAsync("D50210", 20);
                                Log("站3 面1 读取 D50210: " + code);
                                try { OnStation3CodeChanged?.Invoke($"D50205:{code}"); } catch { }// 通知 UI 更新 lblbarcode_3A (D50210)
                                try
                                {
                                    var parsed = ParseVision(vr);
                                    await recorder?.UpdateSideResultAsync(code, 1, parsed.Item1, parsed.Item1 == "NG" ? parsed.Item2 : null);
                                    Log("站3 面1: 已更新数据库 side1 (internal=" + code + ").");
                                    await plc1.WriteintAsync("D50205", 1); // D50205 完成
                                    Log("站3 面1: 已写 D50205=1。");
                                    //收到 面1 到位时清除该 internal 之前的汇总（从头开始计）
                                    if (!string.IsNullOrWhiteSpace(code))
                                    {
                                        try
                                        {
                                            station3FaceResults.TryRemove(code, out _); // 清除旧缓存，避免旧数据影响新一轮
                                            var arr = station3FaceResults.GetOrAdd(code, k => new string[4]);
                                            lock (arr) { arr[0] = parsed.Item1; } // 记录面1结果
                                            Log("站3 面1: 已清除旧汇总并记录新面1结果 (internal=" + code + ").");
                                        }
                                        catch (Exception exRem) { Err("站3 面1: 清除/写入缓存异常: " + exRem.Message); }
                                    }

                                }
                                catch (Exception ex) { Err("站3 面1: 更新数据库失败: " + ex.Message); }
                            }
                            else Log("站3 面1 视觉超时。");
                            visionTcs.TryRemove(3, out _);
                            break;
                    }
                    if (Stn3Signal == 2)// 面2
                    {
                        var ModelNum = await plc1.ReadintAsync("D50500");//机种号
                        Log("站3 读取到 50500 机种号: " + ModelNum);
                        Log("站3 面2 到位 (D50201=2)，等待视觉。");
                        var vtcs = new TaskCompletionSource<VisionResult>(); visionTcs[4] = vtcs;
                        BeginTrigger(1, "CAM3B," + ModelNum + "\r\n");
                        var vdone = await Task.WhenAny(vtcs.Task, Task.Delay(8000, token));
                        if (vdone == vtcs.Task)
                        {
                            var vr = vtcs.Task.Result;
                            Log("站3 面2 视觉结果: " + vr);
                            var code = await plc1.ReadStringAsync("D50210", 20);
                            Log("站3 面2 读取条码 D50210: " + code);
                            try { OnStation3CodeChanged?.Invoke($"D50210:{code}"); } catch { }// 通知 UI 更新 lblbarcode_3A (D50210)
                            try
                            {
                                var parsed = ParseVision(vr);
                                await recorder?.UpdateSideResultAsync(code, 2, parsed.Item1, parsed.Item1 == "NG" ? parsed.Item2 : null);
                                Log("站3 面2: 已更新数据库 side2 (internal=" + code + ").");
                                await plc1.WriteintAsync("D50205", 2); // D50205 完成
                                Log("站3 面2: 已写 D50205=2。");
                                // 记录面2 结果到缓存
                                if (!string.IsNullOrWhiteSpace(code))
                                {
                                    var arr = station3FaceResults.GetOrAdd(code, k => new string[4]);
                                    lock (arr) { arr[1] = parsed.Item1; }
                                    Log("站3 面2: 已记录面2结果到缓存 (internal=" + code + ").");
                                }
                            }
                            catch (Exception ex) { Err("站3 面2: 更新数据库失败: " + ex.Message); }
                        }
                        else Log("站3 面2 视觉超时。");
                        visionTcs.TryRemove(4, out _);
                    }
                    // 面3
                    if (Stn3Signal == 3)
                    {
                        var ModelNum = await plc1.ReadintAsync("D50500");//机种号
                        Log("站3 读取到 50500 机种号: " + ModelNum);
                        Log("站3 面3 到位 (D50201=3)，等待视觉 index=5。");
                        var vtcs = new TaskCompletionSource<VisionResult>(); visionTcs[5] = vtcs;
                        BeginTrigger(1, "CAM3C," + ModelNum + "\r\n");
                        var vdone = await Task.WhenAny(vtcs.Task, Task.Delay(8000, token));
                        if (vdone == vtcs.Task)
                        {
                            var vr = vtcs.Task.Result;
                            Log("站3 面3 视觉结果: " + vr);
                            var code = await plc1.ReadStringAsync("D50210", 20);
                            Log("站3 面3 读取条码 D50210: " + code);
                            try { OnStation3CodeChanged?.Invoke($"D50210:{code}"); } catch { }// 通知 UI 更新 lblbarcode_3A (D50210)
                            try
                            {
                                var parsed = ParseVision(vr);
                                await recorder?.UpdateSideResultAsync(code, 3, parsed.Item1, parsed.Item1 == "NG" ? parsed.Item2 : null);
                                Log("站3 面3: 已更新数据库 side3 (internal=" + code + ").");
                                await plc1.WriteintAsync("D50205", 3); // D50205 完成
                                Log("站3 面3: 已写 D50205=3。");
                                // 记录面3 结果到缓存
                                if (!string.IsNullOrWhiteSpace(code))
                                {
                                    var arr = station3FaceResults.GetOrAdd(code, k => new string[4]);
                                    lock (arr) { arr[2] = parsed.Item1; }
                                    Log("站3 面3: 已记录面3结果到缓存 (internal=" + code + ").");
                                }
                            }
                            catch (Exception ex) { Err("站3 面3: 更新数据库失败: " + ex.Message); }
                        }
                        else Log("站3 面3 视觉超时。");
                        visionTcs.TryRemove(5, out _);
                    }
                    // 面4
                    if (Stn3Signal == 4)
                    {
                        var ModelNum = await plc1.ReadintAsync("D50500");//机种号
                        Log("站3 读取到 50500 机种号: " + ModelNum);
                        Log("站3 面4 到位 (D50201=4)，等待视觉 index=6。");
                        var vtcs = new TaskCompletionSource<VisionResult>(); visionTcs[6] = vtcs;
                        BeginTrigger(1, "CAM3D," + ModelNum + "\r\n");
                        var vdone = await Task.WhenAny(vtcs.Task, Task.Delay(8000, token));
                        if (vdone == vtcs.Task)
                        {
                            var vr = vtcs.Task.Result;
                            Log("站3 面4 视觉结果: " + vr);
                            var code = await plc1.ReadStringAsync("D50210", 20);
                            Log("站3 面4 读取条码 D50210: " + code);
                            try { OnStation3CodeChanged?.Invoke($"D50210:{code}"); } catch { }// 通知 UI 更新 lblbarcode_3A (D50210)
                            try
                            {
                                var parsed = ParseVision(vr);
                                await recorder?.UpdateSideResultAsync(code, 4, parsed.Item1, parsed.Item1 == "NG" ? parsed.Item2 : null);
                                Log("站3 面4: 已更新数据库 side4 (internal=" + code + ").");
                                await plc1.WriteintAsync("D50205", 4); // D50205 完成
                                Log("站3 面4: 已写 D50205=4。");
                                await Task.Delay(50, token);
                                var total = await recorder?.GetTotalResultAsync(code);
                                if (!string.IsNullOrWhiteSpace(total))
                                {
                                    int val = string.Equals(total, "OK", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
                                    try
                                    {
                                        await plc1.WriteintAsync("D50206", val); // 写入四面总结果到 PLC（1=OK,2=NG）
                                        Log($"站3: 查询到 total_result={total}，已写 D50206={val}。");
                                        Log($"站3: 汇总结果 internal={code} -> {val}，已写入 D50206。");
                                    }
                                    catch (Exception ex) { Err("站3: 写入 D50206 失败: " + ex.Message); }
                                }
                                else
                                {
                                    Log("站3: 未能从数据库读取 total_result（返回空）。");
                                }
                                //// 记录面4 结果并在此刻尝试汇总
                                //if (!string.IsNullOrWhiteSpace(code))
                                //{
                                //    var arr = station3FaceResults.GetOrAdd(code, k => new string[4]);
                                //    lock (arr) { arr[3] = parsed.Item1; }
                                //    Log("站3 面4: 已记录面4结果到缓存，准备汇总 (internal=" + code + ").");
                                //    await TryAggregateStation3Async(code);
                                //}
                            }
                            catch (Exception ex) { Err("站3 面4: 更新数据库失败: " + ex.Message); }
                        }
                        else Log("站3 面4 视觉超时。");
                        visionTcs.TryRemove(6, out _);
                    }
                }
                catch (Exception ex) { Err("站3 异常: " + ex.Message); }
                await Task.Delay(80, token);
            }
        }
        // 尝试对站3 的四面结果进行汇总：只有当面4到位时写入PLC = "OK"/"NG"
        async Task TryAggregateStation3Async(string internalLabel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(internalLabel)) return;
                if (!station3FaceResults.TryGetValue(internalLabel, out var arr)) return;
                string overall = null;
                lock (arr)
                {
                    // 若任一面尚未收到结果则返回（等待更多面）
                    if (arr.Any(x => string.IsNullOrEmpty(x))) return;
                    // 全部收到：只要有一个不是 "OK" 则为 NG
                    overall = arr.All(x => string.Equals(x, "OK", StringComparison.OrdinalIgnoreCase)) ? "OK" : "NG";
                }
                // 写入 PLC 的 D50206（写字符串）
                try
                {
                    await plc1.WriteintAsync("D50206", overall == "OK" ? 1 : 2); // D50206 完成
                    Log($"站3: 四面汇总结果 internal={internalLabel} -> {overall}，已写入 D50206。");
                }
                catch (Exception ex)
                {
                    Err("站3: 写入 D50206 失败: " + ex.Message);
                }
                // 清理缓存
                station3FaceResults.TryRemove(internalLabel, out _);
            }
            catch (Exception ex) { Err("TryAggregateStation3Async 异常: " + ex.Message); }
        }
        // ---------- 第四站：打印站（PLC2 / PLC3） ----------
        // PLC2 到位 D50301；读取 D50302(贴标标志)，D50303(层数), D50304(是否需要读码), 若不读取则直接读取PLC D50310
        // PLC3 到位 D1042；读取 D51/D52/D53 与 D501
        async Task MonitorStation4(CancellationToken token)
        {
            Log("站点4(打印站) 监控已启动（PLC2/PLC3）。");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // PLC2 (打印机1)
                    var Stn4Signal = await plc2.ReadintAsync("D50301");
                    try { OnStation4ASignal?.Invoke($"到位信号：D50301={Stn4Signal}"); } catch { }
                    if (Stn4Signal == 1)
                    {
                        Log("PLC2 到位 (D50301=1)。读取 D50302(贴标标志)，D50303(包装层数), D50304(是否读码)。");
                        var Stn4ALabFlag = await plc2.ReadintAsync("D50302"); // 是否贴标
                        var Stn4APackNum = await plc2.ReadintAsync("D50303"); // 包装层数 1/2
                        var Stn4AReadcodeFlag = await plc2.ReadintAsync("D50304"); // 是否需要读码 1/2
                        try
                        {
                            OnStation4APrintChanged?.Invoke($"是否贴标：D50302={Stn4ALabFlag}");
                            OnStation4APackChanged?.Invoke($"包装层数：D50303={Stn4APackNum}");
                            OnStation4AReadFlagChanged?.Invoke($"是否读码：D50304={Stn4AReadcodeFlag}");
                        }
                        catch { }
                        Log($"PLC2 内配置 D50302={Stn4ALabFlag}, D50303={Stn4APackNum}, D50304={Stn4AReadcodeFlag}");

                        if (Stn4ALabFlag == 1) // 需要贴标
                        {
                            if (Stn4AReadcodeFlag == 1) // 需要读码//需要读码时是通过PLC内寄存的箱体条码去数据库查询内部流水标签码
                            {
                                if (Stn4APackNum == 1) // 包1层 -> barcode2 -> 打印机1  
                                {
                                    Log("PLC2: 包1层且需读码，等待读码器2 (barcode2) 的条码，打印到打印机1。");
                                    var tcs = new TaskCompletionSource<string>(); barcodeTcs["PLC2_BC"] = tcs;
                                    TriggerBarcodeSend(2);
                                    var done = await Task.WhenAny(tcs.Task, Task.Delay(8000, token));
                                    if (done == tcs.Task)
                                    {
                                        var code = tcs.Task.Result;
                                        Log("PLC2 收到 barcode2: " + code);

                                        // 从 UI 获取厂商与日期
                                        var vendor = vendorProvider?.Invoke() ?? "DEFAULT";
                                        var selectedDate = dateProvider?.Invoke() ?? DateTime.Now.Date;
                                        // 根据 body_sn (barcode2 的值)、vendor、date 去数据库查找对应的 internal_label_code
                                        string internalLabel = null;
                                        try
                                        {
                                            internalLabel = await recorder?.FindInternalByBodyAsync(code, vendor, selectedDate);
                                            try { OnStation4ACodeChanged?.Invoke($"条码:{internalLabel}"); } catch { }// 通知 UI 更新
                                            string cover_sn = "";
                                            try { await recorder?.Updatecover_snAsync(internalLabel, cover_sn); } catch { }//更新盖体SN
                                        }
                                        catch (Exception ex)
                                        {
                                            Err("PLC2: 查询内部流水标签时发生错误: " + ex.Message);
                                        }
                                        if (!string.IsNullOrWhiteSpace(internalLabel))
                                        {
                                            Log("PLC2: 找到匹配的 internal_label_code: " + internalLabel);
                                            string printLabel = UniqueIdGenerator.GetPrintLabelCode(internalLabel);
                                            string[] strName = new string[] { "Var0" };//string[] strName = new string[] { "张三", "李四", "王五" };
                                            string[] strValue = new string[] { printLabel };
                                            var result = printer1.Print(GetPrinterNameFor(1), Path.Combine(Application.StartupPath, "LAB1.lab"), 1, strName, strValue);   // 执行打印 
                                            if (result.IsSuccess) // if (codePrint.CodesoftUse(strName, strValue, filePath, lastfilepath, strPrinterName, 1))
                                            {
                                                ini.SetValue("APP", "PRINTER1_LastID", printLabel);
                                                Log("PLC2: 已发送资料到打印机 print_label_code (internal=" + internalLabel + ").");
                                            }
                                            else { Log("ERROR,Labeling machine station printing failed, please check the log to see the reason"); }
                                            // SendToPrinter(GetPrinterNameFor(1), printLabel);
                                            try
                                            {
                                                await recorder?.UpdatePrintLabelAsync(internalLabel);
                                                Log("PLC2: 已更新数据库 print_label_code (internal=" + internalLabel + ").");
                                                await plc2.WriteintAsync("D50305", 1); // D50003 处理完成
                                                Log("PLC2站4: 读码器读码完成，写入 D50305=1。");
                                            }
                                            catch (Exception ex)
                                            { Err("PLC2: 更新数据库失败: " + ex.Message); }
                                        }
                                    }
                                    else Log("PLC2 等待 barcode2 超时。");
                                    barcodeTcs.TryRemove("PLC2_BC", out _);
                                }
                                else if (Stn4APackNum == 2) // 包2层 -> barcode2 -> 打印机2
                                {
                                    Log("PLC2: 包2层且需读码，等待读码器2 (barcode2)，用于打印机2。");
                                    var tcs = new TaskCompletionSource<string>(); barcodeTcs["PLC2_BC"] = tcs;
                                    TriggerBarcodeSend(2);
                                    var done = await Task.WhenAny(tcs.Task, Task.Delay(8000, token));
                                    if (done == tcs.Task)
                                    {
                                        var code = tcs.Task.Result;
                                        Log("PLC2 收到 barcode2: " + code);
                                        // 从 UI 获取厂商与日期
                                        var vendor = vendorProvider?.Invoke() ?? "DEFAULT";
                                        var selectedDate = dateProvider?.Invoke() ?? DateTime.Now.Date;
                                        // 根据 body_sn (barcode2 的值)、vendor、date 去数据库查找对应的 internal_label_code
                                        string internalLabel = null;
                                        try
                                        {
                                            internalLabel = await recorder?.FindInternalByBodyAsync(code, vendor, selectedDate);
                                            try { OnStation4BCodeChanged?.Invoke($"条码:{internalLabel}"); } catch { }// 通知 UI 更新
                                            string cover_sn = "";
                                            try { await recorder?.Updatecover_snAsync(internalLabel, cover_sn); } catch { }//更新盖体SN
                                        }
                                        catch (Exception ex)
                                        {
                                            Err("PLC2: 查询内部流水标签时发生错误: " + ex.Message);
                                        }
                                        if (!string.IsNullOrWhiteSpace(internalLabel))
                                        {
                                            Log("PLC2: 找到匹配的 internal_label_code: " + internalLabel);
                                            string printLabel = UniqueIdGenerator.GetPrintLabelCode(internalLabel);
                                            string[] strName = new string[] { "Var0" };//string[] strName = new string[] { "张三", "李四", "王五" };
                                            string[] strValue = new string[] { printLabel };
                                            var result = printer1.Print(GetPrinterNameFor(2), Path.Combine(Application.StartupPath, "LAB1.lab"), 1, strName, strValue);   // 执行打印 
                                            if (result.IsSuccess) // if (codePrint.CodesoftUse(strName, strValue, filePath, lastfilepath, strPrinterName, 1))
                                            {
                                                ini.SetValue("APP", "PRINTER2_LastID", printLabel);
                                                Log("PLC2: 已发送资料到打印机 print_label_code (internal=" + internalLabel + ").");
                                            }
                                            else { Log("ERROR,Labeling machine station printing failed, please check the log to see the reason"); }
                                            // SendToPrinter(GetPrinterNameFor(2), printLabel);
                                            try
                                            {
                                                await recorder?.UpdatePrintLabelAsync(printLabel);
                                                Log("PLC2: 已更新数据库 print_label_code (internal=" + internalLabel + ").");
                                                await plc2.WriteintAsync("D50305", 1); // D50003 处理完成
                                                Log("PLC2站4: 读码器读码完成，写入 D50305=1。");
                                            }
                                            catch (Exception ex) { Err("PLC2: 更新数据库失败: " + ex.Message); }
                                        }
                                    }
                                    else Log("PLC2 等待 barcode2 超时。");
                                    barcodeTcs.TryRemove("PLC2_BC", out _);
                                }
                            }
                            else // 不需要读码，读取 D50310 并发送到打印机1（无论包1/包2，按需求写）
                            {
                                Log("PLC2: 需要贴标但不需读码，读取 D50310 并打印（Codesoft 模板）。");
                                var code = await plc2.ReadStringAsync("D50310", 40);
                                Log("PLC2 读取 D50310: " + code);
                                try { OnStation4ACodeChanged?.Invoke($"D50310:{code}"); } catch { } // 通知 UI 更新 lblbarcode_4A
                                bool bol = await recorder?.ExistsByInternalAsync(code);
                                if (bol)
                                {
                                    string printLabel = UniqueIdGenerator.GetPrintLabelCode(code);
                                    string[] strName = new string[] { "Var0" };//string[] strName = new string[] { "张三", "李四", "王五" };
                                    string[] strValue = new string[] { printLabel };
                                    PrintResult result;
                                    //  PLC条码即内部流水标签码
                                    UniqueIdGenerator.ParseInternalLabel(code, out string vendor, out int seq, out string date);
                                    if (Stn4APackNum == 1)//包1层发送资料到打印机1
                                    {
                                        try { OnStation4ACodeChanged?.Invoke($"条码:{code}"); } catch { }// 通知 UI 更新
                                        result = printer1.Print(GetPrinterNameFor(1), Path.Combine(Application.StartupPath, "LAB1.lab"), 1, strName, strValue);   // 执行打印
                                        ini.SetValue("APP", "PRINTER1_LastID", printLabel);
                                    }
                                    else//包2层发送资料到打印机2
                                    {
                                        try { OnStation4BCodeChanged?.Invoke($"条码:{code}"); } catch { }// 通知 UI 更新
                                        result = printer1.Print(GetPrinterNameFor(2), Path.Combine(Application.StartupPath, "LAB1.lab"), 1, strName, strValue);   // 执行打印
                                        ini.SetValue("APP", "PRINTER2_LastID", printLabel);
                                    }
                                    if (result.IsSuccess)
                                    {
                                        Log("PLC2: 已发送资料到打印机 print_label_code (internal=" + printLabel + ").");//SendToPrinter(GetPrinterNameFor(1), seq + date);
                                        try
                                        {
                                            await recorder?.UpdatePrintLabelAsync(code);
                                            await plc2.WriteintAsync("D50305", 1); // D50003 处理完成
                                            Log("PLC2站4: 读码器读码完成，写入 D50305=1。");
                                        }
                                        catch (Exception ex) { Err("PLC2: 更新数据库失败: " + ex.Message); }
                                    }
                                    else
                                    {
                                        Log("ERROR,Labeling machine station printing failed, please check the log to see the reason");
                                    }
                                }
                                else
                                {
                                    Log("PLC2 未查询到该流水码 " + code);
                                }
                            }
                        }
                        else
                        {
                            Log("PLC2: 配置为不贴标 (D50302=2)，跳过打印处理。");
                        }
                    }

                    // PLC3 (打印机2)
                    var Stn4BSignal = await plc3.ReadintAsync("D50401");
                    try { OnStation4BSignal?.Invoke($"到位信号：D50401={Stn4BSignal}"); } catch { }
                    if (Stn4BSignal == 1)
                    {
                        Log("PLC3 到位 (D50401=1)。读取 D50402/D50403/D50404。");
                        var Stn4BLabFlag = await plc3.ReadintAsync("D50402"); // 是否贴标
                        var Stn4BPackNum = await plc3.ReadintAsync("D50403"); // 包装层数
                        var Stn4BReadcodeFlag = await plc3.ReadintAsync("D50404"); // 是否需要读码
                        try
                        {
                            OnStation4BPrintChanged?.Invoke($"是否贴标：D50402={Stn4BLabFlag}");
                            OnStation4BPackChanged?.Invoke($"包装层数：D50403={Stn4BPackNum}");
                            OnStation4BReadFlagChanged?.Invoke($"是否读码：D50404={Stn4BReadcodeFlag}");
                        }
                        catch { }
                        Log($"PLC3 配置 D50402={Stn4BLabFlag}, D50403={Stn4BReadcodeFlag}, D50404={Stn4BReadcodeFlag}");

                        if (Stn4BLabFlag == 1) // 需要贴标
                        {
                            if (Stn4BReadcodeFlag == 1) // 需要读码
                            {
                                if (Stn4BPackNum == 1) // 包1层 -> barcode3 -> 打印机2
                                {
                                    Log("PLC3: 包1层且需读码，等待读码器3 (barcode3)，打印到打印机2。");
                                    var tcs = new TaskCompletionSource<string>(); barcodeTcs["PLC3_BC"] = tcs;
                                    var done = await Task.WhenAny(tcs.Task, Task.Delay(8000, token));
                                    if (done == tcs.Task)
                                    {
                                        var code = tcs.Task.Result;
                                        Log("PLC3 收到 barcode3: " + code);
                                        // 从 UI 获取厂商与日期
                                        var vendor = vendorProvider?.Invoke() ?? "DEFAULT";
                                        var selectedDate = dateProvider?.Invoke() ?? DateTime.Now.Date;
                                        // 根据 body_sn (barcode2 的值)、vendor、date 去数据库查找对应的 internal_label_code
                                        string internalLabel = null;
                                        try
                                        {
                                            internalLabel = await recorder?.FindInternalByBodyAsync(code, vendor, selectedDate);
                                            try { OnStation4BCodeChanged?.Invoke($"barcode3:{internalLabel}"); } catch { }// 通知 UI 更新
                                            string cover_sn = "";
                                            try { await recorder?.Updatecover_snAsync(internalLabel, cover_sn); } catch { }//更新盖体SN
                                        }
                                        catch (Exception ex)
                                        {
                                            Err("PLC2: 查询内部流水标签时发生错误: " + ex.Message);
                                        }
                                        Log("PLC2: 找到匹配的 internal_label_code: " + internalLabel);
                                        string printLabel = UniqueIdGenerator.GetPrintLabelCode(internalLabel);
                                        string[] strName = new string[] { "Var0" };//string[] strName = new string[] { "张三", "李四", "王五" };
                                        string[] strValue = new string[] { printLabel };
                                        var result = printer1.Print(GetPrinterNameFor(2), Path.Combine(Application.StartupPath, "LAB1.lab"), 1, strName, strValue);   // 执行打印 
                                        if (result.IsSuccess) // if (codePrint.CodesoftUse(strName, strValue, filePath, lastfilepath, strPrinterName, 1))
                                        {
                                            ini.SetValue("APP", "PRINTER2_LastID", printLabel);
                                            Log("PLC2: 已发送资料到打印机 print_label_code (internal=" + internalLabel + ").");
                                        }
                                        else { Log("ERROR,Labeling machine station printing failed, please check the log to see the reason"); }
                                        //SendToPrinter(GetPrinterNameFor(2), printLabel);
                                        try
                                        {
                                            await recorder?.UpdatePrintLabelAsync(code);
                                            await plc3.WriteintAsync("D50405", 1); // D50405 处理完成
                                            Log("PLC2站4: 读码器读码完成，写入 D50405=1。");
                                        }
                                        catch (Exception ex) { Err("PLC3: 更新数据库失败: " + ex.Message); }
                                    }
                                    else Log("PLC3 等待 barcode3 超时。");
                                    barcodeTcs.TryRemove("PLC3_BC", out _);
                                }
                                else
                                {
                                    Log("PLC3 包装层数为2或不处理，跳过读码打印。");
                                }
                            }
                            else // 不需要读码，读取 D50410 并打印到打印机2（当 D52==1）
                            {
                                if (Stn4BPackNum == 1)
                                {
                                    Log("PLC3: 需要贴标但不需读码，读取 D50410 并打印到打印机2。");
                                    var code = await plc3.ReadStringAsync("D50410", 40);
                                    Log("PLC3 读取 D50410: " + code);
                                    // 通知 UI 更新 lblbarcode_4B
                                    try { OnStation4BCodeChanged?.Invoke($"D50410:{code}"); } catch { }
                                    bool bol = await recorder?.ExistsByInternalAsync(code);
                                    if (bol)
                                    {//  PLC条码即内部流水标签码；如不是请根据现场调整
                                        UniqueIdGenerator.ParseInternalLabel(code, out string vendor, out int seq, out string date);
                                        string printLabel = UniqueIdGenerator.GetPrintLabelCode(code);
                                        string[] strName = new string[] { "Var0" };//string[] strName = new string[] { "张三", "李四", "王五" };
                                        string[] strValue = new string[] { printLabel };
                                        var result = printer1.Print(GetPrinterNameFor(2), Path.Combine(Application.StartupPath, "LAB1.lab"), 1, strName, strValue);   // 执行打印 
                                        if (result.IsSuccess) // if (codePrint.CodesoftUse(strName, strValue, filePath, lastfilepath, strPrinterName, 1))
                                        {
                                            ini.SetValue("APP", "PRINTER2_LastID", printLabel);
                                            Log("PLC2: 已发送资料到打印机 print_label_code (internal=" + code + ").");
                                        }
                                        else { Log("ERROR,Labeling machine station printing failed, please check the log to see the reason"); }
                                        // SendToPrinter(GetPrinterNameFor(2), seq + date);
                                        try
                                        {
                                            await recorder?.UpdatePrintLabelAsync(code);
                                            await plc3.WriteintAsync("D50405", 1); // D50405 处理完成
                                            Log("PLC2站4: 读码器读码完成，写入 D50405=1。");
                                        }
                                        catch (Exception ex) { Err("PLC3: 更新数据库失败: " + ex.Message); }
                                    }
                                    else
                                    {
                                        Log("PLC3 未查询到该流水码 " + code);
                                    }
                                }
                                else Log("PLC3 包装层数为2，无需处理。");
                            }
                        }
                        else
                        {
                            Log("PLC3: 配置为不贴标 (D50402=2)，跳过打印处理。");
                        }
                    }
                }
                catch (Exception ex) { Err("站4 异常: " + ex.Message); }

                await Task.Delay(150, token);
            }
        }

        async Task MonitorRePrint(CancellationToken token)
        {
            Log("站点重新贴标站 监控已启动（PLC2）（PLC3）。");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var Stn4ARePrintSignal = await plc2.ReadintAsync("D60101");
                   // Log($"到位信号：D60101={Stn4ARePrintSignal}");
                    if (Stn4ARePrintSignal == 1)
                    {
                        Log("1号列印站 到位 (D60101=1)。读取产品 ID (D60110)。");
                        var idVal = await plc2.ReadStringAsync("D60110", 20);
                        Log("1号列印站 读取到 D60110: " + idVal);
                        string printcode = UniqueIdGenerator.GetPrintLabelCode(idVal);
                        var ModelNum = await plc1.ReadintAsync("D50500");//机种号
                        Log("1号列印站 读取到 50500 机种号: " + ModelNum);
                        string printLabel = ini.GetValue("APP", "PRINTER1_LastID");
                        // 当打印机最后一次记录的 printLabel 与 预计的 printcode 不一致时，弹窗等待人工确认（继续/取消）
                        if (!string.Equals(printLabel, printcode, StringComparison.Ordinal))
                        {
                            try
                            {
                                var shownPrintLabel = string.IsNullOrEmpty(printLabel) ? "<空>" : printLabel;
                                var shownPrintCode = string.IsNullOrEmpty(printcode) ? "<空>" : printcode;
                                var msg = $"检测到打印 ID 不匹配：\r\n  打印机最后记录: {shownPrintLabel}\r\n  预计打印码: {shownPrintCode}\r\n\r\n是否继续本次重新贴标操作？\r\n选择【是】继续，选择【否】取消本次操作。";
                                var dr = MessageBox.Show(msg, "打印 ID 不匹配", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                                if (dr == DialogResult.No)
                                {
                                    Log("人工取消：打印 ID 不匹配。");
                                    // 跳过本次循环，继续等待下一到位信号
                                    continue;
                                }
                                else
                                {
                                    Log("人工确认：继续执行重新贴标。");
                                    string[] strName = new string[] { "Var0" };
                                    string[] strValue = new string[] { printcode };
                                    var result = printer1.Print(GetPrinterNameFor(1), Path.Combine(Application.StartupPath, "LAB1.lab"), 1, strName, strValue);   // 执行打印 
                                    if (result.IsSuccess)
                                    {
                                        Log("1号列印站: 已发送资料到打印机 print_label_code (internal=" + idVal + ").");
                                        try
                                        {
                                            ini.SetValue("APP", "PRINTER1_LastID", printLabel);
                                            Log("已保存打印ID到配置: PRINTER1_LastID=" + printLabel);
                                        }
                                        catch (Exception ex) { Err("保存 PRINTER1_LastID 到配置失败: " + ex.Message); }
                                    }
                                    else { Log("ERROR,Labeling machine station printing failed, please check the log to see the reason"); }
                                    try
                                    {
                                        await recorder?.UpdatePrintLabelAsync(idVal);
                                        await plc2.WriteintAsync("D60405", 1); // D60405 处理完成
                                        Log("1号列印站: 读码器读码完成，写入 D60405=1。");
                                    }
                                    catch (Exception ex) { Err("1号列印站: 更新数据库失败: " + ex.Message); }
                                }
                            }
                            catch (Exception ex)
                            {
                                Err("显示打印 ID 不匹配对话框失败: " + ex.Message);
                                // 如果弹窗失败则按继续处理，避免阻塞生产线
                            }
                        }
                        else
                        {
                            string[] strName = new string[] { "Var0" };
                            string[] strValue = new string[] { printLabel };
                            var result = printer1.Print(GetPrinterNameFor(1), Path.Combine(Application.StartupPath, "LAB1.lab"), 1, strName, strValue);   // 执行打印 
                            if (result.IsSuccess)
                            {
                                Log("1号列印站: 已发送资料到打印机 print_label_code (internal=" + idVal + ").");
                                try
                                {
                                    ini.SetValue("APP", "PRINTER1_LastID", printLabel);
                                    Log("已保存打印ID到配置: PRINTER1_LastID=" + printLabel);
                                }
                                catch (Exception ex) { Err("保存 PRINTER1_LastID 到配置失败: " + ex.Message); }
                            }
                            else { Log("ERROR,Labeling machine station printing failed, please check the log to see the reason"); }
                            try
                            {
                                await recorder?.UpdatePrintLabelAsync(idVal);
                                await plc2.WriteintAsync("D60405", 1); // D60405 处理完成
                                Log("1号列印站: 读码器读码完成，写入 D60405=1。");
                            }
                            catch (Exception ex) { Err("1号列印站: 更新数据库失败: " + ex.Message); }
                        }
                    }
                    var Stn4BRePrintSignal = await plc3.ReadintAsync("D70101");
                    //Log($"到位信号：D70101={Stn4BRePrintSignal}");
                    if (Stn4BRePrintSignal == 1)
                    {


                    }
                }
                catch (Exception ex) { Err("列印站 异常: " + ex.Message); }
                await Task.Delay(120, token);
            }
        }

        // 视觉消息统一入口（格式: "<序号>#<结果>;<原因>" ）
        void VisionReceived(string text)
        {
            try
            {
                if (MessageParser.TryParseVision(text, out var vr))
                {
                    Log("视觉消息解析: " + vr);
                    // 仅当对应的待处理任务存在且到位信号匹配时，才分发
                    if (visionTcs.TryRemove(vr.Index, out var tcs))
                    {
                        tcs.SetResult(vr);
                    }
                    else
                    {
                        Log("没有等待此视觉序号: " + vr.Index);
                    }
                }
                else
                {
                    Log("视觉消息解析失败: " + text);
                }
            }
            catch (Exception ex) { Err("VisionReceived 异常: " + ex.Message); }
        }

        // barcode1 仅用于站1
        void Barcode1Received(string text)
        {
            try
            {
                var code = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                Log("读码器1 收到: " + code);
                if (barcodeTcs.TryRemove("S1", out var tcsS1))
                {
                    tcsS1.SetResult(code);
                    return;
                }
                // 其他情况不接受 barcode1 的数据
                Log("读码器1 当前没有等待任务（仅用于站1）。");
            }
            catch (Exception ex) { Err("Barcode1Received 异常: " + ex.Message); }
        }

        // barcode2 只用于打印站（PLC2），不会为站1提供条码
        void Barcode2Received(string text)
        {
            try
            {
                var code = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                Log("读码器2 收到: " + code);
                if (barcodeTcs.TryRemove("PLC2_BC", out var tcsP2))
                {
                    tcsP2.SetResult(code);
                    return;
                }
                Log("读码器2 当前没有等待打印任务。");
            }
            catch (Exception ex) { Err("Barcode2Received 异常: " + ex.Message); }
        }

        // barcode3 只用于打印站（PLC3）
        void Barcode3Received(string text)
        {
            try
            {
                var code = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                Log("读码器3 收到: " + code);
                if (barcodeTcs.TryRemove("PLC3_BC", out var tcsP3))
                {
                    tcsP3.SetResult(code);
                    return;
                }
                // 有时 barcode3 也可作为备份填补 PLC2 的需求（按需调整）
                if (barcodeTcs.TryRemove("PLC2_BC", out var tcsBackup))
                {
                    tcsBackup.SetResult(code);
                    return;
                }
                Log("读码器3 当前没有等待打印任务。");
            }
            catch (Exception ex) { Err("Barcode3Received 异常: " + ex.Message); }
        }

        // 占位：将条码数据发送到指定打印机（替换为 Codesoft 模板打印）
        void SendToPrinter(string printerName, string data)
        {
            Log($"打印调用 -> 打印机:'{printerName}' , 数据:'{data}'");
        }

        // 根据编号返回打印机名称（从配置或默认）
        /// <summary>
        /// 
        /// </summary>
        /// <param name="使用的是Printer1还是Printer2"></param>
        /// <returns></returns>
        string GetPrinterNameFor(int printerIndex)
        {
            // 从 ini 获取打印机名称（如果配置了）
            try
            {
                var key = printerIndex == 1 ? "PRINTER1_NAME" : "PRINTER2_NAME";
                var name = ini.GetValue("PRINTER", key);
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch { }
            return printerIndex == 1 ? "Printer1" : "Printer2";
        }

        public void Dispose()
        {
            Stop();
        }
    }
}