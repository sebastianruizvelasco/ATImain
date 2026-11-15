using UnityEngine;

public class PlayerControlLock : MonoBehaviour
{
    [Tooltip("Arrastra aquí los componentes que controlan el movimiento y la cámara.")]
    [SerializeField] private Behaviour[] componentsToDisable;

    private bool[] previousStates;
    private int lockCount;

    private void Awake()
    {
        previousStates = new bool[componentsToDisable.Length];
    }

    public void Lock()
    {
        lockCount++;

        if (lockCount > 1)
            return;

        for (int i = 0; i < componentsToDisable.Length; i++)
        {
            if (componentsToDisable[i] == null)
                continue;

            previousStates[i] = componentsToDisable[i].enabled;
            componentsToDisable[i].enabled = false;
        }
    }

    public void Unlock()
    {
        if (lockCount <= 0)
            return;

        lockCount--;

        if (lockCount > 0)
            return;

        for (int i = 0; i < componentsToDisable.Length; i++)
        {
            if (componentsToDisable[i] != null)
                componentsToDisable[i].enabled = previousStates[i];
        }
    }
}