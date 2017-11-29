using System;

namespace FileSender.Dependencies
{
    public class Document
    {
        public Document(string name, byte[] content, DateTime created, string format)
        {
            Name = name;
            Created = created;
            Format = format;
            Content = content;
        }

        public string Name { get;  }
        public DateTime Created { get;  }
        public string Format { get;  }
        public byte[] Content { get;  }
    }
}