using Terraria;
using Terraria.ModLoader;
using System;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses
{
    public class MyWorld : ModWorld
    {
        public static double TIME_BETWEEN_REQUEST_MSEC = 500;
        public static double TIME_BETWEEN_AUTH_CODE_MSEC = 60*1000;

        public static DateTime time_last_requests = DateTime.MinValue;
        public static DateTime time_last_auth_code = DateTime.MinValue;

        public override TagCompound Save()
        {
            return new TagCompound {
                {"global_exp_modifier", ExperienceAndClasses.global_exp_modifier},
                {"global_ignore_caps", ExperienceAndClasses.global_ignore_caps},
                {"AUTH_CODE", ExperienceAndClasses.AUTH_CODE},
            };
        }

        public override void Load(TagCompound tag)
        {
            ExperienceAndClasses.AUTH_CODE = tag.TryGet<double>("AUTH_CODE", -1);
            ExperienceAndClasses.global_exp_modifier = tag.TryGet<double>("global_exp_modifier", 1);
            ExperienceAndClasses.global_ignore_caps = tag.TryGet<bool>("global_ignore_caps", false);
        }

        public override void PostUpdate()
        {
            //initial experience sync
            if (DateTime.Now.AddMilliseconds(-TIME_BETWEEN_REQUEST_MSEC).CompareTo(time_last_requests) > 0)
            {
                time_last_requests = DateTime.Now;
                doExpRequests();
            }
            //write auth code to console
            else if (DateTime.Now.AddMilliseconds(-TIME_BETWEEN_AUTH_CODE_MSEC).CompareTo(time_last_auth_code) > 0)
            {
                time_last_auth_code = DateTime.Now;

                if (ExperienceAndClasses.AUTH_CODE == -1) ExperienceAndClasses.AUTH_CODE = Main.rand.Next(1000) + ((Main.rand.Next(8) + 1) * 1000) + ((Main.rand.Next(8) + 1) * 10000) + ((Main.rand.Next(8) + 1) * 100000); //ExperienceAndClasses.AUTH_CODE = Math.Abs(Main.worldName.GetHashCode());

                Console.WriteLine("Experience&Classes Auth Code: " + ExperienceAndClasses.AUTH_CODE);
            }
        }

        /// <summary>
        /// Looks for active players who have not done the initial experience sync and sends the request.
        /// Requests are limited to once every TIME_BETWEEN_REQUEST_MSEC.
        /// </summary>
        public void doExpRequests()
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
                        (mod as ExperienceAndClasses).PacketSend_ServerRequestExperience(i);

                        //also share class caps status
                        (mod as ExperienceAndClasses).PacketSend_ServerToggleCap(ExperienceAndClasses.global_ignore_caps);
                    }
                }
            }
        }
    }
}