using System;
using System.Security.Cryptography;
using System.Text;

namespace Elite.WebServer.Utility
{
    public class Helper
    {
        public static string md5(string plainText)
        {

            byte[] plainTextArr = Encoding.UTF8.GetBytes(plainText);

            MD5 md5 = new MD5CryptoServiceProvider();
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
        public static bool CheckCRC16(byte[] data, ushort length)
        {
            if (length < 2) return false;
            CRC16 crcObj = new CRC16();
            int value = crcObj.CreateCRC16(data, Convert.ToUInt16((length - 2)));

            byte[] bytes = BitConverter.GetBytes(value);
            return bytes[0] == data[(length - 2)] && bytes[1] == data[(length - 1)];
        }

        public static string GetLocalServIp()
        {
            return System.Configuration.ConfigurationManager.AppSettings["servIp"].ToString();
        }

        public static int GetLocalServPort()
        {
            return Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["servPort"]);
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

        ///编码
        public static string Base64Encode(string code_type, string code)
        {
            string encode = "";
            byte[] bytes = Encoding.GetEncoding(code_type).GetBytes(code);
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = code;
            }
            return encode;
        }
        ///解码
        public static string Base64Decode(string code_type, string code)
        {
            string decode = "";
            byte[] bytes = Convert.FromBase64String(code);
            try
            {
                decode = Encoding.GetEncoding(code_type).GetString(bytes);
            }
            catch
            {
                decode = code;
            }
            return decode;
        }
    }
}