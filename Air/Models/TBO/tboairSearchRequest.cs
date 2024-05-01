using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Air.Models.TBO
{
    public class tboairSearchRequest
    {
        public tboairSearchRequest()
        {

        }
        public string IPAddress { get; set; }
        public string EndUserBrowserAgent { get; set; }
        public string PointOfSale { get; set; }
        public string RequestOrigin { get; set; }
        public string TokenId { get; set; }
        public int JourneyType { get; set; }
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public int InfantCount { get; set; }
        public int FlightCabinClass { get; set; }
        public List<fltSegment> Segment { get; set; }
    }
    public class fltSegment
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateTime PreferredDepartureTime { get; set; }
        public DateTime PreferredArrivalTime { get; set; }
    }
}