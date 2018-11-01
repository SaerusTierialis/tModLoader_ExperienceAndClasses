using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExperienceAndClasses {
    class MPlayer : ModPlayer {

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Constants ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private const long TICKS_PER_FULL_SYNC = TimeSpan.TicksPerMinute * 2;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Static Vars ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        private static DateTime time_next_full_sync = DateTime.MaxValue;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (non-syncing) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public bool is_local_player = false;
        private TagCompound load_tag;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Instance Vars (syncing) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public int sync_test = 0;

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Initialize ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// instanced arrays must be initialized here (also called during cloning, etc)
        /// </summary>
        public override void Initialize() {
            // instanced arrays must be initialized here





        }

        /// <summary>
        /// new player enters
        /// </summary>
        /// <param name="player"></param>
        public override void OnEnterWorld(Player player) {
            // is this the local player?
            if (player.whoAmI == Main.LocalPlayer.whoAmI) {
                //this is the current local player
                is_local_player = true;
                ExperienceAndClasses.LOCAL_MPLAYER = this;

                //start timer for next full sync
                time_next_full_sync = DateTime.Now.AddTicks(TICKS_PER_FULL_SYNC);

                //apply saved ui settings
                ExperienceAndClasses.user_interface_state_main.SetPosition(Commons.TryGet<float>(load_tag, "ui_main_left", 100f), Commons.TryGet<float>(load_tag, "ui_main_top", 100f));
                ExperienceAndClasses.user_interface_state_main.SetAuto(Commons.TryGet<bool>(load_tag, "ui_main_auto", true));
                ExperienceAndClasses.user_interface_state_main.SetPinned(Commons.TryGet<bool>(load_tag, "ui_main_pinned", false));
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void PostUpdate() {
            if (is_local_player) {
                sync_test = player.statLife;
            }

            //Main.NewText(player.name + " " + sync_test); //working
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Hotkeys ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (ExperienceAndClasses.HOTKEY_UI.JustPressed) {
                ExperienceAndClasses.user_interface_state_main.Visible = !ExperienceAndClasses.user_interface_state_main.Visible;
            }
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Syncing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        /// <summary>
        /// store prior state to detect changes to sync
        /// </summary>
        /// <param name="clientClone"></param>
        public override void clientClone(ModPlayer clientClone) {
            MPlayer clone = clientClone as MPlayer;
            clone.sync_test = sync_test;
        }

        /// <summary>
        /// look for changes to sync + send any changes via packet
        /// </summary>
        /// <param name="clientPlayer"></param>
        public override void SendClientChanges(ModPlayer clientPlayer) {
            DateTime now = DateTime.Now;
            int me = player.whoAmI;
            if (now.CompareTo(time_next_full_sync) > 0) {
                //full sync
                time_next_full_sync = now.AddTicks(TICKS_PER_FULL_SYNC);
                SyncPlayer(-1, me, false);
            }
            else {
                //partial sync
                MPlayer clone = clientPlayer as MPlayer;
                if (clone.sync_test != sync_test) {
                    ModPacket packet = mod.GetPacket();
                    packet.Write((byte)ExperienceAndClasses.MessageType.SYNC_TEST);
                    packet.Write((byte)me);
                    packet.Write(sync_test);
                    packet.Send(-1, me);
                    Main.NewText("sent");
                }
            }
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
            //full sync (called to share current players with new players + new player with current players)
        }

        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Drawing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public static void DrawEffects(PlayerDrawInfo drawInfo, bool is_behind) {
            /*
            Player drawPlayer = drawInfo.drawPlayer;
            //Mod mod = ModLoader.GetMod("ExperienceAndClasses");
            //ExamplePlayer modPlayer = drawPlayer.GetModPlayer<ExamplePlayer>();
            //Texture2D texture = mod.GetTexture("NPCs/Lock");

            if (drawPlayer.statLife < 50) {
                return;
            }

            Texture2D texture = Main.flameTexture;

            int drawX = (int)(drawInfo.position.X + drawPlayer.width / 2f - Main.screenPosition.X - texture.Width / 2f);
            int drawY = (int)(drawInfo.position.Y + drawPlayer.height / 2f - Main.screenPosition.Y - texture.Height / 2f);

            DrawData data = new DrawData(texture, new Vector2(drawX, drawY), Color.Green);
            Main.playerDrawData.Add(data);
            */
        }

        public static readonly PlayerLayer MiscEffectsBehind = new PlayerLayer("ExperienceAndClasses", "MiscEffectsBack", PlayerLayer.MiscEffectsBack, delegate (PlayerDrawInfo drawInfo) {
            DrawEffects(drawInfo, true);
        });

        public static readonly PlayerLayer MiscEffects = new PlayerLayer("ExperienceAndClasses", "MiscEffects", PlayerLayer.MiscEffectsFront, delegate (PlayerDrawInfo drawInfo) {
            DrawEffects(drawInfo, false);
        });

        public override void ModifyDrawLayers(List<PlayerLayer> layers) {
            MiscEffectsBehind.visible = true;
            layers.Insert(0, MiscEffectsBehind);
            MiscEffects.visible = true;
            layers.Add(MiscEffects);
        }


        /*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Save/Load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

        public override TagCompound Save() {
            return new TagCompound {
                {"ui_main_left", ExperienceAndClasses.user_interface_state_main.GetLeft() },
                {"ui_main_top", ExperienceAndClasses.user_interface_state_main.GetTop() },
                {"ui_main_auto", ExperienceAndClasses.user_interface_state_main.Auto },
                {"ui_main_pinned", ExperienceAndClasses.user_interface_state_main.GetPinned() },
            };
        }

        public override void Load(TagCompound tag) {
            //ui settings must be stored and applied later when entering game
            load_tag = tag;

            
        }

    }
}
