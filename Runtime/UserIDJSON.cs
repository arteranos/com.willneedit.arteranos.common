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
using System.Linq;

namespace Arteranos.Common
{
    public class UserIDJSON
    {
        public const string PATH_USER_ID = "UserID.json";

        public byte[] SignKeyPair
        {
            get => _signKeyPair;
            set
            {
                if (value != null && _signKeyPair != null && _signKeyPair.SequenceEqual(value)) return;
                _signKeyPair = value;
                _dirty = true;
            }
        }
        public string Nickname
        {
            get => _nickname;
            set
            {
                if (value == _nickname) return;
                _nickname = value;
                _dirty = true;
            }
        }
        public Cid Icon
        {
            get => _icon;
            set
            {
                if (value == _icon) return;
                _icon = value;
                _dirty = true;
            }
        }

        private byte[] _signKeyPair = null;
        private string _nickname = null;
        private Cid _icon = null;
        private SignKey _SignKey = null;
        private bool _dirty = true;

        // Cast into...
        //  - bool      : Validity
        //  - SignKey   : Complete keypair, for signing/verifying
        //  - UserID    ; PublicKey/Nickname/Icon, for public consumption
        //  - PublicKey ; For verifying
        //  - Cid       : Icon's CID
        //  - string    : Nickname

        public static implicit operator bool(UserIDJSON u) => u.SignKeyPair != null && u.Nickname != null;

        public static explicit operator SignKey(UserIDJSON u)
        {
            if (u._SignKey == null)
                u._SignKey = SignKey.ImportPrivateKey(u.SignKeyPair);

            return u._SignKey;
        }

        public static implicit operator PublicKey(UserIDJSON u) => ((SignKey)u).PublicKey;

        public static implicit operator UserID(UserIDJSON u) => new(u, u.Nickname, u.Icon);

        public static explicit operator Cid(UserIDJSON u) => u.Icon;

        public static explicit operator string(UserIDJSON u) => u.Nickname;

        public static UserIDJSON Generate()
        {

            UserIDJSON user = new()
            {
                SignKeyPair = null,
                Nickname = "Anonymous",
                Icon = null
            };

            SignKey key = SignKey.Generate();
            key.ExportPrivateKey(out user._signKeyPair);
            user._dirty = true;

            return user;
        }

        public static UserIDJSON Load()
        {
            UserIDJSON uid;

            try
            {
                string json = ConfigUtils.ReadTextConfig(PATH_USER_ID);
                uid = JsonConvert.DeserializeObject<UserIDJSON>(json);

                uid._dirty = false;
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

                if (!_dirty) return;

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
