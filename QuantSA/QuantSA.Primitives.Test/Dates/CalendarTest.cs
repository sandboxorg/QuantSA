﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantSA.Primitives.Dates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantSA.General.Dates;

namespace GeneralTest.Dates
{
    [TestClass]
    public class CalendarTest
    {
        [TestMethod]
        public void TestCalendarFromFile()
        {
            Calendar calendar = Calendar.FromFile("./TestData/TestCalendar.csv");
            
        }

    }
}
