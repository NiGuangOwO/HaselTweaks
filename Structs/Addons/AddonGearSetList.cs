using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs.Addons;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ?? 33 ED 48 8D 05 ?? ?? ?? ?? 48 89 07 48 8D 9F ?? ?? ?? ?? 48 89 AF ?? ?? ?? ?? 48 89 AF ?? ?? ?? ?? 48 89 AF ?? ?? ?? ?? 8D 75 64"
[StructLayout(LayoutKind.Explicit, Size = 0x3A90)]
public unsafe partial struct AddonGearSetList
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x3A8D)] public bool ResetPosition;
}
