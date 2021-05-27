using Elite.WebServer.Services;
using MySql.Data.MySqlClient;
using StreamCoders.Network;
using System;
using System.Collections.Generic;
using System.IO;

namespace EliteService.Utility
{
    public class WaveSaver
    {

        private Queue<RTPPacket> queue = new Queue<RTPPacket>(3200);

        private long packetCount = 0;
        private long writePackets = 3000; //每秒50包，60秒3000包

        private string fullFileName;
        private string virtualFileName;

        private BinaryWriter writer;
        private FileStream fileStream;

        private int fileLength = 0;

        private object lockObj = new object();
        private int key = 0;

        public WaveSaver(int key)
        {
            this.key = key;
        }
        /// <summary>
        /// 添加音频数据
        /// </summary>
        /// <param name="packet"></param>
        public void AddPacket(RTPPacket packet)
        {
            if (GlobalData.SaveWaveInterval <= 0) return;
            if (!GlobalData.DeviceList.ContainsKey(key)) return;
            if (GlobalData.DeviceList[key].IsAutoRecord != 1) return;

            lock (this.lockObj)
            {
                queue.Enqueue(packet);
                packetCount++;
            }
            //每一分钟写一次数据
            if (packetCount >= writePackets)
            {
                WriteToFile();
                packetCount = 0;
            }
        }

        /// <summary>
        /// 写文件处理
        /// </summary>
        private void WriteToFile()
        {

            try
            {
                AppendToFile();


                if (fileLength >= (GlobalData.SaveWaveInterval * 50 * 1280))
                {
                    int length = fileLength;
                    string path = virtualFileName;
                    FinishFile();
                    WriteToDb(length, path);
                    CreateNewFile();
                }
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("record error:", ex.Message);
            }
        }

        /// <summary>
        /// 取得wave文件头
        /// </summary>
        /// <param name="sampleRate">采样率，如44100</param>
        /// <param name="channels">通道数，如立体声为2</param>
        /// <param name="bitsPerSample">采样精度，即每个采样所占数据位数，如16，表示每个采样16bit数据，即2个字节</param>
        /// <param name="bytePerSecond">音频数据传送速率, 单位是字节。其值为采样率×每次采样大小。播放软件利用此值可以估计缓冲区的大小。bytePerSecond = sampleRate * (bitsPerSample / 8) * channels</param>
        /// <param name="fileLenIncludeHeader">wav文件总数据大小，包括44字节wave文件头大小</param>
        /// <returns>wavHeader</returns>
        public byte[] GetWaveFileHeader(int sampleRate, int channels, int bitsPerSample,
                                         int bytePerSecond, long fileLenIncludeHeader)
        {
            byte[] wavHeader = new byte[44];
            long totalDataLen = fileLenIncludeHeader - 8;
            long audioDataLen = totalDataLen - 36;

            //ckid：4字节 RIFF 标志，大写
            wavHeader[0] = (byte)'R';
            wavHeader[1] = (byte)'I';
            wavHeader[2] = (byte)'F';
            wavHeader[3] = (byte)'F';

            //cksize：4字节文件长度，这个长度不包括"RIFF"标志(4字节)和文件长度本身所占字节(4字节),即该长度等于整个文件长度 - 8
            wavHeader[4] = (byte)(totalDataLen & 0xff);
            wavHeader[5] = (byte)((totalDataLen >> 8) & 0xff);
            wavHeader[6] = (byte)((totalDataLen >> 16) & 0xff);
            wavHeader[7] = (byte)((totalDataLen >> 24) & 0xff);

            //fcc type：4字节 "WAVE" 类型块标识, 大写
            wavHeader[8] = (byte)'W';
            wavHeader[9] = (byte)'A';
            wavHeader[10] = (byte)'V';
            wavHeader[11] = (byte)'E';

            //ckid：4字节 表示"fmt" chunk的开始,此块中包括文件内部格式信息，小写, 最后一个字符是空格
            wavHeader[12] = (byte)'f';
            wavHeader[13] = (byte)'m';
            wavHeader[14] = (byte)'t';
            wavHeader[15] = (byte)' ';

            //cksize：4字节，文件内部格式信息数据的大小，过滤字节（一般为00000010H）
            wavHeader[16] = 0x10;
            wavHeader[17] = 0;
            wavHeader[18] = 0;
            wavHeader[19] = 0;

            //FormatTag：2字节，音频数据的编码方式，1：表示是PCM 编码
            wavHeader[20] = 1;
            wavHeader[21] = 0;

            //Channels：2字节，声道数，单声道为1，双声道为2
            wavHeader[22] = (byte)channels;
            wavHeader[23] = 0;

            //SamplesPerSec：4字节，采样率，如44100
            wavHeader[24] = (byte)(sampleRate & 0xff);
            wavHeader[25] = (byte)((sampleRate >> 8) & 0xff);
            wavHeader[26] = (byte)((sampleRate >> 16) & 0xff);
            wavHeader[27] = (byte)((sampleRate >> 24) & 0xff);

            //BytesPerSec：4字节，音频数据传送速率, 单位是字节。其值为采样率×每次采样大小。播放软件利用此值可以估计缓冲区的大小；
            //bytePerSecond = sampleRate * (bitsPerSample / 8) * channels
            wavHeader[28] = (byte)(bytePerSecond & 0xff);
            wavHeader[29] = (byte)((bytePerSecond >> 8) & 0xff);
            wavHeader[30] = (byte)((bytePerSecond >> 16) & 0xff);
            wavHeader[31] = (byte)((bytePerSecond >> 24) & 0xff);

            //BlockAlign：2字节，每次采样的大小 = 采样精度*声道数/8(单位是字节); 这也是字节对齐的最小单位, 譬如 16bit 立体声在这里的值是 4 字节。
            //播放软件需要一次处理多个该值大小的字节数据，以便将其值用于缓冲区的调整
            wavHeader[32] = (byte)(bitsPerSample * channels / 8);
            wavHeader[33] = 0;

            //BitsPerSample：2字节，每个声道的采样精度; 譬如 16bit 在这里的值就是16。如果有多个声道，则每个声道的采样精度大小都一样的；
            wavHeader[34] = (byte)bitsPerSample;
            wavHeader[35] = 0;

            //ckid：4字节，数据标志符（data），表示 "data" chunk的开始。此块中包含音频数据，小写；
            wavHeader[36] = (byte)'d';
            wavHeader[37] = (byte)'a';
            wavHeader[38] = (byte)'t';
            wavHeader[39] = (byte)'a';

            //cksize：音频数据的长度，4字节，audioDataLen = totalDataLen - 36 = fileLenIncludeHeader - 44
            wavHeader[40] = (byte)(audioDataLen & 0xff);
            wavHeader[41] = (byte)((audioDataLen >> 8) & 0xff);
            wavHeader[42] = (byte)((audioDataLen >> 16) & 0xff);
            wavHeader[43] = (byte)((audioDataLen >> 24) & 0xff);
            return wavHeader;
        }


        /// <summary>
        /// 写入数据库
        /// </summary>
        /// <param name="fileLength"></param>
        /// <param name="virtualPath"></param>
        private void WriteToDb(int fileLength, string virtualPath)
        {
            using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
            {
                conn.Open();
                string query = "insert into dev_record " +
                    " set device_id=" + this.key.ToString() + "," +
                    "size=" + Math.Ceiling((decimal)fileLength / 1024).ToString() + "," +
                    "file_path='" + virtualPath + "'";

                MySqlHelper.ExecuteNonQuery(conn, query);
                conn.Close();
            }

        }

        /// <summary>
        /// 创建新文件
        /// </summary>
        private void CreateNewFile()
        {
            string schoolId = SyncActions.GetSchoolId().ToString();
            string fileName = key.ToString() + "-" + DateTime.Now.ToString("MMdd") + "-" + DateTime.Now.ToString("HHmmss");

            string root = AppDomain.CurrentDomain.BaseDirectory + "\\records";
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);

            root = root + "\\" + schoolId;
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);

            root = root + "\\" + this.key.ToString();
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);

            root = root + "\\" + DateTime.Now.ToString("yyyyMM");
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);

            fullFileName = root + "\\" + fileName + ".wav";
            virtualFileName = "/records/" + schoolId + "/" + this.key.ToString() + "/" + DateTime.Now.ToString("yyyyMM") + "/" + fileName + ".wav";

            byte[] waveFileHeader = this.GetWaveFileHeader(32000, 1, 16, 64000, 44);

            if (GlobalData.IsDebug)
            {
                LogHelper.GetInstance.Write("record file=", virtualFileName);
            }

            fileStream = new FileStream(fullFileName, FileMode.Create, FileAccess.Write);
            writer = new BinaryWriter(fileStream);
            writer.Write(waveFileHeader);
        }

        /// <summary>
        /// 完成一个文件
        /// </summary>
        private void FinishFile()
        {
            try
            {
                writer.Seek(4, SeekOrigin.Begin);
                writer.Write(fileLength + 36);
                writer.Seek(40, SeekOrigin.Begin);
                writer.Write(fileLength);

                fileLength = 0;

                writer.Close();
                fileStream.Close();
                writer = null;
                fileStream = null;
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("finish error:", ex.Message);
            }
        }

        /// <summary>
        /// 追加内容
        /// </summary>
        private void AppendToFile()
        {
            try
            {
                if (fileStream == null) CreateNewFile();

                byte[] data = new byte[1280 * writePackets];
                int current = 0;

                lock (this.lockObj)
                {
                    for (int i = 0; i < writePackets; i++)
                    {
                        RTPPacket packet = queue.Dequeue();
                        packet.DataPointer.CopyTo(data, current);

                        current += (int)packet.DataSize;
                    }
                }
                writer.Write(data, 0, current);

                fileLength += current;
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("appendToFile error:", ex.Message);
            }
        }
    }
}
