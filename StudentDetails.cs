using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentFunction.Models
{
    public class StudentDetails
    {
        public int id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string department { get; set; }
        public string DOB { get; set; }
        public string Gender { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
