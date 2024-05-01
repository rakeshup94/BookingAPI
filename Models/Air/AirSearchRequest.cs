using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.Air
{
    public class AirSearchRequest
    {
        public string Trip
        {
            get;
            set;
        }
        public string Origin
        {
            get;
            set;
        }
        public string Destination
        {
            get;
            set;
        }
        public string DepartDate
        {
            get;
            set;
        }
        public string ReturnDate
        {
            get;
            set;
        }
        public int Adultcount
        {
            get;
            set;
        }
        public int Childcount
        {
            get;
            set;
        }
        public int Infantcount
        {
            get;
            set;
        }
        public string Class
        {
            get;
            set;
        }
    }
}