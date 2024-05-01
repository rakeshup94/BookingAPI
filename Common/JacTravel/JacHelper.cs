using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Common.JacTravel
{
    public static class JacHelper
    {
        public static string DtFormat(string date)
        {

            string arr = DateTime.ParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture).ToString("dd/MM/yyyy");
            return arr;
        }

        public static string MyDate(this string strDate)
        {
            string dtStartDate = DateTime.ParseExact(strDate, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

            return dtStartDate;

        }

        public static string GetDuration(string todt, string fromdt, out int i)
        {
            DateTime Fromdate = DateTime.ParseExact(fromdt, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            DateTime Todate = DateTime.ParseExact(todt, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            string Duration = Convert.ToString((Todate - Fromdate).TotalDays);
            i = Convert.ToInt16(Duration);
            return Duration;
        }

        public static XElement BindChild(IEnumerable<XElement> Child)
        {
            List<XElement> lst = new List<XElement>();
            XElement childage = null;
            foreach (XElement item in Child)
            {


                int age = Convert.ToInt16(item.Value);
                if (age > 2 && age < 18)
                {
                    lst.Add(new XElement("ChildAge", new XElement("Age", age.ToString())));
                }


            }
            if (lst.Count > 0)
            {
                childage = new XElement("ChildAges", lst);
            }
            return childage;

        }


        public static int GetInfantsCount(IEnumerable<XElement> Child)
        {
            int count = 0;
            foreach (XElement item1 in Child)
            {
                int age = Convert.ToInt16(item1.Value);
                if (age < 3)
                {
                    count++;
                }
            }
            return count;
        }


        public static int GetChildCount(IEnumerable<XElement> Child)
        {
            int count = 0;
            foreach (XElement item1 in Child)
            {
                int age = Convert.ToInt16(item1.Value);
                if (age > 2 && age < 18)
                {
                    count++;
                }
            }
            return count;
        }

        public static List<XElement> PriceBreakup(decimal PerN8Price, int totalN8)
        {
            List<XElement> Breakup = new List<XElement>();
            for (int i = 0; i < totalN8; i++)
            {
                XElement ele = new XElement("Price",
                    new XAttribute("Night", i + 1),
                    new XAttribute("PriceValue", PerN8Price));
                Breakup.Add(ele);
            }
            return Breakup;
        }

       public static List<XElement> PreiceBkp(string totalrate, int Duration)
        {
            List<XElement> PriBkp = new List<XElement>();
            double preice = Convert.ToDouble(totalrate);


            preice = preice / Duration;

            int cout = 0;
            for (int i = 0; i < Duration; i++)
            {
                cout = i + 1;
                PriBkp.Add(new XElement("Price",
                            new XAttribute("Night", cout.ToString()),
                             new XAttribute("PriceValue", Math.Round(preice, 2).ToString())));
            }
            return PriBkp;
        }


       public static string PerNightPrice(string totalrate, int Duration)
        {
            double preice = Convert.ToDouble(totalrate);
            preice = preice / Duration;
            return Math.Round(preice, 2).ToString();
        }

       public static string MinRate(IEnumerable<XElement> rmlst, int duration)
        {

            foreach (XElement item in rmlst.Elements("RoomType"))
            {
                double preice = 0;
                if (Convert.ToDouble(item.Element("Total").Value) < preice || preice == 0)
                {
                    preice = Convert.ToDouble(item.Element("Total").Value);
                }

                return Math.Round(preice, 2).ToString();
            }
            return string.Empty;
        }
    }
}