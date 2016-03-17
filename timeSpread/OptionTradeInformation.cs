using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace timeSpread
{
    
    /// <summary>
    /// 获取期权合约的交易数据。提供方法读取盘口tick数据。
    /// </summary>
    class OptionTradeInformation
    {
        /// <summary>
        /// 存储盘口tick数据的表。
        /// </summary>
        public List<tradeInformation> myTradeInformation = new List<tradeInformation>();
        /// <summary>
        /// 从数据表myTradeInformation中查询给定时刻的盘口数据。
        /// </summary>
        /// <param name="time">查询时刻</param>
        /// <returns>返回给定时刻的盘口数据</returns>
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
        /// 从数据库中获取给定期权合约在特定日期中的盘口数据，存储在私有表myTradeInformation中。
        /// </summary>
        /// <param name="optionCode">期权合约代码</param>
        /// <param name="thisday">日期</param>
        /// <param name="startTime">开始时刻</param>
        /// <param name="endTime">结束时刻</param>
        public OptionTradeInformation(int optionCode=0, int thisday=0, int startTime = 93000000, int endTime = 150000000)
        {
            if (optionCode>0 && OptionCodeInformation.IsExists(optionCode))
            {
                using (SqlConnection conn = new SqlConnection(TradeDay.GetConnectString()))
                {
                    conn.Open();//打开数据库  
                    SqlCommand cmd = conn.CreateCommand();
                    tradeInformation myTradeInformation0 = new tradeInformation();
                    try
                    {//从数据库中读取数据流存入reader中  
                        cmd.CommandText = "select * from sh" + Convert.ToString(optionCode) + " where Date=" + Convert.ToString(thisday) + " and ((Time>=93000000 and Time<=113000000) or (Time>=130000000 and Time<=150000000)) and Time>=" + Convert.ToString(startTime) + " and Time<= " + Convert.ToString(endTime);
                        SqlDataReader reader = cmd.ExecuteReader();
                        //从reader中读取下一行数据,如果没有数据,reader.Read()返回flase  
                        while (reader.Read())
                        {
                            myTradeInformation0.time = reader.GetInt32(reader.GetOrdinal("Time")) + reader.GetInt32(reader.GetOrdinal("Tick"))  * 500;
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
                            //仅存储有效的数据。若盘口数据质量不达标(例如买一卖一的价或者量为0)，认为是无效数据。
                            if (myTradeInformation0.ask[0] * myTradeInformation0.askv[0] * myTradeInformation0.bid[0] * myTradeInformation0.bidv[0] != 0 && myTradeInformation0.ask[0]!= myTradeInformation0.bid[0])
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
        }
        
    }
}
