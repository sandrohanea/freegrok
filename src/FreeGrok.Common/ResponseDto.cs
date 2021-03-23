using System;
using System.Collections.Generic;

namespace FreeGrok.Common
{
    public class ResponseDto
    {
        public Guid RequestId { get; set; }

        public byte[] Content { get; set; }
        public int StatusCode { get; set; }

        public List<HeaderDto> Headers { get; set; }
    }
}
