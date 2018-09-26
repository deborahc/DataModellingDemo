using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModellingDemo
{
    public class Person
    {
        public int customerId { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string number { get; set; }
        public string address { get; set; }
        public string type { get; set; }
        public Company company { get; set; }
    }


    public class Company
    {
        public int companyId { get; set; }
        public string companyName { get; set; }
        public string companyAddress { get; set; }
    }
}
