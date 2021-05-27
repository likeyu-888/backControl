using EliteService.DTO;
using EliteService.Utility;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;

namespace EliteService.Control
{
    public class DealRequest
    {
        private bool mRequestFromCloud = false;

        public void Processing(Socket mServerSocket, byte[] request, EndPoint endpoint)
        {
            JsonMsg resultJson;

            bool isArm;

            int schoolId = BitConverter.ToUInt16(request, request.Length - 37);
            int deviceId = BitConverter.ToUInt16(request, request.Length - 19);

            byte commandId = request[4];
            if (!CheckData(request, schoolId))
            {
                byte[] res = ReturnMsg.GetReturn(new JsonMsg { code = 501, message = "数据有效性校验失败" });
                if (commandId != 0x01)
                {
                    mServerSocket.SendTo(res, res.Length, SocketFlags.None, endpoint);
                }
                if (GlobalData.IsDebug)
                {
                    LogHelper.GetInstance.Write("云平台服务执行 error:" + endpoint.ToString(), "数据有效性校验失败");
                }
                return;
            }

            EndPoint ipEndPoint = GlobalData.SchoolList[schoolId].endPoint;

            CommandActions actions = new CommandActions(GlobalData.SchoolList[schoolId].password, ipEndPoint, schoolId, deviceId);

            switch (commandId)
            {
                case 0x01: //心跳
                    actions.ExecuteHeartBeatCmd(endpoint);
                    return;
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
                    resultJson = actions.ExecuteBeginMonitorCmd(request[5]);
                    break;
                case 0x9a: //结束监听
                    resultJson = actions.ExecuteStopMonitorCmd(request[5]);
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
                case 0xb2: //执行下载
                    resultJson = actions.ExecuteDownRecordCmd(request);
                    break;
                case 0xb3: //提交升级
                    resultJson = actions.ExecuteUpgradeCmd(request);
                    break;
                case 0xb6: //数据同步
                    resultJson = actions.ExecuteSyncDataCmd();
                    break;
                default:
                    resultJson = new JsonMsg { code = 404, message = "服务器暂不支持该指令" };
                    break;
            }

            byte[] result = ReturnMsg.GetReturn(resultJson);
            try
            {
                if (GlobalData.IsDebug)
                {
                    LogHelper.GetInstance.Write("云平台服务 send to:" + endpoint.ToString(), resultJson.message + ",length=" + result.Length.ToString());
                }

                mServerSocket.SendTo(result, result.Length, SocketFlags.None, endpoint);
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("云平台服务 send to error:" + endpoint.ToString(), ex.Message);
            }
        }

        /// <summary>
        /// 验证客户端发来数据的md5值
        /// </summary>
        /// <param name="data"></param>
        /// <param name="schoolId"></param>
        /// <returns></returns>
        private bool CheckData(byte[] data, int schoolId = 0)
        {

            if (data.Length < 20) return false;
            int requestFrom = data[data.Length - 17];

            this.mRequestFromCloud = (requestFrom == 1);

            if ((requestFrom != 1) && (data.Length < 36)) return false;

            byte[] fromMd5 = new byte[16];
            Array.Copy(data, data.Length - 16, fromMd5, 0, 16);

            byte[] source = new byte[data.Length];
            Array.Copy(data, 0, source, 0, data.Length - 16);

            if (requestFrom == 1)
            {
                if (GlobalData.SchoolList.ContainsKey(schoolId))
                {
                    Array.Copy(GlobalData.SchoolList[schoolId].password, 0, source, source.Length - 16, 16);
                }
            }
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
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("CheckAuthority error", ex.Message);
            }

            return true;
        }
    }
}