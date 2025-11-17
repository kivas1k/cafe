using System;
using System.Collections.Generic;

namespace MyApp.Models
{
    public class Shift
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public List<int> EmployeeIds { get; set; } = new List<int>();
    }
}