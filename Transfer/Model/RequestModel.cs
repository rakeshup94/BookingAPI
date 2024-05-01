using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Transfer.Model
{
    public class RequestModel
    {
        public long Customer
        {
            get;
            set;
        }
        public string TrackNo
        {
            get;
            set;
        }
        public string Action
        {
            get;
            set;
        }
        public long ActionId
        {
            get;
            set;
        }
        public string RequestStr
        {
            get;
            set;
        }
        public string ResponseStr
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