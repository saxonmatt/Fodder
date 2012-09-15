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

namespace Fodder.Core.Weapons
{
    class MachineGun:BaseWeapon
    {

        public MachineGun(Dude owner) : base(owner)
        {
            Range = 500f;
            StartingAmmo = 100;
            CurrentAmmo = 100;
            WeaponOffset = new Vector2(5, -20) * GameSession.Instance.ScaleFactor;
            FeetPlanted = true;

            _targetAttackTime = 50;
            _isPlantingWeapon = true;

        }

        public override void Draw(SpriteBatch sb)
        {
            sb.Draw(Owner.texDude, Owner._screenRelativePosition - (new Vector2(0, 80 * GameSession.Instance.ScaleFactor) * GameSession.Instance.Map.Zoom), new Rectangle((int)(140 * GameSession.Instance.ScaleFactor), 0, (int)(20 * GameSession.Instance.ScaleFactor), (int)(20 * GameSession.Instance.ScaleFactor)),
                     Color.White,
                     0f,
                     new Vector2(10, 0),
                     1f,
                     SpriteEffects.None, 0);

            base.Draw(sb);
        }

        public override void Attack(Dude targetDude)
        {
            Vector2 velocity = (targetDude.WeaponPosition - Owner.WeaponPosition);
            velocity.Y += ((float)(ProjectileController.Rand.NextDouble() * 10))-5f;
            velocity.Normalize();

            GameSession.Instance.ProjectileController.Add(Owner.WeaponPosition,
                                     velocity * 20f,
                                     0.75f, false, false, 35, Owner.Team);
            CurrentAmmo--;

            AudioController.PlaySFX("machinegun", 0.5f * GameSession.Instance.Map.Zoom, ((float)GameSession.Instance.DudeController.Rand.NextDouble() / 2f) + 0.4f, ((2f / GameSession.Instance.Viewport.Width) * Owner._screenRelativePosition.X) - 1f);

            base.Attack(targetDude);
        }

    }
}

