using EliteService.Utility;
using System;
using System.Text;

namespace EliteService.Api
{
    public class ClientCommand
    {
        private int source = 1;
        private byte[] data;
        private byte[] regPassword;
        private int deviceId;
        private int schoolId;

        public ClientCommand(byte[] regPassword, int schoolId, int deviceId)
        {
            this.regPassword = regPassword;
            this.schoolId = schoolId;
            this.deviceId = deviceId;
        }

        private void AddHead(int len = 45)
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


            byte[] bytes = BitConverter.GetBytes(this.deviceId);
            data[len - 19] = bytes[0];
            data[len - 18] = bytes[1];
            data[len - 17] = (byte)(this.source);

            bytes = BitConverter.GetBytes(this.schoolId);
            data[len - 37] = bytes[0];
            data[len - 36] = bytes[1];

            byte[] chkData = new byte[data.Length];
            Array.Copy(this.data, 0, chkData, 0, data.Length - 16);
            Array.Copy(this.regPassword, 0, chkData, data.Length - 16, 16);
            byte[] authority = Helper.md5(chkData);


            Array.Copy(authority, 0, this.data, data.Length - 16, 16);
        }

        /// <summary>
        /// 远程下载录音文件
        /// </summary>
        /// <returns></returns>
        public byte[] CreateDownRecordCmd(int recordId)
        {
            byte action = 0xb2;
            AddHead(50);
            AddCommand(action);

            byte[] couple = BitConverter.GetBytes((ushort)recordId);
            data[7] = couple[0];
            data[8] = couple[1];
            data[9] = couple[2];
            data[10] = couple[3];

            AddCheckSum();

            return data;
        }

        /// <summary>
        /// 查询设备参数
        /// </summary>
        /// <returns></returns>
        public byte[] CreateCloneCmd(byte[] buff)
        {
            data = new byte[buff.Length];
            Array.Copy(buff, 0, data, 0, buff.Length);
            AddCheckSum();

            return data;
        }


        /// <summary>
        /// 数据同步
        /// </summary>
        /// <returns></returns>
        public byte[] CreateSyncDataCmd()
        {
            byte action = 0xb6;
            AddHead(50);
            AddCommand(action);

            AddCheckSum();

            return data;
        }

        /// <summary>
        /// 查询设备参数
        /// </summary>
        /// <returns></returns>
        public byte[] CreateQueryRecordCmd(int id, string create_time, int page, int page_size, string sort_column, string sort_direction)
        {
            byte action = 0xb1;
            AddHead(119);
            AddCommand(action);

            byte[] couple = BitConverter.GetBytes((ushort)id);
            data[11] = couple[0];
            data[12] = couple[1];

            byte[] bytes = Encoding.UTF8.GetBytes(create_time);
            Array.Copy(bytes, 0, data, 13, bytes.Length);

            couple = BitConverter.GetBytes((ushort)page);
            data[38] = couple[0];
            data[39] = couple[1];

            couple = BitConverter.GetBytes((ushort)page_size);
            data[40] = couple[0];
            data[41] = couple[1];

            bytes = Encoding.UTF8.GetBytes(sort_column);
            Array.Copy(bytes, 0, data, 42, bytes.Length);

            bytes = Encoding.UTF8.GetBytes(sort_direction);
            Array.Copy(bytes, 0, data, 62, bytes.Length);


            AddCheckSum();

            return data;
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
        public byte[] CreateMonitorCmd(int channel, bool isBegin)
        {
            byte action = (byte)(isBegin ? 0x99 : 0x9a);
            byte param = (byte)(channel);

            AddHead();
            AddCommand(action, param);
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
        /// 开始升级命令，新编，服务端根据redisKey拉取文件进行升级
        /// </summary>
        /// <param name="isArm">版本，ARM或DSP</param>
        /// <param name="redisKey">唯一标志</param>
        /// <returns></returns>
        public byte[] CreateUpgradeCmd(bool isArm, int logId, string redisKey)
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

            byte[] key = Encoding.UTF8.GetBytes(redisKey);
            Array.Copy(key, 0, data, 10, key.Length);

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
