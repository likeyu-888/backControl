using System;

namespace EliteService.Service
{
    class DeviceParams
    {
        private int deviceType = 0;
        private byte[] data;

        public int armVersion = 0;
        public int dspVersion = 0;

        public string gateway = "";
        public string mark = "";
        public string mac = "";

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

        public DeviceParams(byte[] data, int deviceType)
        {
            this.deviceType = deviceType;
            this.data = data;
            this.InitParams(deviceType);
        }

        private void InitParams(int deviceType)
        {
            switch (deviceType)
            {
                case 4:
                    InitParamsVersion4();
                    break;
                case 5:
                    InitParamsVersion5();
                    break;
            }
        }

        private void InitParamsVersion4()
        {
            armVersion = (int)data[297 + 8];
            dspVersion = (int)data[300 + 8 + 5];
            gateway = ArmInt(36).ToString() + "." + ArmInt(37).ToString() + "." + ArmInt(38).ToString() + "." + ArmInt(39).ToString();
            mark = ArmInt(40).ToString() + "." + ArmInt(41).ToString() + "." + ArmInt(42).ToString() + "." + ArmInt(43).ToString();
            mac = ArmInt(44).ToString("X2") + "." + ArmInt(45).ToString("X2") + "." + ArmInt(46).ToString("X2") + "." + ArmInt(47).ToString("X2") + "." + ArmInt(48).ToString("X2") + "." + ArmInt(49).ToString("X2");
        }

        private void InitParamsVersion5()
        {
            armVersion = (int)data[297 + 8];
            dspVersion = (int)data[300 + 8 + 5];
            gateway = ArmInt(36).ToString() + "." + ArmInt(37).ToString() + "." + ArmInt(38).ToString() + "." + ArmInt(39).ToString();
            mark = ArmInt(40).ToString() + "." + ArmInt(41).ToString() + "." + ArmInt(42).ToString() + "." + ArmInt(43).ToString();
            mac = ArmInt(44).ToString("X2") + "." + ArmInt(45).ToString("X2") + "." + ArmInt(46).ToString("X2") + "." + ArmInt(47).ToString("X2") + "." + ArmInt(48).ToString("X2") + "." + ArmInt(49).ToString("X2");
        }

    }
}

