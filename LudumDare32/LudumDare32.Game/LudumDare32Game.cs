using System.Threading.Tasks;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.EntityModel;
using SiliconStudio.Paradox.Engine;

namespace LudumDare32
{
    public class LudumDare32Game : Game
    {
        private Entity cameraEntity;
        private Level level;
        private Player player;
        private UiGameHud gameHud;

        public LudumDare32Game()
        {
            // Target 9.1 profile by default
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_9_1 };
            ConsoleLogMode = ConsoleLogMode.Always;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // For now lets set our virtual resolution the same as the actual resolution
            // But we may want to hard code this to some value
            VirtualResolution = new Vector3(GraphicsDeviceManager.PreferredBackBufferWidth, GraphicsDeviceManager.PreferredBackBufferHeight, 20f);

            // Create our camera, cause yay :D
            cameraEntity = new Entity("Camera") { 
                new CameraComponent() { UseProjectionMatrix = true, ProjectionMatrix = SpriteBatch.CalculateDefaultProjection(VirtualResolution) }
            };

            // Create our player entitiy
            var playerEntity = new Entity() {
                new SpriteComponent() { SpriteGroup = Asset.Load<SpriteGroup>("Temp"), CurrentFrame = 0 },
                new TransformationComponent() { Translation = new Vector3(100, 100, 0) }
            };
            // Make it so the engine knows about it
            Entities.Add(playerEntity);
            // A wrapper class for the entity that actual handles our stuff,
            // Yeah, components all the way might of been better, but for now this'll do, just
            // following JumpyJets example :P
            player = new Player(playerEntity, Input);

            LevelReader reader = new LevelReader("/data/TestMap.json");
            reader.Read();

            // Create our level
            var platforms = Asset.Load<SpriteGroup>("TileSetTest");
            level = new Level(new Size2(20, 11));

            for (int x = 0; x < 20; x++)
            {
                level.SetTile(x, 0, new Tile() { Sprite = platforms[0] });
                level.SetTile(x, 10, new Tile() { Sprite = platforms[0] });
            }

            level.SetTile(19, 9, new Tile() { Sprite = platforms[0] });

            level.BuildTileData();

            // Set up the rendering pipeline
            CreatePipeline();

            gameHud = new UiGameHud(Services);
            gameHud.LoadContent();
            // Kick off our update loop
            Script.Add(UpdateLoop);
        }

        private void RenderLevel(RenderContext context)
        {
            var sb = new SpriteBatch(GraphicsDevice);
            sb.Begin(SpriteSortMode.Texture, null);
            level.Draw(sb);
            sb.End();
        }

        private void CreatePipeline()
        {
            var levelRenderer = new DelegateRenderer(Services) { Render = RenderLevel };

            // Setup the default rendering pipeline
            RenderSystem.Pipeline.Renderers.Add(new CameraSetter(Services) { Camera = cameraEntity.Get<CameraComponent>() });
            RenderSystem.Pipeline.Renderers.Add(new RenderTargetSetter(Services) { ClearColor = Color.CornflowerBlue });
            RenderSystem.Pipeline.Renderers.Add(levelRenderer);
            RenderSystem.Pipeline.Renderers.Add(new SpriteRenderer(Services));
            RenderSystem.Pipeline.Renderers.Add(new UIRenderer(Services));
        }

        private async Task UpdateLoop()
        {
            while (IsRunning)
            {
                // Wait next rendering frame
                await Script.NextFrame();
                player.Update((float)UpdateTime.Elapsed.TotalSeconds);

                level.CheckCollision(player);

                gameHud.Update((float)PlayTime.TotalTime.TotalSeconds);
            }
        }
    }
}
