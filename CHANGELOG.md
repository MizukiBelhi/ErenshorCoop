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