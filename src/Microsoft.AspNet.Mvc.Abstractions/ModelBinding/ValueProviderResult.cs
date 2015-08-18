// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Result of an <see cref="IValueProvider.GetValueAsync"/> operation.
    /// </summary>
    public struct ValueProviderResult : IEquatable<ValueProviderResult>
    {
        private static readonly CultureInfo _staticCulture = CultureInfo.InvariantCulture;

        public static ValueProviderResult None = new ValueProviderResult();

        public ValueProviderResult([NotNull] string value)
        {
            Values = new string[] { value };
            Culture = _staticCulture;
        }

        public ValueProviderResult([NotNull] string value, [NotNull] CultureInfo culture)
        {
            Values = new string[] { value };
            Culture = culture;
        }

        public ValueProviderResult([NotNull] string[] values)
        {
            Values = values;
            Culture = _staticCulture;
        }

        public ValueProviderResult([NotNull] string[] values, [NotNull] CultureInfo culture)
        {
            Values = values;
            Culture = culture;
        }

        public CultureInfo Culture { get; private set; }

        public string[] Values { get; private set; }

        public int Length
        {
            get
            {
                return Values == null ? 0 : Values.Length;
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as ValueProviderResult?;
            return other.HasValue ? Equals(other.Value) : false;
        }

        public bool Equals(ValueProviderResult other)
        {
            if (Values == null && other.Values == null)
            {
                return true;
            }
            else if (Values == null ^ other.Values == null)
            {
                return false;
            }
            else if (Values.Length != other.Values.Length)
            {
                return false;
            }
            else
            {
                for (var i = 0; i < Values.Length; i++)
                {
                    if (!string.Equals(Values[i], other.Values[i], StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public override int GetHashCode()
        {
            return ((string)this).GetHashCode();
        }

        public override string ToString()
        {
            return (string)this;
        }

        public static explicit operator string (ValueProviderResult result)
        {
            if (result.Length == 0)
            {
                return null;
            }
            else if (result.Length == 1)
            {
                return result.Values[0];
            }
            else
            {
                return string.Join(",", result.Values);
            }
        }

        public static bool operator ==(ValueProviderResult x, ValueProviderResult y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(ValueProviderResult x, ValueProviderResult y)
        {
            return !x.Equals(y);
        }
    }
}
