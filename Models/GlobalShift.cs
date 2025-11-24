using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Models
{
    public class GlobalShift
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        // Список ID сотрудников, назначенных на смену
        public List<int> EmployeeIds { get; set; } = new List<int>();

        // Навигационное свойство к индивидуальным сменам
        public List<WaiterShift> WaiterShifts { get; set; } = new List<WaiterShift>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public override string ToString()
        {
            return $"{Name} - {EmployeeIds.Count} сотрудников, {WaiterShifts.Count} активных смен";
        }
    }
}