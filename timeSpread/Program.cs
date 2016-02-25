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
更新交易的记录功能，并能按交易日计算每日的资金情况
修改者：毛衡
时间：2016-02-18
版本：v1.1.0
#######################################################
1、备份到github上，方便团队合作以及版本的更新回滚等。
2、对每日交易的逻辑进行了重新整理，使得更加合理。并对
部分细节进行了修改。
3、写了函数，能对每日的交易情况进行核对。
修改者：毛衡
时间：2016-02-25
版本：v1.2.0
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
            TimeSpreadOne myAnalysis = new TimeSpreadOne(20150309, 20150331, 93000000, 150000000);
            myAnalysis.TimeSpreadAnalysis();
            myAnalysis.RecordTradeStatusList();
            Console.WriteLine(myAnalysis.CheckTradeStatusList());
        }
    }
}
