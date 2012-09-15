Fodder
======

Lemmings/Tower Defense type game

Requirements
------------
VS2010
XNA Game Studio 4
Windows Phone 7.1 SDK
Portable Class Library extension (Search "Portable" in Extensions Manager/Online)


The List!
---------

- Scaling factor in GameSession for SourceRects (Phone version assets will be 60% the size of HD versions)
- Gamestate Management with touch/mouse support for menus
- Soul Powers
  - Speed up cooldowns for 15 seconds (20 souls)
  - Rain down meteors that only hurt the opposing team (40 souls)
  - Spawn elite squad of 5 dudes, with weapons, boosted, shielded (60 souls)
- Menus will have AI v AI gameplay behind them (special single screen map)
- Campaign mode
- Quick play mode (can set AI difficulty, map etc.)
- Get some graphics done!
- Music for menus, win + loss
- Investigate P2P networking. Possibly using Lidgren networking library. Might require rewrite of some core game classes.
- Windows phone 7 port
- Monogame port (Win 8 Marketplace App)
- Windows phone 8 port... initially deploy WP7 version, then roll to MonoGame when WP8 support is complete
- Other platforms if it makes enough bux to buy MonoTouch/MonoDroid 