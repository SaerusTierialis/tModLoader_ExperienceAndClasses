using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System;

namespace ExperienceAndClasses.UI
{
    class UIExp : UIState
    {
        public static readonly Color COLOUR_TEXT_INNER = Color.White;
        public static readonly Color COLOUR_TEXT_OUTTER = Color.Black;

        public static readonly int NUMBER_OF_BARS = 1 + ExperienceAndClasses.MAXIMUM_NUMBER_OF_ABILITIES;
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
        private static UIExpOverlay overlay;
        public static UIBar[] bars = new UIBar[NUMBER_OF_BARS];

        protected static int numberActiveBars = 0;
        protected static string[] labelsExp = new string[2];
        protected static string[,] labelsBars = new string[2, NUMBER_OF_BARS];

        public static bool visible = true;
        public static bool transparency = false;
        public static MyPlayer localMyPlayer;

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
            for (int i = 0; i< NUMBER_OF_BARS; i++)
            {
                if (i == 0)
                    bars[i] = new UIBar(panel, BAR_EXP_LEFT, BAR_EXP_TOP_FIRST + (i * PANEL_HEIGHT_PER_BAR), BAR_EXP_WIDTH, BAR_EXP_HEIGHT, transparency, ALPHA_BAR_EXP_BACKGROUND, ALPHA_BAR_EXP_FOREGROUND, ALPHA_BAR_EXP_BACKGROUND_TRANSPARENT, ALPHA_BAR_EXP_FOREGROUND_TRANSPARENT, COLOUR_BAR_BACKGROUND, COLOUR_BAR_FOREGROUND_EXP);
                else
                    bars[i] = new UIBar(panel, BAR_EXP_LEFT_INDENT, BAR_EXP_TOP_FIRST + (i * PANEL_HEIGHT_PER_BAR), BAR_EXP_WIDTH_INDENT, BAR_EXP_HEIGHT, transparency, ALPHA_BAR_EXP_BACKGROUND, ALPHA_BAR_EXP_FOREGROUND, ALPHA_BAR_EXP_BACKGROUND_TRANSPARENT, ALPHA_BAR_EXP_FOREGROUND_TRANSPARENT, COLOUR_BAR_BACKGROUND, COLOUR_BAR_FOREGROUND_ABILITY);
            }

            overlay = new UIExpOverlay();
            overlay.Left.Set(0f, 0f);
            overlay.Top.Set(0f, 0f);
            overlay.Width.Set(panel.Width.Pixels, 0f);
            overlay.Height.Set(panel.Height.Pixels, 1f);
            panel.Append(overlay);

            base.Append(panel);
        }

        public void Init(MyPlayer myPlayer)
        {
            localMyPlayer = myPlayer;
        }

        public void Update(SpriteBatch spriteBatch)
        {
            if (localMyPlayer == null) return;

            numberActiveBars = 0;

            //exp/level and prep main text
            double exp = localMyPlayer.GetExp();
            int level = Methods.Experience.GetLevel(exp);
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
            if (!localMyPlayer.UIExpBar)
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
            int abilityID;
            for (int i = 0; i < ExperienceAndClasses.MAXIMUM_NUMBER_OF_ABILITIES; i++)
            {
                abilityID = localMyPlayer.currentAbilityIDs[i];
                if (abilityID != Abilities.ID_UNDEFINED)
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

            //remove extra bars
            for (int i = numberActiveBars; i < NUMBER_OF_BARS; i++)
            {
                if (bars[i].active) bars[i].Deactivate();
            }

            //adjust panel size
            panel.Height.Set(PANEL_HEIGHT_BASE + (numberActiveBars * PANEL_HEIGHT_PER_BAR), 0f);
        }

        public void SetPosition(float left, float top)
        {
            panel.Left.Set(left, 0f);
            panel.Top.Set(top, 0f);
            Recalculate();
        }

        public float GetLeft() { return panel.Left.Pixels; }
        public float GetTop() { return panel.Top.Pixels; }

        public void SetTransparency(bool newTransparency)
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
            Recalculate();
        }

        Vector2 offset;
        public bool dragging = false;
        private void DragStart(UIMouseEvent evt, UIElement listeningElement)
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

            Recalculate();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
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
            base.DrawSelf(spriteBatch);
            Update(spriteBatch);
            Recalculate();
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
                if (Abilities.COOLDOWN_SECS[abilityID] >= Abilities.THRESHOLD_SHOW_OFF_COOLDOWN_MSG) CombatText.NewText(localMyPlayer.player.getRect(), ExperienceAndClasses.MESSAGE_COLOUR_OFF_COOLDOWN, Abilities.NAME[abilityID] + " Ready!");
            }

            //return text for overlay
            return cooldownText;
        }

        class UIExpOverlay : UIElement
        {
            public const float SCALE_MAIN = 1f;
            public const float SCALE_SMALL = 0.85f;
            public static readonly Vector2 ORIGIN = new Vector2(0f);
            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                float x = Parent.Left.Pixels;
                float y = Parent.Top.Pixels;
                Vector2 textSize;
                //Vector2 v2PctShow = Main.fontMouseText.MeasureString(strPctShow);
                //Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, "LEVEL: " + level, shopx, shopy, Color.White, Color.Black, new Vector2(0.3f), 0.75f);

                //text for level and percent exp
                Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, labelsExp[0], x + TEXT_LEVEL_X, y + TEXT_LEVEL_Y, COLOUR_TEXT_INNER, COLOUR_TEXT_OUTTER, ORIGIN, SCALE_MAIN);
                textSize = Main.fontMouseText.MeasureString(labelsExp[1]) * SCALE_MAIN;
                Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, labelsExp[1], x + TEXT_PCT_X - textSize.X, y + TEXT_PCT_Y, COLOUR_TEXT_INNER, COLOUR_TEXT_OUTTER, ORIGIN, SCALE_MAIN);

                //text for each bar
                float barCenter;
                for (int i = 0; i < numberActiveBars; i++)
                {
                    if (labelsBars[0, i] != null)
                    {
                        textSize = Main.fontMouseText.MeasureString(labelsBars[0, i]) * SCALE_SMALL;
                        Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, labelsBars[0, i], x + TEXT_ABILITY_X - (textSize.X/2), y + bars[i].top + TEXT_ABILITY_Y_DOWN, COLOUR_TEXT_INNER, COLOUR_TEXT_OUTTER, ORIGIN, SCALE_SMALL);
                    }
                    if (labelsBars[1, i] != null)
                    {
                        barCenter = bars[i].left + (bars[i].width / 2);
                        textSize = Main.fontMouseText.MeasureString(labelsBars[1, i]) * SCALE_SMALL;
                        Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, labelsBars[1, i], x + barCenter - (textSize.X / 2), y + bars[i].top + TEXT_ABILITY_Y_DOWN, COLOUR_TEXT_INNER, COLOUR_TEXT_OUTTER, ORIGIN, SCALE_SMALL);
                    }
                }
            }
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
