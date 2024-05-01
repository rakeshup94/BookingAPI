using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Xml.Linq;

/// <summary>
/// Summary description for helper
/// </summary>
/// 
namespace TravillioXMLOutService.Models
{
    public static class helper
    {
        public static DateTime HotelsDate(this string item)
        {

            DateTime dt = DateTime.ParseExact(item, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            return dt;
        }
        public static DateTime trvHotelsDate(this string item)
        {

            DateTime dt = DateTime.ParseExact(item, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            return dt;
        }
        public static DateTime trvHotelsDate2(this string item)
        {

            DateTime dt = DateTime.ParseExact(item, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            return dt;
        }
    }
}
