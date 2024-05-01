using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;
using System.Xml.Linq;

namespace TravillioXMLOutService.Common.Travco
{
    public static class TravcoHelper
    {
        public static DateTime StringToDate(this string strDate)
        {
            if (!string.IsNullOrEmpty(strDate))
            {
               DateTime dte = DateTime.ParseExact(strDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
               return dte.Date;

            }
            else
            {
                return DateTime.MinValue;
            }
           
        }
        public static decimal ModifyToDecimal(this string str)
        {
            decimal num = 0;
            if (!string.IsNullOrEmpty(str))
            {
                num = Convert.ToDecimal(str);
            }
            return num;

        }
        public static int ModifyToInt(this string str)
        {
            int num = 0;
            if (!string.IsNullOrEmpty(str))
            {
                num = int.Parse(str);
            }
            return num;

        }
        public static DateTime ConvertToDate(this string strDate)
        {
            if (!string.IsNullOrEmpty(strDate))
            {
                DateTime dt = DateTime.ParseExact(strDate.Trim(), "dd-MMM-yyyy", CultureInfo.InvariantCulture);
                return dt;
            }
            else
            {
                return DateTime.MinValue;
            }
        }
        public static long ConvertToLong(this string str)
        {
            long num = 0;
            if (!string.IsNullOrEmpty(str))
            {
                num = long.Parse(str);
            }
            return num;

        }
        public static DateTime TravcoToLocalDate(this DateTime travcodate)
        {
            //TimeSpan span = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time").GetUtcOffset(travcodate);
            //DateTimeOffset travcoffset = new DateTimeOffset(travcodate.Ticks, span);
            DateTimeOffset travcoffset = new DateTimeOffset(travcodate.Ticks, new TimeSpan(-1, 0, 0));
            DateTime localdate=travcoffset.LocalDateTime;
            return localdate;
            
        }
        public static XElement getHotelFacilities(this string facilityStr)
        {
            if (string.IsNullOrEmpty(facilityStr))
            {
                return new XElement("Facilities");
            }
            else
            {
                XElement facilities = XElement.Parse(facilityStr);
                return facilities;

            }
        }
        public static XElement getHotelImages(this string imagesStr)
        {
            if (string.IsNullOrEmpty(imagesStr))
            {
                return new XElement("Images");
            }
            else
            {
                XElement images = XElement.Parse(imagesStr);
                return images;

            }
        }
    }
}