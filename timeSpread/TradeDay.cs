using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data.SqlClient;

namespace timeSpread
{
    
    /// <summary>
    /// 获取交易日期信息的类。
    /// </summary>
    class TradeDay
    {
        /// <summary>
        /// 静态变量。记录本地路径。
        /// </summary>
        public static  string filePathName;
        /// <summary>
        /// 存储历史的交易日信息。
        /// </summary>
        private static List<int> TradeDayOfDataBase;
        /// <summary>
        /// 存储2016年交易日信息。
        /// </summary>
        private static List<int> TradeDayOf2016;
        /// <summary>
        /// 存储所有回测时期内的交易日信息。
        /// </summary>
        public List<int> MyTradeDays { get; set; }
        /// <summary>
        /// 存储每日每个tick对应的时刻。
        /// </summary>
        public static int[] MyTradeTicks { get; set; }
        /// <summary>
        /// 静态函数。将数组下标转化为具体时刻。
        /// </summary>
        /// <param name="index">下标</param>
        /// <returns>时刻</returns>
        public static int indexToTime(int index)
        {
            int time0 = index * 500;
            int hour = time0 / 3600000;
            time0 = time0 % 3600000;
            int minute = time0 / 60000;
            time0 = time0 % 60000;
            int second = time0;
            if (hour<2)
            {
                hour += 9;
                minute += 30;
                if (minute>=60)
                {
                    minute -= 60;
                    hour += 1;
                }
            }
            else
            {
                hour += 11;
            }
            return hour * 10000000 + minute * 100000 + second;
        }
        /// <summary>
        /// 静态函数。提供数据库sql连接字符串信息。
        /// </summary>
        /// <returns>sql连接字符串</returns>
        public static string GetConnectString()
        {
            //远程服务器地址
            //return "server=192.168.1.165;database=OptionTick;uid=dfreader;pwd=dfreader;";
            //毛衡的台式机服务器地址
            //return "server=192.168.38.217;database=OptionTick;uid=reader;pwd=reader;";
            //本地服务器地址
            return "server=(local);database=OptionTick;Integrated Security=true;";
        }
        /// <summary>
        /// 静态函数。给出下一交易日。
        /// </summary>
        /// <param name="today">当前交易日</param>
        /// <returns>下一交易日</returns>
        public static int GetNextTradeDay(int today)
        {
            int nextIndex = TradeDayOfDataBase.FindIndex(delegate (int i) { return i == today; })+1;
            if (nextIndex>=TradeDayOfDataBase.Count)
            {
                return 0;
            }
            else
            {
                return TradeDayOfDataBase[nextIndex];
            }
        }
        /// <summary>
        /// 静态函数。给出前一交易日。
        /// </summary>
        /// <param name="today">当前交易日</param>
        /// <returns>返回前一交易日</returns>
        public static int GetPreviousTradeDay(int today)
        {
            int preIndex = TradeDayOfDataBase.FindIndex(delegate (int i) { return i == today; }) - 1;
            if (preIndex <0)
            {
                return 0;
            }
            else
            {
                return TradeDayOfDataBase[preIndex];
            }
        }
        /// <summary>
        /// 静态函数。获取交易日间隔天数。
        /// </summary>
        /// <param name="firstday">开始日期</param>
        /// <param name="lastday">结束日期</param>
        /// <returns>间隔天数</returns>
        public static int GetTimeSpan(int firstday, int lastday)
        {
            if (firstday >= TradeDayOfDataBase[0] && lastday <= TradeDayOfDataBase[TradeDayOfDataBase.Count - 1] && lastday >= firstday)
            {
                int startIndex = -1, endIndex = -1;
                for (int i = 0; i < TradeDayOfDataBase.Count; i++)
                {
                    if (TradeDayOfDataBase[i] == firstday)
                    {
                        startIndex = i;
                    }
                    if (TradeDayOfDataBase[i] > firstday && TradeDayOfDataBase[i - 1] < firstday)
                    {
                        startIndex = i;
                    }
                    if (TradeDayOfDataBase[i] == lastday)
                    {
                        endIndex = i;
                    }
                    if (TradeDayOfDataBase[i] > lastday && TradeDayOfDataBase[i - 1] < lastday)
                    {
                        endIndex = i - 1;
                    }
                }
                if (startIndex != -1 && endIndex != -1)
                {
                    return endIndex - startIndex + 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }

        }
        
        /// <summary>
        /// 获取2016交易日信息。
        /// </summary>
        private void getDaysFrom2016()
        {
            TradeDayOf2016 = new List<int>();
            List<int> HolidayOf2016 = new List<int>(){20160101,20160102,20160103,20160207,20160208,20160209,20160210,20160211,20160212,20160213,
20160402,20160403,20160404,20160430,20160501,20160502,20160609,20160610,20160611,20160915,20160916,20160917,20161001,20161002,20161003,20161004,20161005,20161006,20161007
            };
            bool isDate = false;
            int today = 0;
            for (int month = 1; month <= 12; month++)
            {
                for (int day = 1; day <= 31; day++)
                {
                    string strMonth = (month < 9) ? "0" + month.ToString() : month.ToString();
                    string strDay = (day < 9) ? "0" + day.ToString() : day.ToString();
                    string strDate = "2016-" + strMonth + "-" + strDay;
                    try
                    {
                        today = 20160000 + month * 100 + day;
                        DateTime.Parse(strDate);
                        DateTime mydatetime = Convert.ToDateTime(strDate);
                        isDate = true;
                        if ((int)(mydatetime.DayOfWeek) == 6 || (int)(mydatetime.DayOfWeek) == 0 || (HolidayOf2016.Find(delegate (int tmp) { return tmp == today; }) > 0))
                        {
                            isDate = false;
                        }
                    }
                    catch
                    {
                        isDate = false;
                    }

                    if (isDate == true)
                    {
                        TradeDayOf2016.Add(today);
                    }
                }
            }

        }
        /// <summary>
        /// 从本地文件中读取交易日信息
        /// </summary>
        /// <returns>返回是否读取成功</returns>
        private bool getDaysFromCsv()
        {
            List<string[]> ls = new List<string[]>();

            try
            {
                ls = CsvUtility.ReadCsv(filePathName);
            }
            catch (Exception myError)
            {
                System.Console.WriteLine(myError.Message);
            }
            if (ls.Count > 0)
            {
                TradeDayOfDataBase = new List<int>();
                foreach (var item in ls)
                {
                    foreach (var item0 in item)
                    {
                        TradeDayOfDataBase.Add(Convert.ToInt32(item0));
                    }
                }
                if (TradeDayOfDataBase.Count > 0)
                {
                    if (TradeDayOf2016 == null)
                    {
                        getDaysFrom2016();
                    }
                    foreach (var item in TradeDayOf2016)
                    {
                        if (item > TradeDayOfDataBase[TradeDayOfDataBase.Count - 1])
                        {
                            TradeDayOfDataBase.Add(item);
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 从数据库中读取历史交易日信息
        /// </summary>
        /// <returns>返回是否读取成功</returns>
        private bool getDaysFromDataBase()
        {

            TradeDayOfDataBase = new List<int>();
            //从数据库的表sh510050中读取交易日信息
            using (SqlConnection conn = new SqlConnection(GetConnectString()))
            {
                conn.Open();//打开数据库  
              //  Console.WriteLine("数据库打开成功!");
                //创建数据库命令  
                SqlCommand cmd = conn.CreateCommand();
                //创建查询语句  
                cmd.CommandText = "SELECT distinct(nDate) FROM [OptionTick].[dbo].[sh510050] where nDate>=20150209 order by nDate";
                try
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        TradeDayOfDataBase.Add(reader.GetInt32(reader.GetOrdinal("nDate")));
                    }
                }
                catch (Exception myError)
                {
                    System.Console.WriteLine(myError.Message);
                }
                finally
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
            //若成功读入交易日信息，将交易日信息存到本地文件
            if (TradeDayOfDataBase.Count > 0)
            {
                if (TradeDayOf2016 == null)
                {
                    getDaysFrom2016();
                }
                foreach (var item in TradeDayOf2016)
                {
                    if (item > TradeDayOfDataBase[TradeDayOfDataBase.Count - 1])
                    {
                        TradeDayOfDataBase.Add(item);
                    }
                }
                List<string[]> ls = new List<string[]>();
                foreach (var item in TradeDayOfDataBase)
                {
                    string[] ls0 = { item.ToString() };
                    ls.Add(ls0);
                }
                CsvUtility.WriteCsv(filePathName, ls);

            }
            //成功读取交易日信息返回true，否者返回false
            if (TradeDayOfDataBase.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 构造函数。参数为回测开始日期和回测结束日期。
        /// </summary>
        /// <param name="firstDay">开始日期</param>
        /// <param name="lastDay">结束日期</param>
        public TradeDay(int firstDay = 0, int lastDay = 0)
        {
            int today = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd"));
            if (filePathName == null)
            {
                filePathName = "allTradeDays" + DateTime.Now.ToString("yyyyMMdd") + ".csv";
            }
            if (lastDay < firstDay && lastDay != 0)
            {
                lastDay = 0;
                Console.WriteLine("Error: firstday > lastday!");
            }
            firstDay = Math.Max(20150209, firstDay);
            if (firstDay >= today)
            {
                firstDay = today;
                lastDay = today;
            }
            if (lastDay > today || lastDay == 0)
            {
                lastDay = today;
            }
            bool getData = false;
            if (TradeDayOfDataBase == null)
            {
                getData = getDaysFromCsv();
                if (getData == false)
                {
                    getData = getDaysFromDataBase();
                }
               
            }
            if (getData || TradeDayOfDataBase!=null)
            {
               // Console.WriteLine("Initial tradedays success!");
                foreach (int item in TradeDayOfDataBase)
                {
                    if (item >= firstDay && item <= lastDay)
                    {
                        if (MyTradeDays == null)
                        {
                            MyTradeDays = new List<int>();
                        }
                        MyTradeDays.Add(item);
                    }
                }

            }
            else
            {
                Console.WriteLine("These is no information of tradedays!");
            }
            if (MyTradeTicks == null)
            {
                MyTradeTicks = new int[28800];
            }
            for (int timeIndex = 0; timeIndex < 28800; timeIndex++)
            {
                MyTradeTicks[timeIndex] = indexToTime(timeIndex);
            }
        }
    }
}
