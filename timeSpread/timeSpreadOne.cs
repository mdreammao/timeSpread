using System;
using System.Collections.Generic;


namespace timeSpread
{

    struct parameter
    {
        public int expriyMaxLimit;
        public int expriyMinLimit;
        public double lossStopRatio;
        public double profitStopRatio;
        public parameter(int max,int min,double loss,double profit)
        {
            expriyMaxLimit = max;
            expriyMinLimit = min;
            lossStopRatio = loss;
            profitStopRatio = profit;
        }
    }
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
        /// 给定回测参数的初始值。
        /// </summary>
        private parameter mypara=new parameter(12,7,0.8,1.2);
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
        public TimeSpreadOne(int startDate,int endDate,int startTime,int endTime,double cash=100000000)
        {
            this.startDate = startDate;
            this.endDate = endDate;
            this.startTime = startTime;
            this.endTime = endTime;
            myTradeDay = new TradeDay(startDate, endDate);
            this.totalCash = cash;
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
        /// 给定之前盘口价格的状态，给定目标时刻以及全天的盘口价格变化情况，给出目标时刻的盘口状态。
        /// </summary>
        /// <param name="index">当前盘口状态数组下标</param>
        /// <param name="shot">前一状态盘口价格</param>
        /// <param name="change">全天的盘口价格变化</param>
        /// <param name="time">目标时刻的时间</param>
        /// <returns>目标时刻的盘口状态</returns>
        private tradeInformation GetOpitonShot(ref int index,tradeInformation shot,List<positionChange> change,int time)
        {
            while (index<=change.Count-1 && change[index].thisTime<=time)
            {
                shot = PositionShot.GetPositionShot(shot, change[index]);
                index += 1;
            }
            return shot;
        }
        /// <summary>
        /// 根据盘口价格和其他参数，判断当前时刻是否该开仓，如果开仓，应该开多大的仓位。
        /// </summary>
        /// <param name="etfPrice">50etf的价格</param>
        /// <param name="strike">期权的行权价</param>
        /// <param name="type">期权的类型</param>
        /// <param name="shot">近月期权的盘口价格</param>
        /// <param name="shotFurther">远月期权的盘口价格</param>
        /// <param name="expiry">近月期权的到期日</param>
        /// <param name="expiryFurther">远月期权的到期日</param>
        /// <param name="para">开平仓的参数</param>
        /// <returns>返回开仓数量，如果不开仓返回0</returns>
        private int GetOptionOpenVolumn(double etfPrice,double strike,string type,tradeInformation shot, tradeInformation shotFurther, int expiry, int expiryFurther,parameter para)
        {
            int openVolumn = 0;
            if (expiry<para.expriyMinLimit || expiry>para.expriyMaxLimit)
            {
                return 0;
            }
            double price = shot.bid[0];
            int volumn = shotFurther.bidv[0];
            double priceFurther = shotFurther.ask[0];
            int volumnFurther = shotFurther.askv[0];
            bool open = false;
            if (etfPrice*price * volumn * priceFurther * volumnFurther > 0)
            {
                open = Judgement(etfPrice, price, priceFurther, expiry, expiryFurther, strike, type);
            }
            if (open==true)
            {
                openVolumn = Math.Min(volumn, volumnFurther);
            }
            return openVolumn;
        }
        /// <summary>
        /// 给定近月以及远月期权合约的历史持仓以及当前盘口价格，得到应该平仓的头寸。
        /// </summary>
        /// <param name="option">近月合约历史持仓</param>
        /// <param name="optionFurther">远月合约历史持仓</param>
        /// <param name="shot">近月合约盘口状态</param>
        /// <param name="shotFurther">远月合约盘口状态</param>
        /// <param name="expiry">近月合约的到期日期</param>
        /// <returns>应该平仓的头寸，如果不平仓则返回0</returns>
        private int GetOptionCloseVolumn(optionHold option,optionHold optionFurther,tradeInformation shot,tradeInformation shotFurther,int expiry,parameter para)
        {
            int volumn = 0;
            //在当前情况下，近月合约只能是空头，远月合约是多头。如果以后需要扩展，可以从volume的正负号上体现出来。
            if (option.position<0)
            {
                double price = shot.ask[0];
                double priceFurther = shotFurther.bid[0];
                if ((priceFurther-price)/(optionFurther.price-option.price)>para.profitStopRatio || (priceFurther - price) / (optionFurther.price - option.price) < para.lossStopRatio || expiry<=1)
                {
                    volumn = Math.Min(Math.Min(shot.askv[0], shotFurther.bidv[0]), Math.Abs(option.position));
                }

            }
            return volumn;
        }
        /// <summary>
        /// 改变持仓情况的函数。
        /// </summary>
        /// <param name="oldHold">旧的持仓情况</param>
        /// <param name="price">最近成交的价格</param>
        /// <param name="volumn">最近成交的头寸</param>
        /// <returns>新的持仓情况</returns>
        private optionHold GetNewHold(optionHold oldHold,double price,int volumn)
        {
            optionHold newHold = new optionHold();
            int newPosition = volumn + oldHold.position;
            double newPrice = (newPosition == 0) ? 0 : (price * volumn + oldHold.price * oldHold.position) / newPosition;
            newHold.price = newPrice;
            newHold.position = newPosition;
            return newHold;
        }
        /// <summary>
        /// 根据价格和成交量的信息，生成给出盘口价格变动的信息。
        /// </summary>
        /// <param name="lastTime">上一次交易时间</param>
        /// <param name="thisTime">当前时间</param>
        /// <param name="price">当前成交价格</param>
        /// <param name="volumn">当前成交量</param>
        /// <param name="type">ask还是bid价格的成交</param>
        /// <returns>返回盘口变动的情况</returns>
        private positionChange GetShotChange(int lastTime,int thisTime,double price,int volumn,string type)
        {
            positionChange myChange = new positionChange(lastTime,thisTime);
            positionStatus myChange0 = new positionStatus();
            myChange0.price = price;
            myChange0.volumn = -volumn;
            if (type=="ask")
            {
                myChange.askChange.Add(myChange0);
            }
            else
            {
                myChange.bidChange.Add(myChange0);
            }
            return myChange;
        }
        /// <summary>
        /// 回测的核心函数。利用历史数据模拟交易过程。
        /// </summary>
        public void TimeSpreadAnalysis()
        {
            #region 初始化各类参数
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
            //初始化记录期权行权价和期权类型的哈希表。该表不随交易日变化而变化。
            Dictionary<int, double> optionStrike = new Dictionary<int, double>();
            Dictionary<int, string> optionType = new Dictionary<int, string>();
            #endregion
            //逐步遍历交易日期，逐日进行回测。
            for (int dateIndex = 0; dateIndex < myTradeDay.MyTradeDays.Count; dateIndex++)
            {
                //初始化日内的参数
                int today = myTradeDay.MyTradeDays[dateIndex];
                fee = 0;
                deltaCash = 0;
                optionValue = 0;
                optionCost = 0;
                double optionDelta = 0;
                double optionGamma = 0;
                myHold = myHoldStatus.GetPositionStatus();


                //初始化各类信息，包括记录每个合约具体的盘口价格，具体的盘口价格变动，参与交易之后具体的盘口状态等。
                Dictionary<int, List<tradeInformation>> optionTrade = new Dictionary<int, List<tradeInformation>>();
                Dictionary<int, tradeInformation> optionPositionShot = new Dictionary<int, tradeInformation>();
                Dictionary<int, List<positionChange>> optionPositionChange = new Dictionary<int, List<positionChange>>();
                //记录了期权合约遍历的位置，避免先开仓后平仓的情况。
                Dictionary<int, int> optionIndex = new Dictionary<int, int>();
                //记录了期权合约距离到期的日期
                Dictionary<int, int> optionExpiry = new Dictionary<int, int>();

                //第一步，选取今日应当关注的合约代码，包括平价附近的期权合约以及昨日遗留下来的持仓。其中，平价附近的期权合约必须满足交易日的需求，昨日遗留下来的持仓必须全部囊括。近月合约列入code，远月合约列入codeFurther。
                //注意，某些合约既要进行开仓判断又要进行平仓判断。
                
                //获取当日的etf价格并找出其运动区间。
                EtfTradeInformation myEtfToday = new EtfTradeInformation(today, startTime, endTime);
                int etfIndex = 0;//记录今日etf价格对应的数组下标，从0开始。
                double maxEtfPrice = myEtfToday.GetMaxPrice();
                double minEtfPrice = myEtfToday.GetMinPrice();
                List<int> optionAtTheMoney = OptionCodeInformation.GetOptionCodeInInterval(minEtfPrice, maxEtfPrice, today);
                List<int> optionCode = new List<int>();
                List<int> optionCodeFurther = new List<int>();
                //记入今日平价附近的期权合约。
                foreach (var code in optionAtTheMoney)
                {
                    int expiry = OptionCodeInformation.GetTimeSpan(code, today);
                    if (expiry>=mypara.expriyMinLimit && expiry<=mypara.expriyMaxLimit)
                    {
                        int codeFurther = OptionCodeInformation.GetFurtherOption(code, today);
                        if (codeFurther!=0 && optionCode.Contains(code)==false)
                        {
                            optionCode.Add(code);
                            optionCodeFurther.Add(codeFurther);
                        }
                    }
                }
                //记入昨日持仓的期权合约
                List<int> myHoldyesterday = new List<int>();
                myHoldyesterday.AddRange(myHold.Keys);
                foreach (var code in myHoldyesterday)
                {
                    int codeFurther= OptionCodeInformation.GetFurtherOption(code, today);
                    if (myHoldyesterday.Contains(codeFurther) && optionCode.Contains(code)==false)
                    {
                        optionCode.Add(code);
                        optionCodeFurther.Add(codeFurther);
                    }
                }
                //
                //根据合约代码的信息，从sql数据库中提取今日的数据信息。并初始化code以及codeFurther的初始化状态。
                foreach (var code in optionCode)
                {
                    PositionShot myOptionChange = new PositionShot(code, today);
                    optionPositionChange.Add(code, myOptionChange.GetPositionChange());
                    tradeInformation positionInitial = new tradeInformation(0,0);
                    optionPositionShot.Add(code, positionInitial);
                    optionIndex.Add(code, 0);
                    optionExpiry.Add(code, OptionCodeInformation.GetTimeSpan(code, today));
                    if (optionStrike.ContainsKey(code)==false)
                    {
                        OptionCodeInformation option = new OptionCodeInformation(code);
                        optionStrike.Add(code, option.GetOptionStrike());
                        optionType.Add(code, option.GetOptionType());
                    }
                }
                foreach (var code in optionCodeFurther)
                {
                    PositionShot myOptionChange = new PositionShot(code, today);
                    optionPositionChange.Add(code, myOptionChange.GetPositionChange());
                    tradeInformation positionInitial = new tradeInformation(0, 0);
                    optionPositionShot.Add(code, positionInitial);
                    optionIndex.Add(code, 0);
                    optionExpiry.Add(code, OptionCodeInformation.GetTimeSpan(code, today));
                    if (optionStrike.ContainsKey(code) == false)
                    {
                        OptionCodeInformation option = new OptionCodeInformation(code);
                        optionStrike.Add(code, option.GetOptionStrike());
                        optionType.Add(code, option.GetOptionType());
                    }
                }
                //第二部，根据当日的行情逐tick进行信号的判断。并采取对应的开平仓措施
                #region 逐tick进行开仓以及平仓的判断。
                //按时间的下标进行遍历，4小时对应28800个tick，忽略最后3分钟必定进行集合竞价的时间段。当然，具体的遍历时间可以具体讨论。
                double etfPrice = 0;//etf价格从tick0开始遍历
                //在同一个tick里面，不能刚开仓就平仓，记录平仓发生时刻的时间下标。
                Dictionary<int, int> closeTimeIndex = new Dictionary<int, int>();
                for (int timeIndex = 1; timeIndex < 28440; timeIndex++)
                {
                    int time = TradeDay.MyTradeTicks[timeIndex];
                    //计算实时的标的etf的价格
                    while (etfIndex<= myEtfToday.myTradeInformation.Count - 1 && myEtfToday.myTradeInformation[etfIndex].time<=time)
                    {
                        etfPrice = myEtfToday.myTradeInformation[etfIndex].lastPrice;
                        etfIndex += 1;
                    }
                    //逐对合约进行观察，分析其当前价格的ask以及bid
                    //需要考察的合约必须是 1.有开仓潜力的新合约 2.仓位未平的老合约
                    for (int codeListIndex = 0; codeListIndex < optionCode.Count; codeListIndex++)
                    {
                        int code = optionCode[codeListIndex];
                        int codeFurther = optionCodeFurther[codeListIndex];
                        
                        //合约到日期不再范围之内的不予考虑
                        int expiry = optionExpiry[code];
                        int expiryFurther = optionExpiry[codeFurther];
                        if ((expiry < mypara.expriyMinLimit || expiry > mypara.expriyMaxLimit) && myHold[code].position==0)
                        {
                            continue;
                        }
                        //根据合约代码对应的数组下标以及合约代码对应的盘口快照，生成当前时刻的盘口快照。
                        int index = optionIndex[code];
                        int indexFurther = optionIndex[codeFurther];
                        optionPositionShot[code]=GetOpitonShot(ref index, optionPositionShot[code], optionPositionChange[code], time);
                        optionPositionShot[codeFurther] = GetOpitonShot(ref indexFurther, optionPositionShot[codeFurther], optionPositionChange[codeFurther], time);
                        optionIndex[code] = index;
                        optionIndex[codeFurther] = indexFurther;
                    }
                    //在每个tick如果有仓位就必须进行平仓的判断。如果满足开仓条件就必须进行开仓。对所有的近月option遍历进行判断。
                    for (int codeListIndex = 0; codeListIndex < optionCode.Count; codeListIndex++)
                    {
                        //记录合约代码
                        int code = optionCode[codeListIndex];
                        int codeFurther = optionCodeFurther[codeListIndex];
                        //记录合约到期日
                        int expiry = optionExpiry[code];
                        int expiryFurther = optionExpiry[codeFurther];
                        //如果仓位未平就需要平仓判断。
                        if (myHold.ContainsKey(code) && myHold[code].position!=0)
                        {
                            //为简单起见这里只做关于成本的止盈止损判断。
                            int volumn = GetOptionCloseVolumn(myHold[code], myHold[codeFurther], optionPositionShot[code], optionPositionShot[codeFurther], expiry, mypara);
                            //若通过判断得到的平仓头寸大于0，这进行状态变化的计算以及记录。
                            if (volumn>0) 
                            {
                                //提取盘口买一和卖一价格。
                                double price = optionPositionShot[code].ask[0];
                                double priceFurther = optionPositionShot[codeFurther].bid[0];
                                //保存具体的平仓交易记录（按时间日期分类）。
                                myHoldStatus.InsertTradeStatus(code, dateIndex, timeIndex, price, volumn);
                                myHoldStatus.InsertTradeStatus(codeFurther, dateIndex, timeIndex, priceFurther, -volumn);
                                //保持具体的平仓交易记录（按期权合约代码分类）。
                                myHoldStatus.InsertTradeStatusOrderByCode(code, today, time, price, volumn);
                                myHoldStatus.InsertTradeStatusOrderByCode(codeFurther, today, time, priceFurther, -volumn);
                                //对持仓情况产生了影响。
                                myHold[code] = GetNewHold(myHold[code], price, volumn);
                                myHold[codeFurther] = GetNewHold(myHold[codeFurther], priceFurther, -volumn);
                                //对盘口价格产生了影响。将策略参与到市场之后得到的盘口价格记录下来。
                                optionPositionShot[code] = PositionShot.GetPositionShot(optionPositionShot[code], GetShotChange(optionPositionShot[code].time,time,price,volumn,"ask"));
                                optionPositionShot[codeFurther] = PositionShot.GetPositionShot(optionPositionShot[codeFurther], GetShotChange(optionPositionShot[codeFurther].time, time, priceFurther, volumn, "bid"));
                                //记录整体情况
                                totalCash += volumn * ((priceFurther-price) * 10000 - 2.3 * 2);
                                deltaCash += volumn * ((priceFurther-price) * 10000 - 2.3 * 2);
                                fee += volumn * 2.3 * 2;
                                //记录当日平仓时刻
                                if (closeTimeIndex.ContainsKey(code)==false)
                                {
                                    closeTimeIndex.Add(code, timeIndex);
                                    closeTimeIndex.Add(codeFurther, timeIndex);
                                }
                                else
                                {
                                    closeTimeIndex[code] = timeIndex;
                                    closeTimeIndex[codeFurther] = timeIndex;
                                }
                            }
                        }
                        //如果到日期满足跨期价差的条件，进行开仓的判断
                        int openVolumn = 0;
                        if (closeTimeIndex.ContainsKey(code)==false || closeTimeIndex[code]>timeIndex+600*2)
                        {
                            openVolumn = GetOptionOpenVolumn(etfPrice, optionStrike[code], optionType[code], optionPositionShot[code], optionPositionShot[codeFurther], expiry, expiryFurther, mypara);
                        }
                        if (openVolumn>0)
                        {
                            //提取盘口的买一和卖一价格。
                            double price = optionPositionShot[code].bid[0];
                            double priceFurther = optionPositionShot[codeFurther].ask[0];
                            //记录开仓的逐笔数据，按时间日期分类。
                            myHoldStatus.InsertTradeStatus(code, dateIndex, timeIndex, price, -openVolumn);
                            myHoldStatus.InsertTradeStatus(codeFurther, dateIndex, timeIndex, priceFurther,openVolumn);
                            //记录开仓的逐笔数据，按期权合约代码分类。
                            myHoldStatus.InsertTradeStatusOrderByCode(code, today, time, price, -openVolumn);
                            myHoldStatus.InsertTradeStatusOrderByCode(codeFurther, today, time, priceFurther, openVolumn);
                            //对持仓情况产生了影响。如果持仓未有记录就新生成对应的持仓记录。
                            if (myHold.ContainsKey(code)==false)
                            {
                                optionHold newHold = new optionHold();
                                myHold.Add(code, newHold);
                            }
                            if (myHold.ContainsKey(codeFurther) == false)
                            {
                                optionHold newHold = new optionHold();
                                myHold.Add(codeFurther, newHold);
                            }
                            myHold[code] = GetNewHold(myHold[code], price,-openVolumn);
                            myHold[codeFurther] = GetNewHold(myHold[codeFurther], priceFurther, openVolumn);
                            //对盘口价格产生了影响。将策略参与到市场之后得到的盘口价格记录下来。
                            optionPositionShot[code] = PositionShot.GetPositionShot(optionPositionShot[code], GetShotChange(optionPositionShot[code].time, time, price, openVolumn, "bid"));
                            optionPositionShot[codeFurther] = PositionShot.GetPositionShot(optionPositionShot[codeFurther], GetShotChange(optionPositionShot[codeFurther].time, time, priceFurther, openVolumn, "ask"));
                            //记录整体情况
                            totalCash += openVolumn * ((price - priceFurther) * 10000 - 2.3 * 2);
                            deltaCash += openVolumn * ((price - priceFurther) * 10000 - 2.3 * 2);
                            fee += openVolumn * 2.3 * 2;
                        }
                    }

                 }
                #endregion 
                   
                //对今日的持仓进行清理，如果持仓为0就去除该项记录。
                List<int> myHoldKey = new List<int>();
                myHoldKey.AddRange(myHold.Keys);
                foreach (var myKey in myHoldKey)
                {
                    if (myHold[myKey].position==0)
                    {
                        myHold.Remove(myKey);
                    }
                }

                #region 计算，记录并显示当日交易情况，持仓情况
                //将今日持仓情况存入列表。之后才可以根据该头寸计算保证金，希腊值等。
                myHoldStatus.InsertPositionStatus(today, myHold);
                //计算当日持仓状态,包括维持保证金，期权的当前价值，开仓成本，希腊值等
                ComputePositionStatus(today, myHold, ref optionMargin, ref optionValue, ref optionCost, ref optionDelta, ref optionGamma);
                //计算当日收盘之后的可用资金
                cashAvailable = totalCash - optionMargin;
                //将当天的情况存储进入myHoldStatus
                myHoldStatus.InsertCashStatus(today, cashAvailable, optionMargin, fee, optionValue);
                myHoldStatus.InsertGreekStatus(today, optionDelta, optionGamma);
                //在屏幕上输出每日的情况。
                Console.WriteLine("{0},optionValue: {1}, Margin: {2}, Cash: {3}, total: {4}", today, Math.Round(optionValue), Math.Round(optionMargin), Math.Round(cashAvailable), Math.Round(totalCash + optionValue));
                Console.WriteLine("          delta: {0}, gamma: {1}, optionCost: {2} ", Math.Round(optionDelta), Math.Round(optionGamma), Math.Round(optionCost));
                #endregion

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
