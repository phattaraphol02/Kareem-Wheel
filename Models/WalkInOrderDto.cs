namespace CS66_WebApplication1.Models
{
    public class WalkInOrderDto
    {
        public int CustomerId { get; set; }
        public string PaymentMethod { get; set; } = "เงินสด";
        public List<WalkInCartItemDto> CartItems { get; set; } = new List<WalkInCartItemDto>();
    }

    public class WalkInCartItemDto
    {
        public int ProductId { get; set; }
        public int Qty { get; set; }
    }
}