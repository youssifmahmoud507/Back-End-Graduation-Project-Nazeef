namespace LaundryShared.DTO.User
{
    public class PointsDto
    {
        public int TotalPoints { get; set; }
        public List<PointTransactionDto> History { get; set; } = new List<PointTransactionDto>();
    }
}
