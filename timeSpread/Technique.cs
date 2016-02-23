using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MathNet.Numerics.Financial;

namespace timeSpread
{
    /// <summary>
    /// 存储读取csv文件的工具类。包括读取方法和存储方法。默认路径为当前文件夹。
    /// </summary>
    class CsvUtility
    {
        /// <summary>
        /// 将字符串写入csv文档
        /// </summary>
        /// <param name="filePathName">csv文档路径</param>
        /// <param name="ls">需存储的字符串列表</param>
        public static void WriteCsv(string filePathName, List<string[]> ls)
        {
            WriteCsv(filePathName, false, ls);
        }
        /// <summary>
        /// 将字符串写入csv文档
        /// </summary>
        /// <param name="filePathName">csv文档路径</param>
        /// <param name="append">判断是否是尾部添加的模式</param>
        /// <param name="ls">需存储的字符串列表</param>
        public static void WriteCsv(string filePathName, bool append, List<string[]> ls)
        {
            StreamWriter fileWriter = new StreamWriter(filePathName, append, Encoding.Default);
            foreach (string[] strArr in ls)
            {
                fileWriter.WriteLine(string.Join(",", strArr));
            }
            fileWriter.Flush();
            fileWriter.Close();
        }
        /// <summary>
        /// 读取csv文档
        /// </summary>
        /// <param name="filePathName">读取文档的地址</param>
        /// <returns>返回读取的字符串列表</returns>
        public static List<string[]> ReadCsv(string filePathName)
        {
            List<string[]> ls = new List<string[]>();
            StreamReader fileReader = new StreamReader(filePathName);
            string strLine = "";
            while (strLine != null)
            {
                strLine = fileReader.ReadLine();
                if (strLine != null && strLine.Length > 0)
                {
                    ls.Add(strLine.Split(','));
                }
            }
            fileReader.Close();
            return ls;
        }

    }
    class Technique
    {

        /// <summary>
        /// 得到一个可序列化对象的克隆。
        /// </summary>
        public static object Clone(object obj)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, obj);
            memoryStream.Position = 0;
            return formatter.Deserialize(memoryStream);
        }
        /// <summary>
        /// 得到一个Dictionary<int, optionHold>类型的对象的克隆。
        /// </summary>
        /// <param name="obj">克隆对象</param>
        /// <returns></returns>
        public static Dictionary<int, optionHold> Clone(Dictionary<int, optionHold> obj)
        {
            Dictionary<int, optionHold> newObj = new Dictionary<int, optionHold>();
            foreach (var item in obj)
            {
                //optionHold optionHoldItem = new optionHold();
                //optionHoldItem = item.Value;
                newObj.Add(item.Key, item.Value);
            }
            return newObj;
        }
    }
}
