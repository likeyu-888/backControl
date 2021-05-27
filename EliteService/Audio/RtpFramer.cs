using EliteService.Utility;
using StreamCoders.Network;
using System;
using System.Net;

namespace EliteService.Audio
{
    public class RtpFramer
    {
        public readonly RTPSession session;

        public RTPSender sender;//发送者
        public RTPReceiver receiver;//接收者

        private RTPParticipant participant;
        private RTPParticipant senderParticipant;

        public string clientIp;

        public WaveSaver waveSaver;

        public RtpFramer(int key, int rtpPort)
        {

            try
            {
                session = new RTPSession();
                sender = new RTPSender();
                receiver = new RTPReceiver();

                var rtpEp = new IPEndPoint(IPAddress.Any, rtpPort);
                participant = new RTPParticipant(rtpEp);

                receiver.AddParticipant(participant);
                session.AddReceiver(receiver);

                receiver.AddRTPPacket = NewRTPPacket;

                waveSaver = new WaveSaver(key);
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("rtpFramer error:", ex.Message);
            }
        }

        /// <summary>
        /// 添加发送者。也就是接收音频者。
        /// </summary>
        /// <returns></returns>
        public bool AddSender(string clientIp)
        {
            if (!string.IsNullOrEmpty(this.clientIp))
            {
                if (this.clientIp.Equals(clientIp)) return true;
                else return false;
            }
            try
            {
                sender = new RTPSender();
                IPEndPoint senderEp = new IPEndPoint(IPAddress.Parse(clientIp), GlobalData.ClientAudiolPort);
                senderParticipant = new RTPParticipant(senderEp);
                sender.AddParticipant(senderParticipant);
                session.AddSender(sender);
            }
            catch
            {
                receiver.Dispose();
                sender.Dispose();
                session.Dispose();
                return false;
            }
            this.clientIp = clientIp;
            return true;
        }

        /// <summary>
        /// 移除发送者
        /// </summary>
        /// <param name="clientIp"></param>
        /// <returns></returns>
        public bool RemoveSender(string clientIp)
        {
            if (string.IsNullOrEmpty(this.clientIp)) return true;
            if (!this.clientIp.Equals(clientIp)) return true;
            session.RemoveSender(sender);
            sender.RemoveParticipant(senderParticipant);
            senderParticipant.Dispose();
            sender.Dispose();
            this.clientIp = "";
            return true;
        }

        /// <summary>
        /// 释放RTP连接
        /// </summary>
        public void Dispose()
        {
            LogHelper.GetInstance.Write("移除RTP通讯",session.ToString());//2020-10-19 lky
            RemoveSender(this.clientIp);
            session.Dispose();
            participant.Dispose();
            receiver.Dispose();
        }
        private delegate void delegNewRTPPacket(RTPPacket packet);
        /// <summary>
        /// 新包到达处理
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private void NewRTPPacket(RTPPacket packet)
        {
            try
            {
                LogHelper.GetInstance.Write("收到新包！", "");
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "收到新包！");
                if (this.sender != null)
                {
                    this.sender.Send(packet);
                }
                this.waveSaver.AddPacket(packet);
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("newRTPPacket error:", ex.Message);
            }
        }

        private void NewRTCPPacket(RTCPCompoundPacket packet)
        {
        }

        private void NewSSRC(uint ssrc)
        {
        }
    }

}