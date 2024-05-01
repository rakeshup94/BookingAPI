using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace TravillioXMLOutService.Models
{
    public class VerifySearchHotelRequest
    {
        public VerifySearchHotelRequest()
        {
            
        }
        public bool VerifySearchHotelRequestXML(XmlDocument xmlrequest)
        {
            var input = "";
            if (CheckHowItWorks(input))
            {
                return false;
            }


            return false;
        }
        public static bool CheckHowItWorks(string input)
        {
            return Regex.IsMatch(input, "<HowItWorks>(.*)</HowItWorks>");
        }
    }
}