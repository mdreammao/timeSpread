using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace timeSpread
{
   
    /// <summary>
    /// 提供读取数据服务。利用sqlconnection连接sql服务器，并提供若干读取数据的方法。
    /// </summary>
    class DataBase
    {
        /// <summary>
        /// sql连接字符串，默认值为"server=(local);database=OptionTick;Integrated Security=true;"
        /// </summary>
        private string connectString = "server=(local);database=OptionTick;Integrated Security=true;";
        /// <summary>
        /// 获取sql连接字符串
        /// </summary>
        public string ConnectString
        {
            set { connectString = value; }
            get { return connectString; }
        }
        /// <summary>
        /// 日期。默认值为0。
        /// </summary>
        private int today = 0;
        /// <summary>
        /// 读取日期信息。
        /// </summary>
        public int Today
        {
            get { return today; }
            set
            {
                if (today>0)
                {
                    today = value;
                }
            }
        }
        /// <summary>
        /// 期权合约代码。
        /// </summary>
        private int optionCode;
        /// <summary>
        /// 读取期权合约代码。
        /// </summary>
        public int OptionCode
        {
            get { return optionCode; }
            set
            {
                if (optionCode>10000000 && optionCode<=10002000)
                {
                    optionCode = value;
                }
            }
        }
        /// <summary>
        /// 构造函数。读取期权合约信息以及日期信息。
        /// </summary>
        /// <param name="optionCode">期权合约代码</param>
        /// <param name="today">日期</param>
        public DataBase(int optionCode,int today)
        {
            this.today = today;
            this.optionCode = optionCode;
        }
        /// <summary>
        /// 从数据库读取给定期权的基本信息。
        /// </summary>
        /// <returns>返回基本信息</returns>
        public basicInformation GetBasicInformation()
        {
            basicInformation myInformation = new basicInformation();
            myInformation.optionCode = optionCode;
            myInformation.optionStrike = OptionInformation.myOptionList[optionCode].strike;
            myInformation.optionName = OptionInformation.myOptionList[optionCode].optionName;
            myInformation.startDate = OptionInformation.myOptionList[optionCode].startDate;
            myInformation.endDate = OptionInformation.myOptionList[optionCode].endDate;
            return myInformation;
        }
        /// <summary>
        /// 获取50etf价格的tick数据。主要内容包括交易时间，最新价，盘口价和量。
        /// </summary>
        /// <param name="today">日期</param>
        /// <param name="startTime">开始时刻</param>
        /// <param name="endTime">结束时刻</param>
        /// <returns>返回50etf的tick数据</returns>
        public List<tradeInformation> GetEtfTickData(int today, int startTime = 93000000, int endTime = 150000000)
        {
            List<tradeInformation>  myTradeInformation = new List<tradeInformation>();
            using (SqlConnection conn = new SqlConnection(TradeDay.GetConnectString()))
            {
                conn.Open();//打开数据库  
                SqlCommand cmd = conn.CreateCommand();
                tradeInformation myTradeInformation0 = new tradeInformation();
                try
                {//从数据库中读取数据流存入reader中  
                    cmd.CommandText = "select * from sh510050  where Date=" + Convert.ToString(today) + " and ((Time>=93000000 and Time<=113000000) or (Time>=130000000 and Time<=150000000)) and Time>=" + Convert.ToString(startTime) + " and Time<= " + Convert.ToString(endTime);
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
            return myTradeInformation;
        }
        /// <summary>
        /// 获取给定期权的tick数据。主要内容包括交易时间，最新价，盘口价和量。
        /// </summary>
        /// <param name="thisDay">日期</param>
        /// <param name="startTime">开始时刻</param>
        /// <param name="endTime">结束时刻</param>
        /// <returns>返回给定期权的tick数据</returns>
        public List<tradeInformation> GetTickData(int thisDay, int startTime = 93000000, int endTime = 150000000)
        {
            List<tradeInformation>  myTickData = new List<tradeInformation>();
            OptionCodeInformation code = new OptionCodeInformation(optionCode);
            if (OptionCodeInformation.IsExists(optionCode))
            {
                using (SqlConnection conn = new SqlConnection(TradeDay.GetConnectString()))
                {
                    conn.Open();//打开数据库  
                    SqlCommand cmd = conn.CreateCommand();
                    tradeInformation myTradeInformation0 = new tradeInformation();
                    try
                    {//从数据库中读取数据流存入reader中  
                        cmd.CommandText = "select * from sh" + Convert.ToString(optionCode) + " where [Date]=" + Convert.ToString(thisDay);
                        SqlDataReader reader = cmd.ExecuteReader();
                        //从reader中读取下一行数据,如果没有数据,reader.Read()返回flase  
                        while (reader.Read())
                        {
                            myTradeInformation0.time = reader.GetInt32(reader.GetOrdinal("Time")) + reader.GetInt32(reader.GetOrdinal("Tick")) * 500;
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
                            if (myTradeInformation0.ask[0] * myTradeInformation0.askv[0] * myTradeInformation0.bid[0] * myTradeInformation0.bidv[0] != 0 && (myTradeInformation0.ask[0] != myTradeInformation0.bid[0]))
                            {
                                myTickData.Add(myTradeInformation0);
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
            return myTickData;
        }
    }
}
