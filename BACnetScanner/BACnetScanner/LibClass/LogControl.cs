
using System.IO;
using System;
using System.Diagnostics;

namespace BACnetScanner.LibClass
{
    public class LogControl
    {
        /// <summary>
        /// 디버그 모드로 실행시 콘솔창으로 로그 출력하는 함수
        /// </summary>
        /// <param name="message">로그 내용</param>
        public void ConsoleLogWrite(string message)
        {
            try
            {
                Console.WriteLine(message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 파일로 로그 출력하는 함수
        /// </summary>
        /// <param name="strFnName">함수명 or 로그타이틀</param>
        /// <param name="strData">로그 내용</param>
        public void LogWrite(string strFnName, string strData)
        {
            DirectoryInfo topDir = Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location);
            string PreDirPath = topDir.Parent.FullName;
            string FolderPath = string.Format("{0}\\{1}\\{2}", "Log", DateTime.Now.ToString("yyyyMM"), DateTime.Now.ToString("dd"));
            string DirPath = Path.Combine(PreDirPath, FolderPath);
            string FilePath = DirPath + string.Format("\\{0}.log", "BACnetScanner");
            string temp;

            DirectoryInfo di = new DirectoryInfo(DirPath);
            FileInfo fi = new FileInfo(FilePath);

            try
            {
                if (!di.Exists) Directory.CreateDirectory(DirPath);
                if (!fi.Exists)
                {
                    using (StreamWriter sw = new StreamWriter(FilePath))
                    {
                        temp = string.Format("[{0}] {1} : {2}", DateTime.Now, strFnName, strData);
                        ConsoleLogWrite(temp);
                        sw.WriteLine(temp);
                        sw.Close();
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(FilePath))
                    {
                        temp = string.Format("[{0}] {1} : {2}", DateTime.Now, strFnName, strData);
                        ConsoleLogWrite(temp);
                        sw.WriteLine(temp);
                        sw.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}
