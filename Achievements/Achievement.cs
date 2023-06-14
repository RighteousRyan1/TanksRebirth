using Microsoft.Xna.Framework.Graphics;
using System;

namespace TanksRebirth.Achievements;

public class Achievement : IAchievement {

    public static Texture2D MysteryTexture;

    public Func<bool>? Requirement { get; set; }
    public bool IsComplete { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Texture2D? Texture { get; set; }
    /// <summary>Completes this <see cref="Achievement"/></summary>
    public void Complete() => IsComplete = true;

    public Achievement(string name, string description, Texture2D? texture = null, Func<bool>? requirement = null) {
        Name = name;
        Description = description;
        Texture = texture;
        Requirement = requirement;
    }
}
