using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TanksRebirth.Graphics.DynamicLighting;

/// <summary>
/// A <see cref="BasicEffect"/> that behaves exactly like the original, but reports every draw it
/// is used for to the <see cref="LightingSystem"/>. Models are given these automatically when
/// they are loaded, which is what lets the lighting system see the world without the rest of the
/// game having to change how it draws.
/// </summary>
/// <remarks>
/// One capture effect is made per original effect, so effects that were shared between meshes stay
/// shared. When a mesh is drawn, the bound index buffer tells us which mesh it is (MonoGame gives
/// every mesh its own buffers), and the parts of that mesh using this effect are applied in order,
/// which identifies the part.
/// </remarks>
public sealed class LightCaptureEffect : BasicEffect {
    sealed class Target {
        public ModelMesh Mesh = null!;
        public IndexBuffer? IndexBuffer;
        public readonly List<ModelMeshPart> Parts = [];
        public int Cursor;
    }

    readonly List<Target> _targets = [];

    public LightCaptureEffect(BasicEffect source) : base(source) {
        Name = source.Name;
        Tag = source.Tag;
    }

    internal void Register(ModelMesh mesh, ModelMeshPart part) {
        // ModelMesh.Draw skips empty parts without applying their effect
        if (part.PrimitiveCount <= 0)
            return;

        Target? target = null;
        foreach (var t in _targets) {
            if (t.Mesh == mesh) {
                target = t;
                break;
            }
        }
        if (target is null) {
            target = new Target { Mesh = mesh, IndexBuffer = part.IndexBuffer };
            _targets.Add(target);
        }
        if (!target.Parts.Contains(part))
            target.Parts.Add(part);
    }

    bool TryResolve(IndexBuffer? indices, out ModelMesh? mesh, out ModelMeshPart? part) {
        for (int i = 0; i < _targets.Count; i++) {
            var t = _targets[i];
            if (t.IndexBuffer != indices)
                continue;

            mesh = t.Mesh;
            if (t.Parts.Count == 1) {
                part = t.Parts[0];
            }
            else {
                part = t.Parts[t.Cursor];
                t.Cursor = (t.Cursor + 1) % t.Parts.Count;
            }
            return true;
        }
        mesh = null;
        part = null;
        return false;
    }

    protected override void OnApply() {
        base.OnApply();

        // always resolve (even when not capturing) so the part cursor stays in sync with ModelMesh.Draw
        if (TryResolve(GraphicsDevice.Indices, out var mesh, out var part))
            LightingSystem.OnCaptureEffectApplied(this, mesh!, part!);
    }
}
