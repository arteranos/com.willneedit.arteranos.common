/*
 * Copyright (c) 2025, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Ipfs;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Arteranos.Common;


namespace Arteranos.Common
{
    [ProtoContract]
    public struct KitEntryItem
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public Guid GUID;

        public KitEntryItem(string name, Guid guid)
        {
            Name = name;
            GUID = guid;
        }
    }

    [ProtoContract]
    public struct KitEntryList
    {
        [ProtoMember(1)]
        public List<KitEntryItem> Items;
    }

    public class KitMetaData
    {
        public string KitName = "Unnamed Kit";
        public string KitDescription = string.Empty;
        public UserID AuthorID = null;
        public DateTime Created = DateTime.MinValue;

        public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static KitMetaData Deserialize(string json) => JsonConvert.DeserializeObject<KitMetaData>(json);
    }
}
