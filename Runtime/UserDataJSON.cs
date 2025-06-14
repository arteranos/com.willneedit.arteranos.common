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
using System.Collections.Generic;

namespace Arteranos.Common
{
    public class UserDataJSON
    {
        #region Data structures

        public const string PATH_USER_DATA = "UserID.json";

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
        public virtual string Nickname
        {
            get => _nickname;
            set
            {
                if (value == _nickname) return;
                _nickname = value;
                _dirty = true;
            }
        }
        public virtual Cid Icon
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

        [JsonProperty(PropertyName = "FriendOffered")]
        private readonly HashSet<UserID> _friendOffered = new();
        [JsonProperty(PropertyName = "FriendReceived")]
        private readonly HashSet<UserID> _friendReceived = new();


        [JsonProperty(PropertyName = "BlockImposed")]
        private readonly HashSet<UserID> _blockImposed = new();

        // No need to save.
        // [JsonProperty(PropertyName = "BlockReceived")]
        private readonly HashSet<UserID> _blockReceived = new();



        // Cast into...
        //  - bool      : Validity
        //  - SignKey   : Complete keypair, for signing/verifying
        //  - UserID    ; PublicKey/Nickname/Icon, for public consumption
        //  - PublicKey ; For verifying
        //  - Cid       : Icon's CID
        //  - string    : Nickname

        public static implicit operator bool(UserDataJSON u) => u.SignKeyPair != null && u.Nickname != null;

        public static explicit operator SignKey(UserDataJSON u)
        {
            if (u._SignKey == null)
                u._SignKey = SignKey.ImportPrivateKey(u.SignKeyPair);

            return u._SignKey;
        }

        public static implicit operator PublicKey(UserDataJSON u) => ((SignKey)u).PublicKey;

        public static implicit operator UserID(UserDataJSON u) => new(u, u.Nickname, u.Icon);

        public static explicit operator Cid(UserDataJSON u) => u.Icon;

        public static explicit operator string(UserDataJSON u) => u.Nickname;
        private bool HandleState(UserID target, bool newState, HashSet<UserID> stateList)
        {
            bool oldState = stateList.Contains(target);

            if (newState)
                stateList.Add(target);
            else
                stateList.Remove(target);

            if (oldState != newState) _dirty = true;

            return oldState != newState;
        }

        #endregion
        // ---------------------------------------------------------------
        #region Load & Save

        public static UserDataJSON Generate()
        {

            UserDataJSON user = new()
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

        public static UserDataJSON Load()
        {
            UserDataJSON uid;

            try
            {
                string json = ConfigUtils.ReadTextConfig(PATH_USER_DATA);
                uid = JsonConvert.DeserializeObject<UserDataJSON>(json);

                uid._dirty = false;
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to load User ID: {e.Message}");
                uid = new();
            }

            return uid;
        }

        public virtual void Save()
        {
            try
            {
                if (!this)
                    throw new InvalidDataException("Invalid User ID naterial");

                if (!_dirty) return;

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                ConfigUtils.WriteTextConfig(PATH_USER_DATA, json);

                _dirty = false;
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to save User ID: {e.Message}");
            }
        }

        #endregion
        // ---------------------------------------------------------------
        #region Friend handling (from user and relayed from server)

        public virtual bool OfferFriend(UserID target, bool offering)
        {
            // Think twice before friending a blocked user.
            if (offering && IsBlocked(target)) return false;

            return HandleState(target, offering, _friendOffered);
        }

        public virtual bool ReceiveFriend(UserID target, bool receiving)
        {
            bool changed = HandleState(target, receiving, _friendReceived);

            // Remote user revoked friendshi(t)p, do so, too.
            if (!receiving && changed)
                _friendOffered.Remove(target);

            return changed;
        }
        #endregion
        // ---------------------------------------------------------------
        #region Block handling (from user and relayed from server)

        public virtual bool ImposeBlock(UserID target, bool imposing)
        {
            bool changed = HandleState(target, imposing, _blockImposed);

            // Yes, blocking implies revoking friendships with its ramifications. 
            // Blockings hasn't to be incurred just on a whim.
            if (imposing) changed |= OfferFriend(target, false);

            return changed;
        }

        public virtual bool ReceiveBlock(UserID target, bool receiving)
        {
            bool changed = HandleState(target, receiving, _blockReceived);

            // One of you two were sorely mistaken in the other person...
            if (receiving) changed |= OfferFriend(target, false);

            return changed;
        }
        #endregion
        // ---------------------------------------------------------------
        #region Friend & Block effects (in Client)
        public bool IsFriendOffered(UserID target) => _friendOffered.Contains(target);
        public bool IsFriendReceived(UserID target) => _friendReceived.Contains(target);

        public bool IsFriends(UserID target) => IsFriendOffered(target) && IsFriendReceived(target);
        public bool IsBlocked(UserID target) => _blockImposed.Contains(target) || _blockReceived.Contains(target);

        /// <summary>
        /// Returns combined block/neither/friend state
        /// </summary>
        /// <param name="target">The user</param>
        /// <param name="unilaterally">Only use the view from you to him</param>
        /// <returns>false - blocked;null - neither;true - friend</returns>
        public bool? IsStated(UserID target, bool unilaterally = false)
        {
            if (_blockImposed.Contains(target) || (!unilaterally && IsBlocked(target))) return false;

            if (IsFriends(target) || (unilaterally && IsFriendOffered(target))) return true;

            return null;
        }
        #endregion
    }
}
