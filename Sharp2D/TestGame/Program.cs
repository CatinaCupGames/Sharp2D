﻿using Sharp2D.Core.Graphics;
using Sharp2D.Core.Logic;
using Sharp2D.Core.Settings;
using Sharp2D.Core.Utils;

namespace TestGame
{
    class Program
    {
        public static TestSprite spriteSause;
        public static void Main(string[] args)
        {
            Sharp2D.Game.Sprites.SpriteRenderJob.SetDefaultJob<Sharp2D.Game.Sprites.OpenGL3SpriteRenderJob>();

            ScreenSettings settings = new ScreenSettings();
            settings.UseOpenTKLoop = false;

            Screen.DisplayScreenAsync(settings);

            System.Threading.Thread.Sleep(1000);

            TestWorld world = new TestWorld();

            world.Load();

            world.Display();

            Screen.Camera.Z = 2f;
            //world.AddLogical(new MoveCamera() { Start = Screen.TickCount });

            TestSprite idontevenknowanymore = new TestSprite();
            idontevenknowanymore.ChangeHitbox("PonyHitbox");
            idontevenknowanymore.X = 256;
            idontevenknowanymore.Y = 128;
            world.AddSprite(idontevenknowanymore);

            /*spriteSause = new TestSprite(); 
            spriteSause.X = 256;
            spriteSause.Y = 128;
            spriteSause.MoveFlag = true;

            
            TestSprite enemy = new TestSprite();
            enemy.ChangeHitbox("TriangleHitbox");
            enemy.X = 512;
            enemy.Y = 128;
            world.AddSprite(enemy);
            world.AddSprite(spriteSause);

            TestSprite eddie = new TestSprite();
            eddie.ChangeHitbox("UhidkHitbox");
            eddie.X = 512;
            eddie.Y = 512;
            world.AddSprite(eddie);*/

            Screen.Camera.Z = 100f;
            Screen.Camera.Y = -150f;

            //if (eddie.CurrentWorld != null)
            //    Logger.Debug(eddie.CurrentWorld.Name);

            world.AddLogical(new MoveCamera());

            System.Threading.Thread.Sleep(3000);
            idontevenknowanymore.Animations["happyFace"].Play()
                .Animations["hat"].Play();
            //world.AddLogical(new CheckKeys());
        }
    }

    class MoveCamera : ILogical
    {
        public long Start;
        public void Update()
        {
            Screen.Camera.X -= 2;
            Logger.WriteAt(0, 0, "FPS: " + Screen.FPS);
        }

        public void Dispose()
        {

        }
    }

    class CheckKeys : ILogical
    {
        public void Update()
        {
            if (Input.Keyboard["Jump"])
            {
                Logger.Log("I LIKE TURTLES!");
            }

            if (Input.Mouse["Shoot"])
            {
                Logger.Log("SCOOTALOO IS THE BEST PONY!");
            }
        }

        public void Dispose()
        {
            
        }
    }
}
