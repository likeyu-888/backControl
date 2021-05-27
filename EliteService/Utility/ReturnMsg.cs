using EliteService.DTO;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;

namespace EliteService.Utility
{
    public class ReturnMsg
    {
        public static byte[] GetReturn(JsonMsg obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            byte[] jsonData = Encoding.UTF8.GetBytes(json);
            byte[] data = new byte[jsonData.Length + 2 + 2 + 2]; //2字节的长度,header长度2,crc 长度2

            byte[] bytes = BitConverter.GetBytes(data.Length);

            data[0] = 0xff;
            data[1] = 0xee;
            data[2] = bytes[0];
            data[3] = bytes[1];

            Array.Copy(jsonData, 0, data, 4, jsonData.Length);

            CRC16 crcCheck = new CRC16();
            int value = crcCheck.CreateCRC16(data, Convert.ToUInt16(data.Length - 2));
            byte[] crcs = BitConverter.GetBytes(value);
            data[data.Length - 2] = crcs[0];
            data[data.Length - 1] = crcs[1];

            return data;
        }


        public static JsonMsg<PagedData<T>> GetJsonMsg<T>(int total, int page, int page_size, int page_count, T data, Hashtable hashTable = null)
        {

            PagedData<T> pagedData = new PagedData<T>();
            pagedData.total = total;
            pagedData.page = page;
            pagedData.page_size = page_size;
            pagedData.page_count = page_count;
            pagedData.list = data;
            if (hashTable != null)
            {
                pagedData.dict = hashTable;
            }

            JsonMsg<PagedData<T>> jsonMsg = new JsonMsg<PagedData<T>>();
            jsonMsg.code = 200;
            jsonMsg.message = "操作成功";
            jsonMsg.data = pagedData;

            return jsonMsg;
        }
    }


}
