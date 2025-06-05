# Erenshor Co-Op
Brings Co-Op to Erenshor.

Please see the changelog for updates.<br><br><br>

## Current Features
- Player sync
- Enemy and NPC sync
- Chat (Including Whispers)
- Grouping
- Spell Effects
- Sims in groups are synced (only pre-made sims! If you invite sims that are auto-generated this will not work)
  - Only the host can invite sims to group
- HP and MP healing spells
- UI! Press escape in-game to see the UI (might be off-screen, please report)
- Boss adds
- A few additional spawns are synced
  - Malaroths
  - Chessboard
  - Siraethe wards
- Buffs/Debuffs
- Shared group XP
- Summons
  - When connecting or hosting you are required to re-summon for other players to see it.
- Trading
  - Pick up an item from the inventory or bank and drop it outside the UI
    - (Still need to add a confirmation window)
  - The items get deleted if you disconnect
  - They should appear again after a zone change, if not let me know
  - Confirmation window (can be disabled in the settings)

## Not Implemented
- Treasure Chests
- Group Chat

## Known Bugs
- Other players have sim chat behaviour
- There is a small game bug where sometimes when a player attacks an enemy with a spell, they will not get aggroed.

## Mention Worthy
- Skin Colors are not correctly applied (game bug).
- Interpolation is currently turned off. Expect some lagginess if you're connecting over long distances.
- Each player requires around ~20kb/s upload.
- The host will need around ~60kb/s upload per player.

## Usage
When in-game, use the UI that opens when pressing escape or alternatively:

 - host using ```/host <port>``` 
 - connect using ```/connect <ip> <port>``` 
 - disconnect using ```/disconnect```


## FAQ

    Q: How much bandwidth do I require to host?
    A: You will need around ~60kb/s upload for each player connected to you.

    Q: Can there be a standalone server?
    A: No, due to legal reasons this won't be possible.

    Q: How can I report issues, bugs or make a suggestion?
    A: Please either join the Erenshor discord and post your issues in the appropriate mod channel, or create an issue on Github.

    Q: Is this mod compatible with other mods?
    A: This is largely untested, and probably no.