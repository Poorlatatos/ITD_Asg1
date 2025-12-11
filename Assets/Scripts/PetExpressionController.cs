using UnityEngine;

/// <summary>
/// Name : Jaasper Lee
/// Description : Controls pet expressions based on their stats.
/// Date : 3 December 2025
/// </summary>

public class PetExpressionController : MonoBehaviour
{
    public PetStatsComponent petStatsComponent;
    public GameObject happyExpression;
    public GameObject sadExpression;
    public GameObject hungryExpression;
    public GameObject dirtyExpression;
    public GameObject deadExpression;
    private GameObject currentExpression;

    private void Start()
    {
        UpdateExpression();
    }

    private void Update()
    {
        UpdateExpression();
    }

    public void UpdateExpression()
    {
        if (petStatsComponent == null || petStatsComponent.stats == null) return;

        var stats = petStatsComponent.stats;
        GameObject newExpression = null;

        // Dead state overrides everything: show only when all tracked stats are zero
        if (stats.petHappiness <= 0f && stats.petHunger <= 0f && stats.petCleanliness <= 0f)
        {
            newExpression = deadExpression;
        }
        else if (stats.petHappiness < 30f)
        {
            newExpression = sadExpression;
        }
        else if (stats.petHunger < 30f)
        {
            newExpression = hungryExpression;
        }
        else if (stats.petCleanliness < 30f)
        {
            newExpression = dirtyExpression;
        }
        else
        {
            newExpression = happyExpression;
        }

        if (currentExpression != newExpression)
        {
            // Ensure only the chosen expression is active; hide all others.
            HideAllExpressionsExcept(newExpression);

            currentExpression = newExpression;
            if (currentExpression != null)
                currentExpression.SetActive(true);
        }
    }

    private void HideAllExpressionsExcept(GameObject keep)
    {
        GameObject[] all = { happyExpression, sadExpression, hungryExpression, dirtyExpression, deadExpression };
        foreach (var go in all)
        {
            if (go == null) continue;
            if (go == keep) continue;
            if (go.activeSelf) go.SetActive(false);
        }
    }
}