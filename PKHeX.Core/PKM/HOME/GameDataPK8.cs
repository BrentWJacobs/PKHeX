using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core;

/// <summary>
/// Side game data for <see cref="PK8"/> data transferred into HOME.
/// </summary>
public sealed class GameDataPK8 : HomeOptional1, IGameDataSide, IGigantamax, IDynamaxLevel, ISociability
{
    private const HomeGameDataFormat ExpectFormat = HomeGameDataFormat.PK8;
    private const int SIZE = HomeCrypto.SIZE_1GAME_PK8;
    protected override HomeGameDataFormat Format => ExpectFormat;

    public GameDataPK8() : base(SIZE) { }
    public GameDataPK8(Memory<byte> buffer) : base(buffer) => EnsureSize(SIZE);
    public GameDataPK8 Clone() => new(ToArray());
    public int WriteTo(Span<byte> result) => WriteWithHeader(result);

    #region Structure

    public bool CanGigantamax { get => Data[0x00] != 0; set => Data[0x00] = (byte)(value ? 1 : 0); }
    public uint Sociability { get => ReadUInt32LittleEndian(Data[0x01..]); set => WriteUInt32LittleEndian(Data[0x01..], value); }

    public ushort Move1 { get => ReadUInt16LittleEndian(Data[0x05..]); set => WriteUInt16LittleEndian(Data[0x05..], value); }
    public ushort Move2 { get => ReadUInt16LittleEndian(Data[0x07..]); set => WriteUInt16LittleEndian(Data[0x07..], value); }
    public ushort Move3 { get => ReadUInt16LittleEndian(Data[0x09..]); set => WriteUInt16LittleEndian(Data[0x09..], value); }
    public ushort Move4 { get => ReadUInt16LittleEndian(Data[0x0B..]); set => WriteUInt16LittleEndian(Data[0x0B..], value); }

    public int Move1_PP { get => Data[0x0D]; set => Data[0x0D] = (byte)value; }
    public int Move2_PP { get => Data[0x0E]; set => Data[0x0E] = (byte)value; }
    public int Move3_PP { get => Data[0x0F]; set => Data[0x0F] = (byte)value; }
    public int Move4_PP { get => Data[0x10]; set => Data[0x10] = (byte)value; }
    public int Move1_PPUps { get => Data[0x11]; set => Data[0x11] = (byte)value; }
    public int Move2_PPUps { get => Data[0x12]; set => Data[0x12] = (byte)value; }
    public int Move3_PPUps { get => Data[0x13]; set => Data[0x13] = (byte)value; }
    public int Move4_PPUps { get => Data[0x14]; set => Data[0x14] = (byte)value; }

    public ushort RelearnMove1 { get => ReadUInt16LittleEndian(Data[0x15..]); set => WriteUInt16LittleEndian(Data[0x15..], value); }
    public ushort RelearnMove2 { get => ReadUInt16LittleEndian(Data[0x17..]); set => WriteUInt16LittleEndian(Data[0x17..], value); }
    public ushort RelearnMove3 { get => ReadUInt16LittleEndian(Data[0x19..]); set => WriteUInt16LittleEndian(Data[0x19..], value); }
    public ushort RelearnMove4 { get => ReadUInt16LittleEndian(Data[0x1B..]); set => WriteUInt16LittleEndian(Data[0x1B..], value); }
    public byte DynamaxLevel { get => Data[0x1D]; set => Data[0x1D] = value; }

    private Span<byte> PokeJob => Data.Slice(0x1E, 14);
    public bool GetPokeJobFlag(int index) => FlagUtil.GetFlag(PokeJob, index >> 3, index & 7);
    public void SetPokeJobFlag(int index, bool value) => FlagUtil.SetFlag(PokeJob, index >> 3, index & 7, value);
    public bool GetPokeJobFlagAny() => PokeJob.IndexOfAnyExcept<byte>(0) >= 0;
    public void ClearPokeJobFlags() => PokeJob.Clear();

    public byte Fullness { get => Data[0x2C]; set => Data[0x2C] = value; }

    private Span<byte> RecordFlags => Data.Slice(0x2D, 14);
    public bool GetMoveRecordFlag(int index) => FlagUtil.GetFlag(RecordFlags, index >> 3, index & 7);
    public void SetMoveRecordFlag(int index, bool value) => FlagUtil.SetFlag(RecordFlags, index >> 3, index & 7, value);
    public bool GetMoveRecordFlagAny() => RecordFlags.IndexOfAnyExcept<byte>(0) >= 0;
    public void ClearMoveRecordFlags() => RecordFlags.Clear();

    public int Palma { get => ReadInt32LittleEndian(Data[0x3B..]); set => WriteInt32LittleEndian(Data[0x3B..], value); }
    public int Ball { get => Data[0x3F]; set => Data[0x3F] = (byte)value; }
    public int Egg_Location { get => ReadUInt16LittleEndian(Data[0x40..]); set => WriteUInt16LittleEndian(Data[0x40..], (ushort)value); }
    public int Met_Location { get => ReadUInt16LittleEndian(Data[0x42..]); set => WriteUInt16LittleEndian(Data[0x42..], (ushort)value); }

    #endregion

    #region Conversion

    public PersonalInfo GetPersonalInfo(ushort species, byte form) => PersonalTable.SWSH.GetFormEntry(species, form);

    public void CopyTo(PK8 pk)
    {
        ((IGameDataSide)this).CopyTo(pk);
        pk.CanGigantamax = CanGigantamax;
        pk.Sociability = Sociability;
        pk.DynamaxLevel = DynamaxLevel;
        pk.Fullness = Fullness;
        pk.Palma = Palma;
        PokeJob.CopyTo(pk.PokeJob);
        RecordFlags.CopyTo(pk.RecordFlags);
    }

    public PKM ConvertToPKM(PKH pkh) => ConvertToPK8(pkh);

    public PK8 ConvertToPK8(PKH pkh)
    {
        var pk = new PK8();
        pkh.CopyTo(pk);
        CopyTo(pk);
        return pk;
    }

    #endregion

    /// <summary> Reconstructive logic to best apply suggested values. </summary>
    public static GameDataPK8? TryCreate(PKH pkh)
    {
        if (pkh.DataPB7 is { } x)
            return GameDataPB7.Create<GameDataPK8>(x);

        var side = pkh.DataPB8 as IGameDataSide
                ?? pkh.DataPA8 as IGameDataSide
                ?? pkh.DataPK9;
        if (side is not null)
            return Create(side, pkh.Version);

        return null;
    }

    private static GameDataPK8 Create(IGameDataSide side, int ver)
    {
        var met = side.Met_Location;
        var ball = GetBall(side.Ball);
        var egg = GetEggLocation(side.Egg_Location);
        if (!IsOriginallySWSH(ver, met))
            RemapMetEgg(ver, ref met, ref egg);
        return new GameDataPK8 { Ball = ball, Met_Location = met, Egg_Location = egg };
    }

    private static void RemapMetEgg(int ver, ref int met, ref int egg)
    {
        var remap = Locations.GetMetSWSH((ushort)met, ver);
        if (remap == met)
            return;

        met = remap;
        egg = Locations.HOME_SWSHBDSPEgg;
    }

    private static bool IsOriginallySWSH(int ver, int loc) => ver is (int)GameVersion.SW or (int)GameVersion.SH && !IsFakeMetLocation(loc);
    private static bool IsFakeMetLocation(int met) => met is Locations.HOME_SWLA or Locations.HOME_SWBD or Locations.HOME_SHSP;
    private static int GetBall(int ball) => ball > (int)Core.Ball.Beast ? 4 : ball;
    private static int GetEggLocation(int egg) => egg == Locations.Default8bNone ? 0 : egg;
}
