using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Structs;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class EnhancedIsleworksAgenda : Tweak
{
    public static Configuration Config => Plugin.Config.Tweaks.EnhancedIsleworksAgenda;

    public class Configuration
    {
        [BoolConfig]
        public bool EnableSearchBar = true;

        [BoolConfig]
        public bool DisableTreeListTooltips = true;
    }

    public override void Enable()
    {
        if (Config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"))
            Service.WindowManager.OpenWindow<MJICraftScheduleSettingSearchBar>();
    }

    public override void Disable()
    {
        Service.WindowManager.CloseWindow<MJICraftScheduleSettingSearchBar>();
    }

    public override void OnAddonOpen(string addonName)
    {
        if (Config.EnableSearchBar && addonName == "MJICraftScheduleSetting")
            Service.WindowManager.OpenWindow<MJICraftScheduleSettingSearchBar>();
    }

    public override void OnAddonClose(string addonName)
    {
        if (addonName == "MJICraftScheduleSetting")
            Service.WindowManager.CloseWindow<MJICraftScheduleSettingSearchBar>();
    }

    public override void OnConfigChange(string fieldName)
    {
        if (fieldName == "EnableSearchBar")
        {
            if (Config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"))
                Service.WindowManager.OpenWindow<MJICraftScheduleSettingSearchBar>();
            else
                Service.WindowManager.CloseWindow<MJICraftScheduleSettingSearchBar>();
        }
    }

    [VTableHook<AddonMJICraftScheduleSetting>((int)AtkResNodeVfs.ReceiveEvent)]
    private void AddonMJICraftScheduleSetting_ReceiveEvent(AddonMJICraftScheduleSetting* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5)
    {
        if (eventType == AtkEventType.ListItemRollOver && eventParam == 2 && Config.DisableTreeListTooltips)
        {
            var index = *(uint*)(a5 + 0x10);
            var itemPtr = addon->TreeList->GetItem(index);
            if (itemPtr != null)
            {
                if (itemPtr->Data->Type != AtkComponentTreeListItemType.Group)
                {
                    atkEvent->SetEventIsHandled();
                    return;
                }
            }
        }

        AddonMJICraftScheduleSetting_ReceiveEventHook.OriginalDisposeSafe(addon, eventType, eventParam, atkEvent, a5);
    }
}