using UnityEngine;

public class ShootableIntroPanel : MonoBehaviour
{
    [SerializeField] private CyberSiegeIntroManager introManager;
    [SerializeField] private PanelType panelType;

    private bool alreadyHit = false;

    public enum PanelType
    {
        Panel1,
        Panel2
    }

    public void HitPanel()
    {
        if (alreadyHit) return;
        if (!gameObject.activeInHierarchy) return;

        alreadyHit = true;

        if (introManager == null) return;

        if (panelType == PanelType.Panel1)
            introManager.ShootPanel1();
        else
            introManager.ShootPanel2();

        gameObject.SetActive(false);
    }
}