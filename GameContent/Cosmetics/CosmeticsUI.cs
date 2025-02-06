using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Cosmetics;

#pragma warning disable

// ikik, not in the UI namespace but whatever
public static class CosmeticsUI {
    public static bool IsActive => MainMenu.MenuState == MainMenu.UIState.Cosmetics;

    private static float _interp;
    private static bool _switch;

    public static RenderableCrate CosmeticsChest;

    public static void Initialize() {
        CosmeticsChest = new(new(0, 0, 0), TankGame.GameView, TankGame.GameProjection);
        CosmeticsChest.Rotation = new Vector3(0, 0, MathHelper.Pi + MathHelper.PiOver4);
    }

    public static void Update() {
        CosmeticsChest.View = TankGame.GameView;
        CosmeticsChest.Projection = TankGame.GameProjection;

        GameShaders.BlurFactor += (!IsActive ? 0.000075f : -0.000075f) * TankGame.DeltaTime;
        GameShaders.BlurFactor = MathHelper.Clamp(GameShaders.BlurFactor, 0f, 0.0075f);

        _interp += (_switch ? 0.015f : -0.015f) * TankGame.DeltaTime;

        if (TankGame.RunTime % 120 <= TankGame.DeltaTime) _switch = !_switch;
        if (_interp > 1) _interp = 1;
        if (_interp < 0) _interp = 0;

        CosmeticsChest.Scale = 0.3f;

        CosmeticsChest.ChestPosition = new Vector3(-875f, 992.81537f, 2850f); //, new Vector3(0f, -0.004487315f, -5.86277f)

        CosmeticsChest.LidRotation = new Vector3(0, Easings.GetEasingBehavior(_switch ? EasingFunction.OutBounce : EasingFunction.OutSine, _interp) * (MathHelper.Pi + MathHelper.PiOver4 / 2), 0);
    }
    public static void RenderCrates() {
        CosmeticsChest?.Render();
    }
}
