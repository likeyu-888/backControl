namespace EliteService.DTO
{
    public class Device
    {
        public string Ip { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }
        public int Errors { get; set; }
        public int ListenPort { get; set; }
        public int ListenChannel { get; set; }
        public int ListenStatus { get; set; }
        public int IsAutoSave { get; set; }
        public int IsAutoRecord { get; set; }
        public string ArmVersion { get; set; }
        public int RoomId { get; set; }
        public int Status { get; set; }
        public int DeviceType { get; set; }
        public byte[] Dsp { get; set; }

        /// <summary>
        /// 删除标志，使用时先重置为1，数据库存在时置为0，最后仍为1的表示不存在，需删除
        /// </summary>
        public int DeleteTag { get; set; }
        public Device()
        {
            this.Errors = 0;
            this.DeleteTag = 1;
            this.ListenChannel = 0;
            this.ListenPort = 0;
            this.ListenStatus = 0;
            this.IsAutoSave = 0;
            this.IsAutoRecord = 0;
            this.ArmVersion = "";
            this.Status = 100;
            this.DeviceType = 0;
        }
    }
}
