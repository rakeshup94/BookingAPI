using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.Common
{
    public class ExceptionModel
    {
        public long CustomerId
        {
            get;
            set;
        }
        public string TrackNo
        {
            get;
            set;
        }
        public string Message
        {
            get;
            set;
        }
        public string Source
        {
            get;
            set;
        }
        public string Detail
        {
            get;
            set;
        }
        public string FileName
        {
            get;
            set;
        }
        public string Method
        {
            get;
            set;
        }

    }
}