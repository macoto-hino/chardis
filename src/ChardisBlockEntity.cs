using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Chardis.Gui;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Chardis
{
	public class ChardisBlockEntity : BlockEntityGenericTypedContainer
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
			Api = api;
			ModConfig = api.ModLoader.GetModSystem<Chardis>().ModConfig;
			if (NumSlots == 0)
			{
				NumSlots = ModConfig.BaseSlots + NumInstalledUpgrades * ModConfig.SlotsPerUpgrade;
			}
			base.Initialize(api);
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
		{
			Api = worldForResolving.Api;
			ModConfig = Api.ModLoader.GetModSystem<Chardis>().ModConfig;
			NumInstalledUpgrades = tree.GetInt("numInstalledUpgrades");
			NumSlots = tree.GetInt("numSlots", ModConfig.BaseSlots + NumInstalledUpgrades * ModConfig.SlotsPerUpgrade);
			Pos = new BlockPos(tree.GetInt("posx"), tree.GetInt("posy"), tree.GetInt("posz"));

			base.FromTreeAttributes(tree, worldForResolving);
			_inventory.InitSlots(NumSlots);
			NotifyInventoryResize?.Invoke(NumSlots, NumInstalledUpgrades);
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			tree.SetInt("numInstalledUpgrades", NumInstalledUpgrades);
			tree.SetInt("numSlots", _inventory.Count);
		}

		protected override void InitInventory(Block block)
		{
			if (_inventory != null)
			{
				return;
			}

			_inventory = new ChardisInventory("chardis", block.Id.ToString(), Api, NumSlots);

			GetType().GetField("inventory", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(this, _inventory);
			_inventory.BaseWeight = 1f;
			_inventory.OnGetSuitability = (sourceSlot, targetSlot, isMerge) => (float) ((isMerge ? _inventory.BaseWeight + 3.0 : _inventory.BaseWeight + 1.0) + (sourceSlot.Inventory is InventoryBasePlayer ? 1.0 : 0.0));
			_inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;
			if (block.Attributes != null)
			{
				if (block.Attributes["spoilSpeedMulByFoodCat"][type].Exists)
				{
					_inventory.PerishableFactorByFoodCategory = block.Attributes["spoilSpeedMulByFoodCat"][type].AsObject<Dictionary<EnumFoodCategory, float>>();
				}

				if (block.Attributes["transitionSpeedMulByType"][type].Exists)
				{
					_inventory.TransitionableSpeedMulByType = block.Attributes["transitionSpeedMulByType"][type].AsObject<Dictionary<EnumTransitionType, float>>();
				}
			}
			_inventory.PutLocked = retrieveOnly;
			_inventory.OnInventoryClosed += OnInvClosed;
			_inventory.OnInventoryOpened += OnInvOpened;
		}

		public override void OnReceivedServerPacket(int packetid, byte[] data) 
		{
			var world = (IClientWorldAccessor) Api.World;
			// this packet seems to be treated specially... I attempted removing the unnecessary data and using a custom packet id but it caused bugs. TODO: revisit this later.
			if (packetid == 5000)
			{
				if (invDialog != null)
				{
					if (invDialog?.IsOpened() == true)
					{
						invDialog?.TryClose();
					}
					invDialog?.Dispose();
					invDialog = null;
					return;
				}
				var treeAttribute = new TreeAttribute();
				using (var memoryStream = new MemoryStream(data))
				{
					var stream = new BinaryReader(memoryStream);
					stream.ReadString();
					stream.ReadString();
					stream.ReadByte();
					treeAttribute.FromBytes(stream);
				}
				Inventory.FromTreeAttributes(treeAttribute);
				Inventory.ResolveBlocksOrItems();
				invDialog = new Dialog(Api as ICoreClientAPI, (Api as ICoreClientAPI)?.World.Player, this);
				invDialog.TryOpen();
			}

			if (packetid != 1001)
			{
				return;
			}
			world.Player.InventoryManager.CloseInventory(Inventory);
			if (invDialog?.IsOpened() == true)
			{
				invDialog?.TryClose();
			}

			invDialog?.Dispose();
			invDialog = null;
		}

		private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace) => atBlockFace == BlockFacing.DOWN ? _inventory.FirstOrDefault(slot => !slot.Empty) : null;

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

					NumInstalledUpgrades += 1;
					NumSlots = ModConfig.BaseSlots + (NumInstalledUpgrades + 1) * ModConfig.SlotsPerUpgrade;
					_inventory.InitSlots(NumSlots);
					MarkDirty();

					sapi.SendMessage(player, 0, "Installed upgrade.", EnumChatType.CommandSuccess);
					sapi.World.PlaySoundAt(new AssetLocation("game", "sounds/block/teleporter.ogg"), player.Entity, randomizePitch: false);
					return true;
				}

				return base.OnPlayerRightClick(player, blockSel);
			}

			if (itemStack?.Collectible?.Code?.ToString() == ModConfig.UpgradeItemCode)
			{
				return true;
			}

			return base.OnPlayerRightClick(player, blockSel);
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
