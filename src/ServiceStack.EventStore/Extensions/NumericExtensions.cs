﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.EventStore.Extensions
{
    /// <summary>
    /// Handy helpers to make simple mathematical operations read more like prose.
    /// </summary>
    public static class NumericExtensions
    {
        public static int Add(this int firstAddend, int secondAddend) => firstAddend + secondAddend;

        public static int Subtract(this int minuend, int subtrahend) => minuend - subtrahend;

        public static int MultiplyBy(this int multiplicand, int multiplier) => multiplicand * multiplier;

        public static int DivideBy(this int dividend, int divisor) => dividend / divisor;
    }
}
