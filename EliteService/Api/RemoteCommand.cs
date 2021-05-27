using EliteService.Utility;
using System;

namespace EliteService.Api
{
    public class RemoteCommand
    {
        private byte[] data;

        private void AddHead(int len = 70)
        {
            data = new byte[len];
            byte[] bytes = BitConverter.GetBytes(len);

            data[0] = 0xdd;
            data[1] = 0xbb;
            data[2] = 0xaa;
            data[3] = 0x55;
            data[4] = bytes[0];
            data[5] = bytes[1];
        }

        private void AddCommand(byte action, byte param = 0x00)
        {
            data[6] = action;
            data[7] = param;
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
        /// 开始/停止监听命令
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="isBegin"></param>
        /// <returns></returns>
        public byte[] CreateMonitorCmd(int channel, int clientPort, bool isBegin)
        {
            byte action = (byte)(isBegin ? 0x88 : 0x89);
            byte param = (byte)(channel);

            AddHead();
            AddCommand(action, param);

            byte[] bytes = BitConverter.GetBytes(clientPort);
            data[56] = bytes[0];
            data[57] = bytes[1];

            DeviceCommand devCommand = new DeviceCommand();
            byte[] command = devCommand.CreateMonitorCmd(channel, isBegin);

            Array.Copy(command, 0, data, 58, 10);

            AddCRC();

            return data;
        }
    }
}
