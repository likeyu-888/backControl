using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace EliteService.Utility
{
    public class Helper
    {
        public static string md5(string plainText)
        {

            byte[] plainTextArr = Encoding.UTF8.GetBytes(plainText);

            System.Security.Cryptography.MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(plainTextArr);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < output.Length; i++)
            {
                sb.Append(output[i].ToString("x2"));
            }
            return sb.ToString(); ;
        }

        public static byte[] md5(byte[] plainTextArr)
        {
            ;

            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(plainTextArr);

            return output;
        }

        public static bool CheckCRC16(byte[] data, int length)
        {
            if (length < 2) return false;
            CRC16 crcObj = new CRC16();
            int value = crcObj.CreateCRC16(data, Convert.ToUInt16(length - 2));

            byte[] bytes = BitConverter.GetBytes(value);
            return bytes[0] == data[(length - 2)] && bytes[1] == data[(length - 1)];
        }

        public static string GetConstr()
        {
            return System.Configuration.ConfigurationManager.AppSettings["mysqlConstr"].ToString();
        }

        public static string GetRedisConstr()
        {
            return System.Configuration.ConfigurationManager.AppSettings["redisConstr"].ToString();
        }

        public static string GetLocalServIp()
        {
            return System.Configuration.ConfigurationManager.AppSettings["servIp"].ToString();
        }

        public static string GetLocalServPort()
        {
            return System.Configuration.ConfigurationManager.AppSettings["servPort"].ToString();
        }


        /// <summary> 
        /// 将一个object对象序列化，返回一个byte[]         
        /// </summary> 
        /// <param name="obj">能序列化的对象</param>         
        /// <returns></returns> 
        public static byte[] ObjectToBytes(object obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter(); formatter.Serialize(ms, obj); return ms.GetBuffer();
            }
        }

        /// <summary>
        /// 计算字节数组中非零长度
        /// </summary>
        /// <param name="data"></param>
        /// <param name="begin"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static int GetStrLength(byte[] data, int begin, int count)
        {
            int zeroCount = 0;
            for (int i = begin + count - 1; i >= begin; i--)
            {
                if (data[i] == 0) zeroCount++;
                else break;
            }
            return count - zeroCount;
        }

        /// <summary> 
        /// 将一个序列化后的byte[]数组还原         
        /// </summary>
        /// <param name="Bytes"></param>         
        /// <returns></returns> 
        public static object BytesToObject(byte[] Bytes)
        {
            using (MemoryStream ms = new MemoryStream(Bytes))
            {
                IFormatter formatter = new BinaryFormatter(); return formatter.Deserialize(ms);
            }
        }

        /// <summary>
        /// 将16进制的字符串转回byte[]
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] StrToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        /// <summary>
        /// byte[]转字符串
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string HexByteToStr(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static bool ArrayEquals<T>(T[] a, T[] b)
        {
            if (a.Length != b.Length) return false;
            int i = 0;
            foreach (T obj in a)
            {
                if (obj.ToString() != b[i].ToString()) return false;
                i++;
            }
            return true;
        }


        /// <summary>
        /// 指定类型的端口是否已经被使用了
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="type">端口类型</param>
        /// <returns></returns>
        public static bool udpPortIsFree(int port)
        {
            bool flag = true;
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipendpoints = properties.GetActiveUdpListeners();

            foreach (IPEndPoint ipendpoint in ipendpoints)
            {
                if (ipendpoint.Port == port)
                {
                    flag = false;
                    break;
                }
            }
            ipendpoints = null;
            properties = null;
            return flag;
        }

        /// <summary>
        /// 随机字符串
        /// </summary>        
        /// <param name="length">返回随机的字符串个数</param>
        /// <param name="chars">随机字符串源</param>
        /// <returns></returns>
        public static string RadomStr(int length, string chars = "ABCDEFGHIJKLMNOPQRSTUWVXYZ0123456789abcdefghijklmnopqrstuvwxyz")
        {
            Random random = new Random();
            string strs = string.Empty;
            for (int i = 0; i < length; i++)
            {
                strs += chars[random.Next(chars.Length)];
            }
            return strs;
        }

    }
}