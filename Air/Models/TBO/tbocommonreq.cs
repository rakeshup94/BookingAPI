using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Air.Models.TBO
{
    public class tbocommonreq
    {
        public tbocommonreq()
        {

        }
        public string IPAddress { get; set; }
        public string EndUserBrowserAgent { get; set; }
        public string PointOfSale { get; set; }
        public string RequestOrigin { get; set; }
        public string TokenId { get; set; }        
        public string ResultId { get; set; }
        public string TrackingId { get; set; }
    }
}