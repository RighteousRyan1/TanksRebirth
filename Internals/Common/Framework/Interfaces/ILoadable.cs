namespace TanksRebirth.Internals.Common.Framework.Interfaces;

/// <summary>Provides an API-compliant way of loading and unloading.</summary>
public interface ILoadable {
    /// <summary>Called when the type inheriting this interface is loaded.</summary>
    void OnLoad();
    /// <summary>Called when the type inheriting this interface is unloaded.</summary>
    void OnUnload();
}
