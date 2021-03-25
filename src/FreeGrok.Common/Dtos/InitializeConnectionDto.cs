using System;
using System.Collections.Generic;

namespace FreeGrok.Common.Dtos
{
    public class InitializeConnectionDto
    {
        public Guid RequestId { get; set; }

        public string Method { get; set; }
        public string Path { get; set; }

        public long? BodyLength { get; set; }

        public bool HaveContent { get; set; }

        public List<HeaderDto> Headers { get; set; }
    }
}
