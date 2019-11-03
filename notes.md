# Data flows

## XP Gain
1. Base XP value is calculated when NPC is spawned (based on base stats - does not scale with expert or number of players)
2. During NPCLoot, server calculates reward based on the number of eligible players and rounds up, then adds to xp out buffer (server global rate is applied here)
3. At set intervals, server sends accumulated XP to clients
4. Client applies any player-specific bonuses and rounds up again
5. Client adds XP to character and active classes
