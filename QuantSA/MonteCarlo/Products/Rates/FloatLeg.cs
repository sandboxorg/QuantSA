﻿using MonteCarlo;
using QuantSA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantSA.MonteCarlo
{
    public class FloatLeg : Product
    {

        Date[] paymentDates;
        Date[] resetDates;
        MarketObservable[] floatingIndices;
        double[] spreads;
        double[] notionals;
        double[] accrualFractions;
        Currency ccy;
        
        Date valueDate;
        double[] indexValues;

        public FloatLeg(Currency ccy, Date[] paymentDates, double[] notionals, Date[] resetDates, MarketObservable[] floatingIndices, 
            double[] spreads, double[] accrualFractions)
        {
            this.ccy = ccy;
            this.paymentDates = paymentDates;
            this.notionals = notionals;
            this.resetDates = resetDates;
            this.floatingIndices = floatingIndices;
            this.spreads = spreads;
            this.accrualFractions = accrualFractions;
        }


        public override void SetValueDate(Date valueDate)
        {
            this.valueDate = valueDate;
        }

        public override void Reset()
        {
            indexValues = new double[resetDates.Length];
        }

        public override List<MarketObservable> GetRequiredIndices()
        {
            HashSet<MarketObservable> hashSet = new HashSet<MarketObservable>(floatingIndices);
            return new List<MarketObservable>(hashSet);            
        }

        public override List<Date> GetRequiredIndexDates(MarketObservable index)
        {
            List<Date> requiredDates = new List<Date>();
            for (int i = 0; i < paymentDates.Length; i++)
            {
                if (paymentDates[i] > valueDate)
                {
                    requiredDates.Add(resetDates[i]);
                }
            }
            return requiredDates;
        }

        public override void SetIndexValues(MarketObservable index, double[] indexValues)
        {
            int indexCounter = 0;
            for (int i = 0; i < paymentDates.Length; i++)
            {
                if (paymentDates[i] > valueDate)
                {
                    this.indexValues[i] = indexValues[indexCounter];
                    indexCounter++;
                }
            }
        }

        public override List<Cashflow> GetCFs()
        {
            List<Cashflow> cfs = new List<Cashflow>();
            for (int i = 0; i < paymentDates.Length; i++)
            {
                if (paymentDates[i] > valueDate)
                {
                    double floatingAmount = notionals[i] * accrualFractions[i] * (indexValues[i] + spreads[i]);
                    cfs.Add(new Cashflow(paymentDates[i], floatingAmount, ccy));
                }
            }
            return cfs;
        }

        public override List<Currency> GetCashflowCurrencies()
        {
            return new List<Currency> { ccy };
        }

        public override List<Date> GetCashflowDates(Currency ccy)
        {
            List<Date> dates = new List<Date>();
            for (int i = 0; i < paymentDates.Length; i++)
            {
                if (paymentDates[i] > valueDate) dates.Add(paymentDates[i]);
            }
            return dates;
        }
    }
}
