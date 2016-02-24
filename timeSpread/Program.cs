/**
#######################################################
跨期价差回测程序。
利用期权和50etf的盘口价格来计算跨期价差的收益情况。
作者：毛衡
时间：2016-02-05
版本：v1.0.0
#######################################################
对注释进行标准化处理并删除部分无效函数和类。
修改者：毛衡
时间：2016-02-15
版本：v1.0.1
#######################################################
更新对交易的记录功能，并能按交易日计算每日的资金情况
修改者：毛衡
时间：2016-02-18
版本：v1.1.0
#######################################################
**/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace timeSpread
{
    class Program
    {
        static void Main(string[] args)
        {
            TimeSpreadOne myAnalysis = new TimeSpreadOne(20150713, 20150713, 93000000, 150000000);
            myAnalysis.TimeSpreadAnalysis();
            myAnalysis.RecordTradeStatusList();
            Console.WriteLine(myAnalysis.CheckTradeStatusList());
        }
    }
}
