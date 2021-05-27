using System;
using System.Text.RegularExpressions;

namespace Elite.WebServer.Utility
{

    public class DataValidate
    {
        /// <summary>
        /// 验证正整数
        /// </summary>      
        public static bool IsInteger(string txt)
        {
            Regex objReg = new Regex(@"^0|([1-9]\d*)$");
            return objReg.IsMatch(txt);
        }
        /// <summary>
        /// 验证是否Email
        /// </summary>     
        public static bool IsEmail(string txt)
        {
            Regex objReg = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");
            return objReg.IsMatch(txt);
        }
        /// <summary>
        /// 验证身份证
        /// </summary>        
        public static bool IsIdentityCard(string txt)
        {
            Regex objReg = new Regex(@"^(\d{15}$|^\d{18}$|^\d{17}(\d|X|x))$");
            return objReg.IsMatch(txt);
        }
        /// <summary>
        /// 验证是否Email
        /// </summary>     
        public static bool IsIp(string txt)
        {
            Regex objReg = new Regex(@"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
            return objReg.IsMatch(txt);
        }

        public static bool IsDate(string strDate)
        {
            try
            {
                DateTime.Parse(strDate);  //不是字符串时会出现异常
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}