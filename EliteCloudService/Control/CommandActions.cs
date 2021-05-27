using EliteService.Api;
using EliteService.DTO;
using EliteService.Utility;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EliteService.Control
{
    public class CommandActions
    {
        private ClientCommand cliCommand;
        private byte[] command;
        private EndPoint ipEndPoint;
        private byte[] returns;
        private int deviceId;
        private int schoolId;
        private readonly object lockObj = new object();


        public CommandActions(byte[] regPassword, EndPoint ipEndPoint, int schoolId, int deviceId)
        {
            this.cliCommand = new ClientCommand(regPassword, schoolId, deviceId);
            this.ipEndPoint = ipEndPoint;
            this.schoolId = schoolId;
            this.deviceId = deviceId;
        }

        public CommandActions()
        {
        }

        /// <summary>
        /// 参数查询
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteHeartBeatCmd(EndPoint ipEndPoint)
        {
            lock (this.lockObj)
            {
                if (GlobalData.SchoolList.ContainsKey(schoolId))
                {
                    GlobalData.SchoolList[schoolId].endPoint = ipEndPoint;
                }
            }

            return new JsonMsg { code = 200, message = "操作成功", data = new byte[] { } };
        }



        /// <summary>
        /// 参数查询
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteQueryParamsCmd()
        {
            command = cliCommand.CreateQueryParamsCmd();
            JsonMsg msg = ExecuteCommand(command);

            if (msg.code != 200) return msg;
            return new JsonMsg { code = 200, message = "操作成功", data = msg.data };
        }

        /// <summary>
        /// 参数设置
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteUpdateParamsCmd(byte[] buff)
        {

            byte[] data = new byte[614];
            Array.Copy(buff, 8, data, 0, 614);
            command = cliCommand.CreateUpdateParamsCmd(data);

            JsonMsg msg = ExecuteCommand(command);

            if (msg.code != 200) return msg;
            return new JsonMsg { code = 200, message = "操作成功", data = msg.data };
        }


        /// <summary>
        /// 通用转发
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteCommonForwardCmd(byte[] buff)
        {
            command = cliCommand.CreateCloneCmd(buff);

            JsonMsg msg = ExecuteCommand(command);

            if (msg.code != 200) return msg;
            return new JsonMsg { code = 200, message = "操作成功", data = msg.data };
        }


        /// <summary>
        /// 查询下载
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteQueryRecordCmd(byte[] buff)
        {
            command = cliCommand.CreateCloneCmd(buff);

            JsonMsg msg = ExecuteCommand(command);

            if (msg.code != 200) return msg;
            return new JsonMsg { code = 200, message = "操作成功", data = msg.data };
        }


        /// <summary>
        /// 执行下载
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteDownRecordCmd(byte[] buff)
        {
            command = cliCommand.CreateCloneCmd(buff);
            JsonMsg msg = ExecuteCommand(command);

            if (msg.code != 200) return msg;
            return new JsonMsg { code = 200, message = "操作成功", data = msg.data };
        }

        /// <summary>
        /// 状态查询
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteStatusCmd()
        {
            command = cliCommand.CreateStatusCmd();

            JsonMsg msg = ExecuteCommand(command);
            if (msg.code != 200) return msg;
            return new JsonMsg { code = 200, message = "操作成功", data = msg.data };
        }

        /// <summary>
        /// 设备监听
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public JsonMsg ExecuteBeginMonitorCmd(int channel)
        {
            string key = "device_monitor_" + this.schoolId.ToString() + "_" + deviceId.ToString();

            if (RedisHelper.Exists(key))
            {
                return new JsonMsg { code = 500, message = "设备已处于监听状态，不可重复操作", data = new byte[] { } };
            }

            command = cliCommand.CreateMonitorCmd(channel, true);
            JsonMsg msg = ExecuteCommand(command);

            if (msg.code == 200)
            {
                RedisHelper.Set(key, "", 60 * 24);
            }
            return msg;
        }


        /// <summary>
        /// 设备监听
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public JsonMsg ExecuteStopMonitorCmd(int channel)
        {
            string key = "device_monitor_" + this.schoolId.ToString() + "_" + deviceId.ToString();
            if (!RedisHelper.Exists(key))
            {
                return new JsonMsg { code = 200, message = "操作成功", data = new byte[] { } };
            }

            command = cliCommand.CreateMonitorCmd(channel, false);
            JsonMsg msg = ExecuteCommand(command);

            if (msg.code == 200)
            {
                RedisHelper.Remove(key);
            }
            return msg;
        }

        /// <summary>
        /// 开始升级
        /// </summary>
        /// <param name="isArm"></param>
        /// <returns></returns>
        public JsonMsg ExecuteBeginUpgradeCmd(bool isArm)
        {
            string key = "device_upgrade_" + this.schoolId.ToString() + "_" + deviceId.ToString();

            if (RedisHelper.Exists(key))
            {
                return new JsonMsg { code = 500, message = "设备正在升级中，不可重复操作" };
            }
            command = cliCommand.CreateBeginUpgradeCmd(isArm);

            JsonMsg msg = ExecuteCommand(command);

            if (msg.code == 200)
            {
                RedisHelper.Set(key, "", 10);
            }
            return msg;
        }

        /// <summary>
        /// 新编升级
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public JsonMsg ExecuteUpgradeCmd(byte[] request)
        {
            command = cliCommand.CreateCloneCmd(request);
            JsonMsg msg = ExecuteCommand(command);

            if (msg.code != 200) return msg;
            return new JsonMsg { code = 200, message = "操作成功", data = msg.data };
        }

        /// <summary>
        /// 数据同步
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public JsonMsg ExecuteSyncDataCmd()
        {
            command = cliCommand.CreateSyncDataCmd();
            JsonMsg msg = ExecuteCommand(command);

            if (msg.code != 200) return msg;
            return new JsonMsg { code = 200, message = "操作成功", data = msg.data };
        }

        public JsonMsg CheckResult()
        {
            return CheckResultWithData(this.returns);
        }

        public JsonMsg CheckResultWithData(byte[] returns)
        {

            if ((returns.Length == 1) && (returns[0] == 100))
            {
                return new JsonMsg { code = 501, message = "服务器未启动或数据传输错误" };
            }
            if (!Helper.CheckCRC16(returns, Convert.ToUInt16(returns.Length)))
            {
                return new JsonMsg { code = 501, message = "服务器未启动或数据传输错误" };
            }

            if ((returns[0] != 0xff) || (returns[1] != 0xee)) return new JsonMsg { code = 500, message = "获取结果异常" };
            int len = BitConverter.ToUInt16(returns, 2);

            string result = Encoding.UTF8.GetString(returns, 4, len - 6);
            try
            {
                dynamic obj = JsonConvert.DeserializeObject(result);

                if (obj.code != 200) return new JsonMsg { code = 500, message = obj.message };
                string aaa = obj.data.ToString();
                byte[] bbb = Convert.FromBase64String(aaa);
                return new JsonMsg { code = 200, message = "操作成功", data = bbb };
            }
            catch (Exception ex)
            {
                return new JsonMsg { code = 500, message = "数据转换失败:" + ex.Message };
            }
        }

        public static JsonMsg CheckResult(byte[] data)
        {
            if ((data.Length == 1) && (data[0] == 100)) return new JsonMsg { code = 500, message = "设备未启动或数据传输有误" };
            if (!Helper.CheckCRC16(data, Convert.ToUInt16(data.Length)))
            {
                return new JsonMsg { code = 500, message = "数据传输有误" };
            }
            byte status = data[5];
            if (status == 0xee)
            {
                return new JsonMsg { code = 500, message = "操作失败", data = data };
            }
            else if (status == 0x92)
            {
                return new JsonMsg { code = 500, message = "测试中", data = data };
            }
            else if (status == 0xfd)
            {
                return new JsonMsg { code = 500, message = "故障检测中", data = data };
            }
            return new JsonMsg { code = 200, message = "操作成功", data = data };
        }

        /// <summary>
        /// 发送升级文件
        /// </summary>
        /// <param name="request"></param>
        /// <param name="isFromWeb"></param>
        /// <returns></returns>
        public JsonMsg ExecuteSendUpgradeFileCmd(byte[] request, bool isRequestFromCloud)
        {
            bool isArm = (request[5] == 0x6D);
            int serialId = BitConverter.ToUInt16(request, 6);
            int buffLen = request.Length - 8 - 19;
            if (!isRequestFromCloud) buffLen -= 16;
            byte[] buff = new byte[buffLen];
            Array.Copy(request, 8, buff, 0, buffLen);

            command = cliCommand.CreateSendUpgradeFileCmd(isArm, serialId, buff);

            JsonMsg msg = ExecuteCommand(command);
            return msg;
        }

        /// <summary>
        /// 结束升级
        /// </summary>
        /// <param name="isArm"></param>
        /// <returns></returns>
        public JsonMsg ExecuteFinishUpgradeCmd(bool isArm)
        {
            string key = "device_upgrade_" + this.deviceId.ToString();

            command = cliCommand.CreateFinishUpgradeCmd(isArm);
            JsonMsg msg = ExecuteCommand(command);

            if (msg.code == 200)
            {
                RedisHelper.Remove(key);
            }
            return msg;
        }

        /// <summary>
        /// 强制结束升级
        /// </summary>
        /// <param name="isArm"></param>
        /// <returns></returns>
        public JsonMsg ExecuteAbortUpgradeCmd(bool isArm)
        {
            command = cliCommand.CreateAbortUpgradeCmd(isArm);
            return ExecuteCommand(command);

        }



        /// <summary>
        /// 向设备执行命令
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private JsonMsg ExecuteCommand(byte[] command)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = 1000
            };
            JsonMsg msg;
            int i = 0;
            do
            {
                returns = UdpHelper.SendCommand(command, ipEndPoint, clientSocket);
                msg = CheckResult();
                if (msg.code == 200) break;
                else if (msg.code != 501)
                {
                    break;
                }
            }
            while (++i < 3);

            return msg;
        }

    }
}
