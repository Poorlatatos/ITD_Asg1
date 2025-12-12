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
    public GameObject happyExpressionCake;
    public GameObject sadExpressionCake;
    public GameObject hungryExpressionCake;
    public GameObject dirtyExpressionCake;
    public GameObject deadExpressionCake;
    private GameObject currentExpression;

    private void Start()
    {
    /// Calls UpdateExpression once on start to initialize current expression.
        UpdateExpression();
    }

    private void Update()
    {
    /// Calls UpdateExpression every frame to refresh the expression according to stats.
        UpdateExpression();
    }

    public void UpdateExpression()
    {
        if (petStatsComponent == null || petStatsComponent.stats == null) return;

        var stats = petStatsComponent.stats;
        GameObject newExpression = null;

        if (stats.petHappiness <= 0f && stats.petHunger <= 0f && stats.petCleanliness <= 0f)
        {
            newExpression = deadExpression;
        }
        else if (stats.petHappiness < 30f && stats.petAge < 3f)
        {
            newExpression = sadExpression;
        }
        else if (stats.petHunger < 30f && stats.petAge < 3f)
        {
            newExpression = hungryExpression;
        }
        else if (stats.petCleanliness < 30f && stats.petAge < 3f)
        {
            newExpression = dirtyExpression;
        }
        else if (stats.petHappiness <= 0f && stats.petHunger <= 0f && stats.petCleanliness <= 0f && stats.petAge >= 3f)
        {
            newExpression = deadExpressionCake;
        }
        else if (stats.petHappiness < 30f && stats.petAge >= 3f)
        {
            newExpression = sadExpressionCake;
        }
        else if (stats.petHunger < 30f && stats.petAge >= 3f)
        {
            newExpression = hungryExpressionCake;
        }
        else if (stats.petCleanliness < 30f && stats.petAge >= 3f)
        {
            newExpression = dirtyExpressionCake;
        }
        else if (stats.petAge >= 3f)
        {
            newExpression = happyExpressionCake;
        }
        else if (stats.petAge < 3f)
        {
            newExpression = happyExpression;
        }
        
        if (currentExpression != newExpression)
        {
            HideAllExpressionsExcept(newExpression);

            currentExpression = newExpression;
            if (currentExpression != null)
                currentExpression.SetActive(true);
        }
    }

    /// Hides all expression GameObjects except the provided one.
    private void HideAllExpressionsExcept(GameObject keep)
    {
        GameObject[] all = { happyExpression, sadExpression, hungryExpression, dirtyExpression, 
        deadExpression, happyExpressionCake, sadExpressionCake, hungryExpressionCake, 
        dirtyExpressionCake, deadExpressionCake };
        foreach (var go in all)
        {
            if (go == null) continue;
            if (go == keep) continue;
            if (go.activeSelf) go.SetActive(false);
        }
    }
}