using Cyotek.Drawing.BitmapFont;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TanksRebirth.Achievements;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI;

public static class AchievementsUI {
    public static List<Achievement> VanillaAchievementsToList = new();

    internal static void GetVanillaAchievementsToList() {
        var vAchievements = VanillaAchievements.Repository.GetAchievements();

        foreach (var ach in vAchievements) {
            if (ach.Requirement is not null)
                VanillaAchievementsToList.Add((ach as Achievement)!);
        }
    }

    private static UIPanel _achBgPanel = new();
    public static List<UIImage> AchBtns = new();
    private static float _btnScl = 0.5f;

    private static int _achPerRow;
    public static int AchievementsPerRow {
        get => _achPerRow;
        set {
            _achPerRow = value;
            foreach (var btn in AchBtns)
                btn.Remove();
            AchBtns.Clear();
            InitBtns();
        }
    }

    internal static void InitBtns() {
        // find out why positions are in the two-thousands for some reason...
        var defX = 300f;
        var defY = 300f;
        var posX = defX;
        var posY = defY;
        var dims = 64f; // normally 64 for 0.5 scale
        var padding = 16f;

        var x = defX - padding;
        var y = defY - padding;
        var w = _achPerRow * (dims + padding) + padding;
        var h = (dims + padding) * (MathF.Floor(VanillaAchievementsToList.Count / _achPerRow) + 1) + padding;

        _achBgPanel = new();
        _achBgPanel.IsVisible = true;
        _achBgPanel.SetDimensions(() => new Vector2(x, y), () => new Vector2(w, h));

        for (int i = 0; i < VanillaAchievementsToList.Count; i++) {
            var btn = new UIImage(VanillaAchievementsToList[i].Texture ?? Achievement.MysteryTexture, new(_btnScl)) {
                Tooltip = VanillaAchievementsToList[i].Description,
                IsVisible = true,
            };
            var posX1 = posX;
            var posY1 = posY;
            btn.SetDimensions(() => new Vector2(posX1, posY1), () => new Vector2(dims));

            // kind of hacky fix but we will go with it
            var i1 = i + 1;

            if (i1 % _achPerRow == 0) {
                posY += dims + padding;
                posX = defX;
            }
            else
                posX += dims + padding;
            AchBtns.Add(btn);
        }
    }

    internal static void UpdateBtns() {
        //var defX = 300f;
        //var defY = 300f;
        //var dims = 64f; // normally 64 for 0.5 scale
        //var padding = 16f;
        //var x = defX - padding;
        //var y = defY - padding;
        //var w = _achPerRow * (dims + padding) + padding;
        //var h = (dims + padding) * (MathF.Floor(VanillaAchievementsToList.Count / _achPerRow) + 1) + padding;
        _achBgPanel.Position = _achBgPanel.Position.ToResolution();
        _achBgPanel.Size = _achBgPanel.Size.ToResolution();

        for (int i = 0; i < AchBtns.Count; i++) {
            AchBtns[i].Scale = new(_btnScl);
            AchBtns[i].Position = AchBtns[i].Position.ToResolution();
            AchBtns[i].Scale = AchBtns[i].Scale.ToResolution();

            if (VanillaAchievementsToList[i].IsComplete)
                AchBtns[i].Color = Color.Green;
        }
    }
}
