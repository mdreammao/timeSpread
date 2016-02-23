using System;
using System.Collections.Generic;


namespace timeSpread
{

    
    /// <summary>
    /// 计算跨期价差收益的类类型。
    /// </summary>
    class TimeSpreadOne
    {
        /// <summary>
        /// 记录交易记录的文档地址
        /// </summary>
        public string filePathName = "TradeRecords" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
        /// <summary>
        /// 初始持有的资金。
        /// </summary>
        private double totalCash=100000000;
        private int startDate = 20150209;
        private int endDate = 20151231;
        private int startTime = 93000000;
        private int endTime = 150000000;
        /// <summary>
        /// 回测开始结束日期信息。
        /// </summary>
        private TradeDay myTradeDay;
        /// <summary>
        /// 回测的逐笔交易记录
        /// </summary>
        private HoldStatus myHoldStatus;
        //构造函数记录基本信息，包括回测的开始日期，结束日期，每日的开始时间和结束时间
        /// <summary>
        /// 构造函数。初始化一系列参数，包括开始时间，结束时间，交易日信息等。
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="startTime">日内开始时刻</param>
        /// <param name="endTime">日内结束时刻</param>
        public TimeSpreadOne(int startDate,int endDate,int startTime,int endTime)
        {
            this.startDate = startDate;
            this.endDate = endDate;
            this.startTime = startTime;
            this.endTime = endTime;
            myTradeDay = new TradeDay(startDate, endDate);
        }
        /// <summary>
        /// 开仓信号的核心判断函数。根据隐含波动率以及到期时间等参数进行判断。
        /// </summary>
        /// <param name="etfPrice">50etf价格</param>
        /// <param name="price">近月期权价格</param>
        /// <param name="priceFurther">远月期权价格</param>
        /// <param name="expiry0">近月期权到期天数</param>
        /// <param name="expiryFurther0">远月期权到期天数</param>
        /// <param name="strike">期权行权价</param>
        /// <param name="type">期权类型，看涨还是看跌期权</param>
        /// <returns>返回是否开仓的判断结果</returns>
        private bool Judgement(double etfPrice,double price,double priceFurther,int expiry0,int expiryFurther0,double strike,string type)
        {
            bool open = false;
            double r = 0.05;
            //利用BS公式计算近月以及远月期权的隐含波动率。并用这2个波动率差值得到近月合约到期时候，期权对应的隐含波动率。
            double sigma = Impv.sigma(etfPrice, price, strike, expiry0, r, type);
            double sigmaFurther = Impv.sigma(etfPrice, priceFurther, strike, expiryFurther0, r, type);
            double sigmaNew = Math.Sqrt(sigma * sigma * ( expiry0) / (expiryFurther0 - expiry0) + sigmaFurther * sigmaFurther * (expiryFurther0 - 2 * expiry0) / (expiryFurther0 - expiry0));
            //利用隐含波动率来估计近月期权合约到期时候50etf的价格，这里使用若干倍的sigma来计算。
            double etfPriceFurtherUp = etfPrice * Math.Exp(2*sigma * Math.Sqrt(expiry0 / 252.0));
            double etfPriceFurtherDown= etfPrice * Math.Exp(-2*sigma * Math.Sqrt(expiry0 / 252.0));
            double noChange = Impv.optionPrice(etfPrice, sigmaNew, strike, expiryFurther0 - expiry0, r, type)- Impv.optionPrice(etfPrice, sigmaNew, strike, 0, r, type);
            //计算出持有头寸价值的上下限。
            double up= Impv.optionPrice(etfPriceFurtherUp, sigmaNew, strike, expiryFurther0 - expiry0, r, type) - Impv.optionPrice(etfPriceFurtherUp, sigmaNew, strike, 0, r, type);
            double down = Impv.optionPrice(etfPriceFurtherDown, sigmaNew, strike, expiryFurther0 - expiry0, r, type) - Impv.optionPrice(etfPriceFurtherDown, sigmaNew, strike, 0, r, type);
            double interestNoChange = noChange - (priceFurther - price);
            double interestUp= up - (priceFurther - price);
            double interestDown=down- (priceFurther - price);
            //利用收益风险比例是否大于1来判断开仓信息。
            if (interestNoChange / Math.Abs((Math.Min(interestUp, interestDown)))>1.5)
            {
                open = true;
            }
            return open;
        }
        /// <summary>
        /// 根据今日持仓情况计算今日的维持保证金。
        /// </summary>
        /// <param name="today">当天日期</param>
        /// <param name="myHold">今日的持仓情况</param>
        /// <returns>今日的维持保证金</returns>
        private double ComputeMargin(int today,Dictionary<int, optionHold> myHold)
        {
            double margin=0.0;
            foreach (var item in myHold)
            {

                if (item.Value.position < 0)
                {
                    double myMargin;
                    OptionCodeInformation myOption = new OptionCodeInformation(item.Key);
                    double optionSettle = myOption.GetOptionSettlePirce(today, myOption.GetOptionCode());
                    double etfClose = EtfTradeInformation.getEtfCloseInformation(today);
                    if (myOption.GetOptionType() == "认购")
                    {
                        myMargin = optionSettle + Math.Max(0.12 * etfClose - Math.Max(myOption.GetOptionStrike() - etfClose, 0), 0.07 * etfClose) * 10000;
                    }
                    else
                    {
                        myMargin = Math.Min(optionSettle + Math.Max(0.12 * etfClose - Math.Max(etfClose - myOption.GetOptionStrike(), 0), 0.07 * myOption.GetOptionStrike()), myOption.GetOptionStrike()) * 10000;
                    }
                    margin += myMargin * Math.Abs(item.Value.position);
                }
            }
            return margin;
        }
        /// <summary>
        /// 计算当日持仓期权的价值。
        /// </summary>
        /// <param name="today">日期</param>
        /// <param name="myHold">持仓情况</param>
        /// <returns>期权的价值</returns>
        private double ComputePositionValue(int today, Dictionary<int, optionHold> myHold)
        {
            double value = 0.0;
            foreach (var item in myHold)
            {
                int position = item.Value.position;
                int optionCode = item.Key;
                OptionCodeInformation myOption = new OptionCodeInformation(optionCode);
                double optionClose = myOption.GetOptionClosePirce(today, optionCode);
                value += optionClose * 10000 * position;
            }
            return value;
        }
        /// <summary>
        /// 计算当日持仓的情况
        /// </summary>
        /// <param name="today">日期</param>
        /// <param name="myHold">今日持仓情况</param>
        /// <param name="margin">维持保证金</param>
        /// <param name="value">期权价值</param>
        /// <param name="cost">期权开仓成本</param>
        /// <param name="delta">期权delta值</param>
        /// <param name="gamma">期权gamma值</param>
        private void ComputePositionStatus(int today, Dictionary<int, optionHold> myHold, ref double margin, ref double value, ref double cost,ref double delta,ref double gamma)
        {
            margin = 0;
            value = 0;
            cost = 0;
            delta = 0;
            gamma = 0;
            foreach (var item in myHold)
            {
                int position = item.Value.position;
                double openCost = item.Value.price;
                int optionCode = item.Key;
                OptionCodeInformation myOption = new OptionCodeInformation(optionCode);
                double optionClose = myOption.GetOptionClosePirce(today, optionCode);
                double etfClose = EtfTradeInformation.getEtfCloseInformation(today);
                //计算期权的持仓价值
                value += optionClose * 10000 * position;
                //计算期权的开仓成本
                cost += -openCost * 10000 * position;
                //计算维持保证金
                if (item.Value.position < 0)
                {
                    double myMargin;
                    double optionSettle = myOption.GetOptionSettlePirce(today, myOption.GetOptionCode());
                    if (myOption.GetOptionType() == "认购")
                    {
                        myMargin = optionSettle + Math.Max(0.12 * etfClose - Math.Max(myOption.GetOptionStrike() - etfClose, 0), 0.07 * etfClose) * 10000;
                    }
                    else
                    {
                        myMargin = Math.Min(optionSettle + Math.Max(0.12 * etfClose - Math.Max(etfClose - myOption.GetOptionStrike(), 0), 0.07 * myOption.GetOptionStrike()), myOption.GetOptionStrike()) * 10000;
                    }
                    margin += myMargin * Math.Abs(item.Value.position);
                }
                //计算希腊值
                int expiry = OptionCodeInformation.GetTimeSpan(optionCode,today);
                double sigma = Impv.sigma(etfClose, optionClose, myOption.GetOptionStrike(), expiry, 0.05, myOption.GetOptionType());
                delta += item.Value.position*10000*etfClose*Impv.optionDelta(etfClose, sigma, myOption.GetOptionStrike(), expiry, 0.05, myOption.GetOptionType());
                if (sigma>0)
                {
                    gamma += 100000000 * item.Value.position * etfClose * etfClose * Impv.optionGamma(etfClose, sigma, myOption.GetOptionStrike(), expiry, 0.05);
                }
            }
        }
        /// <summary>
        /// 回测的核心函数。利用历史数据模拟交易过程。
        /// </summary>
        public void TimeSpreadAnalysis()
        {
            //myHoldStauts为记录每日的交易情况的类。
            myHoldStatus = new HoldStatus();
            //myHold记录每日持仓情况，根据该表可以统计每日资金占用以及头寸的情况。
            Dictionary<int, optionHold> myHold = new Dictionary<int, optionHold>();
            double optionMargin = 0;
            double deltaCash = 0;
            double totalCash = this.totalCash;
            double cashAvailable = 0;
            double fee = 0;
            double optionValue = 0;
            double optionCost = 0;
            for (int dateIndex = 0; dateIndex < myTradeDay.MyTradeDays.Count; dateIndex++)
            {
                //初始化参数
                int today = myTradeDay.MyTradeDays[dateIndex];
                fee = 0;
                deltaCash = 0;
                optionValue = 0;
                optionCost = 0;
                double optionDelta = 0;
                double optionGamma = 0;
                myHold = myHoldStatus.GetPositionStatus();
                
                ///利用哈希表optionTrade来记录多个期权的交易信息
                ///利用哈希表optionPositionShot来记录参与交易之后的盘口信息
                ///利用哈希表optionPositionChange来记录多个期权的盘口变化信息
                ///利用哈希表optionIndex来记录期权盘口时间对应的数据列表中的下标
                ///利用哈希表closeCode来记录今日平仓的合约代码，以免后来重复开仓
                Dictionary<int, List<tradeInformation>> optionTrade = new Dictionary<int, List<tradeInformation>>();
                Dictionary<int, tradeInformation> optionPositionShot = new Dictionary<int, tradeInformation>();
                Dictionary<int, List<positionChange>> optionPositionChange = new Dictionary<int, List<positionChange>>();
                Dictionary<int, int> optionIndex = new Dictionary<int, int>(); //记录了期权合约遍历的位置，避免先开仓后平仓的情况。
                List<int> closeCode = new List<int>();

                //获取具体的交易信息
                #region 对持仓的头寸进行平仓信号的判断
                //查询历史持仓，如果满足平仓条件，就平仓
                //根据历史的持仓进行判断
                //主要根据持仓情况进行止盈止损
                //根据myhold的情况提取信息
                List<int> myHoldKey = new List<int>();
                myHoldKey.AddRange(myHold.Keys);
                foreach (var myKey in myHoldKey)
                {
                    //提出持仓中的一对近月合约和远月合约
                    int code = myKey;
                    int codeFurther = OptionCodeInformation.GetFurtherOption(code, today);
                    if (myHold.ContainsKey(code) == false ||myHold[code].position==0|| myHold.ContainsKey(codeFurther)==false || myHold[codeFurther].position==0)
                    {
                        continue;
                    }
                    //获取近月以及远月合约的盘口信息
                    OptionTradeInformation myOptionTrade0 = new OptionTradeInformation(code, today);
                    OptionTradeInformation myOptionTrade1 = new OptionTradeInformation(codeFurther, today);
                    optionTrade.Add(code, myOptionTrade0.myTradeInformation);
                    optionTrade.Add(codeFurther, myOptionTrade1.myTradeInformation);
                    //获取近月以及远月合约的盘口变化列表
                    PositionShot myOptionChange0 = new PositionShot(code, today);
                    PositionShot myOptionChange1 = new PositionShot(codeFurther, today);
                    optionPositionChange.Add(code, myOptionChange0.GetPositionChange());
                    optionPositionChange.Add(codeFurther, myOptionChange1.GetPositionChange());
                    //记录对应合约在参与交易之后盘口的信息
                    tradeInformation positionInitial = new tradeInformation();
                    positionInitial.ask = new double[5];
                    positionInitial.bid = new double[5];
                    positionInitial.askv = new int[5];
                    positionInitial.bidv = new int[5];
                    optionPositionShot.Add(code, positionInitial);
                    optionPositionShot.Add(codeFurther, positionInitial);
                    //存放盘口时间对应的数组下标，初始化为-1
                    optionIndex.Add(code, -1);
                    optionIndex.Add(codeFurther, -1);
                    for (int timeIndex = 1; timeIndex < 28440; timeIndex++)
                    {
                        int time = TradeDay.MyTradeTicks[timeIndex];
                        //分合约计算出盘口数据并对盘口进行判断
                        int index = optionIndex[code];
                        int indexFurther = optionIndex[codeFurther];
                        int k = 0;
                        for (k = index + 1; k < optionPositionChange[code].Count; k++)
                        {
                            if (optionPositionChange[code][k].thisTime > time)
                            {
                                k = k - 1;
                                break;
                            }
                            optionPositionShot[code] = PositionShot.GetPositionShot(optionPositionShot[code], optionPositionChange[code][k]);
                        }
                        optionIndex[code] = k;
                        k = 0;
                        for (k = indexFurther + 1; k < optionPositionChange[codeFurther].Count; k++)
                        {
                            if (optionPositionChange[codeFurther][k].thisTime > time)
                            {
                                k = k - 1;
                                break;
                            }
                            optionPositionShot[codeFurther] = PositionShot.GetPositionShot(optionPositionShot[codeFurther], optionPositionChange[codeFurther][k]);
                        }
                        optionIndex[codeFurther] = k;
                        double price = optionPositionShot[code].ask[0];
                        int volumn = optionPositionShot[code].askv[0];
                        double priceFurther = optionPositionShot[codeFurther].bid[0];
                        int volumnFurther = optionPositionShot[codeFurther].bidv[0];
                        bool close  = false;
                        //核心平仓条件的判断
                        int expiry = OptionCodeInformation.GetTimeSpan(code, today);
                        if (expiry <= 1)
                        {
                            close = true;
                        }
                        if ( price * volumn * priceFurther * volumnFurther > 0)
                        {
                            close = ((priceFurther - price) / (myHold[codeFurther].price - myHold[code].price)<0.9 ? true : close);
                            close = ((priceFurther - price) / (myHold[codeFurther].price - myHold[code].price) >1.4 ? true : close);
                           // close = (((priceFurther - price) - (myHold[codeFurther].price - myHold[code].price)) < -0.005 ? true : close);
                           // close = (((priceFurther - price) - (myHold[codeFurther].price - myHold[code].price)) > 0.02 ? true : close);
                        }
                        #region 平仓。如果满足平仓条件，就进行平仓的操作
                        if (close == true)
                        {
                            
                            //记录当日平仓的合约代码，阻止重复开仓。
                            if (closeCode.Contains(code)==false)
                            {
                                closeCode.Add(code);
                            }
                            if (closeCode.Contains(codeFurther)==false)
                            {
                                closeCode.Add(codeFurther);
                            }
                            optionHold nowHold = new optionHold();
                            optionHold nowHoldFurther = new optionHold();
                            nowHold.price = price;
                            int myChangePosition= Math.Min(Math.Min(volumn, volumnFurther), Math.Abs(myHold[code].position));
                            nowHold.position = myChangePosition;
                            nowHoldFurther.price = priceFurther;
                            nowHoldFurther.position = -myChangePosition;
                            deltaCash+= myChangePosition * ((priceFurther - price) * 10000 - 2.3 * 2);
                            totalCash += myChangePosition * ((priceFurther - price) * 10000 - 2.3*2);
                            fee += myChangePosition * 2.3 * 2;
                            //保存具体的平仓交易记录（按时间日期分类）。
                            myHoldStatus.InsertTradeStatus(code, dateIndex, timeIndex, price, myChangePosition);
                            myHoldStatus.InsertTradeStatus(codeFurther, dateIndex, timeIndex, priceFurther, -myChangePosition);
                            //保持具体的平仓交易记录（按期权合约代码分类）。
                            myHoldStatus.InsertTradeStatusOrderByCode(code, today, time, price, myChangePosition);
                            myHoldStatus.InsertTradeStatusOrderByCode(codeFurther, today, time, priceFurther, -myChangePosition);
                            if (myHold.ContainsKey(code) == true)
                            {
                                optionHold oldHold = myHold[code];
                                int totalPosition = oldHold.position + nowHold.position;
                                if (totalPosition==0)
                                {
                                    myHold.Remove(code);
                                }
                                else
                                {
                                    nowHold.price = (oldHold.price * oldHold.position + nowHold.price * nowHold.position) / totalPosition;
                                    nowHold.position = totalPosition;
                                    myHold[code] = nowHold;
                                }
                            }
                            else
                            {
                                myHold.Add(code, nowHold);
                            }
                            if (myHold.ContainsKey(codeFurther) == true)
                            {
                                optionHold oldHold = myHold[codeFurther];
                                int totalPosition = oldHold.position + nowHoldFurther.position;
                                if (totalPosition==0)
                                {
                                    myHold.Remove(codeFurther);
                                }
                                else
                                {
                                    nowHoldFurther.price = (oldHold.price * oldHold.position + nowHoldFurther.price * nowHoldFurther.position) / totalPosition;
                                    nowHoldFurther.position = totalPosition;
                                    myHold[codeFurther] = nowHoldFurther;
                                }
                            }
                            else
                            {
                                myHold.Add(codeFurther, nowHoldFurther);
                            }

                            //对盘口价格产生了影响
                            positionChange myChange = new positionChange(optionPositionShot[code].time, time);
                            positionStatus myChange0 = new positionStatus();
                            myChange0.price = price;
                            myChange0.volumn = -myChangePosition;
                            myChange.askChange.Add(myChange0);
                            optionPositionShot[code] = PositionShot.GetPositionShot(optionPositionShot[code], myChange);
                            positionChange myChangeFurther = new positionChange(optionPositionShot[codeFurther].time, time);
                            positionStatus myChangeFurther0 = new positionStatus();
                            myChangeFurther0.price = priceFurther;
                            myChangeFurther0.volumn = -myChangePosition;
                            myChangeFurther.bidChange.Add(myChangeFurther0);
                            optionPositionShot[codeFurther] = PositionShot.GetPositionShot(optionPositionShot[codeFurther], myChangeFurther);
                            if (myHold.ContainsKey(code)==false)
                            {
                                break;
                            }
                            if (myHold.ContainsKey(codeFurther) == false)
                            {
                                break;
                            }
                        }
                        #endregion
                    }

                }
                #endregion

                #region 对开仓的信号进行判断
                bool positionOpen = true;
                //获取当日的etf价格并找出其运动区间
                EtfTradeInformation myEtfToday = new EtfTradeInformation(today, startTime, endTime);
                double maxEtfPrice = myEtfToday.GetMaxPrice();
                double minEtfPrice = myEtfToday.GetMinPrice();
                //根据当日etf价格找出平价附近的期权的合约代码
                List<int> optionCode = OptionCodeInformation.GetOptionCodeInInterval(minEtfPrice, maxEtfPrice, today);
                List<int> optionCodeFurther = new List<int>();
                //计算合约的到期时间，仅考虑5-12天之间的期权，其他的跳过
                int expiry0 = OptionCodeInformation.GetTimeSpan(optionCode[0], today);
                if (expiry0 < 5 || expiry0 > 12)
                {
                    positionOpen = false;
                }
                
                if (positionOpen==true)
                {
                    
                    //信息的获取以及初始化
                    for (int i = 0; i < optionCode.Count; i++)
                    {
                        //获取近月合约代码
                        int optionCode0 = optionCode[i];
                        //构造类的实现，包含近月合约的信息
                        OptionCodeInformation myOptionCode0 = new OptionCodeInformation(optionCode0);
                        //构造类的实现，包含远月合约的信息
                        OptionCodeInformation myOptionCode1 = new OptionCodeInformation(OptionCodeInformation.GetFurtherOption(optionCode0, today));
                        //将远月合约的代码记录到数组中
                        optionCodeFurther.Add(myOptionCode1.GetOptionCode());
                        //获取近月以及远月合约的盘口信息
                        OptionTradeInformation myOptionTrade0 = new OptionTradeInformation(myOptionCode0.GetOptionCode(), today);
                        OptionTradeInformation myOptionTrade1 = new OptionTradeInformation(myOptionCode1.GetOptionCode(), today);
                        try
                        {
                            if (optionTrade.ContainsKey(myOptionCode0.GetOptionCode())==false)
                            {
                                optionTrade.Add(myOptionCode0.GetOptionCode(), myOptionTrade0.myTradeInformation);
                            }
                            if (optionTrade.ContainsKey(myOptionCode0.GetOptionCode()) == false)
                            {
                                optionTrade.Add(myOptionCode1.GetOptionCode(), myOptionTrade1.myTradeInformation);
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Data Error!");
                        }
                        //获取近月以及远月合约的盘口变化列表
                        PositionShot myOptionChange0 = new PositionShot(myOptionCode0.GetOptionCode(), today);
                        PositionShot myOptionChange1 = new PositionShot(myOptionCode1.GetOptionCode(), today);
                        try
                        {
                            if (optionPositionChange.ContainsKey(myOptionCode0.GetOptionCode())==false)
                            {
                                optionPositionChange.Add(myOptionCode0.GetOptionCode(), myOptionChange0.GetPositionChange());
                            }
                            if (optionPositionChange.ContainsKey(myOptionCode1.GetOptionCode()) == false)
                            {
                                optionPositionChange.Add(myOptionCode1.GetOptionCode(), myOptionChange1.GetPositionChange());
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Data Error!");
                        }
                        //记录对应合约在参与交易之后盘口的信息
                        tradeInformation positionInitial = new tradeInformation();
                        positionInitial.ask = new double[5];
                        positionInitial.bid = new double[5];
                        positionInitial.askv = new int[5];
                        positionInitial.bidv = new int[5];
                        try
                        {
                            if (optionPositionShot.ContainsKey(myOptionCode0.GetOptionCode())==false)
                            {
                                optionPositionShot.Add(myOptionCode0.GetOptionCode(), positionInitial);
                            }
                            if (optionPositionShot.ContainsKey(myOptionCode1.GetOptionCode()) == false)
                            {
                                optionPositionShot.Add(myOptionCode1.GetOptionCode(), positionInitial);
                            }
                            
                            
                            //存放盘口时间对应的数组下标，初始化为-1
                            if(optionIndex.ContainsKey(myOptionCode0.GetOptionCode()) == false)
                            {
                                optionIndex.Add(myOptionCode0.GetOptionCode(), -1);
                            }
                            if (optionIndex.ContainsKey(myOptionCode1.GetOptionCode()) == false)
                            {
                                optionIndex.Add(myOptionCode1.GetOptionCode(), -1);
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Data Error!");
                        }
                    }
                    //计算远月合约的到期时间
                    int expiryFurther0=OptionCodeInformation.GetTimeSpan(optionCodeFurther[0], today);
                    
                    //按时间顺序查找开仓机会
                    for (int timeIndex = 1; timeIndex < 28440; timeIndex++)
                    {
                        int time = TradeDay.MyTradeTicks[timeIndex];
                        //计算实时的标的etf的价格
                        double etfPrice = 0;
                        for (int etfIndex = 1; etfIndex < myEtfToday.myTradeInformation.Count; etfIndex++)
                        {
                            if (myEtfToday.myTradeInformation[etfIndex].time>time)
                            {
                                etfPrice = myEtfToday.myTradeInformation[etfIndex - 1].lastPrice;
                                break;
                            }
                        }
                        

                        //分合约计算出盘口数据并对盘口进行判断
                        for (int i = 0; i < optionCode.Count; i++)
                        {
                            int code = optionCode[i];
                            int codeFurther = optionCodeFurther[i];
                            int index = optionIndex[code];
                            int indexFurther = optionIndex[codeFurther];
                            //如果今天做过止盈止损，就不再开仓。
                            if (closeCode.Contains(code)==true)
                            {
                                continue;
                            }
                            int k = 0;
                            for (k = index+1; k < optionPositionChange[code].Count; k++)
                            {
                                if (optionPositionChange[code][k].thisTime>time)
                                {
                                    k = k - 1;
                                    break;
                                }
                                optionPositionShot[code] = PositionShot.GetPositionShot(optionPositionShot[code], optionPositionChange[code][k]);
                            }
                            optionIndex[code] = k;
                            k = 0;
                            for (k = indexFurther + 1; k < optionPositionChange[codeFurther].Count; k++)
                            {
                                if (optionPositionChange[codeFurther][k].thisTime > time)
                                {
                                    k = k - 1;
                                    break;
                                }
                                optionPositionShot[codeFurther] = PositionShot.GetPositionShot(optionPositionShot[codeFurther], optionPositionChange[codeFurther][k]);
                            }
                            optionIndex[codeFurther] = k;
                            double price = optionPositionShot[code].bid[0];
                            int volumn= optionPositionShot[code].bidv[0];
                            double priceFurther = optionPositionShot[codeFurther].ask[0];
                            int volumnFurther = optionPositionShot[codeFurther].askv[0];
                            bool open = false;
                            if (etfPrice*price*volumn*priceFurther*volumnFurther>0)
                            {
                                OptionCodeInformation myOptionCode0 = new OptionCodeInformation(code);
                                open = Judgement(etfPrice,price, priceFurther, expiry0, expiryFurther0,myOptionCode0.GetOptionStrike(), myOptionCode0.GetOptionType());
                            }
                            //如果满足开仓条件，就进行开仓的操作
                            #region 开仓
                            if (open == true)
                            {
                                optionHold nowHold = new optionHold();
                                optionHold nowHoldFurther = new optionHold();
                                nowHold.price = price;
                                nowHold.position = -Math.Min(volumn, volumnFurther);
                                nowHoldFurther.price = priceFurther;
                                nowHoldFurther.position = -nowHold.position;
                                //记录开仓的逐笔数据，按时间日期分类。
                                myHoldStatus.InsertTradeStatus(code, dateIndex, timeIndex, nowHold.price, nowHold.position);
                                myHoldStatus.InsertTradeStatus(codeFurther, dateIndex, timeIndex, nowHoldFurther.price,nowHoldFurther.position);
                                //记录开仓的逐笔数据，按期权合约代码分类。
                                myHoldStatus.InsertTradeStatusOrderByCode(code, today, time, nowHold.price, nowHold.position);
                                myHoldStatus.InsertTradeStatusOrderByCode(codeFurther, today, time, nowHoldFurther.price, nowHoldFurther.position);
                                totalCash += nowHoldFurther.position * ((price - priceFurther) * 10000 - 2.3*2);
                                deltaCash+= nowHoldFurther.position * ((price - priceFurther) * 10000 - 2.3 * 2);
                                fee += nowHoldFurther.position * 2.3 * 2;
                                if (myHold.ContainsKey(code) == false)
                                {
                                    myHold.Add(code, nowHold);
                                }
                                else
                                {
                                    optionHold oldHold = myHold[code];
                                    int totalPosition = oldHold.position + nowHold.position;
                                    if (totalPosition == 0)
                                    {
                                        myHold.Remove(code);
                                    }
                                    else
                                    {
                                        nowHold.price = (oldHold.price * oldHold.position + nowHold.price * nowHold.position) / totalPosition;
                                        nowHold.position = totalPosition;
                                        myHold[code] = nowHold;
                                    }
                                }
                                if (myHold.ContainsKey(codeFurther) == false)
                                {
                                    myHold.Add(codeFurther, nowHoldFurther);
                                }
                                else
                                {
                                    optionHold oldHold = myHold[codeFurther];
                                    int totalPosition = oldHold.position + nowHoldFurther.position;
                                    if (totalPosition == 0)
                                    {
                                        myHold.Remove(codeFurther);
                                    }
                                    else
                                    {
                                        nowHoldFurther.price = (oldHold.price * oldHold.position + nowHoldFurther.price * nowHoldFurther.position) / totalPosition;
                                        nowHoldFurther.position = totalPosition;
                                        myHold[codeFurther] = nowHoldFurther;
                                    }
                                }
                                //对盘口价格产生了影响
                                positionChange myChange = new positionChange(optionPositionShot[code].time, time);
                                positionStatus myChange0 = new positionStatus();
                                myChange0.price = price;
                                myChange0.volumn = -Math.Min(volumn, volumnFurther);
                                myChange.bidChange.Add(myChange0);
                                optionPositionShot[code]= PositionShot.GetPositionShot(optionPositionShot[code], myChange);
                                positionChange myChangeFurther = new positionChange(optionPositionShot[codeFurther].time, time);
                                positionStatus myChangeFurther0 = new positionStatus();
                                myChangeFurther0.price =priceFurther;
                                myChangeFurther0.volumn = -Math.Min(volumn, volumnFurther);
                                myChangeFurther.askChange.Add(myChangeFurther0);
                                optionPositionShot[codeFurther] = PositionShot.GetPositionShot(optionPositionShot[codeFurther], myChangeFurther);
                            }
                            #endregion
                            
                        }
                    }
                }
                #endregion

                //对今日的持仓进行清理，如果持仓为0就去除该项记录。
                myHoldKey = new List<int>();
                myHoldKey.AddRange(myHold.Keys);
                foreach (var myKey in myHoldKey)
                {
                    if (myHold[myKey].position==0)
                    {
                        myHold.Remove(myKey);
                    }
                }
                //将今日持仓情况存入列表。之后才可以根据该头寸计算保证金，希腊值等。
                myHoldStatus.InsertPositionStatus(today, myHold);
                //计算当日持仓状态,包括维持保证金，期权的当前价值，开仓成本，希腊值等
                ComputePositionStatus(today, myHold, ref optionMargin, ref optionValue, ref optionCost, ref optionDelta, ref optionGamma);
                //计算当日收盘之后的可用资金
                cashAvailable = totalCash - optionMargin;
                //将当天的情况存储进入myHoldStatus
                myHoldStatus.InsertCashStatus(today, cashAvailable, optionMargin, fee,optionValue);
                myHoldStatus.InsertGreekStatus(today, optionDelta, optionGamma);
                //在屏幕上输出每日的情况。
                Console.WriteLine("{0},optionValue: {1}, Margin: {2}, Cash: {3}, total: {4}", today, Math.Round(optionValue), Math.Round(optionMargin), Math.Round(cashAvailable),Math.Round(totalCash+optionValue));
                Console.WriteLine("          delta: {0}, gamma: {1}, optionCost: {2} ", Math.Round(optionDelta), Math.Round(optionGamma),Math.Round(optionCost));
            }
        }
        /// <summary>
        /// 记录每日逐笔交易的函数。
        /// </summary>
        /// <returns>返回true表示逐笔交易数据存在，否者返回false</returns>
        public bool RecordTradeStatusList()
        {
            if (myHoldStatus==null)
            {
                Console.WriteLine("error!No trade record!");
            }
            else
            {
                //将逐笔交易记录存入csv文件
                myHoldStatus.RecordTradeStatusList(filePathName, myTradeDay.MyTradeDays, TradeDay.MyTradeTicks);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 核对交易记录和盘口价格判断能否成交。
        /// </summary>
        /// <returns>返回判断结果</returns>
        public bool CheckTradeStatusList()
        {
            bool isRight = true;
            OptionTradeInformation myOptionTrade=new OptionTradeInformation();
            double totalInterest = 0;
            foreach (var myHoldList in myHoldStatus.tradeStatusListOrderByCode)
            {
                //初始化
                int position = 0;
                double Interest = 0;
                int startIndex = 0;
                int endIndex = -1;
                List<int> startIndexList = new List<int>();
                //将列表按日期分段。
                for (int index = 0; index < myHoldList.Value.Count; index++)
                {
                    if (index==0)
                    {
                        startIndexList.Add(index);
                    }
                    else
                    {
                        if (myHoldList.Value[index].date>myHoldList.Value[index-1].date)
                        {
                            startIndexList.Add(index);
                        }
                    }
                }
                for (int listIndex = 0; listIndex < startIndexList.Count; listIndex++)
                {
                    if (listIndex==startIndexList.Count-1)
                    {
                        startIndex = startIndexList[listIndex];
                        endIndex = myHoldList.Value.Count - 1;
                    }
                    else
                    {
                        startIndex = startIndexList[listIndex];
                        endIndex = startIndexList[listIndex+1]-1;
                    }
                    int today = myHoldList.Value[startIndex].date;
                    int indexOfTrade = startIndex;
                    //进行当日成交记录的计算
                    PositionShot myOptionChange = new PositionShot(myHoldList.Key, today);
                    List<positionChange> myChange = myOptionChange.GetPositionChange();
                    tradeInformation positionInitial = new tradeInformation();
                    positionInitial.ask = new double[5];
                    positionInitial.bid = new double[5];
                    positionInitial.askv = new int[5];
                    positionInitial.bidv = new int[5];
                    int indexOfChange = 0;
                    while (indexOfChange < myChange.Count)
                    {
                        var change = myChange[indexOfChange];
                        if (change.thisTime <= myHoldList.Value[indexOfTrade].time)
                        {
                            positionInitial = PositionShot.GetPositionShot(positionInitial, change);
                            indexOfChange += 1;
                        }
                        else
                        {
                            var myHoldNow = myHoldList.Value[indexOfTrade];
                            if (myHoldNow.position > 0)  //买入，比较ask1
                            {
                                int indexOfAsk = 5;
                                for (int i = 0; i < 5; i++)
                                {
                                    if (myHoldNow.price == positionInitial.ask[i] && Math.Abs(myHoldNow.position) <= Math.Abs(positionInitial.askv[i]))
                                    {
                                        indexOfAsk = i;
                                        break;
                                    }
                                }
                                if (indexOfAsk < 5)
                                {
                                    position += myHoldNow.position;
                                    Interest += -(myHoldNow.price * 10000 * myHoldNow.position) - 2.3 * Math.Abs(myHoldNow.position);
                                    //整理盘口价格
                                    positionInitial.askv[indexOfAsk] -= Math.Abs(myHoldNow.position);
                                }
                                else
                                {
                                    Console.WriteLine("There is wrong in date:{0},code:{1},time{2}!", today, myHoldList.Key, myHoldNow.time);
                                }
                            }
                            else  //卖出，比较bid1
                            {
                                int indexOfBid = 5;
                                for (int i = 0; i < 5; i++)
                                {
                                    if (myHoldNow.price == positionInitial.bid[i] && Math.Abs(myHoldNow.position) <= Math.Abs(positionInitial.bidv[i]))
                                    {
                                        indexOfBid = i;
                                        break;
                                    }
                                }
                                if (indexOfBid < 5)
                                {
                                    position += myHoldNow.position;
                                    Interest += -(myHoldNow.price * 10000 * myHoldNow.position) - 2.3 * Math.Abs(myHoldNow.position);
                                    //整理盘口价格
                                    positionInitial.bidv[indexOfBid] -= Math.Abs(myHoldNow.position);
                                }
                                else
                                {
                                    Console.WriteLine("There is wrong in date:{0},code:{1},time{2}!", today, myHoldList.Key, myHoldNow.time);
                                }
                            }
                            indexOfTrade += 1;
                            if (indexOfTrade > endIndex)
                            {
                                break;
                            }

                        }
                    }

                }
                if (position!=0)
                {
                    Console.WriteLine("Position:{0} is not balance in option:{1}",position, myHoldList.Key);
                    //foreach (var item in myHoldList.Value)
                    //{
                    //    Console.WriteLine("date:{0},time:{1},price:{2},position:{3}",item.date, item.time, item.price, item.position);
                    //}
                }
                totalInterest += Interest;
            }
            Console.WriteLine(Math.Round(totalInterest));
            return isRight;
        }
    }
}
