using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WitherTorch.Core.Utils
{
    internal class MIMETypes
    {
        public readonly static MediaTypeWithQualityHeaderValue JSON = new MediaTypeWithQualityHeaderValue("application/json");
        public readonly static MediaTypeWithQualityHeaderValue XML = new MediaTypeWithQualityHeaderValue("application/xml");
    }
}
