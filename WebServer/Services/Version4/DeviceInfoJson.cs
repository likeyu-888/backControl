using System;
using System.Data;
using System.Text;

namespace Elite.WebServer.Services.Version4
{
    public class DeviceInfoJson
    {
        private byte[] data;

        private int armBegin = 8;
        private int dspBegin = 308;

        private int GetInt(int index)
        {
            return Convert.ToInt32(this.data[index]);
        }

        private int ArmInt(int index)
        {
            return Convert.ToInt32(this.data[index + 8]);
        }

        private int DspInt(int index)
        {
            return Convert.ToInt32(this.data[index + 308]);
        }

        /// <summary>
        /// 计算字节数组中非零长度
        /// </summary>
        /// <param name="data"></param>
        /// <param name="begin"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private int GetLength(byte[] data, int begin, int count)
        {
            int zeroCount = 0;
            for (int i = begin + count - 1; i >= begin; i--)
            {
                if (data[i] == 0) zeroCount++;
                else break;
            }
            return count - zeroCount;
        }

        public object GetJsonFromHex(DataRow row, byte[] data)
        {
            this.data = data;

            int idLen = GetLength(data, armBegin + 0, 32);
            int nameLen = GetLength(data, armBegin + 136, 32);
            int domanLen = GetLength(data, armBegin + 52, 32);

            object json = new
            {
                update_time = row["update_time"],
                create_time = row["create_time"],
                is_auto_save = row["is_auto_save"],
                is_auto_record = row["is_auto_record"],
                room_name = row["room_name"],
                group_name = row["group_name"],

                id = Encoding.Default.GetString(data, armBegin + 0, idLen),
                name = Encoding.UTF8.GetString(data, armBegin + 136, nameLen),
                status = GetInt(5),
                test_command = GetInt(6),
                test_type = GetInt(7),
                arm = new
                {
                    ip = ArmInt(32).ToString() + "." + ArmInt(33).ToString() + "." + ArmInt(34).ToString() + "." + ArmInt(35).ToString(),
                    gateway = ArmInt(36).ToString() + "." + ArmInt(37).ToString() + "." + ArmInt(38).ToString() + "." + ArmInt(39).ToString(),
                    mark = ArmInt(40).ToString() + "." + ArmInt(41).ToString() + "." + ArmInt(42).ToString() + "." + ArmInt(43).ToString(),
                    mac = ArmInt(44).ToString("X2") + "." + ArmInt(45).ToString("X2") + "." + ArmInt(46).ToString("X2") + "." + ArmInt(47).ToString("X2") + "." + ArmInt(48).ToString("X2") + "." + ArmInt(49).ToString("X2"),
                    domain_enable = ArmInt(50) == 1,
                    domain = Encoding.UTF8.GetString(data, armBegin + 52, domanLen),
                    server_ip = ArmInt(116).ToString() + "." + ArmInt(117).ToString() + "." + ArmInt(118).ToString() + "." + ArmInt(119).ToString(),
                    server_port = BitConverter.ToUInt16(data, armBegin + 126),
                    server_audio_port = BitConverter.ToUInt16(data, armBegin + 130),
                    device_port = BitConverter.ToUInt16(data, armBegin + 124),
                    device_audio_port = BitConverter.ToUInt16(data, armBegin + 128),
                    device_upgrade_port = BitConverter.ToUInt16(data, armBegin + 132),
                    device_format = ArmInt(200),
                    remote_debug = ArmInt(296) == 1,
                    version = ArmInt(297),
                    play_status = ArmInt(298),
                    listen_status = ArmInt(299)
                },
                dsp = new
                {
                    device_type = DspInt(4),
                    version = DspInt(5),
                    agc = new
                    {
                        enable = DspInt(6),
                        target = DspInt(36),
                        dbi = DspInt(37)
                    },
                    input = new
                    {
                        volume = new int[] { DspInt(16), DspInt(17), DspInt(18), DspInt(19), DspInt(20), DspInt(21), DspInt(22), DspInt(23) },
                        diff_in = DspInt(57),
                        in_mute = DspInt(58),
                        power_48v = DspInt(118),
                        line_in_volume = new int[] { DspInt(12), DspInt(13), DspInt(14), DspInt(15), DspInt(69) },
                        line_in_mute = DspInt(121)
                    },
                    out_volume = new
                    {
                        volume = new int[] { DspInt(115), DspInt(119), DspInt(120), DspInt(127) },
                        volume_limit = new int[] { DspInt(122), DspInt(123), DspInt(124), DspInt(125) },
                        out_limit = new int[] { DspInt(110), DspInt(111), DspInt(112), DspInt(113) },
                        mute = DspInt(126),
                        release_time = new int[] { DspInt(102), DspInt(103), DspInt(104), DspInt(105) }
                    },
                    mix = new
                    {
                        enable = DspInt(107) == 1,
                        algorithm = DspInt(75),
                        startup_time = DspInt(116),
                        close_time = DspInt(117),
                        max_mixer = DspInt(128),
                        mix_depth = DspInt(133),
                        out_mix = new int[] { DspInt(70), DspInt(71), DspInt(72), DspInt(73) }
                    },
                    listen_efficiency = new
                    {
                        enable = DspInt(98) == 1,
                        vad = DspInt(99),
                        vad_delay = DspInt(100),
                        max_detect_time = DspInt(101)
                    },
                    denoise = new
                    {
                        mode = DspInt(88),
                        algorithm = DspInt(84),
                        s_class = DspInt(9),
                        w_class = DspInt(85)
                    },
                    aec = new
                    {
                        enable = DspInt(106) == 1,
                        aec_enhance = DspInt(129) == 1,
                        aec_param = DspInt(8),
                        double_talk_param = DspInt(10),
                        nonlinea_param = DspInt(11),
                        convergence_speed = DspInt(68),
                        hf_reject = DspInt(7),
                        intelligent_switch = DspInt(86) == 1,
                        intelligent_switch_time = DspInt(87)
                    },
                    reference_frame_setting = new
                    {
                        detect_threshold = DspInt(131),
                        detect_delay = DspInt(132)
                    },
                    reference_frame = new
                    {
                        enable = DspInt(76) == 1,
                        micro_threshold = DspInt(80),
                        micro_detect_time = DspInt(81),
                        remote_threshold = DspInt(78),
                        remote_detect_time = DspInt(79),
                        decay_depth = DspInt(77),
                        startup_time = DspInt(82),
                        release_time = DspInt(83),
                        micros = DspInt(89)
                    },
                    eq = new
                    {
                        enable = DspInt(56) == 1,
                        eq_value = new int[] { DspInt(40), DspInt(41), DspInt(42), DspInt(43), DspInt(44), DspInt(45), DspInt(46), DspInt(47), DspInt(48), DspInt(49), DspInt(50), DspInt(51), DspInt(52), DspInt(53), DspInt(54), DspInt(55) },
                    },
                    reverb = new
                    {
                        enable = DspInt(109) == 1,
                        suspend_detect = DspInt(74) == 1,
                        courseware_threshold = DspInt(60),
                        wireless_threshold = DspInt(59),
                        pickup_depth = DspInt(65),
                        courseware_depth = DspInt(64),

                        release_time = DspInt(62),
                        startup_time = DspInt(63),
                        detect_time = DspInt(61),
                        wireless_adaptive_enable = DspInt(66) == 1
                    }
                },
                reverb_time = BitConverter.ToSingle(data, 504)
            };


            return json;
        }
    }
}