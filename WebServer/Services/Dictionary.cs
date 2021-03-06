using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Elite.WebServer.Services
{
    public class Dictionary
    {

        public static Hashtable GetDict(MySqlConnection conn, string dict)
        {
            Hashtable hashTable = new Hashtable();

            if (!string.IsNullOrEmpty(dict))
            {
                string[] dictArr = dict.Trim().Split(',');
                if (dictArr.Length > 0)
                {
                    foreach (string key in dictArr)
                    {
                        string redisKey = Helper.md5("dictionary-" + key);

                        RedisHelper.Remove(redisKey);// todo 正式使用要去掉。
                        if (RedisHelper.Exists(redisKey))
                        {
                            hashTable.Add(key, RedisHelper.Get<Dictionary<string, object>>(redisKey));
                            continue;
                        }
                        Dictionary<string, object> dictionary = GetSingleDictionary(conn, key);
                        RedisHelper.Set(redisKey, dictionary, 3600);
                        hashTable.Add(key, dictionary);
                    }
                }
            }

            return hashTable;
        }

        private static Dictionary<string, object> GetSingleDictionary(MySqlConnection conn, string key)
        {

            string commandText = "";
            switch (key)
            {
                case "user":
                    commandText = "select id as value, username as text from ucb_user order by username";
                    break;
                case "device":
                    commandText = "select id as value, ip as text from dev_device where is_delete = 0 order by ip";
                    break;
                case "device_with_room":
                    commandText = "SELECT  " +
                      "dev.id AS VALUE," +
                      "CASE WHEN room.id IS NULL THEN " +
                      "CONCAT(dev.ip, ' [ 教室：无 ]') " +
                      "ELSE " +
                      "CONCAT(dev.ip, ' [ 教室：', room.name, ' ]') " +
                      "END " +
                      "AS TEXT " +
                      "FROM " +
                      "dev_device dev " +
                      "LEFT JOIN sch_room room " +
                      "ON dev.room_id = room.id " +
                      "WHERE dev.is_delete = 0 " +
                      "ORDER BY dev.ip";
                    break;
                case "device_group":
                    commandText = "select id as value, name as text from dev_group order by CONVERT(NAME USING gbk)";
                    break;
                case "arm_version":
                    commandText = "select distinct arm_version as value, arm_version as text from dev_device where arm_version!='' order by arm_version";
                    break;
                case "dsp_version":
                    commandText = "select distinct dsp_version as value, dsp_version as text from dev_device  where dsp_version!='' order by dsp_version";
                    break;
                case "room":
                    commandText = "SELECT rom.id AS VALUE," +
                        "CASE WHEN dev.id IS NULL THEN " +
                        "CONCAT(rom.name, '(设备：无)') " +
                        "ELSE " +
                        "CONCAT(rom.name, '(设备：', dev.`name`, ')') " +
                        "END " +
                        "AS TEXT " +
                        " FROM sch_room rom LEFT JOIN dev_device dev ON rom.device_id = dev.id  WHERE rom.is_delete = 0 ORDER BY rom.sort";
                    break;
                default:
                    return GetDictionary(conn, key);
            }

            Dictionary<string, object> dict = new Dictionary<string, object>();

            DataSet ds = MySqlHelper.ExecuteDataset(conn, commandText);
            if (ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    dict.Add(row["value"].ToString(), row["text"]);
                }
            }
            return dict;
        }

        public static string GetDictionary(MySqlConnection conn, string item_key, string value)
        {

            string commandText = "";

            commandText = "select val.value from sys_config_item item left join sys_config_value val on item.id=val.item_id and item.is_delete=0 " +
                "where item.item_key=@item_key and val.value=@value and val.is_delete=0";

            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@item_key", item_key));
            parameters.Add(new MySqlParameter("@value", value));

            object result = MySqlHelper.ExecuteScalar(conn, commandText, parameters.ToArray());
            if (result != null) return result.ToString();
            return string.Empty;
        }

        private static Dictionary<string, object> GetDictionary(MySqlConnection conn, string item_key)
        {
            string commandText = "";
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@item_key", item_key));

            Dictionary<string, object> dict = new Dictionary<string, object>();

            commandText = "select val.value, val.text from sys_config_item item left join sys_config_value val on item.id=val.item_id and item.is_delete=0 " +
                "where item.item_key=@item_key and val.is_delete=0";

            DataSet ds = MySqlHelper.ExecuteDataset(conn, commandText, parameters.ToArray());
            if (ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    dict.Add(row["value"].ToString(), row["text"]);
                }
            }
            return dict;
        }
    }
}