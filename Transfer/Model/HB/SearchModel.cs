using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravillioXMLOutService.Transfer.Models.HB
{
    public class SearchModel : BaseModel
    {
        public string ftype { get; set; }
        public string fcode { get; set; }
        public string ttype { get; set; }
        public string tcode { get; set; }
        public string departing { get; set; }
        public string comeback { get; set; }
        public int adults { get; set; }
        public int children { get; set; }
        public int infants { get; set; }
    }

    public abstract class BaseModel
    {
        public string BaseAddress { get; set; }
        public string RequestContent { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }
        public string language { get; set; }
    }

}
