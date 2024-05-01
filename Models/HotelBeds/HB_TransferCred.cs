using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.HotelBeds
{
    public class HB_TransferCred
    {
        public string username
        {
            get
            {
                //Holidays Arabia Test (frontend)
                //return "TESTCHAINS";
                // (frontend) LIVE
                return "HOLIDAYARAKW67543";
            }
        }
        public string password
        {
            get
            {
                //Holidays Arabia Test (frontend)
                //return "TESTCHAINS";
                // (frontend) LIVE
                return "HOLIDAYARAKW67543";
            }
        }
        public string SupplierID
        {
            get
            {
                return "10";
            }
        }
        public string CustomerID
        {
            get;
            set;
        }
    }
}