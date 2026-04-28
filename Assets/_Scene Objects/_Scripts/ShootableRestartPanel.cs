using UnityEngine;

public class ShootableEndPanel : MonoBehaviour
{
    [SerializeField] private CyberEndSequence endManager;

    private bool alreadyHit = false;

    public void HitPanel()
    {
        if (alreadyHit) return;
        if (!gameObject.activeInHierarchy) return;

        alreadyHit = true;

        if (endManager == null) return;

        endManager.RestartGame();

        gameObject.SetActive(false);
    }
}