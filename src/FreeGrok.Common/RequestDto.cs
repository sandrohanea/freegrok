using System;
using System.Collections.Generic;

namespace FreeGrok.Common
{
    public class RequestDto
    {
        public Guid RequestId { get; set; }

        public string Method { get; set; }
        public string Path { get; set; }

        public byte[] Content { get; set; }
        public List<HeaderDto> Headers { get; set; }
    }
}
