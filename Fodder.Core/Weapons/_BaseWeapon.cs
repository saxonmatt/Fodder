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
    public class BaseWeapon
    {
        public float Range;
        public int StartingAmmo;
        public int CurrentAmmo;
        public bool FeetPlanted;
        public Dude Owner;
        public Vector2 WeaponOffset;
        public bool IsInRange;

        internal double _targetAttackTime;
        internal double _currentAttackTime;
        internal bool _isPlantingWeapon = false;
        internal bool _needsLOS = true;

        public BaseWeapon(Dude owner)
        {
            Owner = owner;

            // in the inherited classes, set FeetPlanted and _isPlantingWeapon if it's a planting weapon (ie mortar)
        }

        public virtual void Update(GameTime gameTime)
        {
            if (CurrentAmmo == -1 || CurrentAmmo > 0)
            {
                _currentAttackTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                if (_currentAttackTime >= _targetAttackTime * (Owner.BoostTime>0?0.5:1))
                {
                    Dude targetDude = GameSession.Instance.DudeController.EnemyInRange(Owner, Range, _needsLOS);
                    if (targetDude!=null)
                    {
                        IsInRange = true;
                        Attack(targetDude);
                        _currentAttackTime = 0;
                    }
                    else
                    {
                        IsInRange = false;
                        if (!_isPlantingWeapon) FeetPlanted = false;
                    }
                }
            }
            else
            {
                IsInRange = false;
                Owner.GiveWeapon("sword");
            }        
        }

        public virtual void Draw(SpriteBatch sb)
        {
           
        }

        public virtual void Attack(Dude targetDude)
        {
            FeetPlanted = true;
        }

    }
}

