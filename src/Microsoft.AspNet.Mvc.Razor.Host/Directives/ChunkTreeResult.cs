// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    public class ChunkTreeResult
    {
        public ChunkTree ChunkTree { get; set; }

        public string FilePath { get; set; }
    }
}
