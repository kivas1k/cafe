using System;
using Avalonia.Media;
using MyApp.Models;

namespace MyApp.Views
{
    public class ShiftDisplayItem
    {
        private readonly Shift _shift;

        public ShiftDisplayItem(Shift shift)
        {
            _shift = shift;
        }

        public Shift Shift => _shift;
        public int Id => _shift.Id;
        public string Name => _shift.Name;
        public DateTime Date => _shift.Date;
        public int EmployeeCount => _shift.EmployeeIds.Count;

        public string Status
        {
            get
            {
                if (EmployeeCount == 0)
                    return "❌ Нет сотрудников";
                else if (EmployeeCount < 4)
                    return $"⚠️ Неполный штат ({EmployeeCount}/4)";
                else if (EmployeeCount >= 4 && EmployeeCount <= 7)
                    return $"✅ Укомплектована ({EmployeeCount}/7)";
                else
                    return "❌ Переполнена";
            }
        }

        public IBrush StatusColor
        {
            get
            {
                if (EmployeeCount == 0)
                    return Brushes.Red;
                else if (EmployeeCount < 4)
                    return Brushes.Orange;
                else if (EmployeeCount >= 4 && EmployeeCount <= 7)
                    return Brushes.Green;
                else
                    return Brushes.Red;
            }
        }
    }
}