Fodder
======

Lemmings/Tower Defense type game

Requirements
------------
- VS2010
- XNA Game Studio 4
- Windows Phone 7.1 SDK
- Portable Class Library extension (Search "Portable" in Extensions Manager/Online)


The List!
---------
- Solve dude selection on mobile when zoomed out (partially solved, needs more testing and tweaking)
- UX (Controls)
  - Phone pinch should produce a ScaleFactor result to pass to Map.DoZoom                                                      - Phone drag should produce a Delta result to pass to Map.DoScroll
  - Scrolling needs to work in X and Y (currently not supported in Map class)
  - PC/Mouse controls should also support click+drag to scroll (as well as WASD) 
- More campaign missions!    
- Tutorials (ugh)
- Quick play mode (can set AI difficulty, map etc.)
- Get some graphics done!
- Music for menus, win + loss
- More SFX!
- Windows phone 7 port
- Monogame port (Win 8 Marketplace App, linux)
- Investigate P2P networking. Possibly using Lidgren networking library. Might require rewrite of some core game classes.
- Windows phone 8 port... initially deploy WP7 version, then roll to MonoGame when WP8 support is complete
- Other platforms if it makes enough bux to buy MonoTouch/MonoDroid 