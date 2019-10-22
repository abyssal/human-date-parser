﻿using System;
using System.Collections.Generic;
using System.Text;

namespace HumanDateParser
{
#pragma warning disable CA1032
    /// <summary>
    ///     Represents a generic failure in parsing.
    /// </summary>
    public class ParseException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
    {
        /// <summary>
        ///     Gets the fail reason.
        /// </summary>
        public ParseFailReason FailReason { get; }

        internal ParseException(ParseFailReason failReason, string message) : base(message)
        {
            FailReason = failReason;
        }
    }

    /// <summary>
    ///     Represents a reason as to why parsing failed.
    /// </summary>
    public enum ParseFailReason
    {
        /// <summary>
        ///     The unit provided was either unknown, or not suitable for the context.
        /// </summary>
        InvalidUnit,

        /// <summary>
        ///     The provided word is not a day of the week in the Gregorian calendar.
        /// </summary>
        InvalidDayOfWeek,

        /// <summary>
        ///     A time unit was expected, but none was provided.
        /// </summary>
        UnitExpected,

        /// <summary>
        ///     A number was expected, but none was provided.
        /// </summary>
        NumberExpected
    }
}
