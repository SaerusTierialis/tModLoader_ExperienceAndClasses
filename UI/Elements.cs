using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace ExperienceAndClasses.UI {
    //combines a UserInterface and a UIState
    public abstract class UIStateCombo {
        public UserInterface UI = null;
        public UIState state = null;

        public bool Visibility {
            get {
                if (UI == null) {
                    return false;
                }
                else {
                    return UI.IsVisible;
                }
            }
            set {
                if (UI != null) {
                    UI.IsVisible = value;
                    if (!UI.IsVisible) {
                        UIPopup.Instance.EndTextChildren(state);
                    }
                }
            }
        }

        public void Initialize() {
            UI = new UserInterface();
            Visibility = false; //default
            state = new UIState();
            InitializeState();
            state.Activate();
            UI.SetState(state);
        }

        protected abstract void InitializeState();

        public void Update(GameTime game_time) {
            if (Visibility) {
                UI.Update(game_time);
            }
        }

        public void Draw() {
            if (Visibility) {
                state.Draw(Main.spriteBatch);
            }
        }

    }

    
}
