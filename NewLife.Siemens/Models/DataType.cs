using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Siemens.Models
{
    /// <summary>
    /// Types of memory area that can be read
    /// </summary>
    public enum DataType
    {
        /// <summary>
        /// Input area memory
        /// </summary>
        Input = 129,

        /// <summary>
        /// Output area memory
        /// </summary>
        Output = 130,

        /// <summary>
        /// Merkers area memory (M0, M0.0, ...)
        /// </summary>
        Memory = 131,

        /// <summary>
        /// DB area memory (DB1, DB2, ...)
        /// </summary>
        DataBlock = 132,

        /// <summary>
        /// Timer area memory(T1, T2, ...)
        /// </summary>
        Timer = 29,

        /// <summary>
        /// Counter area memory (C1, C2, ...)
        /// </summary>
        Counter = 28
    }
}