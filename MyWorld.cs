using Terraria;
using Terraria.ModLoader;
using System;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses
{
    public class MyWorld : ModWorld
    {
        public const double TIME_BETWEEN_REQUEST_MSEC = 1*1000;
        //public const double TIME_BETWEEN_AUTH_CODE_MSEC = 120*1000;
        public const double TIME_BETWEEN_SYNC_SETTINGS = 10*1000;

        public static DateTime timeLastRequests = DateTime.MinValue;
        //public static DateTime timeLastAuthCode = DateTime.MinValue;
        public static DateTime timeLastSyncSettings = DateTime.MinValue;

        public static bool codeWritten = false;

        public override TagCompound Save()
        {
            return new TagCompound {
                {"AUTH_CODE", ExperienceAndClasses.authCode},
                {"require_auth", ExperienceAndClasses.requireAuth},
                {"global_exp_modifier", ExperienceAndClasses.globalExpModifier},
                {"global_ignore_caps", ExperienceAndClasses.globalIgnoreCaps},
                {"global_damage_reduction", ExperienceAndClasses.globalClassDamageReduction},
                {"global_level_cap", ExperienceAndClasses.globalLevelCap},
                {"traceMap", ExperienceAndClasses.traceMap},
            };
        }

        public override void Load(TagCompound tag)
        {
            ExperienceAndClasses.authCode = Commons.TryGet<double>(tag, "AUTH_CODE", -1);
            ExperienceAndClasses.requireAuth = Commons.TryGet<bool>(tag, "require_auth", true);
            ExperienceAndClasses.traceMap = Commons.TryGet<bool>(tag, "traceMap", false);
            ExperienceAndClasses.globalExpModifier = Commons.TryGet<double>(tag, "global_exp_modifier", ExperienceAndClasses.DEFAULT_EXPERIENCE_MODIFIER);
            ExperienceAndClasses.globalIgnoreCaps = Commons.TryGet<bool>(tag, "global_ignore_caps", ExperienceAndClasses.DEFAULT_IGNORE_CAPS);
            ExperienceAndClasses.globalClassDamageReduction = Commons.TryGet<int>(tag, "global_damage_reduction", ExperienceAndClasses.DEFAULT_DAMAGE_REDUCTION);
            ExperienceAndClasses.globalLevelCap = Commons.TryGet<int>(tag, "global_level_cap", ExperienceAndClasses.DEFAULT_LEVEL_CAP);

            //create AUTH_CODE if it doesn't exist yet (first time map is run)
            if (ExperienceAndClasses.authCode == -1) ExperienceAndClasses.authCode = Main.rand.Next(1000) + ((Main.rand.Next(8) + 1) * 1000) + ((Main.rand.Next(8) + 1) * 10000) + ((Main.rand.Next(8) + 1) * 100000);
        }

        public override void PostUpdate()
        {
            if (!codeWritten && Main.ActivePlayersCount>0)
            {
                Console.WriteLine("Experience&Classes expauth code: " + ExperienceAndClasses.authCode);
                codeWritten = true;
            }

            //initial client experience and settings sync
            if (DateTime.Now.AddMilliseconds(-TIME_BETWEEN_REQUEST_MSEC).CompareTo(timeLastRequests) > 0)
            {
                timeLastRequests = DateTime.Now;
                doClientRequests();
            }
            ////write auth code to console
            //else if (DateTime.Now.AddMilliseconds(-TIME_BETWEEN_AUTH_CODE_MSEC).CompareTo(timeLastAuthCode) > 0)
            //{
            //    //update time of write
            //    timeLastAuthCode = DateTime.Now;

            //    //create AUTH_CODE if it doesn't exist yet (first time map is run)
            //    if (ExperienceAndClasses.authCode == -1) ExperienceAndClasses.authCode = Main.rand.Next(1000) + ((Main.rand.Next(8) + 1) * 1000) + ((Main.rand.Next(8) + 1) * 10000) + ((Main.rand.Next(8) + 1) * 100000);

            //    //write
            //    Console.WriteLine("Experience&Classes Auth Code: " + ExperienceAndClasses.authCode);
            //    //else Console.WriteLine("WARNING: Require Auth mode is disabled. To enable, enter singleplayer and type /expnoauth.");
            //}
            else if (DateTime.Now.AddMilliseconds(-TIME_BETWEEN_SYNC_SETTINGS).CompareTo(timeLastSyncSettings) > 0)
            {
                //update time of write
                timeLastSyncSettings = DateTime.Now;

                //send latest settings
                Methods.PacketSender.ServerUpdateSettings(mod);
            }
        }

        /// <summary>
        /// Looks for active players who have not done the initial sync and sends the request.
        /// Requests are repeated once every TIME_BETWEEN_REQUEST_MSEC until the response is received
        /// (indicated by experience values other than -1).
        /// </summary>
        public void doClientRequests()
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
    }
}