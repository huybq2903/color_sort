using System;
using System.Collections.Generic;

public sealed class TCString
{
    public bool[] PurposeConsents = new bool[25]; // index by purposeId (1..24)
    public HashSet<int> _vendorConsented = new HashSet<int>();

    public bool HasPurposeConsent(int purposeId)
    {
        if (purposeId < 1 || purposeId > 24) return false;
        return PurposeConsents[purposeId];
    }

    public bool HasVendorConsent(int vendorId)
    {
        return _vendorConsented.Contains(vendorId);
    }

    internal void AddVendor(int id) => _vendorConsented.Add(id);
}

public static class TCStringParser
{
    // Purposes of interest
    public const int PurposeStoreAccess = 1; // Store and/or access information on a device
    public const int PurposePersonalisedAds = 4; // Select personalised ads

    public static TCString Parse(string tcString)
    {
        if (string.IsNullOrEmpty(tcString)) return new TCString();

        // Core string is before first '.'
        var firstDot = tcString.IndexOf('.');
        var core = firstDot >= 0 ? tcString.Substring(0, firstDot) : tcString;

        var bytes = Base64UrlDecode(core);
        var br = new BitReader(bytes);

        var result = new TCString();

        // --- Core fields (subset) ---
        br.ReadInt(6); // Version
        br.ReadInt(36); // Created
        br.ReadInt(36); // LastUpdated
        br.ReadInt(12); // CmpId
        br.ReadInt(12); // CmpVersion
        br.ReadInt(6); // ConsentScreen
        br.ReadString6x2(12); // ConsentLanguage (skip)
        br.ReadInt(12); // VendorListVersion
        br.ReadInt(6); // PolicyVersion
        br.ReadBool(); // IsServiceSpecific
        br.ReadBool(); // UseNonStandardStacks

        // SpecialFeatureOptIns (we skip exact count; spec says 12 bits but currently only 2 special features used)
        // For compatibility, read 12 bits total.
        br.ReadBits(12);

        // PurposeConsents: 24 bits (Purposes 1..24)
        for (int i = 1; i <= 24; i++)
        {
            result.PurposeConsents[i] = br.ReadBool();
        }

        // PurposeLITransparency: 24 bits (skip)
        br.ReadBits(24);

        br.ReadBool(); // PurposeOneTreatment
        br.ReadBits(12); // PublisherCC (2 letters)

        // Vendor Consents Section
        int maxVendorId = br.ReadInt(16);
        bool isRangeEncoding = br.ReadBool();

        if (!isRangeEncoding)
        {
            // BitField for vendor consents (1..maxVendorId)
            for (int v = 1; v <= maxVendorId; v++)
            {
                if (br.ReadBool()) result.AddVendor(v);
            }
        }
        else
        {
            // Range encoding
            int numEntries = br.ReadInt(12);
            for (int i = 0; i < numEntries; i++)
            {
                bool thisIsRange = br.ReadBool();
                int startId = br.ReadInt(16);
                if (thisIsRange)
                {
                    int endId = br.ReadInt(16);
                    for (int v = startId; v <= endId; v++) result.AddVendor(v);
                }
                else
                {
                    result.AddVendor(startId);
                }
            }
        }

        // NOTE: There are more segments after core (e.g., DisclosedVendors, PubPurposes, etc.).
        // This minimal parser focuses on purpose & vendor consents sufficient for many mediation needs.

        return result;
    }

    /// <summary>
    /// Heuristic to decide if personalized ads are permitted for a specific vendor.
    /// Requires Purpose 1 (store/access) and Purpose 4 (personalised ads) and vendor consent.
    /// </summary>
    public static bool AllowsPersonalisedAds(TCString tcf, int vendorId)
    {
        if (tcf == null) return false;
        return tcf.HasPurposeConsent(PurposeStoreAccess)
               && tcf.HasPurposeConsent(PurposePersonalisedAds)
               && tcf.HasVendorConsent(vendorId);
    }

    // --- Helpers ---
    private static byte[] Base64UrlDecode(string input)
    {
        string s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2:
                s += "==";
                break;
            case 3:
                s += "=";
                break;
        }

        return Convert.FromBase64String(s);
    }

    private sealed class BitReader
    {
        private readonly byte[] _data;
        private int _bitPos;

        public BitReader(byte[] data)
        {
            _data = data;
            _bitPos = 0;
        }

        public bool ReadBool() => ReadBits(1) == 1;

        public int ReadInt(int bitCount)
        {
            int v = 0;
            for (int i = 0; i < bitCount; i++)
            {
                int bit = ReadBits(1);
                v = (v << 1) | bit;
            }

            return v;
        }

        public void ReadString6x2(int totalBits)
        {
            // Two 6-bit letters (ISO 639-1) * 2 = 12 bits in spec; here we just skip bits.
            ReadBits(totalBits);
        }

        public int ReadBits(int count)
        {
            int value = 0;
            for (int i = 0; i < count; i++)
            {
                int byteIndex = _bitPos >> 3;
                int bitIndex = 7 - (_bitPos & 7);
                int bit = (_data[byteIndex] >> bitIndex) & 1;
                value = (value << 1) | bit;
                _bitPos++;
            }

            return value;
        }
    }
}