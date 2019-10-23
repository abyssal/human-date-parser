﻿using System;
using System.Collections.Generic;

namespace HumanDateParser
{
    internal class Parser
    {
        private readonly Dictionary<string, int> _months = new Dictionary<string, int>();

        private readonly TokenBuffer _tokens;
        private readonly DateTime _baseTime;

        public Parser(string text, DateTime relativeTo)
        {
            _baseTime = relativeTo;
            _months.Add("JAN", 1);
            _months.Add("FEB", 2);
            _months.Add("MAR", 3);
            _months.Add("APR", 4);
            _months.Add("MAY", 5);
            _months.Add("JUN", 6);
            _months.Add("JUL", 7);
            _months.Add("AUG", 8);
            _months.Add("SEPT", 9);
            _months.Add("OCT", 10);
            _months.Add("NOV", 11);
            _months.Add("DEC", 12);

            _months.Add("JANUARY", 1);
            _months.Add("FEBUARY", 2);
            _months.Add("MARCH", 3);
            _months.Add("APRIL", 4);
            _months.Add("JUNE", 6);
            _months.Add("JULY", 7);
            _months.Add("AUGUST", 8);
            _months.Add("SEPTEMBER", 9);
            _months.Add("OCTOBER", 10);
            _months.Add("NOVEMBER", 11);
            _months.Add("DECEMBER", 12);

            _tokens = new TokenBuffer(new Tokeniser(text));
        }

        public void ReadImpliedRelativeTimeSpan(ref DateTime baseTime, ParseToken numberValueToken, ParseToken specifierTypeToken)
        {
            var number = int.Parse(numberValueToken.Text);
            if (_tokens.ContainsKind(TokenKind.Ago)) number *= -1;
            baseTime = specifierTypeToken.Kind switch
            {
                TokenKind.Day => baseTime.AddDays(number),
                TokenKind.Month => baseTime.AddMonths(number),
                TokenKind.Week => baseTime.AddDays(7 * number),
                TokenKind.Year => baseTime.AddYears(number),
                TokenKind.Minute => baseTime.AddMinutes(number),
                TokenKind.Second => baseTime.AddSeconds(number),
                TokenKind.Hour => baseTime.AddHours(number),
                _ => throw new ParseException(ParseFailReason.InvalidUnit, $"Invalid unit following '{numberValueToken.Text}'.")
            };
        }

        public void ReadRelativeDateUnit(bool isFuture, ref DateTime baseTime, ParseToken specifierOrDowUnitToken)
        {
            switch (specifierOrDowUnitToken.Kind)
            {
                case TokenKind.Year:
                    baseTime = baseTime.AddYears(isFuture ? 1 : -1);
                    break;
                case TokenKind.Month:
                    baseTime = baseTime.AddMonths(isFuture ? 1 : -1);
                    break;
                case TokenKind.Week:
                    baseTime = baseTime.AddDays(isFuture ? 7 : -7);
                    break;
                case TokenKind.Day:
                    // this is literally 'last day/next day'
                    baseTime = baseTime.AddDays(isFuture ? 1 : -1);
                    break;
                case TokenKind.AbsoluteMonth:
                    var curMonth = baseTime.Month;
                    var newMonth = _months[specifierOrDowUnitToken.Text.ToUpper()];
                    if (isFuture)
                    {
                        if (curMonth == newMonth) baseTime = baseTime.AddYears(1);
                        else if (curMonth < newMonth) baseTime = baseTime.AddMonths(newMonth - curMonth);
                        else baseTime = baseTime.AddMonths(12 + newMonth - curMonth);
                    }
                    else
                    {
                        if (curMonth == newMonth) baseTime = baseTime.AddYears(-1);
                        else if (curMonth < newMonth) baseTime = baseTime.AddMonths(-12 + (newMonth - curMonth));
                        else baseTime = baseTime.AddMonths(-12 + newMonth - curMonth);
                    }
                    break;
                case TokenKind.AbsoluteDayOfWeek:
                    if (!Enum.TryParse<DayOfWeek>(specifierOrDowUnitToken.Text, true, out var day))
                        throw new ParseException(ParseFailReason.InvalidDayOfWeek, $"{specifierOrDowUnitToken.Text} is not a valid day of the week.");

                    var origBaseTimeDays = baseTime.Date.DayOfYear;
                    if (day == baseTime.DayOfWeek) baseTime = baseTime.AddDays(isFuture ? 7 : -7);
                    else
                    {
                        baseTime = baseTime.AddDays(isFuture ? 7 : -7);

                        if (day > baseTime.DayOfWeek)
                        {
                            baseTime = baseTime.AddDays(day - baseTime.DayOfWeek);
                        } else
                        {
                            baseTime = baseTime.AddDays(-1 * (baseTime.DayOfWeek - day));
                            if (isFuture && (baseTime.Date.DayOfYear - origBaseTimeDays) < 7) baseTime = baseTime.AddDays(7);
                        }
                    }
                    break;

                default:
                    throw new ParseException(ParseFailReason.InvalidUnit, $"'Last {specifierOrDowUnitToken.Text}' is not a valid relative date.");
            }
        }

        public static void ReadRelativeDayTime(ref DateTime date, ParseToken valueToken, ParseToken specifierToken)
        {
            int hours;
            switch (specifierToken.Kind)
            {
                case TokenKind.Am:
                    hours = int.Parse(valueToken.Text);
                    if (hours == 12) hours = 0; // 12 AM == 0000 hours
                    break;
                case TokenKind.Pm:
                    hours = int.Parse(valueToken.Text);
                    if (hours != 12) hours += 12; // 12 PM = 1200 hours 
                    break;
                // todo: Case TokenKind.Colon
                default:
                    throw new ParseException(ParseFailReason.InvalidUnit, $"Invalid unit {specifierToken.Text}.");
            }
            date = new DateTime(date.Year, date.Month, date.Day, hours, 00, 00);
        }

        public DetailedParseResult ParseDetailed()
        {
            var result = Parse();
            return new DetailedParseResult(result, _tokens.All());
        }

        public DateTime Parse()
        {
            var date = _baseTime;

            while (_tokens.MoveNext())
            {
                var currentToken = _tokens.Current;
                switch (currentToken.Kind)
                {
                    case TokenKind.In:
                        if (!_tokens.MoveNext() || _tokens.Current.Kind != TokenKind.Number) throw new ParseException(ParseFailReason.NumberExpected, "Expected a number to come after 'in'.");
                        var numToken = _tokens.Current;
                        if (!_tokens.MoveNext()) throw new ParseException(ParseFailReason.UnitExpected, $"Cannot have number without units following.");
                        ReadImpliedRelativeTimeSpan(ref date, numToken, _tokens.Current);
                        break;
                    case TokenKind.Number:
                        if (!_tokens.MoveNext()) throw new ParseException(ParseFailReason.UnitExpected, $"Cannot have number without units following.");
                        switch (_tokens.Current.Kind)
                        {
                            case TokenKind.Day:
                            case TokenKind.Hour:
                            case TokenKind.Minute:
                            case TokenKind.Second:
                            case TokenKind.Week:
                            case TokenKind.Year:
                            case TokenKind.Month:
                                ReadImpliedRelativeTimeSpan(ref date, currentToken, _tokens.Current);
                                break;
                            case TokenKind.Am:
                            case TokenKind.Pm:
                            case TokenKind.Colon:
                                ReadRelativeDayTime(ref date, currentToken, _tokens.Current);
                                break;

                        }
                        break;
                    case TokenKind.Last:
                        if (!_tokens.MoveNext()) throw new ParseException(ParseFailReason.UnitExpected, $"Cannot have 'last' without day of week, or specifier unit following.");
                        ReadRelativeDateUnit(false, ref date, _tokens.Current);
                        break;
                    case TokenKind.Today:
                        break;
                    case TokenKind.Tomorrow:
                        date = date.AddDays(1);
                        break;
                    case TokenKind.Yesterday:
                        date = date.AddDays(-1);
                        break;
                    case TokenKind.Next:
                        if (!_tokens.MoveNext()) throw new ParseException(ParseFailReason.UnitExpected, $"Cannot have 'next' without day of week, or specifier unit following.");
                        ReadRelativeDateUnit(true, ref date, _tokens.Current);
                        break;
                    case TokenKind.At:
                        if (!_tokens.MoveNext()) throw new ParseException(ParseFailReason.UnitExpected, $"Cannot have 'at' without a time following.");
                        var numericalValue = _tokens.Current;
                        if (!_tokens.MoveNext()) throw new ParseException(ParseFailReason.UnitExpected, $"Cannot have 'at {numericalValue.Text}' without a time unit following.");
                        ReadRelativeDayTime(ref date, numericalValue, _tokens.Current);
                        break;
                }
            }
            return date;
        }
        /*
        private DateTime EvaluateDateExpression()
        {
            var load = true;
            while (load)
            {
                var tokenCanMove = _tokens.MoveNext();
                if (!tokenCanMove)
                {
                    load = false;
                    break;
                }
                var token = _tokens.CurrentToken;
                switch (token.Kind)
                {
                    case TokenKind.Today:
                        return DateTime.Now.Date;
                    case TokenKind.Tomorrow:
                        return DateTime.Now.Date.AddDays(1);
                    case TokenKind.Yesterday:
                        return DateTime.Now.Date.AddDays(-1);
                    case TokenKind.DayAbsolute:
                        return DateTime.Now.Date.AddDays(ParseRelativeDay(token.Text));
                    case TokenKind.Number:
                        if (!_tokens.MoveNext()) throw new ParseException($"Unknown unit type for number '{token.Text}'");
                        var number = int.Parse(token.Text);
                        var unitType = _tokens.CurrentToken;
                        if (_tokens.PeekNext()!.Kind == TokenKind.Ago) number *= -1;
                        return unitType.Kind switch
                        {
                            TokenKind.DaySpecifier => DateTime.Now.AddDays(number),
                            TokenKind.MonthSpecifier => DateTime.Now.AddMonths(number),
                            TokenKind.WeekSpecifier => DateTime.Now.AddDays(7 * number),
                            TokenKind.YearSpecifier => DateTime.Now.AddYears(number),
                            // month relative somewhere here
                            _ => throw new ParseException($"Unknown unit type for number '{token.Text}'"),
                        };
                    case TokenKind.Next:
                        if (!_tokens.MoveNext()) throw new ParseException($"Cannot have 'next' without a following specifier.");
                        return _tokens.CurrentToken.Kind switch
                        {
                            TokenKind.WeekSpecifier => DateTime.Now.AddDays(7),
                            TokenKind.MonthSpecifier => DateTime.Now.AddMonths(1),
                            TokenKind.YearSpecifier => DateTime.Now.AddYears(1)
                        };
                    case TokenKind.Last:
                        if (!_tokens.MoveNext()) throw new ParseException($"Cannot have 'last' without a following specifier.");
                        return _tokens.CurrentToken.Kind switch
                        {
                            TokenKind.WeekSpecifier => DateTime.Now.AddDays(-7),
                            TokenKind.MonthSpecifier => DateTime.Now.AddMonths(-1),
                            TokenKind.YearSpecifier => DateTime.Now.AddYears(-1)
                        };
                }
            }

            //Get Or Time Year
            switch (Peek(1).Kind)
            {
                case TokenKind.Number:
                    Year();
                    Load();
                    if (Peek(1).Kind == TokenKind.At)
                        Time();
                    break;
                case TokenKind.At:
                    Time();
                    break;
            }

            Load();
        }

        private void Time()
        {
            Load();
            if (Peek(1).Kind == TokenKind.Number)
            {
                var hourPart = int.Parse(Peek(1).Text);
                var minPart = 0;
                Load();

                if (Peek(1).Kind == TokenKind.Colon)
                {
                    Load();
                    if(Peek(1).Kind == TokenKind.Number)
                    {
                        minPart = int.Parse(Peek(1).Text);
                        Load();
                    }
                    else
                    {
                        Errors.Add("Minute time part required after ':' keyword");
                        return; 
                    }
                }

                if (Peek(1).Kind == TokenKind.TimeRelative && Peek(1).Text == "PM" && hourPart <= 12) hourPart = hourPart + 12;


                _dateRange.CurrentDate = _dateRange.CurrentDate.SetTime(hourPart, minPart);
            }
            else
            {
                Errors.Add("Time required after 'At' keyword");
                return;
            }
        }

        private void Year()
        {
            _dateRange.CurrentDate = (new DateTime(int.Parse(Peek(1).Text), _dateRange.CurrentDate.Month, _dateRange.CurrentDate.Day));
        }

        private void MonthDateIdent(int num)
        {
            if (Peek(1).Kind == TokenKind.MonthAbsolute)
            {
                _dateRange.AddDate(new DateTime(DateTime.Now.Year, _months[Peek(1).Text], num));
                Load();
            }
            else
                _dateRange.AddDate(new DateTime(DateTime.Now.Year, DateTime.Now.Month, num));
        }

        private static int ParseRelativeDay(string dayString)
        {
            try
            {
                var dayOfWeek = Enum.Parse<DayOfWeek>(dayString, true);

                for (var i = 0; i < 7; i++)
                {
                    if (Today.AddDays(i).DayOfWeek == dayOfWeek)
                    {
                        return i;
                    }
                }
                throw new ParseException($"Unable to parse day '{dayString}'.");
            } catch (ArgumentException)
            {
                throw new ParseException($"Unable to parse day '{dayString}'.");
            }
        }

        private void NumQuickDateIdent(int num)
        {
            if (Peek(2).Kind == TokenKind.Ago) num = num * -1;
            switch (Peek(1).Kind)
            {
                case TokenKind.DaySpecifier:
                    _dateRange.AddDate(Today.AddDays(num));
                    Load();
                    break;
                case TokenKind.WeekSpecifier:
                    _dateRange.AddDate(Today.AddDays(num * 7));
                    Load();
                    break;
                case TokenKind.MonthSpecifier:
                    _dateRange.AddDate(Today.AddMonths(num));
                    Load();
                    break;
                case TokenKind.YearSpecifier:
                    _dateRange.AddDate(Today.AddYears(num));
                    Load();
                    break;
            }
        }*/
    }
}