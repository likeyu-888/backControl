using Elite.WebServer.Utility;
using System;
using System.Text;

namespace Elite.WebServer.Services.Version4
{
    public class DeviceInfoHex
    {
        private byte[] data = new byte[520];
        private byte[] arm = new byte[300];
        private byte[] dsp = new byte[136];
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
            arm[51] = arm[50]; //audio_domain_enable

            bytes = Encoding.UTF8.GetBytes(obj.arm.domain.ToString());
            Array.Copy(bytes, 0, arm, 52, bytes.Length);

            //server_ip
            strs = obj.arm.server_ip.ToString().Split('.');
            arm[116] = (byte)Convert.ToUInt16(strs[0]);
            arm[117] = (byte)Convert.ToUInt16(strs[1]);
            arm[118] = (byte)Convert.ToUInt16(strs[2]);
            arm[119] = (byte)Convert.ToUInt16(strs[3]);
            //audio_server_ip
            Array.Copy(arm, 116, arm, 120, 4);


            //设备配置端口
            byte[] couple = BitConverter.GetBytes((ushort)obj.arm.device_port);
            arm[124] = couple[0];
            arm[125] = couple[1];
            //device_audio_port
            Array.Copy(arm, 124, arm, 128, 2);
            //device_upgrade_port
            Array.Copy(arm, 124, arm, 132, 2);

            couple = BitConverter.GetBytes((ushort)obj.arm.server_port);
            arm[126] = couple[0];
            arm[127] = couple[1];
            //server_audio_port
            Array.Copy(arm, 126, arm, 130, 2);
            //server_upgrade_port
            Array.Copy(arm, 126, arm, 134, 2);

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
            dsp[36] = (byte)o.agc.target;
            dsp[37] = (byte)o.agc.dbi;

            int i;
            //input
            for (i = 16; i <= 23; i++)
            {
                dsp[i] = (byte)((int)o.input.volume[i - 16]);
            }

            dsp[57] = (byte)o.input.diff_in;
            dsp[58] = (byte)o.input.in_mute;
            dsp[118] = (byte)o.input.power_48v;

            dsp[12] = (byte)((int)o.input.line_in_volume[0]);
            dsp[13] = (byte)((int)o.input.line_in_volume[1]);
            dsp[14] = (byte)((int)o.input.line_in_volume[2]);
            dsp[15] = (byte)((int)o.input.line_in_volume[3]);
            dsp[69] = (byte)((int)o.input.line_in_volume[4]);
            dsp[121] = (byte)o.input.line_in_mute;

            //out.volume
            dsp[115] = (byte)((int)o.out_volume.volume[0]);
            dsp[119] = (byte)((int)o.out_volume.volume[1]);
            dsp[120] = (byte)((int)o.out_volume.volume[2]);
            dsp[127] = (byte)((int)o.out_volume.volume[2]);

            //out.volume_limit
            dsp[122] = (byte)((int)o.out_volume.volume_limit[0]);
            dsp[123] = (byte)((int)o.out_volume.volume_limit[1]);
            dsp[124] = (byte)((int)o.out_volume.volume_limit[2]);
            dsp[125] = (byte)((int)o.out_volume.volume_limit[2]);

            //out.out_limit
            dsp[110] = (byte)o.out_volume.out_limit[0];
            dsp[111] = (byte)o.out_volume.out_limit[1];
            dsp[112] = (byte)o.out_volume.out_limit[2];
            dsp[113] = (byte)o.out_volume.out_limit[2];

            dsp[126] = Convert.ToByte((int)o.out_volume.mute);

            //out.out_limit
            dsp[102] = (byte)o.out_volume.release_time[0];
            dsp[103] = (byte)o.out_volume.release_time[1];
            dsp[104] = (byte)o.out_volume.release_time[2];
            dsp[105] = (byte)o.out_volume.release_time[2];

            //mix
            dsp[107] = (byte)o.mix.enable;
            dsp[75] = (byte)o.mix.algorithm;
            dsp[116] = (byte)o.mix.startup_time;
            dsp[117] = (byte)o.mix.close_time;
            dsp[128] = (byte)o.mix.max_mixer;

            dsp[70] = (byte)o.mix.out_mix[0];
            dsp[71] = (byte)o.mix.out_mix[1];
            dsp[72] = (byte)o.mix.out_mix[2];
            dsp[73] = (byte)o.mix.out_mix[3];

            dsp[133] = (byte)o.mix.mix_depth;

            //listen_efficiency
            dsp[98] = (byte)o.listen_efficiency.enable;
            dsp[99] = (byte)o.listen_efficiency.vad;
            dsp[100] = (byte)o.listen_efficiency.vad_delay;
            dsp[101] = (byte)o.listen_efficiency.max_detect_time;

            //denoise
            dsp[88] = (byte)o.denoise.mode;
            dsp[84] = (byte)o.denoise.algorithm;
            dsp[9] = (byte)o.denoise.s_class;
            dsp[85] = (byte)o.denoise.w_class;

            //aec
            dsp[106] = (byte)o.aec.enable;
            dsp[129] = (byte)o.aec.aec_enhance;
            dsp[8] = (byte)o.aec.aec_param;
            dsp[10] = (byte)o.aec.double_talk_param;
            dsp[11] = (byte)o.aec.nonlinea_param;
            dsp[68] = (byte)o.aec.convergence_speed;
            dsp[7] = (byte)o.aec.hf_reject;
            dsp[86] = (byte)o.aec.intelligent_switch;
            dsp[87] = (byte)o.aec.intelligent_switch_time;

            //reference_frame
            dsp[131] = (byte)o.reference_frame_setting.detect_threshold;
            dsp[132] = (byte)o.reference_frame_setting.detect_delay;

            dsp[76] = (byte)o.reference_frame.enable;
            dsp[80] = (byte)o.reference_frame.micro_threshold;
            dsp[81] = (byte)o.reference_frame.micro_detect_time;
            dsp[78] = (byte)o.reference_frame.remote_threshold;
            dsp[79] = (byte)o.reference_frame.remote_detect_time;
            dsp[77] = (byte)o.reference_frame.decay_depth;
            dsp[82] = (byte)o.reference_frame.startup_time;
            dsp[83] = (byte)o.reference_frame.release_time;
            dsp[89] = (byte)o.reference_frame.micros;

            //eq
            dsp[56] = (byte)o.eq.enable;

            for (i = 40; i <= 55; i++)
            {
                dsp[i] = (byte)((int)o.eq.eq_value[i - 40]);
            }

            //reverb
            dsp[109] = (byte)o.reverb.enable;
            dsp[74] = (byte)o.reverb.suspend_detect;
            dsp[60] = (byte)o.reverb.courseware_threshold;
            dsp[59] = (byte)o.reverb.wireless_threshold;
            dsp[65] = (byte)o.reverb.pickup_depth;
            dsp[64] = (byte)o.reverb.courseware_depth;

            dsp[62] = (byte)o.reverb.release_time;
            dsp[63] = (byte)o.reverb.startup_time;
            dsp[61] = (byte)o.reverb.detect_time;
            dsp[66] = (byte)o.reverb.wireless_adaptive_enable;

            //default 20200717
            dsp[24] = 0;
            dsp[25] = 1;
            dsp[26] = 1;
            dsp[27] = 40;
            dsp[28] = 40;
            dsp[29] = 40;

            CRC16 crcCheck = new CRC16();
            int value = crcCheck.CreateCRC16(dsp, Convert.ToUInt16(dsp.Length - 2));
            byte[] bytes = BitConverter.GetBytes(value);
            dsp[dsp.Length - 2] = bytes[0];
            dsp[dsp.Length - 1] = bytes[1];
        }
    }
}
