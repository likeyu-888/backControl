using EliteService.DTO;
using EliteService.Utility;
using Newtonsoft.Json;
using System;
using System.Net;

namespace EliteService.Control
{
    public class DealRequest
    {
        private bool mRequestFromCloud = false;

        public byte[] Processing(byte[] request)
        {
            JsonMsg resultJson;

            bool isArm;
            System.Diagnostics.Debug.WriteLine("begin");
            LogHelper.GetInstance.Write("begin", "begin");
            byte commandId = request[4];
            if (!CheckData(request))
            {
                LogHelper.GetInstance.Write("数据有效性校验失败:"+request.Length.ToString(), request);//lky
                return ReturnMsg.GetReturn(new JsonMsg { code = 501, message = "数据有效性校验失败" });
            }
            if (commandId == 0xb4)
            {
                GlobalData.Initial();
                return ReturnMsg.GetReturn(new JsonMsg { code = 200, message = "操作成功" });
            }
            else if (commandId == 0xb6)//数据同步
            {
                CommandActions actions2 = new CommandActions();
                resultJson = actions2.ExecuteSyncDataCmd();
                return ReturnMsg.GetReturn(new JsonMsg { code = 200, message = "操作成功" });
            }

            System.Diagnostics.Debug.WriteLine("pass");
            LogHelper.GetInstance.Write("pass", "pass");


            int deviceId = BitConverter.ToUInt16(request, request.Length - 19);
            string deviceIp = GetDeviceIp(deviceId);

            if (string.IsNullOrEmpty(deviceIp)) return ReturnMsg.GetReturn(new JsonMsg { code = 500, message = "设备不在线" });
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(deviceIp), GlobalData.DeviceControlPort);

            CommandActions actions = new CommandActions(ipEndPoint, deviceId);

            switch (commandId)
            {
                case 0x02: //查看设备参数
                    resultJson = actions.ExecuteQueryParamsCmd();
                    break;
                case 0x80: //设置设备参数
                    resultJson = actions.ExecuteUpdateParamsCmd(request);
                    break;
                case 0x03: //查看设备状态
                    resultJson = actions.ExecuteStatusCmd();
                    break;
                case 0x99: //开始监听
                    resultJson = actions.ExecuteBeginMonitorCmd(request);
                    break;
                case 0x9a: //结束监听
                    resultJson = actions.ExecuteStopMonitorCmd(request);
                    break;
                case 0x10: //开始升级
                    isArm = (request[5] == 0x6D);
                    resultJson = actions.ExecuteBeginUpgradeCmd(isArm);
                    break;
                case 0x11: //发送升级包
                    resultJson = actions.ExecuteSendUpgradeFileCmd(request, mRequestFromCloud);
                    break;
                case 0x12: //升级完成
                    isArm = (request[5] == 0x6D);
                    resultJson = actions.ExecuteFinishUpgradeCmd(isArm);
                    break;
                case 0x14: //强制中断升级
                    isArm = (request[5] == 0x6D);
                    resultJson = actions.ExecuteAbortUpgradeCmd(isArm);
                    break;
                case 0xa1: //通用转发
                    resultJson = actions.ExecuteCommonForwardCmd(request);
                    break;
                case 0xb1: //查询下载
                    resultJson = actions.ExecuteQueryRecordCmd(request);
                    break;
                case 0xb2: //下载文件
                    resultJson = actions.ExecuteDownRecordCmd(request);
                    break;
                case 0xb3: //提交升级
                    resultJson = actions.ExecuteUpgradeCmd(request);
                    break;
                case 0xb5: //录音
                    resultJson = actions.ExecuteRecordCmd(request[5] == 1);
                    break;
                case 0xb7: //删除服务器
                    resultJson = actions.ExecuteDeleteCmd();
                    break;
                default:
                    resultJson = new JsonMsg { code = 404, message = "服务器暂不支持该指令" };
                    break;
            }

            return ReturnMsg.GetReturn(resultJson);
        }

        /// <summary>
        /// 验证客户端发来数据的md5值
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool CheckData(byte[] data)
        {
            if (data.Length < 20)
            {
               // LogHelper.GetInstance.Write("ss1", "1");
                return false;
            }
            int requestFrom = data[data.Length - 17];

            this.mRequestFromCloud = (requestFrom == 1);

            if (GlobalData.RegPassword.Length < 16)
            {
                //LogHelper.GetInstance.Write("RegPassword.length：", GlobalData.RegPassword.Length.ToString());
                return false;
            }
            if ((requestFrom != 1) && (data.Length < 36))
            {
               // LogHelper.GetInstance.Write("ss3", "3");
                return false;
            }

            byte[] fromMd5 = new byte[16];
            Array.Copy(data, data.Length - 16, fromMd5, 0, 16);

            byte[] source = new byte[data.Length];
            Array.Copy(data, 0, source, 0, data.Length - 16);
            if (requestFrom == 1) Array.Copy(GlobalData.RegPassword, 0, source, source.Length - 16, 16);
            else
            {
                byte[] tokenBytes = new byte[16];
                Array.Copy(data, data.Length - 35, tokenBytes, 0, 16);

                if (!CheckAuthority(tokenBytes)) return false;

                Array.Copy(tokenBytes, 0, source, source.Length - 16, 16);
            }

            return Helper.ArrayEquals(Helper.md5(source), fromMd5);
        }

        /// <summary>
        /// 是否有访问权限
        /// </summary>
        /// <param name="tokenBytes"></param>
        /// <returns></returns>
        private bool CheckAuthority(byte[] tokenBytes)
        {

            string token = Helper.HexByteToStr(tokenBytes);
            try
            {
                object obj = RedisHelper.Get(token);
                if (obj == null) return false;
                UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(obj.ToString());
                if (userInfo == null) return false;
            }
            catch (Exception)
            {

            }

            return true;
        }

        private string GetDeviceIp(int deviceId)
        {
            if (!GlobalData.DeviceList.ContainsKey(deviceId)) return "";
            string ip = GlobalData.DeviceList[deviceId].Ip;
            if (!DataValidate.IsIp(ip)) return "";
            int status = GlobalData.DeviceList[deviceId].Status;

            if ((status != 0) && (status != 1) && (status != 187) && (status != 188)) return "";
            return GlobalData.DeviceList[deviceId].Ip;
        }
    }
}