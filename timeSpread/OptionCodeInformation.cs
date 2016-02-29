using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace timeSpread
{
    
    
    /// <summary>
    /// 记录期权基本信息的重要类类型。记录的数据主要包括期权合约代码，期权类型，开始时间，结束时间，行权价。还包括一个静态的表myOptionInformation记录所有的期权合约信息。
    /// </summary>
    class OptionCodeInformation
    {
        private int optionCode;
        private string optionType;
        private int startdate, enddate;
        private double strike;
        /// <summary>
        /// 存储期权基本合约信息。
        /// </summary>
        public static List<optionInformation> myOptionInformation;
        /// <summary>
        ///存储期权收盘价的信息。
        /// </summary>
        public static SortedDictionary<int, SortedDictionary<int,double>> myOptionClosePrice;
        /// <summary>
        /// 存储期权结算价的信息。
        /// </summary>
        public static SortedDictionary<int, SortedDictionary<int, double>> myOptionSettlePrice;
        /// <summary>
        /// 构造函数。参数为期权合约代码，默认值为0。在该构造函数中，自动生成静态的表存储所有的期权合约信息。
        /// 若合约代码是合法的，类的私有数据更新为该合约代码的基本数据。
        /// </summary>
        /// <param name="code">期权合约代码</param>
        public OptionCodeInformation(int code = 0)
        {
            if (myOptionInformation == null)
            {
                myOptionInformation = new List<optionInformation>();
                using (SqlConnection conn = new SqlConnection(TradeDay.GetConnectString()))
                {
                    conn.Open();//打开数据库  
                    Console.WriteLine("数据库打开成功!");
                    //创建数据库命令  
                    SqlCommand cmd = conn.CreateCommand();
                    //创建查询语句  
                    cmd.CommandText = "select * from SH50ETFoption";
                    try
                    {//从数据库中读取数据流存入reader中  
                        SqlDataReader reader = cmd.ExecuteReader();
                        //从reader中读取下一行数据,如果没有数据,reader.Read()返回flase  
                        while (reader.Read())
                        {
                            optionInformation information0 = new optionInformation();
                            information0.optionCode = (int)(reader.GetDouble(reader.GetOrdinal("contractname")));
                            information0.optionName = reader.GetString(reader.GetOrdinal("optionname")).Trim();
                            information0.optionType = reader.GetString(reader.GetOrdinal("optiontype")).Trim();
                            information0.strike = reader.GetDouble(reader.GetOrdinal("strike"));
                            information0.startdate = Convert.ToInt32(reader.GetString(reader.GetOrdinal("startdate")).Trim());
                            information0.enddate = Convert.ToInt32(reader.GetString(reader.GetOrdinal("enddate")).Trim());
                            myOptionInformation.Add(information0);
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
           
            ///若合约代码是合法的，类的私有数据更新为该合约代码的基本数据。否者基本数据更新为初始值。
            if (IsExists(code))
            {
                optionCode = code;
                foreach (var item in myOptionInformation)
                {
                    if (item.optionCode == code)
                    {
                        optionType = item.optionType;
                        strike = item.strike;
                        startdate = item.startdate;
                        enddate = item.enddate;
                        break;
                    }
                }
            }
            else
            {
                optionCode = 0;
                optionType = "";
                strike = 0.0;
                startdate = 0;
                enddate = 0;
            }
        }
        /// <summary>
        /// 根据给定的合约代码，获取给定的合约代码基本信息。返回this指针。
        /// </summary>
        /// <param name="code">期权合约代码</param>
        /// <returns>this指针</returns>
        public OptionCodeInformation GetCodeInformation(int code)
        {
            if (IsExists(code))
            {
                optionCode = code;
                foreach (var item in myOptionInformation)
                {
                    if (item.optionCode == code)
                    {
                        optionType = item.optionType;
                        strike = item.strike;
                        startdate = item.startdate;
                        enddate = item.enddate;
                        break;
                    }
                }
            }
            else
            {
                optionCode = 0;
                optionType = "";
                strike = 0.0;
                startdate = 0;
                enddate = 0;
            }
            return this;
        }
        /// <summary>
        /// 判断期权合约代码是否存在的静态方法。
        /// </summary>
        /// <param name="code">期权合约代码</param>
        /// <returns>返回bool值</returns>
         public static bool IsExists(int code)
        {
            if (myOptionInformation == null)
            {
                OptionCodeInformation temp = new OptionCodeInformation();
                myOptionInformation = temp.GetOptionTable();
            }
            foreach (var item in myOptionInformation)
            {
                if (item.optionCode == code)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 获取期权合约代码信息。
        /// </summary>
        /// <returns>返回期权合约代码</returns>
        public int GetOptionCode()
        {
            return optionCode;
        }
        /// <summary>
        /// 获取期权类型信息。
        /// </summary>
        /// <returns>返回期权类型</returns>
        public string GetOptionType()
        {
            return optionType;
        }
        /// <summary>
        /// 获取期权行权价信息。
        /// </summary>
        /// <returns>返回期权行权价</returns>
        public double GetOptionStrike()
        {
            return strike;
        }
        /// <summary>
        /// 获取期权开始时间信息。
        /// </summary>
        /// <returns>返回期权开始时间</returns>
        public double GetOptionStartDate()
        {
            return startdate;
        }
        /// <summary>
        /// 获取期权结束时间。
        /// </summary>
        /// <returns>返回期权结束时间</returns>
        public double GetOptionEndDate()
        {
            return enddate;
        }
        /// <summary>
        /// 获取所有期权的基本信息。
        /// </summary>
        /// <returns>返回所有期权的基本信息的列表</returns>
        public List<optionInformation> GetOptionTable()
        {
            return myOptionInformation;
        }
        /// <summary>
        /// 静态函数。根据给定的期权合约代码，获取其对应的远月合约。若未找到返回0。
        /// </summary>
        /// <param name="code">期权合约代码</param>
        /// <param name="today">日期</param>
        /// <returns>返回对应的远月期权合约代码</returns>
         public static int GetFurtherOption(int code, int today)
        {
            int enddateMin = 99999999;
            int find = 0;
            OptionCodeInformation tempCode = new OptionCodeInformation(code);
            foreach (var item in myOptionInformation)
            {
                if (item.optionCode != code && item.strike == tempCode.strike && item.optionType == tempCode.optionType && item.startdate <= today && item.enddate > tempCode.enddate && item.enddate < enddateMin)
                {
                    enddateMin = item.enddate;
                    find = item.optionCode;
                }
            }
            return find;
        }
        /// <summary>
        /// 静态函数。根据给定的期权合约代码，获取对应类型的期权合约。例如看涨期权找到其对应的看跌期权。
        /// </summary>
        /// <param name="code">期权合约代码</param>
        /// <returns>返回其类型相对应的期权合约代码</returns>
         public static int GetCorrespondingOption(int code)
        {
            int find = 0;
            OptionCodeInformation tempCode = new OptionCodeInformation(code);
            foreach (var item in myOptionInformation)
            {
                if (item.optionCode != code && item.strike == tempCode.strike && item.optionType != tempCode.optionType && item.startdate == tempCode.startdate && item.enddate == tempCode.enddate)
                {
                    find = item.optionCode;
                    break;
                }
            }
            return find;
        }
        /// <summary>
        /// 静态函数。给定期权合约代码和当日日期，获取到期天数。
        /// </summary>
        /// <param name="code">期权合约代码</param>
        /// <param name="today">日期</param>
        /// <returns>到期天数</returns>
         public static int GetTimeSpan(int code, int today)
        {
            OptionCodeInformation tempCode = new OptionCodeInformation(code);
            return TradeDay.GetTimeSpan(today, tempCode.enddate);
        }
        /// <summary>
        /// 静态函数。根据日内50etf价格变动，给出到日期最近且平价附近的期权。
        /// </summary>
        /// <param name="minEtfPrice">日内50etf价格最小值</param>
        /// <param name="maxEtfPrice">日内50etf价格最大值</param>
        /// <param name="today">日期</param>
        /// <returns>平价期权列表</returns>
         public static List<int> GetOptionCodeInInterval(double minEtfPrice, double maxEtfPrice, int today)
        {
            List<int> choosedOptionCode = new List<int>();
            double upBound = (maxEtfPrice >= 3) ? maxEtfPrice + 0.1 : maxEtfPrice + 0.05;
            double lowerBound = (minEtfPrice >= 3) ? minEtfPrice - 0.1 : minEtfPrice - 0.05;
            double duration =999;
            if (myOptionInformation == null)
            {
                OptionCodeInformation temp = new OptionCodeInformation();
                myOptionInformation = temp.GetOptionTable();
            }
            foreach (var item in myOptionInformation)
            {
                int thisduration = OptionCodeInformation.GetTimeSpan(item.optionCode, today);
                if (thisduration <= duration && thisduration>0)
                {
                    duration = thisduration;
                }
            }
            foreach (var item in myOptionInformation)
            {
                if ((item.strike > lowerBound && item.strike < upBound) && (item.enddate >= today) && (item.startdate <= today))
                {
                    int thisduration = OptionCodeInformation.GetTimeSpan(item.optionCode, today);
                    if (thisduration == duration)
                    {
                        duration = thisduration;
                        choosedOptionCode.Add(item.optionCode);
                    }

                }
            }
            return choosedOptionCode;
        }
        /// <summary>
        /// 静态函数。根据日期和50etf价格，给出当月平价期权。
        /// </summary>
        /// <param name="etfPrice">50etf当前价格</param>
        /// <param name="today">日期</param>
        /// <returns>当月平价期权列表</returns>
         public static List<int> GetOptionCodeAtTheMoney(double etfPrice, int today)
        {
            List<int> choosedOptionCode = new List<int>();
            if (myOptionInformation == null)
            {
                OptionCodeInformation temp = new OptionCodeInformation();
                myOptionInformation = temp.GetOptionTable();
            }
            foreach (var item in myOptionInformation)
            {
                if ((((Math.Abs(item.strike - etfPrice) < 0.024999999999) && etfPrice <= 3.025) || ((Math.Abs(item.strike - etfPrice) < 0.04999999999) && etfPrice > 3.025)) && (item.enddate >= today) && (item.startdate <= today))
                {
                    choosedOptionCode.Add(item.optionCode);
                }
            }
            return choosedOptionCode;
        }
        /// <summary>
        /// 静态函数。根据日期，50etf价格和期权类型，给出当月特定类型的平价期权。
        /// </summary>
        /// <param name="etfPrice">50etf当前价格</param>
        /// <param name="today">日期</param>
        /// <param name="optionType">期权类型</param>
        /// <returns>当月特定类型的平价期权。</returns>
         public static int GetOptionCodeAtTheMoney(double etfPrice, int today, string optionType)
        {
            if (myOptionInformation == null)
            {
                OptionCodeInformation temp = new OptionCodeInformation();
                myOptionInformation = temp.GetOptionTable();
            }
            foreach (var item in myOptionInformation)
            {
                if ((((Math.Abs(item.strike - etfPrice) < 0.024999999999) && etfPrice <= 3.025) || ((Math.Abs(item.strike - etfPrice) < 0.04999999999) && etfPrice > 3.025)) && (item.enddate >= today) && (item.startdate <= today) && item.optionType.Equals(optionType))
                {
                    return item.optionCode;
                }
            }
            return 0;
        }
        /// <summary>
        /// 期权每日收盘价和结算价的初始化程序。
        /// </summary>
        /// <param name="optionCode">期权合约代码</param>
        private void InitializeOptionSettleAndClosePrice(int optionCode)
        {
            if (myOptionSettlePrice == null)
            {
                myOptionSettlePrice = new SortedDictionary<int, SortedDictionary<int, double>>();
                myOptionClosePrice = new SortedDictionary<int, SortedDictionary<int, double>>();
            }
            if (myOptionSettlePrice.ContainsKey(optionCode)==false)
            {
                using (SqlConnection conn = new SqlConnection(TradeDay.GetConnectString()))
                {
                    conn.Open();//打开数据库  
                    SqlCommand cmd = conn.CreateCommand();
                    SortedDictionary<int, double> mySettle = new SortedDictionary<int, double>();
                    SortedDictionary<int, double> myClose = new SortedDictionary<int, double>();
                    try
                    {
                        cmd.CommandText = "select distinct(nPreSettle),nPreClose,nDate from sh" + Convert.ToString(optionCode) + " order by nDate";
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            int today = reader.GetInt32(reader.GetOrdinal("nDate"));
                            int yesterday    = TradeDay.GetPreviousTradeDay(today);
                            double myPreSettle = reader.GetDouble(reader.GetOrdinal("nPreSettle"));
                            double myPreClose = reader.GetDouble(reader.GetOrdinal("nPreClose"));
                            if (yesterday>0)
                            {
                                mySettle.Add(yesterday, myPreSettle);
                                myClose.Add(yesterday, myPreClose);
                            }
                        }
                        myOptionSettlePrice.Add(optionCode, mySettle);
                        myOptionClosePrice.Add(optionCode, myClose);
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
        /// <summary>
        /// 读取当前日期给定期权合约代码的结算价。主要用于维持保证金的计算。
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="optionCode">期权合约代码</param>
        /// <returns></returns>
        public double GetOptionSettlePirce(int date,int optionCode)
        {
            double settle = 0.0;
            InitializeOptionSettleAndClosePrice(optionCode);
            if (myOptionSettlePrice.ContainsKey(optionCode))
            {
                settle = myOptionSettlePrice[optionCode][date];
            }
            return settle;
        }
        /// <summary>
        /// 读取当前日期给定期权合约代码的收盘价。主要用于希腊值的计算。
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="optionCode">期权合约代码</param>
        /// <returns></returns>
        public double GetOptionClosePirce(int date, int optionCode)
        {
            double close = 0.0;
            InitializeOptionSettleAndClosePrice(optionCode);
            if (myOptionClosePrice.ContainsKey(optionCode))
            {
                close = myOptionClosePrice[optionCode][date];
            }
            return close;
        }
    }
}
