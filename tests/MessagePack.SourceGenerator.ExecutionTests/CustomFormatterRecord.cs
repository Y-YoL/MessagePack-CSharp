﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackFormatter(typeof(CustomFormatterRecordFormatter))]
internal record CustomFormatterRecord
{
    internal int Value { get; set; }
}
