/*
 * Copyright (c) 2025, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Ipfs;
using ProtoBuf;

// A more relaxed concept comparing to the IPFS's CID, using a freeform
// type label and a single hash function.
//
// It's not intended to _save_ the fingerprints, only to compare datasets
// without exposing the actual data with a agreed-upon scheme (like, with same version)
namespace Arteranos.Common.Cryptography
{
    public interface IFingerprintable
    {
        byte[] FingerprintBytes { get; }
    }

    [ProtoContract]
    [TypeConverter(typeof(FingerprintConverter))]
    public class Fingerprint : IEquatable<Fingerprint>
    {
        [ProtoMember(1)]
        public string Type { get; private set; } = null;

        [ProtoMember(2)]
        public byte[] FPBytes { get; private set; } = null;

        #region Construction
        public Fingerprint() { }

        public Fingerprint(Cid cid)
        {
            // CID are hashes with some metadata, but we need it done because
            // them to be comparable, not for tracking to its source.
            Type = cid.GetType().ToString();
            FPBytes = CryptoHelpers.GetFingerprint(cid.ToArray());
        }

        public Fingerprint(object ob)
        {
            if (ob is not IFingerprintable fp_ob)
                throw new InvalidDataException($"{ob.GetType()} is not fingerprintable");

            Type = ob.GetType().ToString();
            FPBytes = fp_ob.FingerprintBytes;
        }

        #endregion

        #region Serializing

        public byte[] Serialize()
        {
            using MemoryStream ms = new();
            Serializer.Serialize(ms, this);
            return ms.ToArray();
        }

        public static Fingerprint Deserialize(byte[] data)
            => Serializer.Deserialize<Fingerprint>(new MemoryStream(data));

        #endregion

        #region Equality

        public bool Equals(Fingerprint other)
        {
            if (Type != other.Type) return false;

            if (other.Type == null || Type == null) return false;

            return FPBytes.SequenceEqual(other.FPBytes);
        }

        public override bool Equals(object obj)
        {
            // Compare directly...
            if (obj is Fingerprint fp_ob) return Equals(fp_ob);

            // ... or first get the targeted object's fingerprint.
            return Equals(new Fingerprint(obj));
        }

        public override int GetHashCode()
        {
            HashCode hc = new();
            foreach(byte b in Serialize()) hc.Add(b);
            return hc.ToHashCode();
        }

        public static bool operator ==(Fingerprint left, Fingerprint right) => EqualityComparer<object>.Default.Equals(left, right);
        public static bool operator !=(Fingerprint left, Fingerprint right) => !(left == right);

        public static bool operator ==(Fingerprint left, IFingerprintable right) => EqualityComparer<object>.Default.Equals(left, right);
        public static bool operator !=(Fingerprint left, IFingerprintable right) => !(left == right);

        public static bool operator ==(IFingerprintable left, Fingerprint right) => EqualityComparer<object>.Default.Equals(right, left);
        public static bool operator !=(IFingerprintable left, Fingerprint right) => !(left == right);

        #endregion

        public override string ToString()
        {
            return $"{Type}:{Convert.ToBase64String(FPBytes)}";
        }
    }

    public class FingerprintConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string)) return true;
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string s)
            {
                byte[] bytes = Convert.FromBase64String(s);
                return Fingerprint.Deserialize(bytes);
            }
            else
                return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                byte[] bytes = ((Fingerprint)value).Serialize();
                return Convert.ToBase64String(bytes);
            }
            else
                return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}