﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.EventStore.Extensions
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Handy helpers for working with IEnumerable
    /// </summary>
    internal static class IEnumerableExtensions
    {
        /// <summary>
        /// Can be used instead of .Any() which can take longer than .Count > 0 in some situations.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns>Boolean value indicating whether the enumerable contains any elements</returns>
        public static bool HasAny<T>(this IEnumerable<T> enumerable) => (enumerable.Count() > 0);
    }
}
