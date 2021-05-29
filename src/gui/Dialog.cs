using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Chardis.Gui
{
    public class Dialog : GuiDialogBlockEntityInventory
    {
        private const int Padding = 15;
        private const int StackListHeight = 300;
        private const string StackListKey = "stackList";
        private const string SearchBoxKey = "search";
        private const string ScrollBarKey = "scrollbar";
        private const int GridColumns = 8;
        private const int InsetSize = 3;

        private readonly ChardisBlockEntity _chardisBlockEntity;
  
        public override bool UnregisterOnClose => true;
        private readonly ItemSlot _upgradeSlot;
        private readonly IPlayer _player;
        private string _filter;

        public Dialog(ICoreClientAPI capi, IPlayer player, ChardisBlockEntity chardisBlockEntity) : base("", chardisBlockEntity.Inventory, chardisBlockEntity.Pos, 8, capi)
        {
            _chardisBlockEntity = chardisBlockEntity;
            _player = player;
            _filter = "";
            if (!string.IsNullOrEmpty(_chardisBlockEntity.ModConfig.UpgradeItemCode))
            {
                var upgradeItem = capi.World.GetItem(new AssetLocation(_chardisBlockEntity.ModConfig.UpgradeItemCode));
                if (upgradeItem != null)
                {
                    _upgradeSlot = new DummySlot(new ItemStack(upgradeItem, chardisBlockEntity.NumInstalledUpgrades));
                }
            }

            SetupDialog(chardisBlockEntity.NumSlots, chardisBlockEntity.NumInstalledUpgrades);
            chardisBlockEntity.NotifyInventoryResize += SetupDialog;
        }

        public override void OnGuiClosed()
        {
            _chardisBlockEntity.NotifyInventoryResize -= SetupDialog;
            DoSendPacket(_player.InventoryManager.CloseInventory(_chardisBlockEntity.Inventory));
            base.OnGuiClosed();
        }

        public override string ToggleKeyCombinationCode => null;

        private void FilterItems(string filter)
        {
            if (_filter == filter)
            {
                return;
            }

            _filter = filter;
            SetupDialog(_chardisBlockEntity.NumSlots, _chardisBlockEntity.NumInstalledUpgrades);

            var slotGrid = SingleComposer.GetSlotGrid(StackListKey);

            if (filter == "")
            {
                slotGrid.DetermineAvailableSlots();
            }
            else
            {
                slotGrid.FilterItemsBySearchText(filter);
            }
        }

        private void SetupDialog(int numSlots, int numInstalledUpgrades)
        {
            // title bar
            var bgBounds = ElementStdBounds.DialogBackground();
            var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithChild(bgBounds);

            int searchCaretPos;
            string searchText;
            bool searchHasFocus;
            if (SingleComposer == null)
            {
                SingleComposer = capi.Gui.CreateCompo("chardis", dialogBounds);
                searchCaretPos = 0;
                searchText = "";
                searchHasFocus = true;
            }
            else
            {
                var oldSearch = SingleComposer.GetTextInput(SearchBoxKey);
                searchCaretPos = oldSearch?.CaretPosInLine ?? 0;
                searchText = oldSearch?.GetText() ?? "";
                searchHasFocus = oldSearch?.HasFocus ?? true;
                SingleComposer.Clear(dialogBounds);
            }

            var numVisibleSlots = GetFilteredSlotCount();
            var rows = (int)Math.Ceiling(numVisibleSlots / (double) GridColumns);

            SingleComposer
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("chardis:title"), () => TryClose())
                .BeginChildElements(bgBounds);
            var titleBarBounds = ElementStdBounds.TitleBar();
            bgBounds.WithChild(titleBarBounds);

            // search text box
            var searchBounds = ElementBounds.FixedSize(200, 30).FixedUnder(titleBarBounds).WithFixedOffset(-InsetSize, 0);
            bgBounds.WithChild(searchBounds);
            SingleComposer.AddInteractiveElement(new TextBox(capi, searchBounds, FilterItems, CairoFont.WhiteSmallishText()), SearchBoxKey);

            var searchBox = SingleComposer.GetTextInput(SearchBoxKey);
            searchBox.SetPlaceHolderText(Lang.Get("chardis:search-placeholder"));
            searchBox.LoadValue(searchText);
            searchBox.SetCaretPos(searchCaretPos);
            if (searchHasFocus)
            {
                SingleComposer.FocusElement(SingleComposer.GetTextInput(SearchBoxKey).TabIndex);
            }

            if (_upgradeSlot != null)
            {
                var countBounds = ElementBounds.FixedSize(32, 32).FixedUnder(titleBarBounds, 10).WithAlignment(EnumDialogArea.RightFixed);
                bgBounds.WithChild(countBounds);
                SingleComposer.AddInteractiveElement(new UpgradeSlot(capi, countBounds, _upgradeSlot));
            }

            // stacklist
            var stacklistBounds =
                ElementBounds.FixedSize(410, StackListHeight).FixedUnder(searchBounds, Padding);
            bgBounds.WithChild(stacklistBounds);
            var clipBounds = stacklistBounds.ForkBoundingParent();
            var insetBounds = stacklistBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);
            var scrollbarBounds =
                insetBounds.CopyOffsetedSibling(3 + stacklistBounds.fixedWidth + 7).WithFixedWidth(20);

            var pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
            var inv = _chardisBlockEntity.Inventory;
            var slotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, 40 + pad, GridColumns, 1).FixedGrow(2 * pad, 0);

            SingleComposer
                .BeginClip(clipBounds)
                .AddInset(insetBounds, InsetSize)
                .AddItemSlotGrid(
                    inv,
                    DoSendPacket,
                    GridColumns,
                    slotBounds,
                    StackListKey
                    )
                .EndClip()
                ;

            var stacklist = SingleComposer.GetSlotGrid(StackListKey);
            SingleComposer.AddVerticalScrollbar(value =>
                {
                    if (SingleComposer.GetSlotGrid(StackListKey).Bounds.fixedHeight > StackListHeight)
                    {
                        stacklist.Bounds.fixedY = InsetSize - value;
                    }
                    else
                    {
                        stacklist.Bounds.fixedY = 0;
                    }
                    stacklist.Bounds.CalcWorldBounds();
                }, scrollbarBounds, ScrollBarKey)
                ;

            stacklist.Bounds.fixedHeight = rows * (GuiElementPassiveItemSlot.unscaledSlotSize +
                                                   GuiElementItemSlotGridBase.unscaledSlotPadding);
            stacklist.Bounds.CalcWorldBounds();
            var scrollbar = SingleComposer.GetScrollbar(ScrollBarKey);
            scrollbar.SetHeights(
                StackListHeight,
                Math.Max(StackListHeight, (float)stacklist.Bounds.fixedHeight)
            );
            scrollbar.OnMouseWheel(capi, new MouseWheelEventArgs { deltaPrecise = 999999 }); // scroll to top hax

            SingleComposer.EndChildElements(); // bgBounds
            SingleComposer.Compose();
        }

        private int GetFilteredSlotCount()
        {
            return string.IsNullOrEmpty(_filter) ? _chardisBlockEntity.Inventory.Count : _chardisBlockEntity.Inventory.Count(slot => slot?.Itemstack?.MatchesSearchText(capi.World, _filter) ?? false);
        }
    }
}