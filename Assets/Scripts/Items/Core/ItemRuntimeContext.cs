
using UnityEngine;

public sealed class ItemRuntimeContext
{
    public GameObject Owner { get; }

    public ItemRuntimeContext(GameObject owner)
    {
        Owner = owner;
    }
}
