namespace AnoMech.Core.Native.Interfaces;

public sealed record NpcAppearance(uint BodyModel, ulong MainhandWeaponModel,
    uint? HeadModel = null, uint? LegsModel = null, ulong? OffhandWeaponModel = null, byte? AnimationState = null);
