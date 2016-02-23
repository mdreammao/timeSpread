using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace timeSpread
{
    /// <summary>
    /// 记录期权合约基本信息，包括合约代码，合约名称，行权价，开始时间，到期时间
    /// </summary>
    struct basicInformation
    {
        public int optionCode;
        public string optionName;
        public double optionStrike;
        public int startDate;
        public int endDate;
    }
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
            using (SqlConnection conn = new SqlConnection(connectString))
            {
                conn.Open();//打开数据库  
                SqlCommand cmd = conn.CreateCommand();
                try
                {   
                    //从数据库中读取数据流存入reader中  
                    cmd.CommandText = "select * from sh" + optionCode.ToString() + " where nDate=" + Convert.ToString(today) + " and nTime>=93000000 and nTime<=93100000";
                    SqlDataReader reader = cmd.ExecuteReader();
                    //从reader中读取下一行数据,如果没有数据,reader.Read()返回flase  
                    while (reader.Read())
                    {
                        myInformation.optionCode = optionCode;
                        myInformation.optionStrike = Convert.ToDouble(reader.GetString(reader.GetOrdinal("strike")).Trim());
                        myInformation.optionName = reader.GetString(reader.GetOrdinal("optionname")).Trim();
                        myInformation.startDate = reader.GetInt32(reader.GetOrdinal("startdate"));
                        myInformation.endDate = reader.GetInt32(reader.GetOrdinal("enddate"));
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
                    cmd.CommandText = "select * from sh510050  where nDate=" + Convert.ToString(today) + " and ((nTime>=93000000 and nTime<=113000000) or (nTime>=130000000 and nTime<=150000000)) and nTime>=" + Convert.ToString(startTime) + " and nTime<= " + Convert.ToString(endTime);
                    SqlDataReader reader = cmd.ExecuteReader();
                    //从reader中读取下一行数据,如果没有数据,reader.Read()返回flase  
                    while (reader.Read())
                    {
                        myTradeInformation0.time = reader.GetInt32(reader.GetOrdinal("nTime")) + (reader.GetInt32(reader.GetOrdinal("nIndex")) - 1) * 500;
                        myTradeInformation0.lastPrice = reader.GetDouble(reader.GetOrdinal("nPrice"));
                        myTradeInformation0.ask = new double[5];
                        myTradeInformation0.ask[0] = reader.GetDouble(reader.GetOrdinal("ask1"));
                        myTradeInformation0.ask[1] = reader.GetDouble(reader.GetOrdinal("ask2"));
                        myTradeInformation0.ask[2] = reader.GetDouble(reader.GetOrdinal("ask3"));
                        myTradeInformation0.ask[3] = reader.GetDouble(reader.GetOrdinal("ask4"));
                        myTradeInformation0.ask[4] = reader.GetDouble(reader.GetOrdinal("ask5"));
                        myTradeInformation0.askv = new int[5];
                        myTradeInformation0.askv[0] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("askv1")));
                        myTradeInformation0.askv[1] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("askv2")));
                        myTradeInformation0.askv[2] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("askv3")));
                        myTradeInformation0.askv[3] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("askv4")));
                        myTradeInformation0.askv[4] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("askv5")));
                        myTradeInformation0.bid = new double[5];
                        myTradeInformation0.bid[0] = reader.GetDouble(reader.GetOrdinal("bid1"));
                        myTradeInformation0.bid[1] = reader.GetDouble(reader.GetOrdinal("bid2"));
                        myTradeInformation0.bid[2] = reader.GetDouble(reader.GetOrdinal("bid3"));
                        myTradeInformation0.bid[3] = reader.GetDouble(reader.GetOrdinal("bid4"));
                        myTradeInformation0.bid[4] = reader.GetDouble(reader.GetOrdinal("bid5"));
                        myTradeInformation0.bidv = new int[5];
                        myTradeInformation0.bidv[0] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("bidv1")));
                        myTradeInformation0.bidv[1] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("bidv2")));
                        myTradeInformation0.bidv[2] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("bidv3")));
                        myTradeInformation0.bidv[3] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("bidv4")));
                        myTradeInformation0.bidv[4] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("bidv5")));
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
                        cmd.CommandText = "select * from sh" + Convert.ToString(optionCode) + " where nDate=" + Convert.ToString(thisDay) + " and ((nTime>=93000000 and nTime<=113000000) or (nTime>=130000000 and nTime<=150000000)) and nTime>=" + Convert.ToString(startTime) + " and nTime<= " + Convert.ToString(endTime);
                        SqlDataReader reader = cmd.ExecuteReader();
                        //从reader中读取下一行数据,如果没有数据,reader.Read()返回flase  
                        while (reader.Read())
                        {
                            myTradeInformation0.time = reader.GetInt32(reader.GetOrdinal("nTime")) + (reader.GetInt32(reader.GetOrdinal("nIndex")) - 1) * 500;
                            myTradeInformation0.lastPrice = reader.GetDouble(reader.GetOrdinal("nPrice"));
                            myTradeInformation0.ask = new double[5];
                            myTradeInformation0.ask[0] = reader.GetDouble(reader.GetOrdinal("ask1"));
                            myTradeInformation0.ask[1] = reader.GetDouble(reader.GetOrdinal("ask2"));
                            myTradeInformation0.ask[2] = reader.GetDouble(reader.GetOrdinal("ask3"));
                            myTradeInformation0.ask[3] = reader.GetDouble(reader.GetOrdinal("ask4"));
                            myTradeInformation0.ask[4] = reader.GetDouble(reader.GetOrdinal("ask5"));
                            myTradeInformation0.askv = new int[5];
                            myTradeInformation0.askv[0] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("askv1")));
                            myTradeInformation0.askv[1] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("askv2")));
                            myTradeInformation0.askv[2] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("askv3")));
                            myTradeInformation0.askv[3] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("askv4")));
                            myTradeInformation0.askv[4] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("askv5")));
                            myTradeInformation0.bid = new double[5];
                            myTradeInformation0.bid[0] = reader.GetDouble(reader.GetOrdinal("bid1"));
                            myTradeInformation0.bid[1] = reader.GetDouble(reader.GetOrdinal("bid2"));
                            myTradeInformation0.bid[2] = reader.GetDouble(reader.GetOrdinal("bid3"));
                            myTradeInformation0.bid[3] = reader.GetDouble(reader.GetOrdinal("bid4"));
                            myTradeInformation0.bid[4] = reader.GetDouble(reader.GetOrdinal("bid5"));
                            myTradeInformation0.bidv = new int[5];
                            myTradeInformation0.bidv[0] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("bidv1")));
                            myTradeInformation0.bidv[1] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("bidv2")));
                            myTradeInformation0.bidv[2] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("bidv3")));
                            myTradeInformation0.bidv[3] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("bidv4")));
                            myTradeInformation0.bidv[4] = Convert.ToInt32(reader.GetDouble(reader.GetOrdinal("bidv5")));
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
