using FontStashSharp;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.LevelEditor;

public partial class LevelEditorUI {
    public static void DrawCampaigns() {
        if (loadedCampaign != null) {
            var heightDiff = 40;
            _missionButtonScissor = new Rectangle(_missionTab.X, _missionTab.Y + heightDiff, _missionTab.Width, _missionTab.Height - heightDiff * 2).ToResolution();
            TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], _missionTab.ToResolution(), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(_missionTab.X, _missionTab.Y, _missionTab.Width, heightDiff).ToResolution(), null, Color.White, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont,
                TankGame.GameLanguage.MissionList,
                new Vector2(175, 153).ToResolution(),
                Color.Black,
                Vector2.One.ToResolution(),
                0f,
                Anchor.TopCenter.GetAnchor(FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.MissionList)));
        }
    }
    public static void SetupMissionsBar(Campaign campaign, bool setCampaignData = true) {
        RemoveMissionButtons();

        if (setCampaignData) {
            CampaignName.Text = campaign.MetaData.Name;
            CampaignDescription.Text = campaign.MetaData.Description;
            CampaignAuthor.Text = campaign.MetaData.Author;
            CampaignTags.Text = string.Join(',', campaign.MetaData.Tags);
            CampaignVersion.Text = campaign.MetaData.Version;
            CampaignVersion.Text = campaign.MetaData.Version;
            CampaignLoadingBGColor.Text = campaign.MetaData.BackgroundColor.ToString();
            CampaignLoadingBannercolor.Text = campaign.MetaData.MissionStripColor.ToString();
            _hasMajorVictory = campaign.MetaData.HasMajorVictory;
        }

        float totalOff = 0;

        if (loadedCampaign != null) {
            for (int i = 0; i < campaign.CachedMissions.Length; i++) {
                var mission = campaign.CachedMissions[i];
                if (mission == default || mission.Name is null)
                    break;
                var btn = new UITextButton(mission.Name, FontGlobals.RebirthFont, Color.White, () => Vector2.One.ToResolution());
                btn.SetDimensions(() => new Vector2(_missionButtonScissor.X + 15.ToResolutionX(), _missionButtonScissor.Y + _missionsOffset), () => new Vector2(_missionButtonScissor.Width - 30.ToResolutionX(), 25.ToResolutionY()));

                btn.Offset = new(0, i * 30);
                totalOff += btn.Offset.Y;

                btn.HasScissor = true;
                btn.Scissor = () => _missionButtonScissor;

                int index = i;
                var len = btn.Text.Length;
                btn.TextScale = () => Vector2.One * (len > 20 ? 1f - (len - 20) * 0.03f : 1f);

                btn.OnLeftClick = (a) => {
                    _missionButtons.ForEach(x => x.Color = UnselectedColor);
                    btn.Color = SelectedColor;

                    var mission = loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId];

                    // save what we have before changing
                    loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId] = Mission.GetCurrent(mission.Name);
                    loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].GrantsExtraLife = mission.GrantsExtraLife;

                    loadedCampaign.LoadMission(index);
                    loadedCampaign.SetupLoadedMission(true);

                    MissionName.Text = loadedCampaign.CachedMissions[index].Name;

                    // update the mission we are wanting to rate
                    difficultyRating = DifficultyAlgorithm.GetDifficulty(loadedCampaign.CurrentMission);
                };
                btn.IsVisible = IsActive;
                _missionButtons.Add(btn);
            }
        }
    }
    public static void AddMission() {
        _missionButtons.ForEach(x => x.Color = Color.White);

        // resize so we can add the new mission
        Array.Resize(ref loadedCampaign.CachedMissions, loadedCampaign.CachedMissions.Length + 1);
        var count = loadedCampaign.CachedMissions.Count(c => c != default);
        var id = loadedCampaign.CurrentMissionId;
        loadedCampaign.CachedMissions[id] = Mission.GetCurrent(loadedCampaign.CachedMissions[id].Name);

        // move every mission up by 1 in the array.
        for (int i = count; i > id + 1; i--) {
            if (i + 1 >= loadedCampaign.CachedMissions.Length)
                Array.Resize(ref loadedCampaign.CachedMissions, loadedCampaign.CachedMissions.Length + 1);
            loadedCampaign.CachedMissions[i] = loadedCampaign.CachedMissions[i - 1];
        }
        if (id + 1 >= loadedCampaign.CachedMissions.Length)
            Array.Resize(ref loadedCampaign.CachedMissions, loadedCampaign.CachedMissions.Length + 1);
        loadedCampaign.CachedMissions[id + 1] = new([], []) {
            Name = $"Mission {id + 2}"
        };
        loadedCampaign.LoadMission(id + 1);
        loadedCampaign.SetupLoadedMission(true);

        MissionName.Text = loadedCampaign.CachedMissions[id].Name;

        SetupMissionsBar(loadedCampaign, false);

        _missionButtons[id + 1].Color = SelectedColor;
    }
    public static void RemoveMission() {
        _missionButtons.ForEach(x => x.Color = Color.White);

        Array.Resize(ref loadedCampaign.CachedMissions, loadedCampaign.CachedMissions.Length - 1);
        var count = loadedCampaign.CachedMissions.Count(c => c != default);
        var id = loadedCampaign.CurrentMissionId;
        loadedCampaign.CachedMissions[id] = Mission.GetCurrent(loadedCampaign.CachedMissions[id].Name);

        // move every mission back by 1 in the array.
        for (int i = id; i < loadedCampaign.CachedMissions.Length - 1; i++) {
            loadedCampaign.CachedMissions[i] = loadedCampaign.CachedMissions[i + 1];
        }
        loadedCampaign.CachedMissions[^1] = default;
        //if (id + 1 >= loadedCampaign.CachedMissions.Length)
        //Array.Resize(ref loadedCampaign.CachedMissions, loadedCampaign.CachedMissions.Length + 1);
        SetupMissionsBar(loadedCampaign, false);

        // if there is no fallback mission
        if (loadedCampaign.CachedMissions.Length == 1) {
            loadedCampaign.LoadMission(new Mission() {
                Blocks = [],
                Name = $"{TankGame.GameLanguage.Mission} 1",
                Tanks = []
            });
            return;
        }
        var newId = id > 0 ? id - 1 : id;
        loadedCampaign.LoadMission(newId);
        loadedCampaign.SetupLoadedMission(true);

        _missionButtons[newId].Color = SelectedColor;
    }
    /// <summary>
    /// Moves the currently loaded mission on the loaded levels tab. Will throw a <see cref="IndexOutOfRangeException"/> if the mission is too high or low.
    /// </summary>
    /// <param name="up">Whether to move it up (a mission BACK) or down (a mission FORWARD)</param>
    private static void MoveMission(bool up) {
        if (up) {
            if (loadedCampaign.CurrentMissionId == 0) {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("No mission above this one!", Color.Red);
                return;
            }
        }
        else {
            var count = loadedCampaign.CachedMissions.Count(x => x != default);
            if (loadedCampaign.CurrentMissionId >= count - 1) {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("No mission below this one!", Color.Red);
                return;
            }
        }


        var thisMission = loadedCampaign.CurrentMission;
        var targetMission = loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId + (up ? -1 : 1)];

        // CHECKME: works?
        loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId] = targetMission;
        loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId + (up ? -1 : 1)] = thisMission;

        loadedCampaign.LoadMission(loadedCampaign.CurrentMissionId + (up ? -1 : 1));

        // _campaignElems.First(x => x.Text == _loadedCampaign.CurrentMission.Name).Color = SelectedColor;

        SetupMissionsBar(loadedCampaign);

        _missionButtons[loadedCampaign.CurrentMissionId].Color = SelectedColor;
    }
    private static void RemoveMissionButtons() {
        for (int i = 0; i < _missionButtons.Count; i++)
            _missionButtons[i].Remove();
        _missionButtons.Clear();
    }
    private static void RemoveEditButtons() {
        for (int i = 0; i < _listModifyButtons.Count; i++)
            _listModifyButtons[i].Remove();
        _listModifyButtons.Clear();
    }
}
