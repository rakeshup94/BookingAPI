using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TravillioXMLOutService.App_Code;

namespace TravillioXMLOutService.App_Code
{
    public class SearchHotelRequest
    {
        public string Response_Type;
        public long Country_ID;
        public long City_ID;
        public long Area_ID;
        public string Area_Name;
        public string From_Date;
        public string To_Date;
        public long Min_StarRating;
        public long Max_StarRating;
        public string Hotel_Name;

        //public long Adult_Pax;
        //public long Child_Pax;

        //public string[] Children_Ages;


        

        public long Nationality_CountryID;
        public long CurrencyID;
        public long LanguageID;

        

        




    }


}