using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Fodder.Core
{
    public class Function
    {
        public string Name;
        public double CoolDown;
        public bool IsEnabled;

        public Function(string name, double cd, bool enabled)
        {
            Name = name;
            CoolDown = cd;
            IsEnabled = enabled;
        }
    }

    public enum GameClientType
    {
        Human,
        AI,
        Network
    }

    public class GameSession
    {
        public static GameSession Instance;
        internal DudeController DudeController;
        internal ButtonController ButtonController;
        internal ParticleController ParticleController;
        internal ProjectileController ProjectileController;
        internal SoulController SoulController;
        internal HUD HUD;

        internal Map Map;

        internal GameClientType Team1ClientType;
        internal GameClientType Team2ClientType;
        internal int Team1Reinforcements;
        internal int Team2Reinforcements;
        internal int Team1StartReinforcements;
        internal int Team2StartReinforcements;
        internal double Team1SpawnRate;
        internal double Team2SpawnRate;

        internal int Team1ActiveCount;
        internal int Team2ActiveCount;
        internal int Team1PlantedCount;
        internal int Team2PlantedCount;
        internal int Team1DeadCount;
        internal int Team2DeadCount;

        internal bool Team1Win;
        internal bool Team2Win;

        internal Viewport Viewport;

        internal List<Function> AvailableFunctions;

        AIController AI1 = new AIController();
        AIController AI2 = new AIController();

        int lastScrollWheelValue = 0;

        public GameSession(GameClientType t1CT, GameClientType t2CT, double t1SpawnRate, double t2SpawnRate, int t1Reinforcements, int t2Reinforcements, List<Function> availableFunctions, string map, Viewport vp)
        {
            Instance = this;

            Team1ClientType = t1CT;
            Team2ClientType = t2CT;
            Team1Reinforcements = t1Reinforcements;
            Team2Reinforcements = t2Reinforcements;
            Team1StartReinforcements = t1Reinforcements;
            Team2StartReinforcements = t2Reinforcements;
            Team1SpawnRate = t1SpawnRate;
            Team2SpawnRate = t2SpawnRate;

            Team1DeadCount = 0;
            Team2DeadCount = 0;

            Team1Win = false;
            Team2Win = false;

            AvailableFunctions = availableFunctions;

            DudeController = new DudeController();
            ButtonController = new ButtonController();
            SoulController = new SoulController();
            ProjectileController = new ProjectileController();
            ParticleController = new ParticleController();
            HUD = new HUD();

            AI1.Initialize(5000);
            AI2.Initialize(5000);

            Viewport = vp;

            Map = new Map(map);
        }

        public void LoadContent(ContentManager content)
        {
            DudeController.LoadContent(content);
            ButtonController.LoadContent(content);
            SoulController.LoadContent(content);
            ProjectileController.LoadContent(content);
            ParticleController.LoadContent(content);
            HUD.LoadContent(content);

            Map.LoadContent(content);

            lastScrollWheelValue = Mouse.GetState().ScrollWheelValue;
        }

        public void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Reset();

            if (Mouse.GetState().ScrollWheelValue > lastScrollWheelValue) Map.DoZoom(0.05f, 0);
            if (Mouse.GetState().ScrollWheelValue < lastScrollWheelValue) Map.DoZoom(-0.05f, 0);
            lastScrollWheelValue = Mouse.GetState().ScrollWheelValue;

            if (Keyboard.GetState().IsKeyDown(Keys.A)) Map.ScrollPos.X -= (10f);
            if (Keyboard.GetState().IsKeyDown(Keys.D)) Map.ScrollPos.X += (10f);

            Map.Update(gameTime);
            DudeController.Update(gameTime);

            if (Team1ClientType == GameClientType.AI) AI1.Update(gameTime, 0);
            else if (Team1ClientType == GameClientType.Human) DudeController.HandleInput(Mouse.GetState(), 0);
            if (Team2ClientType == GameClientType.AI) AI2.Update(gameTime, 1);
            else if (Team1ClientType == GameClientType.Human) DudeController.HandleInput(Mouse.GetState(), 1);

            ButtonController.Update(gameTime);
            ButtonController.HandleInput(Mouse.GetState(), Keyboard.GetState());

            SoulController.Update(gameTime);
            HUD.Update(gameTime);
            ProjectileController.Update(gameTime);
            ParticleController.Update(gameTime);

            CalculateWinConditions(gameTime);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Map.DrawBG(spriteBatch);
            DudeController.DrawShield(spriteBatch);
            Map.DrawFG(spriteBatch);
            SoulController.Draw(spriteBatch);
            DudeController.Draw(spriteBatch);
            ProjectileController.Draw(spriteBatch);
            ParticleController.Draw(spriteBatch);

            // UI always comes last!
            ButtonController.Draw(spriteBatch);
            HUD.Draw(spriteBatch);
        }

        public void Reset()
        {
            Team1Reinforcements = Team1StartReinforcements;
            Team2Reinforcements = Team2StartReinforcements;

            Team1DeadCount = 0;
            Team2DeadCount = 0;

            Team1Win = false;
            Team2Win = false;

            AI1.Reset();
            AI2.Reset();

            DudeController.Reset();
            ProjectileController.Reset();
            ParticleController.Reset();
            ButtonController.Reset();
            SoulController.Reset();
        }

        internal void CalculateWinConditions(GameTime gameTime)
        {
            Team1ActiveCount = 0;
            Team2ActiveCount = 0;
            Team1PlantedCount = 0;
            Team2PlantedCount = 0;
            foreach (Dude d in DudeController.Dudes)
            {
                if (d.Active)
                {
                    if (d.Team == 0) Team1ActiveCount++; else Team2ActiveCount++;

                    if(d.Weapon.GetType() == typeof(Weapons.Sniper) || 
                       d.Weapon.GetType() == typeof(Weapons.MachineGun) ||
                       d.Weapon.GetType() == typeof(Weapons.Mortar))
                        if(!d.Weapon.IsInRange)
                            if (d.Team == 0) Team1PlantedCount++; else Team2PlantedCount++;

                    if (d.Team == 0 && d.Position.X > Map.Width) Team1Win = true;
                    if (d.Team == 1 && d.Position.X < 0) Team2Win = true;
                }
            }

            if(Team1Reinforcements==0 && Team2Reinforcements==0)
            {
                if (Team1ActiveCount == 0 || Team2ActiveCount == 0)
                {
                    if (Team2ActiveCount > Team1ActiveCount) Team2Win = true;
                    if (Team1ActiveCount > Team2ActiveCount) Team1Win = true;
                    if (Team1ActiveCount == Team2ActiveCount) { Team1Win = true; Team2Win = true; }
                }
                else
                {
                    if ((Team1ActiveCount - Team1PlantedCount) == 0 && (Team2ActiveCount - Team2PlantedCount) == 0)
                    {
                        if (Team2ActiveCount > Team1ActiveCount) Team2Win = true;
                        if (Team1ActiveCount > Team2ActiveCount) Team1Win = true;
                        if (Team1ActiveCount == Team2ActiveCount) { Team1Win = true; Team2Win = true; }
                    }
                }
            }


        }
       
    }
}
