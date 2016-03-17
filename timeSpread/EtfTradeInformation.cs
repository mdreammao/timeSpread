using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace timeSpread
{
    /// <summary>
    /// 获取50etf的交易数据。
    /// </summary>
    class EtfTradeInformation
    {
        /// <summary>
        /// 记录给定日期50etf的盘口价格
        /// </summary>
        public List<tradeInformation> myTradeInformation = new List<tradeInformation>();
        public double volatility = 0;
        public double maxPrice = 0, miLastPrice = 0;
        /// <summary>
        /// 静态列表。记录50etf每日的收盘价。
        /// </summary>
        public static SortedDictionary<int, double> myEtfClose;
        /// <summary>
        /// 根据给定的时刻，获取价格盘口数据。
        /// </summary>
        /// <param name="time">时刻</param>
        /// <returns>返回价格盘口信息</returns>
        public tradeInformation GetTradeInformation(int time)
        {
            tradeInformation trade0 = new tradeInformation();
            for (int i = 0; i < myTradeInformation.Count - 1; i++)
            {
                if (myTradeInformation[i].time <= time && myTradeInformation[i + 1].time > time)
                {
                    trade0 = myTradeInformation[i];
                }
            }
            return trade0;
        }
        /// <summary>
        /// 给出最高的价格。
        /// </summary>
        /// <returns></returns>
        public double GetMaxPrice()
        {
            foreach (var item in myTradeInformation)
            {
                maxPrice = (maxPrice > item.lastPrice) ? maxPrice : item.lastPrice;
            }
            return maxPrice;
        }
        /// <summary>
        /// 给出最低的价格。
        /// </summary>
        /// <returns></returns>
        public double GetMiLastPrice()
        {
            foreach (var item in myTradeInformation)
            {
                miLastPrice = (miLastPrice < item.lastPrice && miLastPrice > 0) ? miLastPrice : item.lastPrice;
            }
            return miLastPrice;
        }
        /// <summary>
        /// 获取日内历史波动率。
        /// </summary>
        /// <returns>返回历史波动率</returns>
        public double GetVolatility()
        {
            return 0;
        }
        /// <summary>
        /// 构造函数。获取特定日期的，特定时刻之内的盘口tick数据。
        /// </summary>
        /// <param name="thisday">日期</param>
        /// <param name="startTime">开始时刻</param>
        /// <param name="endTime">结束时刻</param>
        public EtfTradeInformation(int thisday, int startTime = 93000000, int endTime = 150000000)
        {
            using (SqlConnection conn = new SqlConnection(TradeDay.GetConnectString()))
            {
                conn.Open();//打开数据库  
                SqlCommand cmd = conn.CreateCommand();
                tradeInformation myTradeInformation0 = new tradeInformation();
                try
                {//从数据库中读取数据流存入reader中  
                    cmd.CommandText = "select * from "+Configuration.tableOf50ETF+ " where [Date]=" + Convert.ToString(thisday) + " and (([Time]>=93000000 and [Time]<=113000000) or ([Time]>=130000000 and [Time]<=150000000)) and [Time]>=" + Convert.ToString(startTime) + " and [Time]<= " + Convert.ToString(endTime);
                    SqlDataReader reader = cmd.ExecuteReader();
                    //从reader中读取下一行数据,如果没有数据,reader.Read()返回flase  
                    while (reader.Read())
                    {
                        myTradeInformation0.time = reader.GetInt32(reader.GetOrdinal("Time")) + (reader.GetInt32(reader.GetOrdinal("Tick")) )  * 500;
                        myTradeInformation0.lastPrice = reader.GetDouble(reader.GetOrdinal("LastPrice"));
                        myTradeInformation0.ask = new double[5];
                        myTradeInformation0.ask[0] = reader.GetDouble(reader.GetOrdinal("Ask1"));
                        myTradeInformation0.ask[1] = reader.GetDouble(reader.GetOrdinal("Ask2"));
                        myTradeInformation0.ask[2] = reader.GetDouble(reader.GetOrdinal("Ask3"));
                        myTradeInformation0.ask[3] = reader.GetDouble(reader.GetOrdinal("Ask4"));
                        myTradeInformation0.ask[4] = reader.GetDouble(reader.GetOrdinal("Ask5"));
                        myTradeInformation0.askv = new int[5];
                        myTradeInformation0.askv[0] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("Askv1")));
                        myTradeInformation0.askv[1] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("Askv2")));
                        myTradeInformation0.askv[2] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("Askv3")));
                        myTradeInformation0.askv[3] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("Askv4")));
                        myTradeInformation0.askv[4] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("Askv5")));
                        myTradeInformation0.bid = new double[5];
                        myTradeInformation0.bid[0] = reader.GetDouble(reader.GetOrdinal("Bid1"));
                        myTradeInformation0.bid[1] = reader.GetDouble(reader.GetOrdinal("Bid2"));
                        myTradeInformation0.bid[2] = reader.GetDouble(reader.GetOrdinal("Bid3"));
                        myTradeInformation0.bid[3] = reader.GetDouble(reader.GetOrdinal("Bid4"));
                        myTradeInformation0.bid[4] = reader.GetDouble(reader.GetOrdinal("Bid5"));
                        myTradeInformation0.bidv = new int[5];
                        myTradeInformation0.bidv[0] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("Bidv1")));
                        myTradeInformation0.bidv[1] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("Bidv2")));
                        myTradeInformation0.bidv[2] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("Bidv3")));
                        myTradeInformation0.bidv[3] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("Bidv4")));
                        myTradeInformation0.bidv[4] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("Bidv5")));
                        if (myTradeInformation0.ask[0] * myTradeInformation0.askv[0] * myTradeInformation0.bid[0] * myTradeInformation0.bidv[0] != 0)
                        {
                            myTradeInformation.Add(myTradeInformation0);
                        }

                    }
                    reader.Close();
                }
                catch (Exception myerror)
                {
                    System.Console.WriteLine(myerror.Message);
                }
                finally
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
        }
        /// <summary>
        /// 静态函数。从数据库中读取当前日期50etf的收盘价。主要用于维持保证金的计算。
        /// </summary>
        /// <param name="today">日期</param>
        /// <returns>当日结算价</returns>
        public static  double getEtfCloseInformation(int today)
        {
            double close = 0.0;
            if (myEtfClose==null)
            {
                myEtfClose = new SortedDictionary<int, double>();
                using (SqlConnection conn = new SqlConnection(TradeDay.GetConnectString()))
                {
                    conn.Open();//打开数据库  
                    SqlCommand cmd = conn.CreateCommand();
                    try
                    {
                        cmd.CommandText = "select distinct(PreClose),Date from sh510050 order by Date";
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            int date = reader.GetInt32(reader.GetOrdinal("Date"));
                            int yesterday = TradeDay.GetPreviousTradeDay(date);
                            double myPreClose = reader.GetDouble(reader.GetOrdinal("PreClose"));
                            if (yesterday>0)
                            {
                                myEtfClose.Add(yesterday, myPreClose);
                            }
                        }
                    }
                    catch (Exception myerror)
                    {
                        System.Console.WriteLine(myerror.Message);
                    }
                    finally
                    {
                        conn.Close();
                        conn.Dispose();
                    }
                }
            }
            if (myEtfClose.ContainsKey(today))
            {
                close = myEtfClose[today];
            }
            return close;
        }
    }
}
