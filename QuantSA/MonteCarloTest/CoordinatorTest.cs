﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonteCarlo;
using System.Collections.Generic;
using QuantSA;

namespace MonteCarloTest
{
    [TestClass]
    public class CoordinatorTest
    {
        [TestMethod]
        public void TestCoordinator1()
        {            
            Date exerciseDate = new Date(2017, 08, 28);
            String shareCode = "AAA";
            double strike = 100.0;
            Product p = new EuropeanOption(shareCode, strike, exerciseDate);

            Date valueDate = new Date(2016, 08, 28);
            double riskfreeRate = 0.07;
            double divYield = 0.02;
            double vol = 0.22;
            double spotPrice = 100.0;
            Simulator sim = new SimpleBlackEquity(valueDate, shareCode, spotPrice, vol, riskfreeRate, divYield);
            NumeraireSimulator numeraire = new DeterministicNumeraire(Currency.ZAR, valueDate, riskfreeRate);
            Coordinator coordinator = new Coordinator(numeraire, new List<Simulator> { sim }, 10000);
            double value = coordinator.Value(new List<Product> { p }, valueDate);
            double refValue = Formulae.BlackScholes(PutOrCall.Call, strike, (exerciseDate - valueDate) / 365, spotPrice,
                                                    vol, riskfreeRate, divYield);
            Assert.AreEqual(refValue, value, refValue*0.01);
        }
    }
}
