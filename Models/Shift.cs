using System;
using System.Collections.Generic;

namespace MyApp.Models
{
    public class Shift
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public List<int> EmployeeIds { get; set; } = new List<int>();
        
        public override string ToString()
        {
            return Name;
        }
    }
}