using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace TravillioXMLOutService.Models
{
    [Serializable]
    public class CustomException : Exception
    {
        public CustomException(Exception innerException)
          {
              MsgName = innerException.Message.ToString();
              exctype = innerException.GetType().Name.ToString();
              ExcSource = innerException.StackTrace.ToString();              
        }
        public CustomException()
            : base() { }

        public CustomException(string message)
            : base(message) { }

        public CustomException(string format, params object[] args)
            : base(string.Format(format, args)) { }

        public CustomException(string message, Exception innerException)
            : base(message, innerException) { }

        public CustomException(string format, Exception innerException, params object[] args)
            : base(string.Format(format, args), innerException) { }

        protected CustomException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        private string mName;
        private string pName;
        private string TrID;
        private string cusID;
        private string msg;
        private string exctype;
        private string src;
        // ...
        public string CustomerID
        {
            get { return this.cusID; }
            set { this.cusID = value; }
        }
        public string MethodName
        {
            get { return this.mName; }
            set { this.mName = value; }
        }
        public string TranID
        {
            get { return this.TrID; }
            set { this.TrID = value; }
        }
        public string PageName
        {
            get { return this.pName; }
            set { this.pName = value; }
        }
        public string MsgName
        {
            get { return this.msg; }
            set { this.msg = value; }
        }
        public string ExcType
        {
            get { return this.exctype; }
            set { this.exctype = value; }
        }
        public string ExcSource
        {
            get { return this.src; }
            set { this.src = value; }
        }
    }
}