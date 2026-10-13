using System;
using System.Collections.Generic;

[Serializable]
public sealed class ItemStats
{
    // =====================================================================
    // SERIALIZED DATA
    // =====================================================================

    public List<ItemStatModifier> modifiers =
        new List<ItemStatModifier>();


    // =====================================================================
    // PUBLIC API
    // =====================================================================

    public int Count =>
        modifiers != null
            ? modifiers.Count
            : 0;


    // =====================================================================
    // VALIDATION
    // =====================================================================

    public bool HasModifiers()
    {
        return modifiers != null &&
               modifiers.Count > 0;
    }


    public void EnsureInitialized()
    {
        if (modifiers == null)
        {
            modifiers = new List<ItemStatModifier>();
        }
    }
}