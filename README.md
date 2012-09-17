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
- Solve dude selection on mobile when zoomed out
- Pre-battle intro (zoom/pan from fully zoomed out to full/half zoom on spawn point depending on map size)
- Campaign mode
  - Campaign Scenario select screen (Scenario = one "level"/map)
    - Needs async content loading for map preview and Scenario deserialization
    - Launch into CampaignGameplayScreen
    - Accept ScenarioResult back from CampaignGameplayScreen (including stats on the game... time, deaths, etc)
    - Scenario needs a performance rating (ie. out of three stars)                                                                                      
- Tutorials (ugh)
- Quick play mode (can set AI difficulty, map etc.)
- Get some graphics done!
- Music for menus, win + loss
- Investigate P2P networking. Possibly using Lidgren networking library. Might require rewrite of some core game classes.
- Windows phone 7 port
- Monogame port (Win 8 Marketplace App)
- Windows phone 8 port... initially deploy WP7 version, then roll to MonoGame when WP8 support is complete
- Other platforms if it makes enough bux to buy MonoTouch/MonoDroid 