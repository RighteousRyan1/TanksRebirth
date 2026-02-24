namespace TanksRebirth.Internals.Common.Framework.Interfaces; 

/// <summary>
/// Describes a piece of content with a given modded content of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The modded content.</typeparam>
public interface IHasModContent<T> where T : IModContent {
    T? ModdedData { get; }
}
