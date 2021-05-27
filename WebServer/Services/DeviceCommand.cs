using Elite.WebServer.Utility;
using System;
using System.Text;

namespace Elite.WebServer.Services
{
    public class DeviceCommand
    {
        private int source = 0;
        private byte[] data;
        private byte[] token;
        private int deviceId;

        public DeviceCommand(byte[] token, int deviceId)
        {
            this.token = token;
            this.deviceId = deviceId;
        }

        private void AddHead(int len = 43)
        {
            data = new byte[len];
            byte[] bytes = BitConverter.GetBytes(len);
            data[0] = 0xf0;
            data[1] = 0xaa;
            data[2] = bytes[0];
            data[3] = bytes[1];

        }

        private void AddCommand(byte action, byte param = 0x00)
        {
            data[4] = action;
            data[5] = param;
        }

        private void AddCheckSum()
        {

            int len = data.Length;

            Array.Copy(this.token, 0, data, len - 35, 16);

            byte[] bytes = BitConverter.GetBytes(this.deviceId);
            data[len - 19] = bytes[0];
            data[len - 18] = bytes[1];
            data[len - 17] = (byte)(this.source);

            byte[] chkData = new byte[data.Length];
            Array.Copy(this.data, 0, chkData, 0, data.Length - 16);
            Array.Copy(this.token, 0, chkData, data.Length - 16, 16);
            byte[] authority = Helper.md5(chkData);


            Array.Copy(authority, 0, this.data, data.Length - 16, 16);
        }



        /// <summary>
        /// 查询设备参数
        /// </summary>
        /// <returns></returns>
        public byte[] CreateQueryParamsCmd()
        {
            byte action = 0x02;
            AddHead();
            AddCommand(action);
            AddCheckSum();

            return data;
        }

        /// <summary>
        /// 修改设备参数
        /// </summary>
        /// <param name="buff">参数主体</param>
        /// <returns></returns>
        public byte[] CreateUpdateParamsCmd(byte[] buff)
        {
            byte action = 0x80;
            AddHead(657);
            AddCommand(action);
            Array.Copy(buff, 0, data, 8, buff.Length);
            AddCheckSum();

            return data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] CreateUpdateDeviceListCmd()
        {
            byte action = 0xb4;
            AddHead();
            AddCommand(action);
            AddCheckSum();

            return data;
        }


        /// <summary>
        /// 查询设备状态
        /// </summary>
        /// <returns></returns>
        public byte[] CreateStatusCmd()
        {
            byte action = 0x03;
            AddHead();
            AddCommand(action);
            AddCheckSum();

            return data;
        }


        /// <summary>
        /// 通用转发
        /// </summary>
        /// <returns></returns>
        public byte[] CreateCommonForwardCmd(byte[] buff)
        {
            byte action = 0xa1;
            AddHead(buff.Length + 43);
            AddCommand(action);
            Array.Copy(buff, 0, data, 8, buff.Length);
            AddCheckSum();

            return data;
        }

        /// <summary>
        /// 开始/停止监听命令
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="isBegin"></param>
        /// <returns></returns>
        public byte[] CreateMonitorCmd(int channel, bool isBegin, bool isAutoRecord, string ip)
        {
            byte action = (byte)(isBegin ? 0x99 : 0x9a);
            byte param = (byte)(channel);

            AddHead(50);
            AddCommand(action, param);
            data[6] = (byte)(isAutoRecord ? 1 : 0);

            string[] strs = ip.Split('.');
            data[7] = (byte)Convert.ToUInt16(strs[0]);
            data[8] = (byte)Convert.ToUInt16(strs[1]);
            data[9] = (byte)Convert.ToUInt16(strs[2]);
            data[10] = (byte)Convert.ToUInt16(strs[3]);

            AddCheckSum();

            return data;
        }

        /// <summary>
        /// 开始升级命令，新编，服务端根据redisKey拉取文件进行升级
        /// </summary>
        /// <param name="isArm">版本，ARM或DSP</param>
        /// <param name="fileName">唯一标志</param>
        /// <returns></returns>
        public byte[] CreateUpgradeCmd(bool isArm, int logId, string fileName)
        {
            byte action = 0xb3;
            byte param = (byte)(isArm ? 0x6D : 0x73);

            AddHead(80);
            AddCommand(action, param);

            byte[] couple = BitConverter.GetBytes(logId);
            data[6] = couple[0];
            data[7] = couple[1];
            data[8] = couple[2];
            data[9] = couple[3];

            byte[] key = Encoding.UTF8.GetBytes(fileName);
            Array.Copy(key, 0, data, 10, key.Length);

            AddCheckSum();

            return data;
        }


        /// <summary>
        /// 开始/停止录音命令
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="isBegin"></param>
        /// <returns></returns>
        public byte[] CreateRecordCmd(bool isBegin)
        {
            byte action = 0xb5;
            byte param = (byte)(isBegin ? 1 : 0);

            AddHead(50);
            AddCommand(action, param);

            AddCheckSum();

            return data;
        }


        /// <summary>
        /// 删除服务器，停止监听
        /// </summary>
        /// <returns></returns>
        public byte[] CreateDeleteCmd()
        {
            byte action = 0xb7;

            AddHead(50);
            AddCommand(action);

            AddCheckSum();

            return data;
        }

        /// <summary>
        /// 开始升级命令
        /// </summary>
        /// <param name="isArm">版本，ARM或DSP</param>
        /// <returns></returns>
        public byte[] CreateBeginUpgradeCmd(bool isArm)
        {
            byte action = 0x10;
            byte param = (byte)(isArm ? 0x6D : 0x73);

            AddHead();
            AddCommand(action, param);
            AddCheckSum();

            return data;
        }

        /// <summary>
        /// 发送升级文件命令
        /// </summary>
        /// <param name="isArm">版本，ARM或DSP</param>
        /// <param name="serialId">序号Id</param>
        /// <param name="bytes">文件内容</param>
        /// <returns></returns>
        public byte[] CreateSendUpgradeFileCmd(bool isArm, long serialId, byte[] bytes)
        {
            byte action = 0x11;
            byte param = (byte)(isArm ? 0x6D : 0x73);

            AddHead(bytes.Length + 43);
            AddCommand(action, param);

            byte[] tempBytes = BitConverter.GetBytes(serialId);
            data[6] = tempBytes[0];
            data[7] = tempBytes[1];
            Array.Copy(bytes, 0, data, 8, bytes.Length);
            this.AddCheckSum();

            return data;
        }

        /// <summary>
        /// 结束升级命令
        /// </summary>
        /// <param name="isArm">版本，ARM或DSP</param>
        /// <returns></returns>
        public byte[] CreateFinishUpgradeCmd(bool isArm)
        {
            byte action = 0x12;
            byte param = (byte)(isArm ? 0x6D : 0x73);

            this.AddHead();
            this.AddCommand(action, param);
            this.AddCheckSum();

            return data;
        }

        /// <summary>
        /// 强制停止升级命令
        /// </summary>
        /// <param name="isArm">版本，ARM或DSP</param>
        /// <returns></returns>
        public byte[] CreateAbortUpgradeCmd(bool isArm)
        {
            byte action = 0x14;
            byte param = (byte)(isArm ? 0x6D : 0x73);

            this.AddHead();
            this.AddCommand(action, param);
            this.AddCheckSum();

            return data;
        }
    }
}
