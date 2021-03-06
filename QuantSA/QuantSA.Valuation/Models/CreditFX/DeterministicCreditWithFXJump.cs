﻿using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math.Random;
using Accord.Statistics.Distributions.Univariate;
using QuantSA.General;
using QuantSA.Primitives.Dates;

namespace QuantSA.Valuation
{
    /// <summary>
    /// Provides an FX process and the default event for a single name.
    /// <para/>
    /// Regression variables are FX, defaultedFlag (0,1), 1 year default probability
    /// </summary>
    /// <seealso cref="QuantSA.Valuation.NumeraireSimulator" />
    [Serializable]
    public class DeterministicCreditWithFXJump : NumeraireSimulator
    {
        // The simulations
        private List<Date> allRequiredDates; // the set of all dates that will be simulated.
        private readonly Date anchorDate;
        private readonly MarketObservable currencyPair;
        private readonly MarketObservable defaultRecovery;
        private readonly MarketObservable defaultTime;
        private readonly IFXSource fxSource;
        private readonly double fxVol;
        private readonly double relJumpSizeInDefault;
        private double simDefaultTime;
        private readonly double simRecoveryRate;
        private Dictionary<int, double> simulation; // stores the simulated share prices at each required date
        private readonly double spot;

        private readonly ISurvivalProbabilitySource survivalProbSource;
        private readonly Currency valueCurrency;
        private readonly IDiscountingSource valueCurrencyDiscount;


        /// <summary>
        /// Initializes a new instance of the <see cref="DeterministicCreditWithFXJump"/> class.
        /// </summary>
        /// <param name="survivalProbSource">A curve that provides survival probabilities.  Usually a hazard curve.</param>
        /// <param name="otherCurrency">The other currency required in the simulation.  The valuation currency will 
        /// be inferred from the <paramref name="valueCurrencyDiscount"/>.  This value needs to be explicitly set
        /// since <paramref name="fxSource"/> may provide multiple pairs.</param>
        /// <param name="fxSource">The source FX spot and forwards.</param>
        /// <param name="valueCurrencyDiscount">The value currency discount curve.</param>
        /// <param name="fxVol">The fx volatility.</param>
        /// <param name="relJumpSizeInDefault">The relative jump size in default.  For example if the value currency is ZAR and the 
        /// other currency is USD then the fx is modelled as ZAR per USD and in default the fx rate will change to:
        /// rate before default * (1 + relJumpSizeInDefault).</param>
        /// <param name="expectedRecoveryRate">The constant recovery rate that will be assumed to apply in default.</param>
        public DeterministicCreditWithFXJump(ISurvivalProbabilitySource survivalProbSource,
            Currency otherCurrency, IFXSource fxSource, IDiscountingSource valueCurrencyDiscount,
            double fxVol, double relJumpSizeInDefault, double expectedRecoveryRate)
        {
            this.survivalProbSource = survivalProbSource;
            valueCurrency = valueCurrencyDiscount.GetCurrency();
            this.fxSource = fxSource;
            this.valueCurrencyDiscount = valueCurrencyDiscount;
            this.fxVol = fxVol;
            this.relJumpSizeInDefault = relJumpSizeInDefault;
            var refEntity = survivalProbSource.GetReferenceEntity();
            defaultTime = new DefaultTime(refEntity);
            defaultRecovery = new DefaultRecovery(refEntity);
            currencyPair = new CurrencyPair(otherCurrency, valueCurrency);
            anchorDate = valueCurrencyDiscount.GetAnchorDate();
            spot = fxSource.GetRate(anchorDate);
            simRecoveryRate = expectedRecoveryRate;
        }

        /// <summary>
        /// Gets the indices.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="requiredTimes">The required times.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// defaultTime must only be queried with a single date.
        /// or
        /// defaultRecovery must only be queried with a single date.
        /// or
        /// </exception>
        public override double[] GetIndices(MarketObservable index, List<Date> requiredTimes)
        {
            if (index == currencyPair)
            {
                var result = new double[requiredTimes.Count];
                for (var i = 0; i < requiredTimes.Count; i++)
                    if (requiredTimes[i] <= anchorDate)
                        result[i] = fxSource.GetRate(requiredTimes[i]);
                    else
                        result[i] = simulation[requiredTimes[i]];
                return result;
            }

            if (index == defaultTime)
            {
                if (requiredTimes.Count > 1)
                    throw new ArgumentException("defaultTime must only be queried with a single date.");
                return new[] {simDefaultTime};
            }

            if (index == defaultRecovery)
            {
                if (requiredTimes.Count > 1)
                    throw new ArgumentException("defaultRecovery must only be queried with a single date.");
                return new[] {simRecoveryRate};
            }

            throw new ArgumentException(index + " is not simulated by this model.");
        }

        /// <summary>
        /// Indicate whether the required share price is simulated by this model
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override bool ProvidesIndex(MarketObservable index)
        {
            if (index == currencyPair)
                return true;
            if (index == defaultTime)
                return true;
            if (index == defaultRecovery)
                return true;
            return false;
        }

        /// <summary>
        /// Clear the dates that have been set
        /// </summary>
        public override void Reset()
        {
            allRequiredDates = new List<Date>();
        }


        /// <summary>
        /// Remove duplicate dates and sort the list
        /// </summary>
        public override void Prepare()
        {
            allRequiredDates = allRequiredDates.Distinct().ToList();
            allRequiredDates.Sort();
        }

        /// <summary>
        /// Run a simulation and store the results for later use by <see cref="GetIndices(MarketObservable, List{Date})"/>
        /// </summary>
        /// <param name="simNumber"></param>
        public override void RunSimulation(int simNumber)
        {
            simulation = new Dictionary<int, double>();
            var simRate = spot;
            var oldFxFwd = spot;
            double newFXfwd;

            // Simulate the default
            var normal = new NormalDistribution();
            var uniform = new UniformContinuousDistribution();
            var hazEst = survivalProbSource.GetSP(survivalProbSource.getAnchorDate().AddTenor(Tenor.Years(1)));
            hazEst = -Math.Log(hazEst);
            Generator.Seed =
                -533776581 * simNumber; // This magic number is: "DeterministicCreditWithFXJump".GetHashCode();
            var tau = uniform.Generate();
            tau = Math.Log(tau) / -hazEst;
            simDefaultTime = anchorDate.value + tau * 365;

            for (var timeCounter = 0; timeCounter < allRequiredDates.Count; timeCounter++)
            {
                double dt = timeCounter > 0
                    ? allRequiredDates[timeCounter] - allRequiredDates[timeCounter - 1]
                    : allRequiredDates[timeCounter] - anchorDate.value;
                newFXfwd = fxSource.GetRate(new Date(anchorDate.value + dt));

                dt = dt / 365.0;
                var sdt = Math.Sqrt(dt);
                var dW = normal.Generate();
                // TODO: drift needs to be adjusted for default rate * jump size
                simRate = simRate * newFXfwd / oldFxFwd * Math.Exp(-0.5 * fxVol * fxVol * dt + fxVol * sdt * dW);
                if (simDefaultTime < allRequiredDates[timeCounter])
                    simulation[allRequiredDates[timeCounter]] = simRate * (1 + relJumpSizeInDefault);
                else
                    simulation[allRequiredDates[timeCounter]] = simRate;
            }
        }

        public override void SetRequiredDates(MarketObservable index, List<Date> requiredDates)
        {
            allRequiredDates.AddRange(requiredDates);
        }

        public override Currency GetNumeraireCurrency()
        {
            return valueCurrency;
        }

        public override double Numeraire(Date valueDate)
        {
            return 1.0 / valueCurrencyDiscount.GetDF(valueDate);
        }

        public override void SetNumeraireDates(List<Date> requiredDates)
        {
            // Nothing needs to be done since we are using a deterministic curve.
        }

        public override double[] GetUnderlyingFactors(Date date)
        {
            var regressors = new double[3];
            var fxRate = GetIndices(currencyPair, new List<Date> {date})[0];
            var defaultIndicator = date < simDefaultTime ? 0.0 : 1.0;
            var fwdDefaultP = 1.0 - survivalProbSource.GetSP(date.AddTenor(Tenor.Years(1))) /
                              survivalProbSource.GetSP(date);
            regressors[0] = fxRate;
            regressors[1] = defaultIndicator;
            regressors[2] = fwdDefaultP;
            return regressors;
        }
    }
}