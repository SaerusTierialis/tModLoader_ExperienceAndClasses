using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System;

namespace ExperienceAndClasses.UI
{
    class UIExp : UIState
    {
        public static readonly Color COLOUR_TEXT_INNER = Color.White;
        public static readonly Color COLOUR_TEXT_OUTTER = Color.Black;

        public static readonly int NUMBER_OF_BARS = 1 + ExperienceAndClasses.NUMBER_OF_ABILITY_SLOTS;
        protected static readonly Color COLOUR_PANEL = new Color(73, 94, 171);
        protected static readonly Color COLOUR_BAR_BACKGROUND = Color.Gray;
        protected static readonly Color COLOUR_BAR_FOREGROUND_EXP = Color.GreenYellow;
        protected static readonly Color COLOUR_BAR_FOREGROUND_ABILITY = new Color(206, 239, 107); //new Color(255, 242, 0); //new Color(163, 73, 174);
        protected static readonly byte ALPHA_PANEL = 255;
        protected static readonly byte ALPHA_PANEL_TRANSPARENT = 0;
        protected static readonly byte ALPHA_BAR_EXP_FOREGROUND = 255;
        protected static readonly byte ALPHA_BAR_EXP_FOREGROUND_TRANSPARENT = 150;
        protected static readonly byte ALPHA_BAR_EXP_BACKGROUND = 255;
        protected static readonly byte ALPHA_BAR_EXP_BACKGROUND_TRANSPARENT = 200;

        protected const float PANEL_WIDTH = 200f;
        protected const float PANEL_HEIGHT_BASE = 37f;
        protected const float PANEL_HEIGHT_PER_BAR = 24f;

        protected const float BAR_EXP_TOP_FIRST = 35f;
        protected const float BAR_EXP_LEFT = 5f;
        protected const float BAR_EXP_LEFT_INDENT = 40f;
        protected const float BAR_EXP_WIDTH = PANEL_WIDTH - (BAR_EXP_LEFT * 2);
        protected const float BAR_EXP_WIDTH_INDENT = PANEL_WIDTH - BAR_EXP_LEFT_INDENT - BAR_EXP_LEFT;
        protected const float BAR_EXP_HEIGHT = 22f;

        protected const float TEXT_LEVEL_X = 5f;
        protected const float TEXT_LEVEL_Y = 10f;

        protected const float TEXT_PCT_X = PANEL_WIDTH - TEXT_LEVEL_X;
        protected const float TEXT_PCT_Y = TEXT_LEVEL_Y;

        protected const float TEXT_ABILITY_X = (TEXT_LEVEL_X + BAR_EXP_LEFT_INDENT) / 2f;
        protected const float TEXT_ABILITY_Y_DOWN = 2f;

        protected static UIPanel panel;
        public static UIBar[] bars = new UIBar[NUMBER_OF_BARS];

        protected static int numberActiveBars = 0;
        protected static string[] labelsExp = new string[2];
        protected static string[,] labelsBars = new string[2, NUMBER_OF_BARS];

        public static bool transparency = false;
        public static MyPlayer localMyPlayer;
        private static bool initialized = false;

        public const float SCALE_MAIN = 1f;
        public const float SCALE_SMALL = 0.85f;
        public static readonly Vector2 ORIGIN = new Vector2(0f);

        public override void OnInitialize()
        {
            panel = new UIPanel();
            panel.SetPadding(0);
            panel.Left.Set(0, 0f);
            panel.Top.Set(0, 0f);
            panel.Width.Set(PANEL_WIDTH, 0f);
            panel.Height.Set(PANEL_HEIGHT_BASE + (NUMBER_OF_BARS * PANEL_HEIGHT_PER_BAR), 0f);
            panel.BackgroundColor = COLOUR_PANEL;

            panel.OnMouseDown += new UIElement.MouseEvent(DragStart);
            panel.OnMouseUp += new UIElement.MouseEvent(DragEnd);

            //add all bars that could be needed
            for (int i = 0; i < NUMBER_OF_BARS; i++)
            {
                if (i == 0)
                    bars[i] = new UIBar(panel, BAR_EXP_LEFT, BAR_EXP_TOP_FIRST + (i * PANEL_HEIGHT_PER_BAR), BAR_EXP_WIDTH, BAR_EXP_HEIGHT, transparency, ALPHA_BAR_EXP_BACKGROUND, ALPHA_BAR_EXP_FOREGROUND, ALPHA_BAR_EXP_BACKGROUND_TRANSPARENT, ALPHA_BAR_EXP_FOREGROUND_TRANSPARENT, COLOUR_BAR_BACKGROUND, COLOUR_BAR_FOREGROUND_EXP);
                else
                    bars[i] = new UIBar(panel, BAR_EXP_LEFT_INDENT, BAR_EXP_TOP_FIRST + (i * PANEL_HEIGHT_PER_BAR), BAR_EXP_WIDTH_INDENT, BAR_EXP_HEIGHT, transparency, ALPHA_BAR_EXP_BACKGROUND, ALPHA_BAR_EXP_FOREGROUND, ALPHA_BAR_EXP_BACKGROUND_TRANSPARENT, ALPHA_BAR_EXP_FOREGROUND_TRANSPARENT, COLOUR_BAR_BACKGROUND, COLOUR_BAR_FOREGROUND_ABILITY);
            }

            base.Append(panel);
        }

        public static void Init(MyPlayer myPlayer)
        {
            initialized = true;
            localMyPlayer = myPlayer;
        }

        public void Update(SpriteBatch spriteBatch)
        {
            if (!initialized) return;

            numberActiveBars = 0;

            //exp/level and prep main text
            double exp = localMyPlayer.GetExp();
            if (exp < 0) return;

            int level = localMyPlayer.GetLevel();
            double expHave = Methods.Experience.GetExpTowardsNextLevel(exp);
            double expNeed = Methods.Experience.GetExpReqForLevel(level + 1, false);
            float pct = (float)(expHave / expNeed);
            float pctShow = (float)Math.Round((double)pct * 100, 2);
            if (pctShow == 100) pctShow = 99.99f;
            if (exp == MyPlayer.MAX_EXPERIENCE)
            {
                pct = 1f;
                pctShow = 100;
                expHave = 0;
                expNeed = 0;
            }
            labelsExp[0] = "LEVEL: " + level; 
            labelsExp[1] = pctShow + "%";

            //exp bar
            if (localMyPlayer.UIExpBar)
            {
                //bar
                bars[numberActiveBars].left = BAR_EXP_LEFT;
                bars[numberActiveBars].width = BAR_EXP_WIDTH;
                bars[numberActiveBars].colourFgd = COLOUR_BAR_FOREGROUND_EXP;
                bars[numberActiveBars].Activate();
                bars[numberActiveBars].SetValue(pct);
                //text
                labelsBars[0, numberActiveBars] = null;
                if (expNeed <= 999999999 && expNeed > 0)
                {
                    labelsBars[1, numberActiveBars] = expHave + " / " + expNeed;
                }
                else
                {
                    labelsBars[1, numberActiveBars] = null;
                }
                numberActiveBars++;
            }

            //ability bars
            if (localMyPlayer.UICDBars)
            {
                int abilityID;
                for (int i = 0; i < ExperienceAndClasses.NUMBER_OF_ABILITY_SLOTS; i++)
                {
                    abilityID = localMyPlayer.currentAbilityIDs[i];
                    if (abilityID != Abilities.ID.UNDEFINED)
                    {
                        //bar
                        bars[numberActiveBars].left = BAR_EXP_LEFT_INDENT;
                        bars[numberActiveBars].width = BAR_EXP_WIDTH_INDENT;
                        bars[numberActiveBars].Activate();
                        //set bar values and get text to diplay
                        labelsBars[0, numberActiveBars] = Abilities.SHORTFORM[abilityID];
                        labelsBars[1, numberActiveBars] = DisplayCooldown(abilityID, numberActiveBars);
                        numberActiveBars++;
                    }
                }
            }

            //remove extra bars
            for (int i = numberActiveBars; i < NUMBER_OF_BARS; i++)
            {
                if (bars[i].active) bars[i].Deactivate();
            }

            //adjust panel size
            panel.Height.Set(PANEL_HEIGHT_BASE + (numberActiveBars * PANEL_HEIGHT_PER_BAR), 0f);
        }

        public static void SetPosition(float left, float top)
        {
            panel.Left.Set(left, 0f);
            panel.Top.Set(top, 0f);
            //Recalculate();
        }

        public static float GetLeft() { return panel.Left.Pixels; }
        public static float GetTop() { return panel.Top.Pixels; }

        public static void SetTransparency(bool newTransparency)
        {
            transparency = newTransparency;
            if (transparency)
            {
                panel.BackgroundColor.A = ALPHA_PANEL_TRANSPARENT;
            }
            else
            {
                panel.BackgroundColor.A = ALPHA_PANEL;
            }
            for (int i = 0; i < NUMBER_OF_BARS; i++)
            {
                if (bars[i].active)
                {
                    bars[i].SetTransparency(transparency);
                }
            }
            //Recalculate();
        }

        private static Vector2 offset;
        private static bool dragging = false;
        private static void DragStart(UIMouseEvent evt, UIElement listeningElement)
        {
            offset = new Vector2(evt.MousePosition.X - panel.Left.Pixels, evt.MousePosition.Y - panel.Top.Pixels);
            dragging = true;
        }

        private void DragEnd(UIMouseEvent evt, UIElement listeningElement)
        {
            Vector2 end = evt.MousePosition;
            dragging = false;

            panel.Left.Set(end.X - offset.X, 0f);
            panel.Top.Set(end.Y - offset.Y, 0f);

            //Recalculate();
        }

        public static bool AllowDraw()
        {
            if (!initialized || (!localMyPlayer.UIShow && (!Main.playerInventory || !localMyPlayer.UIInventory)))
                return false;
            else
                return true;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (!AllowDraw()) return;

            Vector2 MousePosition = new Vector2((float)Main.mouseX, (float)Main.mouseY);
            if (panel.ContainsPoint(MousePosition))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
            if (dragging)
            {
                panel.Left.Set(MousePosition.X - offset.X, 0f);
                panel.Top.Set(MousePosition.Y - offset.Y, 0f);
            }
            Update(spriteBatch);
            Recalculate();
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            if (!AllowDraw()) return;

            //draw the not text stuff (bars)
            base.DrawChildren(spriteBatch);

            //draw text
            if (initialized)
            {
                Vector2 textSize;

                //text for level and percent exp
                Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, labelsExp[0], panel.Left.Pixels + TEXT_LEVEL_X, panel.Top.Pixels + TEXT_LEVEL_Y, COLOUR_TEXT_INNER, COLOUR_TEXT_OUTTER, ORIGIN, SCALE_MAIN);
                textSize = Main.fontMouseText.MeasureString(labelsExp[1]) * SCALE_MAIN;
                Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, labelsExp[1], panel.Left.Pixels + TEXT_PCT_X - textSize.X, panel.Top.Pixels + TEXT_PCT_Y, COLOUR_TEXT_INNER, COLOUR_TEXT_OUTTER, ORIGIN, SCALE_MAIN);

                //text for each bar
                float barCenter;
                for (int i = 0; i < numberActiveBars; i++)
                {
                    if (labelsBars[0, i] != null)
                    {
                        textSize = Main.fontMouseText.MeasureString(labelsBars[0, i]) * SCALE_SMALL;
                        Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, labelsBars[0, i], panel.Left.Pixels + TEXT_ABILITY_X - (textSize.X / 2), panel.Top.Pixels + bars[i].top + TEXT_ABILITY_Y_DOWN, COLOUR_TEXT_INNER, COLOUR_TEXT_OUTTER, ORIGIN, SCALE_SMALL);
                    }
                    if (labelsBars[1, i] != null)
                    {
                        barCenter = bars[i].left + (bars[i].width / 2);
                        textSize = Main.fontMouseText.MeasureString(labelsBars[1, i]) * SCALE_SMALL;
                        Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, labelsBars[1, i], panel.Left.Pixels + barCenter - (textSize.X / 2), panel.Top.Pixels + bars[i].top + TEXT_ABILITY_Y_DOWN, COLOUR_TEXT_INNER, COLOUR_TEXT_OUTTER, ORIGIN, SCALE_SMALL);
                    }
                }
            }
        }

        public static string DisplayCooldown(int abilityID, int barIndex)
        {
            float cooldownSec = 0;
            float cooldownPct = 0f;
            string cooldownText = null;

            //determine bar percent and text to overlay
            long timeNow = DateTime.Now.Ticks;
            long timeAllow = localMyPlayer.abilityCooldowns[abilityID];
            switch (abilityID)
            {
                //any unique cooldown displays

                default:
                    cooldownSec = (float)(timeAllow - timeNow) / TimeSpan.TicksPerSecond;
                    if (cooldownSec < 0) cooldownSec = 0;
                    cooldownPct = (Abilities.COOLDOWN_SECS[abilityID] - cooldownSec) / Abilities.COOLDOWN_SECS[abilityID];
                    if (cooldownPct != 1f) cooldownText = Math.Round((double)cooldownSec, 1).ToString();
                    break;
            }

            //set bar value
            bars[barIndex].SetValue(cooldownPct, COLOUR_BAR_FOREGROUND_ABILITY);

            //display off cooldown message
            if ((cooldownSec <= 0) && Abilities.ON_COOLDOWN[abilityID])
            {
                Abilities.ON_COOLDOWN[abilityID] = false;
                if ((Abilities.COOLDOWN_SECS[abilityID] >= localMyPlayer.thresholdCDMsg) && (localMyPlayer.thresholdCDMsg >= 0f)) CombatText.NewText(localMyPlayer.player.getRect(), ExperienceAndClasses.MESSAGE_COLOUR_OFF_COOLDOWN, Abilities.NAME[abilityID] + " Ready!");
            }

            //return text for overlay
            return cooldownText;
        }
    }

    //some of this implementation is crap - redo if bored or reusing
    class UIBar
    {
        public UIPanel bgd, fgd, parentPanel;
        public bool active, transparency;
        public float left, top, width, height;
        public byte alphaBgd, alphaFgd, alphaBgdTrans, alphaFgdTrans;
        public Color colourBgd, colourFgd;

        public UIBar(UIPanel parentPanel, float left, float top, float width, float height, bool transparency, byte alphaBgd, byte alphaFgd, byte alphaBgdTrans, byte alphaFgdTrans, Color colourBgd, Color colourFgd)
        {
            this.parentPanel = parentPanel;
            this.left = left;
            this.top = top;
            this.width = width;
            this.height = height;
            this.transparency = transparency;
            this.alphaBgd = alphaBgd;
            this.alphaFgd = alphaFgd;
            this.alphaBgdTrans = alphaBgdTrans;
            this.alphaFgdTrans = alphaFgdTrans;
            this.colourBgd = colourBgd;
            this.colourFgd = colourFgd;

            Activate();
        }

        private const float MIN_BAR_VALUE_SHOW = 0.11f;
        public void SetValue(float value)
        {
            value = MIN_BAR_VALUE_SHOW + (value * (1f - MIN_BAR_VALUE_SHOW));
            fgd.Width.Set(width * value, 0f);
            fgd.Recalculate();
        }

        public void SetValue(float value, Color colourFgd)
        {
            this.colourFgd = colourFgd;
            SetValue(value);
        }

        public void SetTransparency(bool transparency)
        {
            if (transparency)
            {
                bgd.BackgroundColor.A = alphaBgdTrans;
                fgd.BackgroundColor.A = alphaFgdTrans;
            }
            else
            {
                bgd.BackgroundColor.A = alphaBgd;
                fgd.BackgroundColor.A = alphaFgd;
            }
        }

        public void Activate()
        {
            if (!active) bgd = new UIPanel();
            bgd.Left.Set(left, 0f);
            bgd.Top.Set(top, 0f);
            bgd.Width.Set(width, 0f);
            bgd.Height.Set(height, 0f);
            bgd.BackgroundColor = colourBgd;
            if (!active) parentPanel.Append(bgd);
            bgd.Recalculate();

            if (!active) fgd = new UIPanel();
            fgd.Left.Set(left, 0f);
            fgd.Top.Set(top, 0f);
            fgd.Width.Set(width, 0f);
            fgd.Height.Set(height, 0f);
            fgd.BackgroundColor = colourFgd;
            if (!active) parentPanel.Append(fgd);
            fgd.Recalculate();

            SetTransparency(transparency);

            active = true;
        }

        public void Deactivate()
        {
            active = false;
            bgd.Remove();
            fgd.Remove();
        }

    }
}
