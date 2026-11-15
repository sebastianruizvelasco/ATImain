using UnityEngine;

public abstract class InteractionAction : ScriptableObject
{
    public abstract void Execute(InteractionContext context);
}