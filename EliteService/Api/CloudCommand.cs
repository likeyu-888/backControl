using EliteService.Utility;
using System;

namespace EliteService.Api
{
    public class CloudCommand
    {
        private int source = 1;
        private byte[] data;
        private int deviceId;
        private int schoolId;

        public CloudCommand(int schoolId, int deviceId)
        {
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
            Array.Copy(GlobalData.RegPassword, 0, chkData, data.Length - 16, 16);
            byte[] authority = Helper.md5(chkData);

            Array.Copy(authority, 0, this.data, data.Length - 16, 16);
        }

        /// <summary>
        /// 查询设备参数
        /// </summary>
        /// <returns></returns>
        public byte[] CreateHeartBeatCmd()
        {
            byte action = 0x01;
            AddHead();
            AddCommand(action);
            AddCheckSum();

            return data;
        }
    }
}
