using System.Collections.Generic;

namespace CosmicWorks.Models
{
    public class CustomerV4 : IEntity
    {
        public string id { get; set; }
        public string type { get; set; }
        public string customerId { get; set; }
        public string title { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string emailAddress { get; set; }
        public string phoneNumber { get; set; }
        public string creationDate { get; set; }
        public List<CustomerAddress> addresses { get; set; }
        public Password password { get; set; }
        public int salesOrderCount { get; set; }
    }
}