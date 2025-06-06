/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using UnityEngine;

using Newtonsoft.Json;
using Arteranos.Common.Cryptography;
using Ipfs;
using Ipfs.Cryptography.Proto;
using System.IO;

namespace Arteranos.Common
{
    public class UserIDJSON
    {
        public const string PATH_USER_ID = "UserID.json";

        public byte[] SignKeyPair = null;
        public string Nickname = null;
        public Cid Icon = null;

        private SignKey _SignKey = null;

        public static implicit operator bool(UserIDJSON u) => u.SignKeyPair != null && u.Nickname != null;

        public static implicit operator SignKey(UserIDJSON u)
        {
            if (u._SignKey == null)
                u._SignKey = SignKey.ImportPrivateKey(u.SignKeyPair);

            return u._SignKey;
        }

        public static implicit operator PublicKey(UserIDJSON u) => ((SignKey)u).PublicKey;

        public static implicit operator UserID(UserIDJSON u) => new(u, u.Nickname, u.Icon);

        public static UserIDJSON Generate()
        {

            UserIDJSON user = new()
            {
                SignKeyPair = null,
                Nickname = "Anonymous",
                Icon = null
            };

            SignKey key = SignKey.Generate();
            key.ExportPrivateKey(out user.SignKeyPair);

            return user;
        }

        public static UserIDJSON Load()
        {
            UserIDJSON uid;

            try
            {
                string json = ConfigUtils.ReadTextConfig(PATH_USER_ID);
                uid = JsonConvert.DeserializeObject<UserIDJSON>(json);
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to load User ID: {e.Message}");
                uid = new();
            }

            return uid;
        }

        public void Save()
        {
            try
            {
                if (!this)
                    throw new InvalidDataException("Invalid User ID naterial");

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                ConfigUtils.WriteTextConfig(PATH_USER_ID, json);
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to save User ID: {e.Message}");
            }
        }
    }
}
