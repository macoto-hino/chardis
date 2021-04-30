using Chardis.Models;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

[assembly: ModInfo( "ChARDIS" )]

namespace Chardis
{
	public delegate void OnInventoryResize(InventoryResize message);

	public class Chardis : ModSystem
	{
		public const string NetworkChannel = "chardis";
		public ModConfig ModConfig;

		public event OnInventoryResize OnInventoryResize;

		public override void Start(ICoreAPI api)
		{
			ModConfig = ModConfig.Load(api);

			api.Network.RegisterChannel(NetworkChannel)
				.RegisterMessageType(typeof(InventoryResize))
			;

			api.RegisterBlockEntityClass(ChardisBlockEntity.Name, typeof(ChardisBlockEntity));
			api.RegisterBlockClass(ChardisBlock.Name, typeof(ChardisBlock));
		}
		
		public override void StartClientSide(ICoreClientAPI api)
		{
			api.Network.GetChannel(NetworkChannel)
				.SetMessageHandler<InventoryResize>(message =>
				{
					OnInventoryResize?.Invoke(message);
				});
		}
	}
}
