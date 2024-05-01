using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.Common
{
    public class RequestModel
    {


        public string HostName
        {
            get;
            set;
        }
        public string Method
        {
            get;
            set;
        }
        public string ContentType
        {
            get;
            set;
        }
        public string RequestStr
        {
            get;
            set;
        }

        public string Header
        {
            get;
            set;
        }

        public long  CustomerId
        {
            get;
            set;
        }

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

     

        public int ActionId
        {
            get;
            set;
        }
        public string Action
        {
            get;
            set;
        }




    }
}