using Terraria;
using Terraria.ModLoader;
using System;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses
{
    public class MyWorld : ModWorld
    {
        public const double TIME_BETWEEN_REQUEST_MSEC = 1000;
        public static DateTime timeLastRequests = DateTime.MinValue;

        public const double TIME_BETWEEN_SYNC_EXP_CHANGES_MSEC = 250;
        public static DateTime timeLastSyncExpChanges = DateTime.MinValue;

        public const double TIME_BETWEEN_SYNC_MAP_SETTINGS_MSEC = 20 * 1000;
        public static DateTime timeLastSyncMapSettings = DateTime.MinValue;

        public const double TIME_BETWEEN_SYNC_EXP_ALL_MSEC = 20 * 1000;
        public static DateTime timeLastSyncExpAll = DateTime.MinValue;

        public static bool expmapAuthCodeWritten = false;

        public static bool[] clientNeedsExpUpdate = new bool[256];
        public static int clientNeedsExpUpdate_counter = 0;
        public static int[] clientNeedsExpUpdate_indices = new int[256];

        public override TagCompound Save()
        {
            return new TagCompound {
                {"AUTH_CODE", ExperienceAndClasses.worldAuthCode},
                {"require_auth", ExperienceAndClasses.worldRequireAuth},
                {"global_exp_modifier", ExperienceAndClasses.worldExpModifier},
                {"global_ignore_caps", ExperienceAndClasses.worldIgnoreCaps},
                {"global_damage_reduction", ExperienceAndClasses.worldClassDamageReduction},
                {"global_level_cap", ExperienceAndClasses.worldLevelCap},
                {"global_death_penalty", ExperienceAndClasses.worldDeathPenalty},
                {"traceMap", ExperienceAndClasses.worldTrace},
            };
        }

        public override void Load(TagCompound tag)
        {
            ExperienceAndClasses.worldAuthCode = Commons.TryGet<double>(tag, "AUTH_CODE", -1);
            ExperienceAndClasses.worldRequireAuth = Commons.TryGet<bool>(tag, "require_auth", true);
            ExperienceAndClasses.worldTrace = Commons.TryGet<bool>(tag, "traceMap", false);
            ExperienceAndClasses.worldExpModifier = Commons.TryGet<double>(tag, "global_exp_modifier", ExperienceAndClasses.DEFAULT_EXPERIENCE_MODIFIER);
            ExperienceAndClasses.worldIgnoreCaps = Commons.TryGet<bool>(tag, "global_ignore_caps", ExperienceAndClasses.DEFAULT_IGNORE_CAPS);
            ExperienceAndClasses.worldClassDamageReduction = Commons.TryGet<int>(tag, "global_damage_reduction", ExperienceAndClasses.DEFAULT_DAMAGE_REDUCTION);
            ExperienceAndClasses.worldLevelCap = Commons.TryGet<int>(tag, "global_level_cap", ExperienceAndClasses.DEFAULT_LEVEL_CAP);
            ExperienceAndClasses.worldDeathPenalty = Commons.TryGet<double>(tag, "global_death_penalty", ExperienceAndClasses.DEFAULT_DEATH_PENALTY);

            //create AUTH_CODE if it doesn't exist yet (first time map is run)
            if (ExperienceAndClasses.worldAuthCode == -1) ExperienceAndClasses.worldAuthCode = Main.rand.Next(1000) + ((Main.rand.Next(8) + 1) * 1000) + ((Main.rand.Next(8) + 1) * 10000) + ((Main.rand.Next(8) + 1) * 100000);
        }

        public override void PostUpdate()
        {
            if (!expmapAuthCodeWritten && Main.ActivePlayersCount>0)
            {
                Console.WriteLine("Experience&Classes expauth code: " + ExperienceAndClasses.worldAuthCode);
                expmapAuthCodeWritten = true;
            }

            //initial client experience and settings sync
            DateTime now = DateTime.Now;
            if (now.AddMilliseconds(-TIME_BETWEEN_REQUEST_MSEC).CompareTo(timeLastRequests) > 0)
            {
                timeLastRequests = now;
                SendClientFirstSyncs();
            }
            else if (now.AddMilliseconds(-TIME_BETWEEN_SYNC_MAP_SETTINGS_MSEC).CompareTo(timeLastSyncMapSettings) > 0)
            {
                timeLastSyncMapSettings = now;
                Methods.PacketSender.ServerUpdateMapSettings(mod);
            }
            else if (now.AddMilliseconds(-TIME_BETWEEN_SYNC_EXP_CHANGES_MSEC).CompareTo(timeLastSyncExpChanges) > 0)
            {
                timeLastSyncExpChanges = now;
                Methods.PacketSender.ServerSyncExp(mod);
            }
            else if (now.AddMilliseconds(-TIME_BETWEEN_SYNC_EXP_ALL_MSEC).CompareTo(timeLastSyncExpAll) > 0)
            {
                timeLastSyncExpAll = now;
                Methods.PacketSender.ServerSyncExp(mod, true);
            }
        }

        /// <summary>
        /// Looks for active players who have not done the initial sync and sends the request.
        /// Requests are repeated once every TIME_BETWEEN_REQUEST_MSEC until the response is received
        /// (indicated by experience values other than -1).
        /// </summary>
        public void SendClientFirstSyncs()
        {
            MyPlayer myPlayer;
            //DateTime target_time = DateTime.Now.AddMilliseconds(-TIME_BETWEEN_REQUEST_MSEC);
            for (int i = 0; i <= 255; i++)
            {
                if (Main.player[i].active)
                {
                    myPlayer = Main.player[i].GetModPlayer<MyPlayer>(mod);
                    if (myPlayer.GetExp() == -1)// && target_time.CompareTo(time_last_player_request[i])>0)
                    {
                        //Server's request to new player (includes map settings)
                        Methods.PacketSender.ServerNewPlayerSync(mod, i);
                    }
                }
            }
        }

        public static void FlagAllForSyncExp()
        {
            clientNeedsExpUpdate_counter = 0;
            for (int playerIndex = 0; playerIndex<255; playerIndex++)
            {
                if (Main.player[playerIndex].active)
                {
                    clientNeedsExpUpdate[playerIndex] = true;
                    clientNeedsExpUpdate_indices[clientNeedsExpUpdate_counter] = playerIndex;
                    clientNeedsExpUpdate_counter++;
                }
            }
        }
    }
}