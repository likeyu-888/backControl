using System;
using System.Data;
using System.Text;

namespace Elite.WebServer.Services.Version5
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
                        target = DspInt(7),
                        dbi = DspInt(8)
                    },
                    input = new
                    {
                        volume = new int[] { DspInt(11), DspInt(12), DspInt(13), DspInt(14), DspInt(15), DspInt(16), DspInt(17), DspInt(18) },
                        diff_in = DspInt(19),
                        in_mute = DspInt(20),
                        power_48v = DspInt(21),
                        handing_micro = DspInt(26),
                        line_in_volume = new int[] { DspInt(30), DspInt(31), DspInt(32), DspInt(33) },
                        line_in_mute = DspInt(34),
                        is_afc = DspInt(27)
                    },
                    out_volume = new
                    {
                        volume = new int[] { DspInt(44), DspInt(45), DspInt(46), DspInt(47) },
                        out_limit = new int[] { DspInt(22), DspInt(23), DspInt(24), DspInt(25) },
                        mute = DspInt(48),
                        release_time = DspInt(114)
                    },
                    mix = new
                    {
                        enable = DspInt(118) == 1,
                        algorithm = DspInt(51),
                        startup_time = DspInt(120),
                        close_time = DspInt(121),
                        max_volume = DspInt(122),
                        max_mixer = DspInt(123),
                        out_mix = new int[] { DspInt(53), DspInt(54), DspInt(55), DspInt(56) },
                        mix_depth = DspInt(124)
                    },
                    other = new
                    {
                        boot_sound = DspInt(49),
                        startup_time = DspInt(65),
                        startup_volume = DspInt(66),
                        param_save = DspInt(50),
                        fault_threshold = DspInt(127)
                    },
                    listen_efficiency = new
                    {
                        enable = DspInt(60) == 1,
                        vad = DspInt(29),
                        vad_delay = DspInt(244),
                        max_detect_time = DspInt(245)
                    },
                    denoise = new
                    {
                        algorithm = DspInt(72),
                        s_class = DspInt(77),
                        w_class = DspInt(73)
                    },
                    afc = new
                    {
                        enable = DspInt(75) == 1,
                        afc_param = DspInt(76)
                    },
                    eq = new
                    {
                        enable = DspInt(78) == 1,
                        eq_value = new int[] { DspInt(79), DspInt(80), DspInt(81), DspInt(82), DspInt(83), DspInt(84), DspInt(85), DspInt(86), DspInt(87), DspInt(88), DspInt(89), DspInt(90), DspInt(91), DspInt(92), DspInt(93), DspInt(94) },
                        eq_rate = new int[] { BitConverter.ToUInt16(data, dspBegin + 128),
                        BitConverter.ToUInt16(data, dspBegin + 130),
                        BitConverter.ToUInt16(data, dspBegin + 132),
                        BitConverter.ToUInt16(data, dspBegin + 134),
                        BitConverter.ToUInt16(data, dspBegin + 136),
                        BitConverter.ToUInt16(data, dspBegin + 138),
                        BitConverter.ToUInt16(data, dspBegin + 140),
                        BitConverter.ToUInt16(data, dspBegin + 142),
                        BitConverter.ToUInt16(data, dspBegin + 144),
                        BitConverter.ToUInt16(data, dspBegin + 146),
                        BitConverter.ToUInt16(data, dspBegin + 148),
                        BitConverter.ToUInt16(data, dspBegin + 150),
                        BitConverter.ToUInt16(data, dspBegin + 152),
                        BitConverter.ToUInt16(data, dspBegin + 154)
                        },
                        eq_q = new float[] { BitConverter.ToSingle(data, dspBegin + 156),
                        BitConverter.ToSingle(data, dspBegin + 160),
                        BitConverter.ToSingle(data, dspBegin + 164),
                        BitConverter.ToSingle(data, dspBegin + 168),
                        BitConverter.ToSingle(data, dspBegin + 172),
                        BitConverter.ToSingle(data, dspBegin + 176),
                        BitConverter.ToSingle(data, dspBegin + 180),
                        BitConverter.ToSingle(data, dspBegin + 184),
                        BitConverter.ToSingle(data, dspBegin + 188),
                        BitConverter.ToSingle(data, dspBegin + 192),
                        BitConverter.ToSingle(data, dspBegin + 196),
                        BitConverter.ToSingle(data, dspBegin + 200),
                        BitConverter.ToSingle(data, dspBegin + 204),
                        BitConverter.ToSingle(data, dspBegin + 208)
                        }
                    },
                    reverb = new
                    {
                        enable = DspInt(95) == 1,
                        suspend_detect = DspInt(35) == 1,
                        handing_threshold = DspInt(96),
                        courseware_threshold = DspInt(97),
                        wireless_threshold = DspInt(98),
                        handing_depth = DspInt(99),
                        courseware_depth = DspInt(100),
                        wireless_depth = DspInt(101),
                        handing_class = DspInt(102),
                        courseware_class = DspInt(103),
                        wireless_class = DspInt(104),
                        release_time = DspInt(105),
                        startup_time = DspInt(106),
                        detect_time = DspInt(107),
                        wireless_adaptive_enable = DspInt(108) == 1
                    },
                    noice_handle = new
                    {
                        enable = DspInt(109) == 1,
                        micros = DspInt(119),
                        startup_time = DspInt(125),
                        release_time = DspInt(126),
                        high_threshold = DspInt(52),
                        high_depth = DspInt(67),
                        high_class = DspInt(115),
                        medium_threshold = DspInt(110),
                        medium_depth = DspInt(111),
                        medium_class = DspInt(116),
                        low_threshold = DspInt(112),
                        low_depth = DspInt(113),
                        low_class = DspInt(117)
                    }
                },
                reverb_time = BitConverter.ToSingle(data, 616)
            };


            return json;
        }
    }
}