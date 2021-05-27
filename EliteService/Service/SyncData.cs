using Elite.WebServer.Services;
using EliteService.Utility;
using MySql.Data.MySqlClient;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Net;

namespace EliteService.Service
{
    class SyncData
    {

        public void SendData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                {
                    conn.Open();

                    DataSet ds = MySqlHelper.ExecuteDataset(conn, "select id,name,remark,sort from dev_group");

                    ArrayList list = new ArrayList();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        Dictionary<string, object> dRow = new Dictionary<string, object>();
                        foreach (DataColumn dCol in ds.Tables[0].Columns)
                        {
                            dRow.Add(dCol.ColumnName, row[dCol.ColumnName]);
                        }
                        list.Add(dRow);
                    }

                    string apiName = "api/syncdatas/" + SyncActions.GetSchoolId().ToString() + "/groups";

                    IRestResponse response = SyncActions.Request(apiName, Method.POST, new { list });

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        LogHelper.GetInstance.Write("同步结果", "分组同步失败:" + response.Content);
                    }

                    ds = MySqlHelper.ExecuteDataset(conn, "select id,name,remark,reverb_time,sort,device_id from sch_room where is_delete=0");
                    list = new ArrayList();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        Dictionary<string, object> dRow = new Dictionary<string, object>();
                        foreach (DataColumn dCol in ds.Tables[0].Columns)
                        {
                            dRow.Add(dCol.ColumnName, row[dCol.ColumnName]);
                        }
                        list.Add(dRow);
                    }

                    apiName = "api/syncdatas/" + SyncActions.GetSchoolId().ToString() + "/rooms";

                    response = SyncActions.Request(apiName, Method.POST, new { list });

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        LogHelper.GetInstance.Write("同步结果", "教室同步失败:" + response.Content);
                    }

                    ds = MySqlHelper.ExecuteDataset(conn, "select id,name,group_id,status,room_id,is_auto_save," +
                        "is_auto_record,sampling_rate,device_type,snr,listen_efficiency," +
                        "attendence_difficulty,anbient_noice,ip,gateway,mark,mac,arm_version,dsp_version " +
                        " from dev_device where is_delete=0");
                    list = new ArrayList();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        Dictionary<string, object> dRow = new Dictionary<string, object>();
                        foreach (DataColumn dCol in ds.Tables[0].Columns)
                        {
                            dRow.Add(dCol.ColumnName, row[dCol.ColumnName]);
                        }
                        list.Add(dRow);
                    }

                    apiName = "api/syncdatas/" + SyncActions.GetSchoolId().ToString() + "/devices";

                    response = SyncActions.Request(apiName, Method.POST, new { list });

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        LogHelper.GetInstance.Write("同步结果", "设备同步失败:" + response.Content);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("同步结果", "数据同步失败:" + ex.Message);
            }
        }
    }
}
