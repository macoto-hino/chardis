using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Chardis.Gui
{
    public class TextBox : GuiElementTextInput
    {
        public TextBox(ICoreClientAPI capi, ElementBounds bounds, Vintagestory.API.Common.Action<string> OnTextChanged, CairoFont font) : base(capi, bounds, OnTextChanged, font)
        {
        }

        public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
        {
            if (args.Button == EnumMouseButton.Right)
            {
                api.World.RegisterCallback(delta => SetValue(""), 0);
                args.Handled = true;
            }
            else
            {
                base.OnMouseUpOnElement(api, args);
            }
        }
    }
}