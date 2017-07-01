using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSiteUpdateChecker
{
    public class PrevState
    {
        public string Url { get; set; }
        public string Element { get; set; }
        public string RegexPattern { get; set; }
    }
}
