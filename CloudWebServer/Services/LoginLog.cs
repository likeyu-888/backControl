using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Elite.WebServer.Services
{
    public class LoginLog
    {
        public static long AddLog(MySqlConnection conn,
            int status,
            string username,
            int user_id,
            string remark,
            int action_id = 0
            )
        {

            string ip = ClientInfo.GetRealIp;

            string commandText = "insert into log_login set " +
                "username=@username," +
                "status=@status," +
                "user_id=@user_id," +
                "ip=@ip," +
                "remark=@remark," +
                "action_id=@action_id";

            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@username", username));
            parameters.Add(new MySqlParameter("@status", status));
            parameters.Add(new MySqlParameter("@user_id", user_id));
            parameters.Add(new MySqlParameter("@ip", ip));
            parameters.Add(new MySqlParameter("@remark", remark));
            parameters.Add(new MySqlParameter("@action_id", action_id));

            long LastId = MysqlHelper.ExecuteNonQuery(conn, System.Data.CommandType.Text, commandText, parameters.ToArray(), true);

            return LastId;
        }

        public static void Finished(MySqlConnection conn, long id)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@id", id));

            string commandText = "update log_login set status=1 where id=@id";
            MysqlHelper.ExecuteNonQuery(conn, System.Data.CommandType.Text, commandText, parameters.ToArray());
        }
    }
}
