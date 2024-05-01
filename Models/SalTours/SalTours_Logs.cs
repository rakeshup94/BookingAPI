using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.SalTours
{
    public class SalTours_Logs
    {
        public long CustomerID
        {
            get;
            set;
        }
        public string TrackNo
        {
            get;
            set;
        }
        public string Logtype
        {
            get;
            set;
        }
        public long LogtypeID
        {
            get;
            set;
        }
    }
}