using Elite.WebServer.Utility;
using System;
using System.Text;

namespace Elite.WebServer.Services.Version5
{
    public class DeviceInfoHex
    {
        private byte[] data = new byte[620];
        private byte[] arm = new byte[300];
        private byte[] dsp = new byte[248];
        private dynamic obj;

        public byte[] GetHexFromJson(dynamic obj)
        {
            this.obj = obj;
            BuildArm();
            BuildDsp();
            Array.Copy(arm, 0, data, 0, arm.Length);
            Array.Copy(dsp, 0, data, 300, dsp.Length);

            //616-8=608,4字节 为混响时间


            return data;
        }

        private void BuildArm()
        {

            byte[] bytes = Encoding.UTF8.GetBytes(obj.name.ToString());
            Array.Copy(bytes, 0, arm, 136, bytes.Length);
            string[] strs;

            //Ip地址
            strs = obj.arm.ip.ToString().Split('.');
            arm[32] = (byte)Convert.ToUInt16(strs[0]);
            arm[33] = (byte)Convert.ToUInt16(strs[1]);
            arm[34] = (byte)Convert.ToUInt16(strs[2]);
            arm[35] = (byte)Convert.ToUInt16(strs[3]);

            //gateway
            strs = obj.arm.gateway.ToString().Split('.');
            arm[36] = (byte)Convert.ToUInt16(strs[0]);
            arm[37] = (byte)Convert.ToUInt16(strs[1]);
            arm[38] = (byte)Convert.ToUInt16(strs[2]);
            arm[39] = (byte)Convert.ToUInt16(strs[3]);

            //mark
            strs = obj.arm.mark.ToString().Split('.');
            arm[40] = (byte)Convert.ToUInt16(strs[0]);
            arm[41] = (byte)Convert.ToUInt16(strs[1]);
            arm[42] = (byte)Convert.ToUInt16(strs[2]);
            arm[43] = (byte)Convert.ToUInt16(strs[3]);

            //mac
            strs = obj.arm.mac.ToString().Split('.');
            arm[44] = Convert.ToByte(strs[0], 16);
            arm[45] = Convert.ToByte(strs[1], 16);
            arm[46] = Convert.ToByte(strs[2], 16);
            arm[47] = Convert.ToByte(strs[3], 16);
            arm[48] = (byte)Convert.ToByte(strs[4], 16);
            arm[49] = (byte)Convert.ToByte(strs[5], 16);

            arm[50] = (byte)obj.arm.domain_enable;
            bytes = Encoding.UTF8.GetBytes(obj.arm.domain.ToString());
            Array.Copy(bytes, 0, arm, 52, bytes.Length);

            //server_ip
            strs = obj.arm.server_ip.ToString().Split('.');
            arm[116] = (byte)Convert.ToUInt16(strs[0]);
            arm[117] = (byte)Convert.ToUInt16(strs[1]);
            arm[118] = (byte)Convert.ToUInt16(strs[2]);
            arm[119] = (byte)Convert.ToUInt16(strs[3]);

            //设备配置端口
            byte[] couple = BitConverter.GetBytes((ushort)obj.arm.device_port);
            arm[124] = couple[0];
            arm[125] = couple[1];
            couple = BitConverter.GetBytes((ushort)obj.arm.server_port);
            arm[126] = couple[0];
            arm[127] = couple[1];
            couple = BitConverter.GetBytes((ushort)obj.arm.device_audio_port);
            arm[128] = couple[0];
            arm[129] = couple[1];
            couple = BitConverter.GetBytes((ushort)obj.arm.server_audio_port);
            arm[130] = couple[0];
            arm[131] = couple[1];

            //设备Id
            bytes = Encoding.Default.GetBytes(obj.id.ToString());
            Array.Copy(bytes, 0, arm, 0, bytes.Length);

            arm[296] = (byte)((bool)obj.arm.remote_debug ? 1 : 0);
            arm[297] = (byte)obj.arm.version;
            arm[298] = (byte)obj.arm.play_status;
            arm[299] = (byte)obj.arm.listen_status;
        }

        private void BuildDsp()
        {
            dynamic o = obj.dsp;
            dsp[0] = 0xf0;
            dsp[1] = 0xaa;

            byte[] couple = BitConverter.GetBytes(dsp.Length);
            dsp[2] = couple[0];

            dsp[3] = 0x03;

            dsp[4] = o.device_type;
            dsp[5] = (byte)o.version;

            //agc
            dsp[6] = (byte)o.agc.enable;
            dsp[7] = (byte)o.agc.target;
            dsp[8] = (byte)o.agc.dbi;

            int i;
            //input
            for (i = 11; i <= 18; i++)
            {
                dsp[i] = (byte)((int)o.input.volume[i - 11]);
            }

            dsp[19] = (byte)o.input.diff_in;
            dsp[20] = (byte)o.input.in_mute;
            dsp[21] = (byte)o.input.power_48v;
            dsp[26] = (byte)o.input.handing_micro;
            dsp[27] = (byte)o.input.is_afc;

            dsp[30] = (byte)((int)o.input.line_in_volume[0]);
            dsp[31] = (byte)((int)o.input.line_in_volume[1]);
            dsp[32] = (byte)((int)o.input.line_in_volume[2]);
            dsp[33] = (byte)((int)o.input.line_in_volume[3]);
            dsp[34] = (byte)o.input.line_in_mute;

            //out_volume
            dsp[22] = (byte)((int)o.out_volume.out_limit[0]);
            dsp[23] = (byte)((int)o.out_volume.out_limit[1]);
            dsp[24] = (byte)((int)o.out_volume.out_limit[2]);
            dsp[25] = (byte)((int)o.out_volume.out_limit[3]);

            dsp[44] = (byte)o.out_volume.volume[0];
            dsp[45] = (byte)o.out_volume.volume[1];
            dsp[46] = (byte)o.out_volume.volume[2];
            dsp[47] = (byte)o.out_volume.volume[3];

            dsp[48] = Convert.ToByte((int)o.out_volume.mute);
            dsp[114] = (byte)o.out_volume.release_time;

            //mix
            dsp[118] = (byte)o.mix.enable;
            dsp[51] = (byte)o.mix.algorithm;
            dsp[120] = (byte)o.mix.startup_time;
            dsp[121] = (byte)o.mix.close_time;
            dsp[122] = (byte)o.mix.max_volume;
            dsp[123] = (byte)o.mix.max_mixer;

            dsp[53] = (byte)o.mix.out_mix[0];
            dsp[54] = (byte)o.mix.out_mix[1];
            dsp[55] = (byte)o.mix.out_mix[2];
            dsp[56] = (byte)o.mix.out_mix[3];

            dsp[124] = (byte)o.mix.mix_depth;

            //other
            dsp[49] = (byte)o.other.boot_sound;
            dsp[50] = (byte)o.other.param_save;
            dsp[65] = (byte)o.other.startup_time;
            dsp[66] = (byte)o.other.startup_volume;
            dsp[127] = (byte)o.other.fault_threshold;

            //listen_efficiency
            dsp[60] = (byte)o.listen_efficiency.enable;
            dsp[29] = (byte)o.listen_efficiency.vad;
            dsp[244] = (byte)o.listen_efficiency.vad_delay;
            dsp[245] = (byte)o.listen_efficiency.max_detect_time;

            //denoise
            dsp[72] = (byte)o.denoise.algorithm;
            dsp[77] = (byte)o.denoise.s_class;
            dsp[73] = (byte)o.denoise.w_class;

            //afc
            dsp[75] = (byte)o.afc.enable;
            dsp[76] = (byte)o.afc.afc_param;

            //eq
            dsp[78] = (byte)o.eq.enable;

            for (i = 79; i <= 94; i++)
            {
                dsp[i] = (byte)((int)o.eq.eq_value[i - 79]);
            }

            int j;
            for (i = 128, j = 0; i <= 154; i += 2)
            {
                couple = BitConverter.GetBytes((ushort)o.eq.eq_rate[j++]);
                dsp[i] = couple[0];
                dsp[i + 1] = couple[1];
            }

            byte[] four;
            for (i = 156, j = 0; i <= 208; i += 4)
            {
                four = BitConverter.GetBytes((float)o.eq.eq_q[j++]);
                dsp[i] = four[0];
                dsp[i + 1] = four[1];
                dsp[i + 2] = four[2];
                dsp[i + 3] = four[3];
            }



            //reverb
            dsp[95] = (byte)o.reverb.enable;
            dsp[35] = (byte)o.reverb.suspend_detect;
            dsp[96] = (byte)o.reverb.handing_threshold;
            dsp[97] = (byte)o.reverb.courseware_threshold;
            dsp[98] = (byte)o.reverb.wireless_threshold;
            dsp[99] = (byte)o.reverb.handing_depth;
            dsp[100] = (byte)o.reverb.courseware_depth;
            dsp[101] = (byte)o.reverb.wireless_depth;
            dsp[102] = (byte)o.reverb.handing_class;
            dsp[103] = (byte)o.reverb.courseware_class;
            dsp[104] = (byte)o.reverb.wireless_class;
            dsp[105] = (byte)o.reverb.release_time;
            dsp[106] = (byte)o.reverb.startup_time;
            dsp[107] = (byte)o.reverb.detect_time;
            dsp[108] = (byte)o.reverb.wireless_adaptive_enable;

            //noice_handle
            dsp[109] = (byte)o.noice_handle.enable;
            dsp[119] = (byte)o.noice_handle.micros;
            dsp[125] = (byte)o.noice_handle.startup_time;
            dsp[126] = (byte)o.noice_handle.release_time;
            dsp[52] = (byte)o.noice_handle.high_threshold;
            dsp[67] = (byte)o.noice_handle.high_depth;
            dsp[115] = (byte)o.noice_handle.high_class;
            dsp[110] = (byte)o.noice_handle.medium_threshold;
            dsp[111] = (byte)o.noice_handle.medium_depth;
            dsp[116] = (byte)o.noice_handle.medium_class;
            dsp[112] = (byte)o.noice_handle.low_threshold;
            dsp[113] = (byte)o.noice_handle.low_depth;
            dsp[117] = (byte)o.noice_handle.low_class;

            CRC16 crcCheck = new CRC16();
            int value = crcCheck.CreateCRC16(dsp, Convert.ToUInt16(dsp.Length - 2));
            byte[] bytes = BitConverter.GetBytes(value);
            dsp[dsp.Length - 2] = bytes[0];
            dsp[dsp.Length - 1] = bytes[1];
        }
    }
}
