using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HslCommunication;
using MySql.Data.MySqlClient;
namespace AutoSystem.Services
{
    public class InspectionRecord
    {
        public string InternalLabelCode { get; set; }
        public string PrintLabelCode { get; set; }
        public string CoverSn { get; set; }
        public string BodySn { get; set; }
        public string BuckleResult { get; set; }
        public string BuckleNgCode { get; set; }
        public DateTime FeedTime { get; set; }
        public string Station1Result { get; set; }
        public string TopCoverResult { get; set; }
        public string TopCoverNgCode { get; set; }
        public DateTime? TopDetectionTime { get; set; }
        public string Side1Result { get; set; }
        public string Side1NgCode { get; set; }
        public string Side2Result { get; set; }
        public string Side2NgCode { get; set; }
        public string Side3Result { get; set; }
        public string Side3NgCode { get; set; }
        public string Side4Result { get; set; }
        public string Side4NgCode { get; set; }
        public DateTime? Side4DetectionTime { get; set; }
        public DateTime? PrintTime { get; set; }
    }

    public class InspectionRecorder
    {
        readonly string connString;

        public InspectionRecorder(IniFile ini)
        {// 尝试在运行时启用 TLS1.2（兼容 .NET 4.5.1）
            try
            {
                // SecurityProtocolType.Tls12 有时在旧框架枚举不可用，使用数值 3072 以兼容
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            }
            catch { }
            try
            {
                var conn = ini.GetValue("MYSQL", "MYSQL_CONN");
                if (!string.IsNullOrEmpty(conn))
                {
                    connString = conn;
                    // 如果用户没有显式指定 SslMode，可以临时在本地追加以排查问题
                    if (connString.IndexOf("SslMode", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        connString = connString.TrimEnd(';') + ";SslMode=None;";
                    }
                    return;
                }
            }
            catch { }

            var server = ini.GetValue("MYSQL", "HOST") ?? "192.168.10.101";
            var port = ini.GetValue("MYSQL", "PORT") ?? "3306";
            var database = ini.GetValue("MYSQL", "DB") ?? "mydatabase";
            var user = ini.GetValue("MYSQL", "USER") ?? "root";
            var password = ini.GetValue("MYSQL", "PASSWORD") ?? "";

            connString = $"Server={server};Port={port};Database={database};Uid={user};Pwd={password};CharSet=utf8mb4;SslMode=none;";
        }

        public async Task InsertAsync(InspectionRecord r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));

            const string sql = @"INSERT INTO product_inspection_record
                                (internal_label_code, print_label_code, cover_sn, body_sn,
                                buckle_result, buckle_ng_code, feed_time,Station1_Result,
                                top_cover_result, top_cover_ng_code,Top_DetectionTime,
                                side1_result, side1_ng_code, side2_result, side2_ng_code,
                                side3_result, side3_ng_code, side4_result, side4_ng_code,Side4_DetectionTime,
                                print_time)
                                VALUES
                                (@internal_label_code, @print_label_code, @cover_sn, @body_sn,
                                @buckle_result, @buckle_ng_code, @feed_time,@Station1_Result,
                                @top_cover_result, @top_cover_ng_code,@Top_DetectionTime,
                                @side1_result, @side1_ng_code, @side2_result, @side2_ng_code,
                                @side3_result, @side3_ng_code, @side4_result, @side4_ng_code,@Side4_DetectionTime,
                                @print_time);";
            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@internal_label_code", (object)r.InternalLabelCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@print_label_code", (object)r.PrintLabelCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cover_sn", (object)r.CoverSn ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@body_sn", (object)r.BodySn ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@buckle_result", (object)r.BuckleResult ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@buckle_ng_code", (object)r.BuckleNgCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@feed_time", r.FeedTime);
                    cmd.Parameters.AddWithValue("@Station1_Result", (object)r.Station1Result ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@top_cover_result", (object)r.TopCoverResult ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@top_cover_ng_code", (object)r.TopCoverNgCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Top_DetectionTime", r.TopDetectionTime.HasValue ? (object)r.TopDetectionTime.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@side1_result", (object)r.Side1Result ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@side1_ng_code", (object)r.Side1NgCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@side2_result", (object)r.Side2Result ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@side2_ng_code", (object)r.Side2NgCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@side3_result", (object)r.Side3Result ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@side3_ng_code", (object)r.Side3NgCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@side4_result", (object)r.Side4Result ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@side4_ng_code", (object)r.Side4NgCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Side4_DetectionTime", r.Side4DetectionTime.HasValue ? (object)r.Side4DetectionTime.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@print_time", r.PrintTime.HasValue ? (object)r.PrintTime.Value : DBNull.Value);

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
        /// <summary>
        /// 查询内部标签码，数据库查一下有没有这条记录，有就返回 true，没有就返回 false。
        /// </summary>
        /// <param name="internalLabel"></param>
        /// <returns></returns>
        public async Task<bool> ExistsByInternalAsync(string internalLabel)
        {
            if (string.IsNullOrWhiteSpace(internalLabel)) return false;
            const string sql = "SELECT COUNT(1) FROM product_inspection_record WHERE internal_label_code = @internal_label_code";
            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@internal_label_code", internalLabel);
                    var o = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                    if (o == null || o == DBNull.Value) return false;
                    var cnt = Convert.ToInt32(o);
                    return cnt > 0;
                }
            }
        }
        public async Task Updatecover_snAsync(string internalLabel, string cover_sn)
        {
            if (string.IsNullOrWhiteSpace(internalLabel)) return;
            const string sql = @"UPDATE product_inspection_record
                                SET cover_sn = @cover_sn,
                                update_time = CURRENT_TIMESTAMP
                                WHERE internal_label_code = @internal_label_code";
            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@cover_sn", (object)cover_sn ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@internal_label_code", internalLabel);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
        public async Task UpdateTopCoverAsync(string internalLabel, string topCoverResult, string topCoverNgCode)
        {
            if (string.IsNullOrWhiteSpace(internalLabel)) return;
            const string sql = @"UPDATE product_inspection_record
                                SET top_cover_result = @top_cover_result,
                                top_cover_ng_code = @top_cover_ng_code,
                                Top_DetectionTime = @Top_DetectionTime,
                                update_time = CURRENT_TIMESTAMP
                                WHERE internal_label_code = @internal_label_code";
            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@top_cover_result", (object)topCoverResult ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@top_cover_ng_code", (object)topCoverNgCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Top_DetectionTime", DateTime.Now);
                    cmd.Parameters.AddWithValue("@internal_label_code", internalLabel);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task UpdateSideResultAsync(string internalLabel, int sideIndex, string sideResult, string sideNgCode)
        {
            if (string.IsNullOrWhiteSpace(internalLabel)) return;
            string colResult, colNg;
            switch (sideIndex)
            {
                case 1: colResult = "side1_result"; colNg = "side1_ng_code"; break;
                case 2: colResult = "side2_result"; colNg = "side2_ng_code"; break;
                case 3: colResult = "side3_result"; colNg = "side3_ng_code"; break;
                case 4: colResult = "side4_result"; colNg = "side4_ng_code"; break;
                default: throw new ArgumentOutOfRangeException(nameof(sideIndex));
            }
            var sql = $@"UPDATE product_inspection_record
                        SET {colResult} = @side_result,
                            {colNg} = @side_ng,
                            Side4_DetectionTime = @Side4_DetectionTime,
                            update_time = CURRENT_TIMESTAMP
                        WHERE internal_label_code = @internal_label_code";
            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@side_result", (object)sideResult ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@side_ng", (object)sideNgCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Side4_DetectionTime", DateTime.Now);
                    cmd.Parameters.AddWithValue("@internal_label_code", internalLabel);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
        /*
         SELECT internal_label_code  FROM product_inspection_record WHERE body_sn = '你要查询的盒体SN值'
            AND internal_label_code LIKE 'A02%20260901';
         */
        /// <summary>
        /// 根据盒体条码(body_sn)、厂商码和日期查找匹配的 internal_label_code（按 feed_time 降序取最新一条）。
        /// 返回 null 表示未找到。
        /// </summary>
        public async Task<string> FindInternalByBodyAsync(string bodySn, string vendorCode, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(bodySn) || string.IsNullOrWhiteSpace(vendorCode)) return null;
            var dateStr = date.ToString("yyyyMMdd");
            var likePattern = vendorCode + "%" + dateStr;

            const string sql = @"SELECT internal_label_code FROM product_inspection_record WHERE body_sn = @body_sn  AND internal_label_code LIKE @like_pattern ORDER BY feed_time DESC LIMIT 1;";
            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@body_sn", bodySn);
                    cmd.Parameters.AddWithValue("@like_pattern", likePattern);
                    var o = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                    if (o == null || o == DBNull.Value) return null;
                    return o.ToString();
                }
            }
        }
        public async Task UpdatePrintLabelAsync(string internalLabel)
        {
            if (string.IsNullOrWhiteSpace(internalLabel)) return;
            const string sql = @"UPDATE product_inspection_record
                                SET print_time = @print_time,
                                update_time = CURRENT_TIMESTAMP
                                WHERE internal_label_code = @internal_label_code";
            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    //cmd.Parameters.AddWithValue("@print_label_code", (object)printLabelCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@print_time", DateTime.Now);
                    cmd.Parameters.AddWithValue("@internal_label_code", internalLabel);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// 获取指定 internal_label_code 的 total_result（优先读取表中 total_result 字段，如果不存在则根据四面结果推断 OK/NG）。
        /// 返回 null 表示未查到记录。
        /// </summary>
        public async Task<string> GetTotalResultAsync(string internalLabel)
        {
            if (string.IsNullOrWhiteSpace(internalLabel)) return null;

            const string sql = @" SELECT COALESCE( (SELECT total_result FROM product_inspection_record WHERE internal_label_code = @internal_label_code LIMIT 1),
                            (SELECT CASE 
                                        WHEN Station1_Result = 'OK' AND top_cover_result = 'OK'AND side1_result = 'OK' AND side2_result = 'OK' AND side3_result = 'OK' AND side4_result = 'OK' THEN 'OK' 
                                        ELSE 'NG' 
                                        END FROM product_inspection_record WHERE internal_label_code = @internal_label_code LIMIT 1)
                            ) AS total_result;";

            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@internal_label_code", internalLabel);
                    var o = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                    if (o == null || o == DBNull.Value) return null;
                    return o.ToString();
                }
            }
        }
    }
}
