using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.DotW
{
    public class LogModel
    {

        
        public string TrackNo
        {
            get;
            set;
        }
        public int SupplierId
        {
            get;
            set;
        }
        public DateTime StartTime
        {
            get;
            set;
        }

        public DateTime EndTime
        {
            get;
            set;
        }
    }
}