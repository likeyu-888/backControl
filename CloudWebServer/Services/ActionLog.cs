using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Elite.WebServer.Services
{
    public class ActionLog
    {
        public static long AddLog(MySqlConnection conn,
           int action_id,
           int school_id,
           int device_id,
           int status,
           string username,
           int user_id,
           string remark
           )
        {
            string ip = ClientInfo.GetRealIp;

            string commandText = "insert into log_action set " +
                "username=@username," +
                "action_id=@action_id," +
                "school_id=@school_id," +
                "device_id=@device_id," +
                "status=@status," +
                "user_id=@user_id," +
                "ip=@ip," +
                "remark=@remark";

            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@username", username));
            parameters.Add(new MySqlParameter("@action_id", action_id));
            parameters.Add(new MySqlParameter("@school_id", school_id));
            parameters.Add(new MySqlParameter("@device_id", device_id));
            parameters.Add(new MySqlParameter("@status", status));
            parameters.Add(new MySqlParameter("@user_id", user_id));
            parameters.Add(new MySqlParameter("@ip", ip));
            parameters.Add(new MySqlParameter("@remark", remark));

            long LastId = MysqlHelper.ExecuteNonQuery(conn, System.Data.CommandType.Text, commandText, parameters.ToArray(), true);

            return LastId;
        }

        public static void Finished(MySqlConnection conn, long id)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@id", id));

            string commandText = "update log_action set status=1 where id=@id";
            MysqlHelper.ExecuteNonQuery(conn, System.Data.CommandType.Text, commandText, parameters.ToArray());
        }


        public static void Failed(MySqlConnection conn, long id)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@id", id));

            string commandText = "update log_action set status=0 where id=@id";
            MysqlHelper.ExecuteNonQuery(conn, System.Data.CommandType.Text, commandText, parameters.ToArray());
        }
    }
}
