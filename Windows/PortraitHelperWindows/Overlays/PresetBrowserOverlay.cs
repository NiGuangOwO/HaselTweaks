using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class PresetBrowserOverlay : Overlay, IDisposable
{
    public Guid? SelectedTagId { get; set; }
    public Dictionary<Guid, PresetCard> PresetCards { get; init; } = new();

    public CreateTagDialog CreateTagDialog { get; init; } = new();
    public RenameTagDialog RenameTagDialog { get; init; } = new();
    public DeleteTagDialog DeleteTagDialog { get; init; }
    public DeletePresetDialog DeletePresetDialog { get; init; }
    public EditPresetDialog EditPresetDialog { get; init; } = new();

    private int reorderTagOldIndex = -1;
    private int reorderTagNewIndex = -1;

    public PresetBrowserOverlay(PortraitHelper tweak) : base("[HaselTweaks] Portrait Helper PresetBrowser", tweak)
    {
        DeleteTagDialog = new(this);
        DeletePresetDialog = new(this);
    }

    public void Dispose()
    {
        foreach (var (_, card) in PresetCards)
            card.Dispose();

        PresetCards.Clear();
    }

    public override void Draw()
    {
        ImGui.PopStyleVar(); // WindowPadding from PreDraw()

        using (var table = ImRaii.Table("##PresetBrowser_Table", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoPadInnerX))
        {
            if (table != null && table.Success)
            {
                ImGui.TableSetupColumn("Tags", ImGuiTableColumnFlags.WidthFixed, 180);
                ImGui.TableSetupColumn("Presets", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();
                DrawPresetBrowserSidebar();

                ImGui.TableNextColumn();
                DrawPresetBrowserContent();
            }
        }

        CreateTagDialog.Draw();
        RenameTagDialog.Draw();
        DeleteTagDialog.Draw();
        DeletePresetDialog.Draw();
        EditPresetDialog.Draw();
    }

    private void DrawSidebarTag(SavedPresetTag tag, ref bool removeUnusedTags)
    {
        var count = Config.Presets.Count(preset => preset.Tags.Contains(tag.Id));

        var treeNodeFlags =
            ImGuiTreeNodeFlags.SpanAvailWidth |
            ImGuiTreeNodeFlags.FramePadding |
            ImGuiTreeNodeFlags.DefaultOpen |
            ImGuiTreeNodeFlags.Leaf |
            (tag.Id == SelectedTagId ? ImGuiTreeNodeFlags.Selected : ImGuiTreeNodeFlags.None);

        using var treeNode = ImRaii.TreeNode($"{tag.Name} ({count})##PresetBrowser_SideBar_Tag{tag.Id}", treeNodeFlags);
        if (treeNode == null || !treeNode.Success)
            return;

        if (ImGui.IsItemClicked())
        {
            SelectedTagId = tag.Id;
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source != null && source.Success)
            {
                ImGui.TextUnformatted($"Moving {tag.Name}");

                var idPtr = Marshal.StringToHGlobalAnsi(tag.Id.ToString());
                ImGui.SetDragDropPayload("MoveTag", idPtr, (uint)MemoryUtils.strlen(idPtr));
                Marshal.FreeHGlobal(idPtr);
            }
        }

        using (var target = ImRaii.DragDropTarget())
        {
            if (target != null && target.Success)
            {
                var payload = ImGui.AcceptDragDropPayload("MoveTag");
                if (payload.NativePtr != null && payload.IsDelivery() && payload.Data != 0)
                {
                    var tagId = Marshal.PtrToStringAnsi(payload.Data, payload.DataSize);
                    reorderTagOldIndex = Config.PresetTags.IndexOf((tag) => tag.Id.ToString() == tagId);
                    reorderTagNewIndex = Config.PresetTags.IndexOf(tag);
                }

                payload = ImGui.AcceptDragDropPayload("MovePresetCard");
                if (payload.NativePtr != null && payload.IsDelivery() && payload.Data != 0)
                {
                    var presetId = Marshal.PtrToStringAnsi(payload.Data, payload.DataSize);
                    var preset = Config.Presets.FirstOrDefault((preset) => preset?.Id.ToString() == presetId, null);
                    if (preset != null)
                    {
                        preset.Tags.Add(tag.Id);
                        Plugin.Config.Save();
                    }
                }
            }
        }

        if (ImGui.BeginPopupContextItem($"##PresetBrowser_SideBar_Tag{tag.Id}Popup"))
        {
            if (ImGui.MenuItem("Create Tag"))
            {
                CreateTagDialog.Open();
            }

            if (ImGui.MenuItem("Rename Tag"))
            {
                RenameTagDialog.Open(tag);
            }

            if (ImGui.MenuItem("Remove Tag"))
            {
                DeleteTagDialog.Open(tag);
            }

            if (ImGui.MenuItem("Remove unused Tags"))
            {
                removeUnusedTags = true;
            }

            ImGui.EndPopup();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(4);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.TextUnformatted(FontAwesomeIcon.Tag.ToIconString());
        }
    }

    private void DrawPresetBrowserSidebar()
    {
        var style = ImGui.GetStyle();

        var removeUnusedTags = false;

        ImGui.TextColored(ImGuiUtils.ColorGold, "Tags");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - style.ItemSpacing.Y + 3);
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);

        using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var child = ImRaii.Child("##PresetBrowser_SideBar", ImGui.GetContentRegionAvail() - style.ItemInnerSpacing);
        if (!child.Success)
            return;
        framePadding?.Dispose();

        DrawAllTag(ref removeUnusedTags);

        foreach (var tag in Config.PresetTags)
        {
            DrawSidebarTag(tag, ref removeUnusedTags);
        }

        if (reorderTagOldIndex > -1 && reorderTagOldIndex < Config.PresetTags.Count && reorderTagNewIndex > -1 && reorderTagNewIndex < Config.PresetTags.Count)
        {
            var item = Config.PresetTags[reorderTagOldIndex];
            Config.PresetTags.RemoveAt(reorderTagOldIndex);
            Config.PresetTags.Insert(reorderTagNewIndex, item);
            Plugin.Config.Save();
            reorderTagOldIndex = -1;
            reorderTagNewIndex = -1;
        }

        if (removeUnusedTags)
            RemoveUnusedTags();
    }

    private void DrawAllTag(ref bool removeUnusedTags)
    {
        var treeNodeFlags =
            ImGuiTreeNodeFlags.SpanAvailWidth |
            ImGuiTreeNodeFlags.FramePadding |
            ImGuiTreeNodeFlags.DefaultOpen |
            ImGuiTreeNodeFlags.Leaf |
            (SelectedTagId == null ? ImGuiTreeNodeFlags.Selected : ImGuiTreeNodeFlags.None);

        using var allTreeNode = ImRaii.TreeNode($"All ({Config.Presets.Count})##PresetBrowser_SideBar_All", treeNodeFlags);
        if (allTreeNode == null || !allTreeNode.Success)
            return;

        if (ImGui.IsItemClicked())
            SelectedTagId = null;

        if (ImGui.BeginPopupContextItem("##PresetBrowser_SideBar_AllPopup"))
        {
            if (ImGui.MenuItem("Create Tag"))
                CreateTagDialog.Open();

            if (ImGui.MenuItem("Remove unused Tags"))
                removeUnusedTags = true;

            ImGui.EndPopup();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(4);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.TextUnformatted(FontAwesomeIcon.Tags.ToIconString());
        }
    }

    private static void RemoveUnusedTags()
    {
        foreach (var tag in Config.PresetTags.ToArray())
        {
            var isUsed = false;

            foreach (var preset in Config.Presets)
            {
                if (preset.Tags.Contains(tag.Id))
                {
                    isUsed = true;
                    break;
                }
            }

            if (!isUsed)
                Config.PresetTags.Remove(tag);
        }

        Plugin.Config.Save();
    }

    private void DrawPresetBrowserContent()
    {
        var style = ImGui.GetStyle();

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + style.ItemSpacing.X);
        ImGui.TextColored(ImGuiUtils.ColorGold, "Presets");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - style.ItemSpacing.Y + 3);
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);

        using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var child = ImRaii.Child("##PresetBrowser_Content", ImGui.GetContentRegionAvail());
        if (child == null || !child.Success)
            return;
        framePadding?.Dispose();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);
        ImGui.Indent(style.ItemSpacing.X);

        var presetCards = Config.Presets
            .Where((preset) => (SelectedTagId == null || preset.Tags.Contains(SelectedTagId.Value)) && preset.Preset != null)
            .Select((preset) =>
            {
                if (!PresetCards.TryGetValue(preset.Id, out var card))
                {
                    PresetCards.Add(preset.Id, new(this, preset));
                }

                return card;
            })
            .ToArray();

        var presetsPerRow = 3;
        var availableWidth = ImGui.GetContentRegionAvail().X - style.ItemInnerSpacing.X * presetsPerRow;

        var presetWidth = availableWidth / presetsPerRow;
        var scale = presetWidth / PresetCard.PortraitSize.X;

        ImGuiListClipperPtr clipper;
        unsafe
        {
            clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        }

        clipper.Begin((int)Math.Ceiling(presetCards.Length / (float)presetsPerRow), PresetCard.PortraitSize.Y * scale);
        while (clipper.Step())
        {
            for (var row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
            {
                using (ImRaii.Group())
                {
                    for (int i = 0, index = row * presetsPerRow; i < presetsPerRow && index < presetCards.Length; i++, index++)
                    {
                        presetCards[index]?.Draw(scale);

                        if (i < presetsPerRow - 1 && index + 1 < presetCards.Length)
                            ImGui.SameLine(0, style.ItemInnerSpacing.X);
                    }
                }
            }
        }
        clipper.Destroy();

        ImGui.Unindent();
    }
}
