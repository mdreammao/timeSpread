using System;
using System.Collections.Generic;
using System.Linq;

namespace timeSpread
{
    /// <summary>
    /// 包含挂单价格和挂单量的结构体。
    /// </summary>
    struct positionStatus
    {
        public double price;
        public int volumn;
    }
    /// <summary>
    /// 存储盘口变动信息的结构体。包括前交易时间，交易时间，买报价和卖报价的信息。
    /// </summary>
    struct positionChange
    {
        public int lastTime,thisTime;
        public List<positionStatus> askChange,bidChange;
        public positionChange(int lastTime,int thisTime)
        {
            this.lastTime = lastTime;
            this.thisTime = thisTime;
            askChange = new List<positionStatus>();
            bidChange = new List<positionStatus>();
        }
    }
    /// <summary>
    /// 记录盘口信息变动的类。通过方法 GetPositionChange()将静态的盘口信息转化为盘口的增量信息并记录下来。
    /// </summary>
    class PositionShot
    {
        private int optionCode = 0;
        private int today = 0;
        /// <summary>
        /// 构造函数获取期权合约代码和日期信息。
        /// </summary>
        /// <param name="optionCode">期权合约代码</param>
        /// <param name="today">日期</param>
        public PositionShot(int optionCode,int today)
        {
            this.optionCode = optionCode;
            this.today = today;
        }
        /// <summary>
        /// 静态函数。根据上一次盘口信息lastShot，以及盘口变动信息nowChange，生成最新的盘口信息。
        /// </summary>
        /// <param name="lastShot">上一次盘口信息</param>
        /// <param name="nowChange">盘口变动信息</param>
        /// <returns>最新的盘口信息</returns>
        public static  tradeInformation GetPositionShot(tradeInformation lastShot,positionChange nowChange)
        {
            tradeInformation nextShot = new tradeInformation();
            nextShot.ask = new double[5];
            nextShot.askv = new int[5];
            nextShot.bid = new double[5];
            nextShot.bidv = new int[5];
            positionChange nextChange = new positionChange(nowChange.lastTime,nowChange.thisTime);
            nextShot.time = nowChange.thisTime;
            //分别处理ask以及bid部分的价格变化。利用前一状态的盘口价格，叠加上当前盘口价格变动，得到下一状态的盘口价格。数据结构使用哈希表便于理解和处理。
            #region 处理ask部分新方法
            SortedDictionary<double, int> askModified = new SortedDictionary<double, int>();
            for (int index = 0; index < 5; index++)
            {
                if (lastShot.ask[index]*lastShot.askv[index]!=0)
                {
                    askModified.Add(lastShot.ask[index], lastShot.askv[index]);
                }
            }
            foreach (var item in nowChange.askChange)
            {
                if (askModified.ContainsKey(item.price))
                {
                    askModified[item.price] += item.volumn;
                }
                else
                {
                    askModified.Add(item.price, item.volumn);
                }
            }
            int t = 0;
            foreach (var item in askModified)
            {
                if (item.Value>0)
                {
                    nextShot.ask[t] = item.Key;
                    nextShot.askv[t] = item.Value;
                    t = t + 1;
                }
                if (t>=5)
                {
                    break;
                }
            }
            #endregion
            #region 处理bid部分新方法
            Dictionary<double, int> bidModified = new Dictionary<double, int>();
            for (int index = 0; index < 5; index++)
            {
                if (lastShot.bid[index] * lastShot.bidv[index] != 0)
                {
                    bidModified.Add(lastShot.bid[index], lastShot.bidv[index]);
                }
            }
            foreach (var item in nowChange.bidChange)
            {
                if (bidModified.ContainsKey(item.price))
                {
                    bidModified[item.price] += item.volumn;
                }
                else
                {
                    bidModified.Add(item.price, item.volumn);
                }
            }
            bidModified = bidModified.OrderByDescending(o => o.Key).ToDictionary(o => o.Key, p => p.Value);
            t = 0;
            foreach (var item in bidModified)
            {
                if (item.Value>0)
                {
                    nextShot.bid[t] = item.Key;
                    nextShot.bidv[t] = item.Value;
                    t = t + 1;
                }
                if (t >= 5)
                {
                    break;
                }
            }
            #endregion
            return nextShot;
        }
        /// <summary>
        /// 根据当日盘口信息，分解出盘口的变动信息。
        /// </summary>
        /// <returns>盘口变动信息列表</returns>
        public List<positionChange> GetPositionChange()
        {
            List<positionChange> myChange = new List<positionChange>();
            DataBase myData = new DataBase(optionCode,today);
            basicInformation data = myData.GetBasicInformation();
            List<tradeInformation> tickData = myData.GetTickData(myData.Today);
            for (int i = 1; i < tickData.Count; i++)
            {
                tradeInformation lastStatus = tickData[i - 1];
                tradeInformation thisStatus = tickData[i];
                positionChange change = new positionChange(lastStatus.time,thisStatus.time);
                //分别处理ask和bid的盘口价格，通过分析前一状态的盘口价格和后一状态的盘口价格，得到两个盘口价格之间的具体变动信息。数据结构使用哈希表便于理解和处理。
                #region ask处理的新方法
                SortedDictionary<double, int> askChange = new SortedDictionary<double, int>();
                for (int index = 0; index < 5; index++)
                {
                    if (askChange.ContainsKey(thisStatus.ask[index]))
                    {
                        askChange[thisStatus.ask[index]] += thisStatus.askv[index];
                    }
                    else
                    {
                        askChange.Add(thisStatus.ask[index], thisStatus.askv[index]);
                    }
                    if (askChange.ContainsKey(lastStatus.ask[index]))
                    {
                        askChange[lastStatus.ask[index]] -= lastStatus.askv[index];
                    }
                    else
                    {
                        askChange.Add(lastStatus.ask[index], -lastStatus.askv[index]);
                    }
                }
                foreach (var item in askChange)
                {
                    positionStatus myChange0 = new positionStatus();
                    myChange0.price = item.Key;
                    myChange0.volumn = item.Value;
                    if (item.Value!=0)
                    {
                        change.askChange.Add(myChange0);
                    }
                }
                #endregion
                #region bid处理的新方法
                Dictionary<double, int> bidChange = new Dictionary<double, int>();
                for (int index = 0; index < 5; index++)
                {
                    if (bidChange.ContainsKey(thisStatus.bid[index]))
                    {
                        bidChange[thisStatus.bid[index]] += thisStatus.bidv[index];
                    }
                    else
                    {
                        bidChange.Add(thisStatus.bid[index], thisStatus.bidv[index]);
                    }
                    if (bidChange.ContainsKey(lastStatus.bid[index]))
                    {
                        bidChange[lastStatus.bid[index]] -= lastStatus.bidv[index];
                    }
                    else
                    {
                        bidChange.Add(lastStatus.bid[index], -lastStatus.bidv[index]);
                    }
                }
                bidChange = bidChange.OrderByDescending(o => o.Key).ToDictionary(o => o.Key, p => p.Value);
                foreach (var item in bidChange)
                {
                    positionStatus myChange0 = new positionStatus();
                    myChange0.price = item.Key;
                    myChange0.volumn = item.Value;
                    if (item.Value!=0)
                    {
                        change.bidChange.Add(myChange0);
                    }
                    
                }
                #endregion
                myChange.Add(change);
            }
            return myChange;
        }
    }
}
