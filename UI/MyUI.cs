using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System;

namespace ExperienceAndClasses.UI
{
    class MyUI : UIState
    {
        public UIPanel expCounterPanel;
        public UIMoneyDisplay moneyDiplay;
        public UIPanel expBar;
        public UIPanel expBarBack;
        public static bool visible = true;

        public override void OnInitialize()
        {
            expCounterPanel = new UIPanel();
            expCounterPanel.SetPadding(0);
            expCounterPanel.Left.Set(400f, 0f);
            expCounterPanel.Top.Set(100f, 0f);
            expCounterPanel.Width.Set(170f, 0f);
            expCounterPanel.Height.Set(70f, 0f);
            expCounterPanel.BackgroundColor = new Color(73, 94, 171);

            expCounterPanel.OnMouseDown += new UIElement.MouseEvent(DragStart);
            expCounterPanel.OnMouseUp += new UIElement.MouseEvent(DragEnd);

            expBarBack = new UIPanel();
            expBarBack.Left.Set(0, 0f);
            expBarBack.Top.Set(36, 0f);
            expBarBack.Width.Set(100f, 1f);
            expBarBack.Height.Set(0, 0.33f);
            expBarBack.BackgroundColor = Color.Gray;
            expCounterPanel.Append(expBarBack);

            expBar = new UIPanel();
            expBar.Left.Set(0, 0f);
            expBar.Top.Set(36, 0f);
            expBar.Width.Set(100f, 1f);
            expBar.Height.Set(0, 0.33f);
            expBar.BackgroundColor = Color.GreenYellow;
            expCounterPanel.Append(expBar);

            moneyDiplay = new UIMoneyDisplay(expBar);
            moneyDiplay.Left.Set(15, 0f);
            moneyDiplay.Top.Set(20, 0f);
            moneyDiplay.Width.Set(100f, 0f);
            moneyDiplay.Height.Set(0, 1f);
            expCounterPanel.Append(moneyDiplay);

            base.Append(expCounterPanel);
        }

        Vector2 offset;
        public bool dragging = false;
        private void DragStart(UIMouseEvent evt, UIElement listeningElement)
        {
            offset = new Vector2(evt.MousePosition.X - expCounterPanel.Left.Pixels, evt.MousePosition.Y - expCounterPanel.Top.Pixels);
            dragging = true;
        }

        private void DragEnd(UIMouseEvent evt, UIElement listeningElement)
        {
            Vector2 end = evt.MousePosition;
            dragging = false;

            expCounterPanel.Left.Set(end.X - offset.X, 0f);
            expCounterPanel.Top.Set(end.Y - offset.Y, 0f);

            Recalculate();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Vector2 MousePosition = new Vector2((float)Main.mouseX, (float)Main.mouseY);
            if (expCounterPanel.ContainsPoint(MousePosition))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
            if (dragging)
            {
                expCounterPanel.Left.Set(MousePosition.X - offset.X, 0f);
                expCounterPanel.Top.Set(MousePosition.Y - offset.Y, 0f);
                Recalculate();
            }
        }

        public void updateValue(double experience)//, Mod mod)
        {
            moneyDiplay.exp = experience;
        }

        public void setPosition(float left, float top)
        {
            //Main.NewText("SET POSITION");
            expCounterPanel.Left.Set(left, 0f);
            expCounterPanel.Top.Set(top, 0f);
            Recalculate();
        }

        public float getLeft() { return expCounterPanel.Left.Pixels; }
        public float getTop() { return expCounterPanel.Top.Pixels; }

        public void setTrans(bool isTrans)
        {
            if (isTrans)
            {
                expCounterPanel.BackgroundColor.A = 0;
                expBarBack.BackgroundColor.A = 150;
                expBar.BackgroundColor.A = 200;
            }
            else
            {
                expCounterPanel.BackgroundColor.A = 255;
                expBarBack.BackgroundColor.A = 255;
                expBar.BackgroundColor.A = 255;
            }
            Recalculate();
        }
    }

    public class UIMoneyDisplay : UIElement
    {
        public double exp;
        public UIPanel expBar;

        public UIMoneyDisplay(UIPanel bar)
        {
            Width.Set(100, 0f);
            Height.Set(40, 0f);
            expBar = bar;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle innerDimensions = base.GetInnerDimensions();
            //Vector2 drawPos = new Vector2(innerDimensions.X + 5f, innerDimensions.Y + 30f);

            float shopx = innerDimensions.X - 2f;
            float shopy = innerDimensions.Y - 5f;

            int level = Methods.Experience.GetLevel(exp);
            double exp_have = Methods.Experience.GetExpTowardsNextLevel(exp);
            double exp_need = Methods.Experience.GetExpReqForLevel(level+1,false);
            float pct = (float)(exp_have / exp_need);
            float pctShow = (float)Math.Round((double)pct * 100, 2);
            if (pctShow == 100) pctShow = 99.99f;

            if (exp==MyPlayer.MAX_EXPERIENCE)
            {
                pct = 1f;
                pctShow = 100;
                exp_have = 0;
                exp_need = 0;
            }

            string str_exp_num = exp_have + "/" + exp_need;
            Vector2 v2_exp_num = Main.fontMouseText.MeasureString(str_exp_num);

            string str_pct_show = pctShow + "%";
            Vector2 v2_pct_show = Main.fontMouseText.MeasureString(str_pct_show);

            Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, "LEVEL: " + level, shopx, shopy, Color.White, Color.Black, new Vector2(0.3f), 0.75f);

            Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, str_pct_show, shopx + 120f - (v2_pct_show.X/3), shopy, Color.White, Color.Black, new Vector2(0.3f), 0.75f);
            //shopx + (float)(24 * 4)

            if (exp_need<=999999999 && exp_need>0)
                Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, str_exp_num, (shopx+65f) - (v2_exp_num.X/3), shopy + 25f, Color.White, Color.Black, new Vector2(0.3f), 0.75f);

            //shopx + (float)(24 * 1.6f)

            if (pct<.10f)
            {
                pct = .10f;
            }
            expBar.Width.Set(0f, pct);
            expBar.Recalculate();
        }

    }

    public class MoneyCounterGlobalItem : GlobalItem
    {
        /*
        public override void UpdateInventory(Item item, Player player)
        {
            if (item.type == mod.ItemType("Experience"))
            {
                (mod as ExperienceAndClasses).myUI.updateValue(player, mod);
            }
            base.UpdateInventory(item, player);
        }
        */
    }
}
