using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models
{
    public class IssueType
    {
        public string Id { get; set; }
        public string Description { get; set; }

        public string Severity { get; set; }

        public string WikiUrl { get; set; }

        public CategoryType Category { get; set; }

        public class CategoryType
        {
            public string Id { get; set; }
            public string Description { get; set; }
        }
    }
}
