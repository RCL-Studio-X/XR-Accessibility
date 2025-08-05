using UnityEngine;

public class GameStateTester : MonoBehaviour
{
    public GameManager gameManager; // Drag your GameManager here

    public void TriggerState0()
    {
        if (gameManager != null)
            gameManager.SetGameState(0);
    }

    public void TriggerState1()
    {
        if (gameManager != null)
            gameManager.SetGameState(1);
    }

    public void TriggerState2()
    {
        if (gameManager != null)
            gameManager.SetGameState(2);
    }
}
