// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelState
    {
        public string OriginalValue { get; set; }

        public object Value { get; set; }

        public ModelErrorCollection Errors { get; } = new ModelErrorCollection();

        public ModelValidationState ValidationState { get; set; }
    }
}
