using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Supplier.TBOHolidays
{
    public class PolicyHelper
    {
        public long Index { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long NoShow { get; set; }
        public double Amount { get; set; }
        
    }
}