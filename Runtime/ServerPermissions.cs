/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ProtoBuf;
using UnityEngine;

namespace Arteranos.Common
{
    public static class BoolExtensions
    {
        /// <summary>
        /// Returns a relevance index for the comparison.
        /// </summary>
        /// <param name="setting">The server settings</param>
        /// <param name="user">The user's search filter</param>
        /// <returns>5 for an exact determinate match, 1 for an inexact match, 0 for a mismatch</returns>
        public static int FuzzyEq(this bool? setting, bool? user)
        {
            if(setting == null &&  user == null) return 2;

            if(setting == null || user == null) return 1;

            return (setting == user) ? 5 : 0;
        }
    }

    [ProtoContract]
    public class PermissionsJSON : IEquatable<PermissionsJSON>, ICloneable
    {
        // CONTENT MODERATION / FILTERING
        // null allowed, and the user's filter could yield an inexact match, second only
        // to an exact one, like....
        //
        //  Setting     User        Priority
        //  false       false           1
        //  false       true            --
        //  false       null            1 (because the user says 'don't care')
        //  true        false           --
        //  true        true            1
        //  true        null            1 (because the user says 'don't care')
        //  null        false           2
        //  null        true            2
        //  null        null            2 (see below)
        //
        // as a side effect, server adminitrators get their servers a better ranking if they
        // put down a definite answer, in opposite being wishy-washy.
        //
        // ref. https://www.techdirt.com/2023/04/20/bluesky-plans-decentralized-composable-moderation/
        //      Defaults to Bluesky in the aforementioned website, with modifications
        //
        // OMITTED
        //
        // (Political) Hate Groups - FALSE - Conflicts the law in many occasions
        // (eg. Germany, §1 GG, §130 StGB)
        //
        // Spam - FALSE - Self-explanatory
        //
        // Impersonation - FALSE - Self-explanatory

        [ProtoMember(1)]
        public bool? Flying
        {
            get => _flying;
            set
            {
                if (_flying == value) return;
                _flying = value;
                _dirty = true;
            }
        }

        // Other Nudity (eg. non-sexual or artistic)
        [ProtoMember(2)]
        public bool? Nudity
        {
            get => _nudity;
            set
            {
                if (_nudity == value) return;
                _nudity = value;
                _dirty = true;
            }
        }

        // Sexually suggestive (does not include nudity)
        [ProtoMember(3)]
        public bool? Suggestive
        {
            get => _suggestive;
            set
            {
                if (_suggestive == value) return;
                _suggestive = value;
                _dirty = true;
            }
        }

        // Violence (Cartoon / "Clean" violence)
        [ProtoMember(4)]
        public bool? Violence
        {
            get => _violence;
            set
            {
                if (_violence == value) return;
                _violence = value;
                _dirty = true;
            }
        }

        // Explicit Sexual Images
        [ProtoMember(5)]
        public bool? ExplicitNudes
        {
            get => _explicitNudes;
            set
            {
                if (_explicitNudes == value) return;
                _explicitNudes = value;
                _dirty = true;
            }
        }

        // NEW - Excessive Violence / Blood (Gore, self-harm, torture)
        [ProtoMember(6)]
        public bool? ExcessiveViolence
        {
            get => _excessiveViolence;
            set
            {
                if (_excessiveViolence == value) return;
                _excessiveViolence = value;
                _dirty = true;
            }
        }

        private bool? _flying;
        private bool? _nudity;
        private bool? _suggestive;
        private bool? _violence;
        private bool? _explicitNudes;
        private bool? _excessiveViolence;


        private bool _dirty = true;

        public PermissionsJSON() { }

        public PermissionsJSON(bool _)
        {
            Flying = false;

            Nudity = null;
            Suggestive = null;
            Violence = null;
            ExplicitNudes = null;
            ExcessiveViolence = false;
        }

        public (int, int) MatchRatio(PermissionsJSON user)
        {
            static int possibleScore(bool? b1) => b1 == null ? 2 : 5;

            int index = 0;
            int possible = 10;

            index += Flying.FuzzyEq(user.Flying) * 2;


            // Aggregate the matches of the permission settings against the user's
            // filter settings.
            possible += possibleScore(Nudity);
            index += Nudity.FuzzyEq(user.Nudity);

            possible += possibleScore(Suggestive);
            index += Suggestive.FuzzyEq(user.Suggestive);

            possible += possibleScore(Violence);
            index += Violence.FuzzyEq(user.Violence);

            possible += possibleScore(ExcessiveViolence);
            index += ExcessiveViolence.FuzzyEq(user.ExcessiveViolence);

            possible += possibleScore(ExplicitNudes);
            index += ExplicitNudes.FuzzyEq(user.ExplicitNudes);

            return (index, possible);
        }

        public string HumanReadableMI(PermissionsJSON user)
        {
            (int index, int possible) = MatchRatio(user);
            float ratio = (float)index / (float)possible;

            string str = ratio switch
            {
                >= 1.0f => "perfect",
                > 0.8f => "very good",
                > 0.6f => "good",
                > 0.4f => "mediocre",
                > 0.2f => "poor",
                _ => "very poor"
            };

            return $"{index} ({str})";
        }

        public bool IsInViolation(PermissionsJSON serverPerms)
        {
            int points = 0;

            static int penalty(bool world, bool? server, int unclear = 1, int clear = 5)
            {
                // The world has the warning label unset, so it's okay.
                if (!world) return 0;

                // And, the world's warning label _is_ set, so....
                // The server's permissions are unclear.
                if (server == null) return unclear;

                // The server okays the warning label.
                if (server.Value) return 0;

                // The world is in clear violation of the server's permissions.
                return clear;
            }

            points += penalty(Violence ?? true, serverPerms.Violence);
            points += penalty(Nudity ?? true, serverPerms.Nudity);
            points += penalty(Suggestive ?? true, serverPerms.Suggestive);
            points += penalty(ExcessiveViolence ?? true, serverPerms.ExcessiveViolence, 2);
            points += penalty(ExplicitNudes ?? true, serverPerms.ExplicitNudes, 2);

            return points > 2;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PermissionsJSON);
        }

        public bool Equals(PermissionsJSON other)
        {
            return other is not null &&
                   Flying == other.Flying &&
                   ExplicitNudes == other.ExplicitNudes &&
                   Nudity == other.Nudity &&
                   Suggestive == other.Suggestive &&
                   Violence == other.Violence &&
                   ExcessiveViolence == other.ExcessiveViolence;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Flying, ExplicitNudes, Nudity, Suggestive, Violence, ExcessiveViolence);
        }

        public object Clone() => MemberwiseClone();

        public static bool operator ==(PermissionsJSON left, PermissionsJSON right)
        {
            return EqualityComparer<PermissionsJSON>.Default.Equals(left, right);
        }

        public static bool operator !=(PermissionsJSON left, PermissionsJSON right)
        {
            return !(left == right);
        }

        // ---------------------------------------------------------------
        public const string PATH_USER_PERMS = "Permissions.json";

        public static PermissionsJSON Load()
        {
            PermissionsJSON perms;

            try
            {
                string json = ConfigUtils.ReadTextConfig(PATH_USER_PERMS);
                perms = JsonConvert.DeserializeObject<PermissionsJSON>(json);

                perms._dirty = false;
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to load Content permissions: {e.Message}");
                perms = null;
            }

            return perms;
        }

        public void Save()
        {
            try
            {
                if (!_dirty) return;

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                ConfigUtils.WriteTextConfig(PATH_USER_PERMS, json);

                _dirty = false;

            }
            catch (Exception e)
            {
                Debug.Log($"Failed to save Content permissions: {e.Message}");
            }
        }

    }
}