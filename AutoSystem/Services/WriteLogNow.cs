using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSystem

{
    public class WriteLogNow
    {
        private string fileName;
        private string strPath;
        private string strPath2;
        private string strBefore = "LOG_";
        public WriteLogNow()
        { }

        public WriteLogNow(string strName)
        {
            strBefore = strBefore + strName;
        }

        public void WriteLog(string strLog)
        {
            try
            {
                if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\log") == false)
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\log");
                }
                  string path = AppDomain.CurrentDomain.BaseDirectory + "\\log\\"+ strBefore + System.DateTime.Now.ToString("yyyyMMdd") + ".txt";
                FileInfo file = new FileInfo(path);
                if (!file.Exists)
                {
                    FileStream fs2;
                    fs2 = File.Create(path);
                    fs2.Close();

                }
              

                FileStream fs = new FileStream(path, FileMode.Append);
                StreamWriter sw1 = new StreamWriter(fs, Encoding.Default);  //字体编码是默认的
              //  strLog = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff") + "-->" + strLog;
                sw1.WriteLine(strLog);
                sw1.Flush();
                sw1.Close();
            }
            catch (Exception ex) { }

        }
 
        public  string TraceMethodInfo()
        {
            // 注意：StackTrace的参数1非常重要，表示获得父一级函数调用的相关信息。
            // 如果修改为0，则返回本身的行列信息，即TraceMethodInfo()函数的信息。
            StackFrame st = new System.Diagnostics.StackTrace(1, true).GetFrame(0);
            string info = string.Format("{0}->{1}@{2}:{3}",
                st.GetMethod().DeclaringType.FullName,  // 类全名
                st.GetMethod().Name,                    // 方法名
                st.GetFileLineNumber(),                 // 所在行号
                st.GetFileColumnNumber());              // 所有列号
            return info;
        }

    }
}
