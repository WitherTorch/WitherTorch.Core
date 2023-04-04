using System.Net.Http.Headers;

namespace WitherTorch.Core.Utils
{
    internal class MIMETypes
    {
        public readonly static MediaTypeWithQualityHeaderValue JSON = new MediaTypeWithQualityHeaderValue("application/json");
        public readonly static MediaTypeWithQualityHeaderValue XML = new MediaTypeWithQualityHeaderValue("application/xml");
    }
}
