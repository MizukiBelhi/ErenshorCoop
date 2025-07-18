## v2.0.7
- Lobbies and direct connections are now being closed when the game is exited
- You can no longer interact with left-click while a prompt is open
- Changed error logs that aren't errors to non-errors

## v2.0.6
- Fixed sims sometimes not appearing (hopefully)
- Fixed sims being removed from group for other players
- Fixed sims not respawning with the player (hopefully)
- Fixed client settings not being saved
- Added additional checks so sims health is less likely to desync
- Added player markers!
  - Can be turned off in the settings
  - By default only group members have off-screen markers

## v2.0.5
- Fixed rare bug where you would get stuck with a black screen after zoning
- Fixed other players sims staying active after they zoned
- Fixed complaints about spawn data missing for mobs when you zone quickly
- Fixed other sims/players in different zones attacking mobs spamming the log
- Fixed players and sims still being in aggro lists after they zone
- Fixed zone ownership transfer being broken (hopefully)
- Fixed sims that have been removed from group still acting like they are in the group
- Fixed a bug where you could not respawn if a player in your group already respawned
  - Will not work if they respawn on the same map you died on
- Added additional checks so players health is less likely to desync
- Removed auto heal on other players/sims/mobs

## v2.0.4
- Added the ability to right click->join your friends on steam
- Fixed MP healing not working, also removed the text when healing for 0
- Fixed a bug where dropped items came back when they were looted
- Fixed a bug where you were unable to click on players or sims after they died
- Fixed other players/sims being able to revive mid combat (hopefully)
- You can no longer invite players and sims outside of your level range

## v2.0.3
- Fixed sims becoming a skateboard when dead
- Fixed sims not attacking under certain circumstances
- Fixed sims from players not taking damage
- Fixed sims showing two messages for gaining xp
- Fixed players not being able to heal
- Fixed clients not being removed from groups in steam lobbies
- Fixed error with wands
- Added further preventions against saving other players or sims
- Added further preventions to networked sims and players against navigation

## v2.0.2
- Fixed zoning breaking when a player is in the third group slot
- Fixed sims duplicating on zone entry
- Fixed a bug where the playerlist was updating icons incorrectly

## v2.0.1
- Fixed errors related to sims
- Fixed sim summons (hopefully)

## v2.0.0
- Added New UI!
  - Can be found in the escape menu
- Added Steam Lobby Support!
  - Lobbies have a maximum player count of 100
  - Supports Friend invites
  - Supports starting the game and auto joining through an invite
- Added Full Sim Sync!
  - You are now able to invite sims even if you're not the host
  - Does not work across zones unfortunately (yet)
  - Sims from other players are automatically kicked from the group when zoning
- Weather/Time sync is now slightly more accurate
- Removed Connect/Disconnect chat commands
- Added Kick/Ban commands
  - Can only be used in lobbies
- Added the ability to add moderators
  - Can only be used in lobbies
  - Enables them to kick/ban other players
- Fixed healing not resonating
- Fixed healing affecting charmed npcs not being synced
- Wand bolts are now visible for all players (i hope)
- Fixed damage sync being broken due to game update
- Players are no longer allowed to swap/delete/upgrade gear on sims from others
- Summons of players and sims will get sent to connecting players now

## v1.5.2
- Fixed an error with useable AE items

## v1.5.1
- Fixed non-damaging spells not adding aggro
- Fixed (hopefully) MP not properly being displayed when player joins
- Fixed treasure guards not syncing their stats

## v1.5.0
- Added treasure guardian sync
  - Stats are not synced, but should not cause issues
- Added Server Settings sync
- Added mod check
- Added group chat
- Added default port
- Added sound when receiving whispers from players
- Removed healing text from enemies
- Fixed adds being spawned twice for clients
- Fixed targeting causing battle music to play at wrong times
- Fixed summons not working
- Fixed summons not showing spell casts
- Fixed summons not being a valid target
- Fixed enemies not receiving spell effects
- Fixed /r working when receiving whispers from players
- Fixed host not redirecting packets only to players that should receive them
- Fixed error when going back to the menu regarding weather sync
- Changed text color in battle log to be easier to read
- Changed versioning numbers to correctly use semver

## v1.4.0
- Fixed players not being affected by status effects
- Fixed sims ignoring when players were being attacked while in group
- Fixed players not being able to heal sims
- Added being able to target players(and sims) target by using the "Assist MA" button
- Added nameplate flashing

## v1.3.1
- Fixed drops sticking around if only a single item was put and looted
- Fixed another duplication exploit
- Fixed players wanting that loot (sim chat behaviour, more to do here)

## v1.3.0
- Fixed not being able to select other players
- Fixed a duplication exploit
- Added Weather Sync
- Added Confirmation Window for item dropping
- Added Toggles for:
  - Confirmation Window
  - Metrics
  - Weather Sync

## v1.2.0
- Fixed taunts not working
- Fixed (hopefully) players sometimes not swapping visibility
- Fixed Sim duplication
- Added TRADING!
  - Pick up an item from the inventory or bank and drop it outside the UI
  - The items get deleted if you disconnect
  - They should appear again after a zone change, if not let me know

## v1.1.2
- Fixed UI getting pushed into the menu on small resolutions
- Fixed Zone manager thinking you're still hosting after stopping
- Fixed (hopefully) clients getting confused about zone ownership
- Fixed disbanding your group on certain occasions, even if disconnected

## v1.1.1
- Fixed desync on player respawn

## v1.1.0
- Fixed XP Gain
- Fixed potentially duplicating and wrong sims appearing when inviting them to group
- Fixed Healing MP/HP
- Fixed player not being removed from group if they disconnect
- Fixed Sims not being "cleaned" when the host moves zones
- Fixed players that are revived through group revive staying dead for others
- Added status effects for entities (Enemies, Sims, NPC)
- Added MP Sync

## v1.0.2
- Fixed host not being able to gain xp when party lead
- Removed some debug text
- Increased character limit on the UI (15 -> 20)

## v1.0.1
- Fixed wrong file being included for Thunderstore

## v1.0.0
- Initial release