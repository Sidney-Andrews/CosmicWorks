namespace CosmicWorks.Models
{
    public class CategorySales : IEntity
    {
        public string id { get; set; }
        public string categoryId { get; set; }
        public string categoryName { get; set; }
        public int totalSales { get; set; }
    }
}