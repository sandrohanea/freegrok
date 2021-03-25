using System;
using System.Collections.Generic;

namespace FreeGrok.Common.Dtos
{
    public class ResponseDto
    {
        public Guid RequestId { get; set; }

        public int StatusCode { get; set; }

        public bool HaveContent { get; set; }

        public List<HeaderDto> Headers { get; set; }
    }
}
