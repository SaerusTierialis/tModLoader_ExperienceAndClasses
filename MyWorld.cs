using Terraria;
using Terraria.ModLoader;
using System;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses
{
    public class MyWorld : ModWorld
    {
        public const double TIME_BETWEEN_REQUEST_MSEC = 500;
        public const double TIME_BETWEEN_AUTH_CODE_MSEC = 120*1000;

        public static DateTime time_last_requests = DateTime.MinValue;
        public static DateTime time_last_auth_code = DateTime.MinValue;

        public override TagCompound Save()
        {
            return new TagCompound {
                {"AUTH_CODE", ExperienceAndClasses.AUTH_CODE},
                {"require_auth", ExperienceAndClasses.require_auth},
                {"global_exp_modifier", ExperienceAndClasses.global_exp_modifier},
                {"global_ignore_caps", ExperienceAndClasses.global_ignore_caps},
            };
        }

        public override void Load(TagCompound tag)
        {
            ExperienceAndClasses.AUTH_CODE = tag.TryGet<double>("AUTH_CODE", -1);
            ExperienceAndClasses.require_auth = tag.TryGet<bool>("require_auth", true);
            ExperienceAndClasses.global_exp_modifier = tag.TryGet<double>("global_exp_modifier", 1);
            ExperienceAndClasses.global_ignore_caps = tag.TryGet<bool>("global_ignore_caps", false);
        }

        public override void PostUpdate()
        {
            //initial client experience and settings sync
            if (DateTime.Now.AddMilliseconds(-TIME_BETWEEN_REQUEST_MSEC).CompareTo(time_last_requests) > 0)
            {
                time_last_requests = DateTime.Now;
                doClientRequests();
            }
            //write auth code to console
            else if (DateTime.Now.AddMilliseconds(-TIME_BETWEEN_AUTH_CODE_MSEC).CompareTo(time_last_auth_code) > 0)
            {
                //update time of write
                time_last_auth_code = DateTime.Now;

                //create AUTH_CODE if it doesn't exist yet (first time map is run)
                if (ExperienceAndClasses.AUTH_CODE == -1) ExperienceAndClasses.AUTH_CODE = Main.rand.Next(1000) + ((Main.rand.Next(8) + 1) * 1000) + ((Main.rand.Next(8) + 1) * 10000) + ((Main.rand.Next(8) + 1) * 100000);

                //write
                if (ExperienceAndClasses.require_auth) Console.WriteLine("Experience&Classes Auth Code: " + ExperienceAndClasses.AUTH_CODE);
                else Console.WriteLine("WARNING: Require Auth mode is disabled. To enable, enter singleplayer and type /expnoauth.");
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
                        //request experience from player
                        Methods.PacketSender.ServerRequestExperience(i);

                        //also share class caps status to ensure that token tooltips are correct
                        Methods.PacketSender.ServerToggleCap(ExperienceAndClasses.global_ignore_caps);
                    }
                }
            }
        }
    }
}