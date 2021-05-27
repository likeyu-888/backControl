using StreamCoders.Network;
using System.Net;

namespace EliteService.Audio
{
    class AudioServer
    {
        RTPSession _rtpSession = null;
        RTPReceiver _rtpRecv = null;
        RTPSender _rtpSend = null;

        /// <summary>
        /// 接收RTP信息后发送
        /// </summary>
        /// <param name="rtpPack">RTP数据包</param>
        /// <returns>发送是否成功</returns>
        void NewRTPPacket(RTPPacket rtpPack)
        {
            try
            {
                //if (_rtpSession != null)
                //    _rtpSession.SendPacket(rtpPack);
                if (_rtpSend != null)
                    _rtpSend.Send(rtpPack);
            }
            catch
            {
                return;
            }
            return;
        }

        public bool Start(string ip, int tarPort, int port)
        {
            IPEndPoint localPoint = new IPEndPoint(IPAddress.Any, port);
            IPEndPoint tarPoint = new IPEndPoint(IPAddress.Parse(ip), tarPort);

            _rtpSession = new RTPSession();

            try
            {
                _rtpRecv = new RTPReceiver();
                RTPParticipant rtpRecvAdd = new RTPParticipant(localPoint);
                _rtpRecv.AddParticipant(rtpRecvAdd);
            }
            catch
            {
                _rtpRecv.Dispose();
                _rtpSession.Dispose();
                return false;
            }
            try
            {
                _rtpSend = new RTPSender();
                RTPParticipant rtpSendAdd = new RTPParticipant(tarPoint);
                _rtpSend.AddParticipant(rtpSendAdd);
            }
            catch
            {
                _rtpRecv.Dispose();
                _rtpSend.Dispose();
                _rtpSession.Dispose();
                return false;
            }

            try
            {
                _rtpSession.AddReceiver(_rtpRecv);
            }
            catch
            {
            }
            try
            {
                _rtpSession.AddSender(_rtpSend);
            }
            catch
            {
            }
            _rtpRecv.AddRTPPacket = NewRTPPacket;

            return true;
        }

        public void Stop(int port)
        {
            if (_rtpSend != null)
            {
                _rtpSend.Dispose();
                _rtpSend = null;
            }
            if (_rtpRecv != null)
            {
                _rtpRecv.Dispose();
                _rtpRecv = null;
            }
            if (_rtpSession != null)
            {
                _rtpSession.Dispose();
                _rtpSession = null;
            }
        }

        public void Stop()
        {
            if (_rtpSend != null)
            {
                _rtpSend.Dispose();
                _rtpSend = null;
            }
            if (_rtpRecv != null)
            {
                _rtpRecv.Dispose();
                _rtpRecv = null;
            }
            if (_rtpSession != null)
            {
                _rtpSession.Dispose();
                _rtpSession = null;
            }
        }
    }

}