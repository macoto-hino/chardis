using Vintagestory.API.Common;

[assembly: ModInfo( "ChARDIS" )]

namespace Chardis
{
	public class Chardis : ModSystem
	{
		public ModConfig ModConfig;

		public override void Start(ICoreAPI api)
		{
			ModConfig = ModConfig.Load(api);

			api.RegisterBlockEntityClass(ChardisBlockEntity.Name, typeof(ChardisBlockEntity));
			api.RegisterBlockClass(ChardisBlock.Name, typeof(ChardisBlock));
		}
	}
}
