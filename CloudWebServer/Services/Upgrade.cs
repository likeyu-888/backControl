using Elite.WebServer.Utility;
using System;

namespace Elite.WebServer.Services
{
    public class Upgrade
    {
        public static bool IsValidFile(string type, string fileName, byte[] header, byte[] data, int deviceType)
        {
            try
            {

                if (!fileName.ToLower().EndsWith(".elite"))
                {
                    return false;
                }
                if (type.Equals("arm"))
                {
                    if (!fileName.ToLower().StartsWith("m"))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!fileName.ToLower().StartsWith("s"))
                    {
                        return false;
                    }
                }

                if (header.Length < 1024) return false;

                if (data.Length < 1) return false;

                if ((header[0] != 0xf0) || (header[1] != 0xaa)) return false;

                long length = BitConverter.ToInt64(header, 2);

                CRC16 crcObj = new CRC16();
                int value = crcObj.CreateCRC16(data, Convert.ToUInt32(length));

                if (((int)header[12]) != deviceType) return false;

                byte[] bytes = BitConverter.GetBytes(value);
                return bytes[0] == header[10] && bytes[1] == header[11];
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
