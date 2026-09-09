using UnityEngine;

public interface IAnchorable
{
    bool IsAnchored { get; }
    void Anchor(float duration);
}