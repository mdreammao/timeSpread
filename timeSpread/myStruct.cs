using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// 记录持仓头寸以及开仓的成本等信息的结构体。
    /// </summary>
    struct optionHold
    {
        public double price;
        public int position;
    }
    /// <summary>
    /// 记录持仓头寸以及开仓成本的结构体（包括时间和日期）。
    /// </summary>
    struct optionHoldWithTime
    {
        public int date;
        public int time;
        public double price;
        public int position;
    }
    /// <summary>
    /// 资金管理
    /// </summary>
    struct asset
    {
        public double cash;
        public double margin;
        public double fee;
        public double optionValue;
    }
    /// <summary>
    /// 希腊值
    /// </summary>
    struct greek
    {
        public double delta;
        public double gamma;
    }
    /// <summary>
    /// 存储期权基本信息的结构体。包括期权合约代码，期权名称，期权类型，行权价，开始时间，结束时间。
    /// </summary>
    struct optionInformation
    {
        public int optionCode;
        public string optionName;
        public string optionType;
        public double strike;
        public int startdate;
        public int enddate;

    }
    /// <summary>
    /// 记录价格信息的结构体。包括交易时间，最新价，五档的买价买量，五档的卖价卖量。
    /// </summary>
    struct tradeInformation
    {
        public int time;
        public double lastPrice;
        public double[] ask, bid;
        public int[] askv, bidv;
        public tradeInformation(int time, double price)
        {
            this.time = time;
            lastPrice = price;
            ask = new double[5];
            bid = new double[5];
            askv = new int[5];
            bidv = new int[5];
        }
    }
    /// <summary>
    /// 包含挂单价格和挂单量的结构体。
    /// </summary>
    struct positionStatus
    {
        public double price;
        public int volumn;
    }
    /// <summary>
    /// 回测参数的设置
    /// </summary>
    struct parameter
    {
        public int expriyMaxLimit;
        public int expriyMinLimit;
        public double lossStopRatio;
        public double profitStopRatio;
        public parameter(int max, int min, double loss, double profit)
        {
            expriyMaxLimit = max;
            expriyMinLimit = min;
            lossStopRatio = loss;
            profitStopRatio = profit;
        }
    }
    /// <summary>
    /// 记录每日持仓以及资金情况
    /// </summary>
    struct portfolioStatus
    {
        public double optionValue;
        public double optionCost;
        public double availableCash;
        public double optionMargin;
        public double optionDelta;
        public double optionGamma;
        public double totalDelta;
        public double totalCash;
        public double totalFee;
        public double portfolioValue;
    }

    /// <summary>
    /// 存储期权基本信息的结构体。
    /// </summary>
    struct optionFormat
    {
        public int optionCode;
        public string optionName;
        public int startDate;
        public int endDate;
        public string optionType;
        public string executeType;
        public double strike;
        public string market;
    }

    /// <summary>
    /// 存储期权数据的结构体。包含30多个字段。
    /// </summary>
    struct optionDataFormat
    {
        public int optionCode;
        public string optionType;
        public double strike;
        public int startDate;
        public int endDate;
        public int date;
        public int time;
        public int tick;
        public double volumn;
        public double turnover;
        public double accVolumn;
        public double accTurnover;
        public double open;
        public double high;
        public double low;
        public double lastPrice;
        public double preSettle;
        public double preClose;
        public double[] ask, askv, bid, bidv;
        public double openMargin;
        public double askVolatility, bidVolatility, midVolatility;
        public double askDelta, bidDelta, midDelta;
    }
}
