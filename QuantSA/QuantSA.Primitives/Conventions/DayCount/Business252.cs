﻿using QuantSA.General.Dates;
using QuantSA.Primitives.Dates;

namespace QuantSA.General.Conventions.DayCount
{
    public class Business252 : DayCountConvention
    {
        private readonly Calendar calendar;

        public Business252(Calendar calendar)
        {
            this.calendar = calendar;
        }

        public double YearFraction(Date date1, Date date2)
        {
            var busDays = calendar.businessDaysBetween(date1, date2);
            return busDays / 252.0;
        }
    }
}