﻿namespace HumanDateParser.Tokenisation.Tokens
{
    internal class TimeUnitToken : IParseToken
    {
        public TimeUnit Unit { get; }

        internal TimeUnitToken(TimeUnit unit)
        {
            Unit = unit;
        }
    }

    internal enum TimeUnit
    {
        Millisecond,
        Second,
        Minute,
        Hour,
        Day,
        Week,
        Month,
        Year
    }
}
