using UnityEngine;

public class InteractionInventory : MonoBehaviour
{
    public bool HasBuildingKey { get; private set; }

    public void PickupBuildingKey()
    {
        HasBuildingKey = true;
        Debug.Log("Has recogido la llave del edificio.");
    }
}