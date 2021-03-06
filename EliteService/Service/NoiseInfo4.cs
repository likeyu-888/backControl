using System;

namespace EliteService.Service
{

    public class NoiseInfo4
    {
        private byte[] dsp;

        public float noise = 0;
        public float snr = 0;
        public float efficiency = 0;
        public float difficulty = 0;

        public void QueryStatus(byte[] datas, byte[] dsp)
        {
            this.dsp = new byte[400];
            Array.Copy(dsp, 0, this.dsp, 0, dsp.Length);

            StatusInfo status = GetStatusInfo(datas);
            ShowStatus(status);
        }

        private delegate void ShowStatusDeleg(StatusInfo info);
        /// <summary>
        /// 声环境参数显示
        /// </summary>
        /// <param name="info"></param>
        public void ShowStatus(StatusInfo info)
        {
            ShowEnvironment(info);
        }

        //ParamInfo.Instance.datas_dsp:DSP参数
        /// <summary>
        /// 获取环境噪声dB值
        /// </summary>
        /// <param name="noise">环境噪声dBV值</param>
        /// <param name="num">吊麦值（0~7）</param>
        /// <returns></returns>
        private float GetNoise(float noise, int num)
        {
            int agcGain = dsp[37] / 4;//AGC增益
            byte agcEnable = dsp[6];//AGC使能
            byte tmp = 0x01;
            for (int i = 0; i < num; i++)
            {
                try
                {
                    tmp = Convert.ToByte(tmp * 2);
                }
                catch
                {
                    break;
                }
            }

            byte bTmp = Convert.ToByte(agcEnable & tmp);
            int gain = 0;
            if (bTmp != 0)
            {
                gain = agcGain;
            }
            else
            {

                gain = dsp[16 + num] - 12;//对应吊麦的增益
            }
            float result = GetNoiseConvert(noise, gain);

            return result;
        }

        /// <summary>
        /// dBV转dB
        /// </summary>
        /// <param name="value"></param>
        /// <param name="gain"></param>
        /// <returns></returns>
        private float GetNoiseConvert(float value, int gain)
        {
            int i = 0;
            float result = 0;
            switch (i)
            {
                case 0://MS-040
                    result = 117 + value - gain;
                    break;
            }
            return result;
        }

        private void ShowEnvironment(StatusInfo info)
        {
            if (info != null)
            {
                int num = 0;
                float sum_noise = 0;
                float sum_snr = 0;
                float sum_efficiency = 0;
                float sum_difficulty = 0;
                float noise_min_volume = -80;
                for (int i = 0; i < 8; i++)
                {
                    //CmdInfo.noise_min_volume:最小音量一般为-80
                    if (info.status_noise_input[i] < GetNoise(noise_min_volume, i))
                    {
                        info.status_noise_input[i] = 0;
                    }
                    else
                    {
                        //info.status_noise_input[i] = 124.5f + info.status_noise_input[i]
                        //    - ParamInfo.Instance.info_device.InputVolume[i];
                        sum_noise += info.status_noise_input[i];
                        sum_snr += info.status_snr_input[i];
                        sum_efficiency += info.status_efficiency_input[i];
                        sum_difficulty += info.status_difficulty_input[i];
                        num++;
                    }
                }

                if (num > 0)
                {
                    noise = sum_noise / num;
                    snr = sum_snr / num;
                    efficiency = sum_efficiency / num;
                    difficulty = sum_difficulty / num;
                }
            }
        }
        /// <summary>
        /// 获取声环境信息
        /// </summary>
        /// <param name="datas">状态查询的数组返回</param>
        /// <returns></returns>
        private StatusInfo GetStatusInfo(byte[] datas)
        {
            if (datas.Length < 80)
            {
                return null;
            }
            StatusInfo info = new StatusInfo();
            int i = 0;
            float value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_input[i] = value;
            i++;
            value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_input[i] = value;
            i++;
            value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_input[i] = value;
            i++;
            value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_input[i] = value;
            i++;
            value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_input[i] = value;
            i++;
            value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_input[i] = value;
            i++;
            value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_input[i] = value;
            i++;
            value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_input[i] = value;
            i++;
            value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_linein[i - 8] = value;
            i++;
            value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_linein[i - 8] = value;
            i++;
            value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_linein[i - 8] = value;
            i++;
            value = BitConverter.ToSingle(datas, 8 + 4 * i);
            info.status_volume_linein[i - 8] = value;
            info.status_monitoring = datas[57] == 0 ? false : true;

            for (int k = 0; k < 8; k++)
            {
                value = BitConverter.ToSingle(datas, 80 + 4 * k);
                info.status_noise_input[k] = GetNoise(value, k);//环境噪声
                info.status_snr_input[k] = BitConverter.ToSingle(datas, 112 + 4 * k);//信噪比
                info.status_efficiency_input[k] = datas[k + 192];//听课效率
                info.status_difficulty_input[k] = datas[k + 224];//听课难度
            }
            return info;
        }
    }
}