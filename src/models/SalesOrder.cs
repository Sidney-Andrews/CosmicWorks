using System.Collections.Generic;

namespace CosmicWorks.Models
{
    public class SalesOrder : IEntity
    {
        public string id { get; set; }
        public string type { get; set; }
        public string customerId { get; set; }
        public string orderDate { get; set; }
        public string shipDate { get; set; }
        public List<SalesOrderDetails> details { get; set; }
    }
}