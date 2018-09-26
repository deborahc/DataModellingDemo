using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModellingDemo
{
    public class ItemsOrdered
    {
        public int itemId { get; set; }
        public string itemName { get; set; }
        public string price { get; set; }
        public int qty { get; set; }
    }

    public class Order
    {
        public int orderId { get; set; }
        public int customerId { get; set; }
        public DateTime orderDate { get; set; }
        public List<ItemsOrdered> itemsOrdered { get; set; }
        public string type { get; set; }

    }
}