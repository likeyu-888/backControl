using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Elite.WebServer.Services
{
    public class SyncLog
    {
        public static long AddLog(MySqlConnection conn,
           int school_id,
           int device_id,
           int status,
           string api_name,
           string remark
           )
        {
            string ip = ClientInfo.GetRealIp;

            string commandText = "insert into log_sync set " +
                "school_id=@school_id," +
                "api_name=@api_name," +
                "device_id=@device_id," +
                "status=@status," +
                "ip=@ip," +
                "remark=@remark";

            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@school_id", school_id));
            parameters.Add(new MySqlParameter("@api_name", api_name));
            parameters.Add(new MySqlParameter("@device_id", device_id));
            parameters.Add(new MySqlParameter("@status", status));
            parameters.Add(new MySqlParameter("@ip", ip));
            parameters.Add(new MySqlParameter("@remark", remark));

            long LastId = MysqlHelper.ExecuteNonQuery(conn, System.Data.CommandType.Text, commandText, parameters.ToArray(), true);

            return LastId;
        }

        public static void Finished(MySqlConnection conn, long id)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@id", id));

            string commandText = "update log_sync set status=1 where id=@id";
            MysqlHelper.ExecuteNonQuery(conn, System.Data.CommandType.Text, commandText, parameters.ToArray());
        }
    }
}
