[<AutoOpen>]
module GameObjects

    open Microsoft.Xna.Framework
    open Microsoft.Xna.Framework.Graphics

    [<AutoOpen>]
    module Weapons = 

        type WeaponBase = {
            Range : float
            StartingAmmo : int
            CurrentAmmo : int
        }

        type Weapon = 
            | Sword       of WeaponBase
            | Pistol      of WeaponBase
            | Shotgun     of WeaponBase
            | SMG         of WeaponBase
            | MachineGun  of WeaponBase
            | SniperRifle of WeaponBase
            | Mortar      of WeaponBase

    [<AutoOpen>]
    module Dudes = 

        type Team = 
            | RedTeam
            | BlueTeam

        type Dude = {
            Team   : Team
            Weapon : Weapon

            Position       : Vector2
            WeaponPosition : Vector2
            HitPosition    : Vector2
        
            DrawRectangle : Rectangle
            Texture       : Texture2D
        }