using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace BACnetScanner.LibClass
{
    public class SqliteDBService
    {

        private static string dirPath = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
        private static string dbPath = Path.Combine(dirPath, "point.db");

        /// <summary>
        /// DB Init 
        /// - 파일 삭제 후 재생성 
        /// - Device, Tag 테이블 생성
        /// </summary>
        public void fnDBInit()
        {
            try
            {
                // DB 파일 존재하면 삭제 후 생성 
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
                SQLiteConnection.CreateFile(dbPath);

                // DB 연결 후 테이블 생성
                using (SQLiteConnection conn = new SQLiteConnection($"Data Source={ dbPath }; Version=3;"))
                {
                    string query = string.Empty;
                    conn.Open();

                    query = "CREATE TABLE Device(" +
                        "deviceNo INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
                        "deviceId INTEGER NOT NULL, " +
                        "deviceAddress VARCHAR(100) NOT NULL);";
                    using(SQLiteCommand command = new SQLiteCommand(query, conn))
                    {
                        command.ExecuteNonQuery();
                    }

                    query = "CREATE TABLE Tag(" +
                        "tagNo INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
                        "deviceNo INTEGER NOT NULL, " +
                        "tagInstance INTEGER NOT NULL, " +
                        "tagType VARCHAR(100) NOT NULL, " +
                        "tagName VARCHAR(100) NOT NULL, " +
                        "tagValue VARCHAR(100) NOT NULL, " +
                        "tagDesc VARCHAR(100) NOT NULL);";
                    using (SQLiteCommand command = new SQLiteCommand(query, conn))
                    {
                        command.ExecuteNonQuery();
                    }
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// 디바이스 DB 조회
        /// </summary>
        /// <param name="deviceId">디바이스 고유 인스턴스 번호</param>
        /// <param name="deviceAddress">디바이스 주소(IP:Port)</param>
        public int fnSelectDeviceNo(uint deviceId, string deviceAddress)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection($"Data Source={ dbPath }; Version=3;"))
                {
                    conn.Open();
                    string query = "SELECT deviceNo FROM Device WHERE deviceId = @deviceId AND deviceAddress = @deviceAddress";

                    using (SQLiteCommand command = new SQLiteCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@deviceId", deviceId);
                        command.Parameters.AddWithValue("@deviceAddress", deviceAddress);

                        object result = command.ExecuteScalar();

                        if (result == null)
                        {
                            throw new Exception("No devices are being queried");
                        }

                        conn.Close();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return -1;
        }
        
        /// <summary>
        /// 디바이스 DB 삽입
        /// </summary>
        /// <param name="deviceId">디바이스 고유 인스턴스 번호</param>
        /// <param name="deviceAddress">디바이스 주소(IP:Port)</param>
        public void fnInsertDevice(uint deviceId, string deviceAddress)
        {
            fnExecuteNonQuery("INSERT INTO Device (deviceId, deviceAddress) VALUES (@deviceId, @deviceAddress)",
                new SQLiteParameter("@deviceId", deviceId),
                new SQLiteParameter("@deviceAddress", deviceAddress));
        }

        /// <summary>
        /// 태그 DB 삽입
        /// </summary>
        /// <param name="deviceNo">DB에 등록된 디바이스 번호</param>
        /// <param name="tagInstance">태그 고유 인스턴스 번호</param>
        /// <param name="tagType">태그 타입</param>
        /// <param name="tagName">태그 명칭</param>
        /// <param name="tagValue">태그 현재값</param>
        /// <param name="tagDesc">태그 설명</param>
        public void fnInsertTag(int deviceNo, uint tagInstance, string tagType, string tagName, string tagValue, string tagDesc)
        {
            fnExecuteNonQuery("INSERT INTO Tag (deviceNo, tagInstance, tagType, tagName, tagValue, tagDesc) VALUES (@deviceNo, @tagInstance, @tagType, @tagName, @tagValue, @tagDesc)",
                new SQLiteParameter("@deviceNo", deviceNo),
                new SQLiteParameter("@tagInstance", tagInstance),
                new SQLiteParameter("@tagType", tagType),
                new SQLiteParameter("@tagName", tagName),
                new SQLiteParameter("@tagValue", tagValue),
                new SQLiteParameter("@tagDesc", tagDesc));
        }

        /// <summary>
        /// 논쿼리 실행
        /// </summary>
        /// <param name="query">쿼리</param>
        /// <param name="parameters">파라미트 리스트</param>
        private void fnExecuteNonQuery(string query, params SQLiteParameter[] parameters)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection($"Data Source={ dbPath }; Version=3;"))
                {
                    conn.Open();
                    using (var command = new SQLiteCommand(query, conn))
                    {
                        command.Parameters.AddRange(parameters);
                        command.ExecuteNonQuery();
                    }
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}
