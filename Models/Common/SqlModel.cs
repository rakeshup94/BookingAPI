using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.Common
{
    public class SqlModel
    {
        public int flag 
        {
            get;
            set;
        }
        public string columnList
        {
            get;
            set;
        }
        public string table
        {
            get;
            set;
        }
        public string filter
        {
            get;
            set;
        }

        public string HotelCode
        {
            get;
            set;
        }

        public int SupplierId
        {
            get;
            set;
        }
        
    }
}