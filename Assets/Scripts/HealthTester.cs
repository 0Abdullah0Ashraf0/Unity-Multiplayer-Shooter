using UnityEngine;
using UnityEngine.InputSystem;

public class HealthTester : MonoBehaviour
{
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.hKey.wasPressedThisFrame)
        {
            HUDManager.Instance?.UpdateHealth(75);
            HUDManager.Instance?.ShowDamageNumber(25, 75);
        }
        if (kb.jKey.wasPressedThisFrame)
        {
            HUDManager.Instance?.UpdateHealth(50);
            HUDManager.Instance?.ShowDamageNumber(25, 50);
        }
        if (kb.kKey.wasPressedThisFrame)
        {
            HUDManager.Instance?.UpdateHealth(25);
            HUDManager.Instance?.ShowDamageNumber(25, 25);
        }
        if (kb.lKey.wasPressedThisFrame)
        {
            HUDManager.Instance?.UpdateHealth(0);
            HUDManager.Instance?.ShowDamageNumber(25, 0);
        }
    }
}