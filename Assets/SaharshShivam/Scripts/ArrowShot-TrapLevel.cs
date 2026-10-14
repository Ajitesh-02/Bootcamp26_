using System.Collections;
using UnityEngine;

public class ArrowShot : MonoBehaviour
{
    [Header("Arrow Settings")]
    public GameObject arrowPrefab;          // Assign your arrow prefab here
    public Transform targetBlock;           // Assign 'StandTrapArrow 1' here
    public int arrowCount = 5;              // Number of arrows in the burst
    public float timeBetweenArrows = 0.08f; // Delay between each arrow firing
    public float arrowSpeed = 12f;          // Adjusted speed

    [Header("Tight Spread Settings")]
    [Tooltip("Small random offset at the spawn point")]
    public float spawnOffset = 0.15f;

    [Tooltip("Maximum distance arrows can deviate from the block's center")]
    public float targetOffset = 0.35f;

    private bool isTriggered = false;

    public void TriggerTrap()
    {
        if (isTriggered) return;
        isTriggered = true;
        StartCoroutine(FireVolley());
    }

    private IEnumerator FireVolley()
    {
        for (int i = 0; i < arrowCount; i++)
        {
            if (arrowPrefab != null && targetBlock != null)
            {
                // 1. Tiny variation at the spawn origin
                Vector3 spawnPos = transform.position + new Vector3(
                    Random.Range(-0.05f, 0.05f),
                    Random.Range(-spawnOffset, spawnOffset),
                    0f
                );

                // 2. Aim at a point tightly clustered directly on/around the block
                Vector3 aimTarget = targetBlock.position + new Vector3(
                    Random.Range(-targetOffset * 0.5f, targetOffset * 0.5f),
                    Random.Range(-targetOffset, targetOffset),
                    0f
                );

                // 3. Direction towards the clamped target point
                Vector2 direction = (aimTarget - spawnPos).normalized;

                GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

                ArrowProjectile proj = arrow.GetComponent<ArrowProjectile>();
                if (proj != null)
                {
                    proj.Initialize(direction, arrowSpeed);
                }
            }

            yield return new WaitForSeconds(timeBetweenArrows);
        }
    }
}