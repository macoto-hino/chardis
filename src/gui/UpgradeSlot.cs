using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Chardis.Gui
{
    public class UpgradeSlot : GuiElement
    {
        private readonly ItemSlot _itemSlot;

        public UpgradeSlot(ICoreClientAPI capi, ElementBounds bounds, ItemSlot itemSlot) : base(capi, bounds)
        {
            _itemSlot = itemSlot;
        }

        public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic) => Bounds.CalcWorldBounds();

        public override void RenderInteractiveElements(float deltaTime)
        {
            api.Render.RenderItemstackToGui(_itemSlot, Bounds.absX, Bounds.absY, 90, (float) Bounds.fixedHeight,
                ColorUtil.WhiteArgb);
        }
    }
}