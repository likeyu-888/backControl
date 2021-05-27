using EliteService.Utility;
using System;

namespace EliteService.Api
{
    public class DeviceCommand
    {

        private byte[] data;

        private void AddHead(int len = 10)
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

        private void AddCRC()
        {

            CRC16 crcCheck = new CRC16();
            int value = crcCheck.CreateCRC16(data, Convert.ToUInt16(data.Length - 2));
            byte[] bytes = BitConverter.GetBytes(value);
            data[data.Length - 2] = bytes[0];
            data[data.Length - 1] = bytes[1];
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
            AddCRC();

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
            AddHead(624);
            AddCommand(action);
            Array.Copy(buff, 0, data, 8, 614);
            AddCRC();

            return data;
        }

        /// <summary>
        /// 查询设备状态
        /// </summary>
        /// <returns></returns>
        public byte[] CreateHeartBeatCmd()
        {
            byte action = 0x01;
            AddHead();
            AddCommand(action);
            AddCRC();

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
            AddCRC();

            return data;
        }

        /// <summary>
        /// 开始/停止监听命令
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="isBegin"></param>
        /// <returns></returns>
        public byte[] CreateMonitorCmd(int channel, bool isBegin)
        {
            byte action = (byte)(isBegin ? 0x99 : 0x9a);
            byte param = (byte)(channel);

            AddHead();
            AddCommand(action, param);
            AddCRC();

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
            AddCRC();

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

            AddHead(bytes.Length + 10);
            AddCommand(action, param);

            byte[] tempBytes = BitConverter.GetBytes(serialId);
            data[6] = tempBytes[0];
            data[7] = tempBytes[1];
            Array.Copy(bytes, 0, data, 8, bytes.Length);
            this.AddCRC();

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
            this.AddCRC();

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
            this.AddCRC();

            return data;
        }
    }
}
