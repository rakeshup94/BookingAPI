using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.App_Code
{
    public class RoomAvailabilityRequest
    {
        public string Response_Type;
        public long Country_ID;
        public long City_ID;
        public long Area_ID;
        public string Area_Name;
        public long HotelID;
        public string Hotel_Name;
        public long Nationality_CountryID;
        public long CurrencyID;
    }
}