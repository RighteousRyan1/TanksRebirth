namespace TanksRebirth.Internals.Core.Interfaces;

public interface IGameObject {
    public void OnDestroy();

    public void OnInitialize();

    public void OnPreRender();

    public void OnRender();

    public void OnPostRender();

    public void OnUpdate();
}