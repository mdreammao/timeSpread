using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace timeSpread
{
    class HoldStatus
    {
        /// <summary>
        /// 记录每日持仓以及开仓成本的哈希表。<日期，<期权合约代码，持仓情况>>。
        /// </summary>
        public SortedDictionary<int, Dictionary<int, optionHold>> positionStatusList = new SortedDictionary<int, Dictionary<int, optionHold>>();
        /// <summary>
        /// 记录每一天参与交易的期权合约代码的名称。<日期，list<期权合约代码>>
        /// </summary>
        public Dictionary<int, List<int>> codeNameList = new Dictionary<int, List<int>>();
        /// <summary>
        /// 记录每日交易情况的三维数组。[日期，时间] <期权合约代码,期权头寸情况>。主要为了记录逐笔数据的方便。
        /// </summary>
        public Dictionary<int, optionHold>[,] tradeStatusList = new Dictionary<int, optionHold>[500,28800];
        /// <summary>
        /// 记录每个合约交易情况的哈希表。<期权合约代码，期权交易情况>。按合约代码进行分类。
        /// </summary>
        public Dictionary<int, List<optionHoldWithTime>> tradeStatusListOrderByCode = new Dictionary<int, List<optionHoldWithTime>>();
        /// <summary>
        /// 记录每日资金情况的哈希表。
        /// </summary>
        public SortedDictionary<int, asset> cashStatusList = new SortedDictionary<int, asset>();
        /// <summary>
        /// 记录每日希腊值情况的哈希表。
        /// </summary>
        public SortedDictionary<int, greek> greekStatusList = new SortedDictionary<int, greek>();
        /// <summary>
        /// 从状态列表中根据日期来读取持仓信息。如果未提供日期信息，就读取列表中一日的最后的持仓信息。注意：这里提供的拷贝是深拷贝！
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>返回持仓信息</returns>
        public Dictionary<int, optionHold> GetPositionStatus(int date=0)
        {
            Dictionary<int, optionHold> status = new Dictionary<int, optionHold>();
            if (date==0 && positionStatusList.Count>0)
            {
                status = Technique.Clone(positionStatusList[positionStatusList.Keys.Last<int>()]);
            }
            if (date!=0 && positionStatusList.ContainsKey(date))
            {
                status =Technique.Clone(positionStatusList[date]);
            }
            return status;
        }
        /// <summary>
        /// 插入给定日期的头寸情况。
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="positionStatusToday">持仓情况</param>
        public void InsertPositionStatus(int date, Dictionary<int, optionHold> positionStatusToday)
        {
            if (positionStatusList.ContainsKey(date)==false)
            {
                positionStatusList.Add(date, positionStatusToday);
            }
        }
        /// <summary>
        /// 记录逐笔交易信息的函数。
        /// </summary>
        /// <param name="optionCode">期权合约代码</param>
        /// <param name="date">日期</param>
        /// <param name="time">时刻</param>
        /// <param name="tradeStatus">成交情况包括价格和数量</param>
        public void InsertTradeStatus(int optionCode,int dateIndex,int timeIndex,double price,int volumn)
        {
            optionHold myHold = new optionHold();
            myHold.price = price;
            myHold.position = volumn;
            if (tradeStatusList[dateIndex, timeIndex]==null)
            {
                tradeStatusList[dateIndex, timeIndex] = new Dictionary<int, optionHold>();
            }
            tradeStatusList[dateIndex, timeIndex].Add(optionCode,myHold);
        }
        /// <summary>
        /// 按照期权合约代码进行分类，保持交易记录。
        /// </summary>
        /// <param name="optionCode">期权合约代码</param>
        /// <param name="date">日期</param>
        /// <param name="time">时间</param>
        /// <param name="price">成交价格</param>
        /// <param name="volumn">成交量</param>
        public void InsertTradeStatusOrderByCode(int optionCode, int date, int time, double price, int volumn)
        {
            optionHoldWithTime myHold = new optionHoldWithTime();
            List<optionHoldWithTime> myHoldList = new List<optionHoldWithTime>();
            myHold.date = date;
            myHold.time = time;
            myHold.price = price;
            myHold.position = volumn;
            if (tradeStatusListOrderByCode.ContainsKey(optionCode)==false)
            {
                myHoldList.Add(myHold);
                tradeStatusListOrderByCode.Add(optionCode, myHoldList);
            }
            else
            {
                tradeStatusListOrderByCode[optionCode].Add(myHold);
            }
        }
        /// <summary>
        /// 记录每日资金情况的函数。
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="cash">可用资金</param>
        /// <param name="margin">保证金</param>
        /// <param name="fee">交易费用</param>
        public void InsertCashStatus(int date,double cash,double margin,double fee,double optionValue)
        {
            asset myMoney = new asset();
            myMoney.cash = cash;
            myMoney.margin = margin;
            myMoney.fee = fee;
            myMoney.optionValue = optionValue;
            cashStatusList.Add(date, myMoney);
        }
        /// <summary>
        /// 记录每日希腊值情况的函数。
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="delta">希腊值delta</param>
        /// <param name="gamma">希腊值gamma</param>
        public void InsertGreekStatus(int date,double delta,double gamma)
        {
            greek myGreek = new greek();
            myGreek.delta = delta;
            myGreek.gamma = gamma;
            greekStatusList.Add(date, myGreek);
        }
        /// <summary>
        /// 打印交易信息
        /// </summary>
        public void RecordTradeStatusList(string filePathName,List<int> myTradeDays, int[] myTradeTicks)
        {
            List<int> myPreHoldKey = new List<int>();
            
            for (int dateIndex = 0; dateIndex < myTradeDays.Count; dateIndex++)
            {
                List<string[]> printStream = new List<string[]>();
                int today = myTradeDays[dateIndex];
                List<int> myHoldKey = new List<int>();
                myHoldKey.AddRange(positionStatusList[today].Keys);
                //List<int> Result = myHoldKey.Union(myPreHoldKey).ToList<int>();          //剔除重复项 
                List<int> Result = codeNameList[today];
                for (int timeIndex = 1; timeIndex < 28800; timeIndex++)
                {
                    List<string> printStream0 = new List<string>();
                    
                    bool first = true;
                    foreach (int optionCode in Result)
                    {
                        if (tradeStatusList[dateIndex, timeIndex]!=null && tradeStatusList[dateIndex,timeIndex].ContainsKey(optionCode))
                        {
                            if (first)
                            {
                                first = false;
                                printStream0.Add(today.ToString());
                                printStream0.Add(TradeDay.myTradeTicks[timeIndex].ToString());
                            }
                            printStream0.Add(optionCode.ToString());
                            printStream0.Add(tradeStatusList[dateIndex, timeIndex][optionCode].price.ToString());
                            printStream0.Add(tradeStatusList[dateIndex, timeIndex][optionCode].position.ToString());
                            //Console.WriteLine("date:{0}, code:{1}, time:{2}, price:{3}, position:{4}", today, optionCode, TradeDay.myTradeTicks[timeIndex], tradeStatusList[dateIndex, timeIndex][optionCode].price, tradeStatusList[dateIndex, timeIndex][optionCode].position);
                        }
                    }
                    if (printStream0.Count>0)
                    {
                        printStream.Add(printStream0.ToArray());
                    }
                }
                myPreHoldKey = myHoldKey;
                //将当日交易记录写入csv文件
                CsvUtility.WriteCsv(filePathName,true,printStream);
            }
        }
    }
}
