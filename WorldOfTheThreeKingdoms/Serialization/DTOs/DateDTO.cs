namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Data Transfer Object for GameDate
    /// 用于反序列化 JSON 中嵌套的 Date 对象
    /// </summary>
    public class DateDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int DaysLeft { get; set; }
        public bool IsRunning { get; set; }
        public int Season { get; set; }
    }
}
