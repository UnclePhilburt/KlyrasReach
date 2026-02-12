using UnityEngine;

public abstract class ColorBlockExtension : ScriptableObject {
    // Blends with the given extension then applies the result to the world.
    public abstract void BlendAndApply(ColorBlockExtension other, float weight);

    // For use during editing. Copy from world to block.
    public virtual void PullFromWorld() { }

    public void SingleBlock() => BlendAndApply(this, 0);

    public void TwoBlock(ColorBlockExtension other, float weight)
    {
        BlendAndApply(other, weight);
    }
}
