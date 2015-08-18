// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IValueProvider"/> adapter for data stored in an
    /// <see cref="IDictionary{string, object}"/>.
    /// </summary>
    public class DictionaryBasedValueProvider: BindingSourceValueProvider
    {
        private readonly IDictionary<string, object> _values;
        private PrefixContainer _prefixContainer;

        /// <summary>
        /// Creates a new <see cref="DictionaryBasedValueProvider"/>.
        /// </summary>
        /// <param name="bindingSource">The <see cref="BindingSource"/> of the data.</param>
        /// <param name="values">The values.</param>
        public DictionaryBasedValueProvider(
            [NotNull] BindingSource bindingSource,
            [NotNull] IDictionary<string, object> values)
            : base(bindingSource)
        {
            _values = values;
        }

        protected PrefixContainer PrefixContainer
        {
            get
            {
                if (_prefixContainer == null)
                {
                    _prefixContainer = new PrefixContainer(_values.Keys);
                }

                return _prefixContainer;
            }
        }

        /// <inheritdoc />
        public override Task<bool> ContainsPrefixAsync(string key)
        {
            return Task.FromResult(PrefixContainer.ContainsPrefix(key));
        }

        /// <inheritdoc />
        public override Task<ValueProviderResult> GetValueAsync([NotNull] string key)
        {
            object value;
            ValueProviderResult result;
            if (_values.TryGetValue(key, out value))
            {
                var stringValue = value as string ?? value?.ToString() ?? string.Empty;
                result = new ValueProviderResult(stringValue, CultureInfo.InvariantCulture);
            }
            else
            {
                result = ValueProviderResult.None;
            }

            return Task.FromResult(result);
        }
    }
}
