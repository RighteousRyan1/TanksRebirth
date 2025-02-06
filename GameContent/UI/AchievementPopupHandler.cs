using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Achievements;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using FontStashSharp;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.GameContent.Globals;

namespace TanksRebirth.GameContent.UI;

public class AchievementPopupHandler {

    private Vector2 _popupPos;
    private Vector2 _popupDims;
    private string _title;
    private string _description;
    private Texture2D? _curTexture;

    private volatile bool _isCurrentlyActive;

    private float _textScale = 0.5f;

    public AchievementRepository Repo;
    public AchievementPopupHandler(AchievementRepository repo) {
        Repo = repo;
        _title = string.Empty;
        _description = string.Empty;
    }
    public void SummonOrQueue(int achievementId) {
        if (_isCurrentlyActive) {
            Task.Run(async () => {
                await Task.Delay(50);
                SummonOrQueue(achievementId);
            });
            return;
        }
        var achs = Repo.GetAchievements();

        if (achievementId >= achs.Count) {
            TankGame.ClientLog.Write($"Invalid achievement has been unlocked. achievementId = {achievementId}, Count = {achs.Count}", LogType.ErrorSilent);
        }

        var ach = achs[achievementId] as Achievement; // convert it to a standardized achievement.

        var hasTexture = ach!.Texture is not null;
        #pragma warning disable CS8604, CS8601
        if (hasTexture) {
            var achievementGet = "Assets/sounds/menu/achievement.ogg";

            SoundPlayer.PlaySoundInstance(achievementGet, SoundContext.Effect, 1f, 0f, 0f, gameplaySound: true);
            _curTexture = ach.Texture;
            if (!string.IsNullOrEmpty(ach.Name)) {
                _title = ach.Name;
                _description = ach.Description;
                var titleMeasure = TankGame.TextFontLarge.MeasureString(_title) * _textScale;
                var descMeasure = TankGame.TextFontLarge.MeasureString(_description) * _textScale / 2;
                var biggerFloat = titleMeasure.X > descMeasure.X ? titleMeasure : descMeasure;
                _popupDims = ach.Texture.Size() + biggerFloat + new Vector2(100, 0);
                float interp = 0f;
                bool opening = true;
                var easingDelta = 0.02f;
                _isCurrentlyActive = true;
                Task.Run(async () => {
                    while (interp < 1f && opening) {
                        var val = Easings.OutBounce(interp);
                        interp += easingDelta;
                        _popupPos = WindowUtils.WindowTop - new Vector2(_popupDims.X / 2, _popupDims.Y) + new Vector2(0, val * _popupDims.Y);
                        await Task.Delay(TimeSpan.FromMilliseconds((int)TankGame.LogicTime.TotalMilliseconds));
                    }
                    if (interp > 1f)
                        interp = 1f;
                    // 2 secs of delay before it closes.
                    await Task.Delay(2000);
                    opening = false;

                    while (interp > 0f) {
                        var val = Easings.InOutSine(interp);
                        interp -= easingDelta;
                        _popupPos = WindowUtils.WindowTop - new Vector2(_popupDims.X / 2, _popupDims.Y) + new Vector2(0, val * _popupDims.Y);
                        await Task.Delay(TimeSpan.FromMilliseconds((int)TankGame.LogicTime.TotalMilliseconds));
                    }
                    if (interp < 0f)
                        interp = 0f;
                    _isCurrentlyActive = false;
                });
            }
        }
        #pragma warning restore
    }

    public void DrawPopup(SpriteBatch spriteBatch) {
        spriteBatch.Draw(TextureGlobals.Pixels[Color.White], _popupPos, null, (Color.Beige.ToVector3() * 0.33f).ToColor(), 0f, Vector2.Zero, _popupDims, default, 0f);
        var texOff = Vector2.Zero;
        if (_curTexture is not null) {
            texOff = new Vector2(100, _popupDims.Y / 2);
            spriteBatch.Draw(_curTexture, _popupPos + texOff, null, Color.Beige, 0f, _curTexture.Size() / 2, Vector2.One, default, 0f);
        }
        var titleOff = new Vector2(100, 25);
        var descOff = new Vector2(0, 60);

        spriteBatch.DrawString(TankGame.TextFontLarge, _title, _popupPos + new Vector2(texOff.X, 0) + titleOff, Color.White, Vector2.One * _textScale, textStyle: TextStyle.Underline);
        spriteBatch.DrawString(TankGame.TextFontLarge, _description, _popupPos + new Vector2(texOff.X, 0) + titleOff + descOff, Color.White, Vector2.One * _textScale / 2);
    }
}
