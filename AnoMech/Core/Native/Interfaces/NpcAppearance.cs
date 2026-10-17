namespace AnoMech.Core.Native.Interfaces;

public sealed record NpcAppearance(uint BodyModel = 0, ulong MainhandWeaponModel = 0,
    uint? HeadModel = null, uint? LegsModel = null, ulong? OffhandWeaponModel = null, byte? AnimationState = null,
    uint[]? Equipment = null, byte[]? Customize = null);
