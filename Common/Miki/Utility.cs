using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Common.Miki
{
    public  class Utility
    {
        //public  string MikiDate(this string strDate)
        //{
        //    string dtStartDate = DateTime.ParseExact(strDate, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

        //    return dtStartDate;

        //}
        public XElement getGuests(XElement req)
        {
            XElement guests = new XElement("guests");

            int adultnum = Convert.ToInt32(req.Descendants("Adult").FirstOrDefault().Value);
            int childnum = req.Descendants("Child").Any() ? Convert.ToInt32(req.Descendants("Child").FirstOrDefault().Value) : 0;
            int i = 0;

            if (adultnum > 0)
            {
                for (i = 0; i < adultnum; i++)
                {
                    var adults = new XElement("guest",
                                      new XElement("type", "ADT"));


                    guests.Add(adults);
                }
            }
            if (childnum > 0)
            {

                for (i = 0; i < childnum; i++)
                {
                    var children =
                          new XElement("guest",
                                          new XElement("type", "CHD"),
                                          new XElement("age", req.Element("ChildAge").Value));
                    guests.Add(children);

                }
            }
            return guests;
        }
        public XElement starRating(XElement req)
        {
            int minRating = Convert.ToInt32(req.Descendants("MinStarRating").FirstOrDefault().Value);
            int max = Convert.ToInt32(req.Descendants("MaxStarRating").FirstOrDefault().Value);
            if (minRating == 0 && max == 0)
            {
                minRating = 1;
                max = 5;
                var root = new XElement("starRatings");
                for (int i = minRating; i <= max; i++)
                {
                    if (i > 0)
                    {
                        var rating = new XElement("starRating", Convert.ToString(i));
                        root.Add(rating);
                    }
                }
                return root;
            }
            else
            {
                if (minRating < 1)
                    minRating = 1;
                if (max < minRating)
                    max = minRating;
                var root = new XElement("starRatings");
                for (int i = minRating; i <= max; i++)
                {
                    if (i > 0)
                    {
                        var rating = new XElement("starRating", Convert.ToString(i));
                        root.Add(rating);
                    }
                }
                return root;
            }
        }
       
    }
}