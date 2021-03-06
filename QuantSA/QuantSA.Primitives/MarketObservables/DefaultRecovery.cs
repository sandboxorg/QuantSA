﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantSA.General
{
    /// <summary>
    /// The recovery rate in a default event.  It is undefined until there is a default.  When it is undefined 
    /// set it to Double.NaN in the simulation, this will make sure that it is not inadvertently used.
    /// </summary>
    [Serializable]
    public class DefaultRecovery : MarketObservable
    {
        private ReferenceEntity refEntity;
        private string toString;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTime"/> class.
        /// </summary>
        /// <param name="refEntity">The reference entity for whom the default time will be monitored.</param>
        public DefaultRecovery(ReferenceEntity refEntity)
        {            
            this.refEntity = refEntity;
            toString = "DEFAULT:RECOVERYRATE:" + refEntity.ToString().ToUpper();
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.  Used to compare <see cref="MarketObservable"/>s.  
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return toString;
        }
    }
}
