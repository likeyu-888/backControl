using MySql.Data.MySqlClient;
using System;

namespace EliteService.Utility
{
    class SqlHelper
    {

        public static int PageCount(int total, int pageSize)
        {
            return (int)Math.Ceiling(Convert.ToDouble(total) / pageSize);
        }

        public static int TotalCount(MySqlConnection conn)
        {
            return Convert.ToInt32(MySqlHelper.ExecuteScalar(conn, "select FOUND_ROWS()"));
        }

        public static String QueryLimit(int page_size, int page)
        {
            return " limit " + (page_size * (page - 1)).ToString() + "," + page_size.ToString();
        }

        public static String QueryOrder(string sort_column, string sort_direction)
        {
            if (string.IsNullOrEmpty(sort_column)) return string.Empty;
            if (string.IsNullOrEmpty(sort_direction)) return string.Empty;
            if (sort_direction.ToLower().Equals("normal")) return string.Empty;
            if ((sort_column.IndexOf('.') > 0) && ((sort_column.IndexOf('.') + 1) >= sort_column.Length)) return string.Empty;

            return " order by " + sort_column + " " + sort_direction;
        }
    }
}
