using System;
using Chardis.Gui;
using Chardis.Models;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Chardis
{
	public class ChardisBlockEntity : BlockEntityOpenableContainer
	{
		public delegate void Notify(int numSlots, int numInstalledUpgrades);

		public event Notify NotifyInventoryResize;

		public const string Name = "ChardisBlockEntity";

		public ModConfig ModConfig { get; private set; }

		private ChardisInventory _inventory;
		public override InventoryBase Inventory => _inventory;
		public override string InventoryClassName => "chardis";
		public int NumInstalledUpgrades { get; private set; }
		public int NumSlots { get; private set; }

		public override void Initialize(ICoreAPI api)
		{
			SetupInventory(api);
			base.Initialize(api);
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
		{
			Api = worldForResolving.Api;
			NumInstalledUpgrades = tree.GetInt("numInstalledUpgrades", NumInstalledUpgrades);
			NumSlots = tree.GetInt("numSlots", NumSlots);
			SetupInventory(worldForResolving.Api);
			base.FromTreeAttributes(tree, worldForResolving);
			_inventory.FromTreeAttributes(tree);
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			_inventory.ToTreeAttributes(tree);
			tree.SetInt("numInstalledUpgrades", NumInstalledUpgrades);
			tree.SetInt("numSlots", _inventory.Count);
		}

		private void SetupInventory(ICoreAPI api)
		{
			if (_inventory == null)
			{
				if (!(api.ModLoader.GetModSystem(typeof(Chardis).FullName) is Chardis chardis))
				{
					throw new NullReferenceException("Could not get Chardis Mod");
				}

				ModConfig = chardis.ModConfig;
				if (NumSlots == 0)
				{
					NumSlots = ModConfig.BaseSlots;
				}

				_inventory = new ChardisInventory("chardis", Block.Id.ToString(), api, NumSlots);

				if (api is ICoreClientAPI)
				{
					chardis.OnInventoryResize += message =>
					{
						if (message.ChardisBlockPos == Pos)
						{
							ResizeInventory(message.NumSlots, message.NumInstalledUpgrades);
						}
					};
				}
			}
			else
			{
				ResizeInventory(NumSlots, NumInstalledUpgrades);
			}
		}

		private bool ResizeInventory(int numSlots, int numInstalledUpgrades)
		{
			NumInstalledUpgrades = numInstalledUpgrades;

			if (numSlots != NumSlots && !_inventory.InitSlots(numSlots))
			{
				NotifyInventoryResize?.Invoke(NumSlots, numInstalledUpgrades);
				return false;
			}

			NumSlots = numSlots;
			if (Api is ICoreServerAPI sapi)
			{
				sapi.Network.GetChannel(Chardis.NetworkChannel)
					.BroadcastPacket(new InventoryResize { ChardisBlockPos = Pos, NumSlots = NumSlots, NumInstalledUpgrades = NumInstalledUpgrades });
			}
			NotifyInventoryResize?.Invoke(numSlots, numInstalledUpgrades);
			return true;
		}

		public override bool OnPlayerRightClick(IPlayer player, BlockSelection blockSel)
		{
			var itemStack = player.InventoryManager.ActiveHotbarSlot?.Itemstack;
			if (Api is ICoreServerAPI sapi)
			{
				if (itemStack?.Collectible?.Code?.ToString() == ModConfig.UpgradeItemCode)
				{
					// itemStack can't be null here, but keep my IDE happy.
					if (player.WorldData?.CurrentGameMode != EnumGameMode.Creative && itemStack != null)
					{
						itemStack.StackSize -= 1;
						if (itemStack.StackSize <= 0)
						{
							player.InventoryManager.ActiveHotbarSlot.Itemstack = null;
						}

						player.InventoryManager.ActiveHotbarSlot.MarkDirty();
					}

					var newCount = ModConfig.BaseSlots + (NumInstalledUpgrades + 1) * ModConfig.SlotsPerUpgrade;
					if (!ResizeInventory(newCount, NumInstalledUpgrades + 1))
					{
						return false;
					}
					sapi.SendMessage(player, 0, "Installed upgrade.", EnumChatType.CommandSuccess);
					sapi.World.PlaySoundAt(new AssetLocation("game", "sounds/block/teleporter.ogg"), player.Entity, randomizePitch: false);
					return true;
				}

				player.InventoryManager.OpenInventory(_inventory);
				sapi.Network.GetChannel(Chardis.NetworkChannel)
					.SendPacket(new InventoryResize { ChardisBlockPos = Pos, NumSlots = NumSlots, NumInstalledUpgrades = NumInstalledUpgrades }, player as IServerPlayer);
			}
			else if (Api.World.Api is ICoreClientAPI capi)
			{
				if (itemStack?.Collectible?.Code?.ToString() == ModConfig.UpgradeItemCode)
				{
					return true;
				}

				player.InventoryManager.OpenInventory(_inventory);
				var dialog = new Dialog(capi, player, this);
				if (!dialog.TryOpen())
				{
					// xxx - do something??
				}
			}

			return true;
		}

		public ItemStack[] GetDrops()
		{
			var upgradeItem = Api.World.GetItem(new AssetLocation(ModConfig.UpgradeItemCode));
			if (upgradeItem == null)
			{
				return null;
			}

			var itemStacks = new ItemStack[NumInstalledUpgrades];
			for (var i = 0; i < NumInstalledUpgrades; i += 1)
			{
				itemStacks[i] = new ItemStack(upgradeItem);
			}

			return itemStacks;
		}
	}
}
