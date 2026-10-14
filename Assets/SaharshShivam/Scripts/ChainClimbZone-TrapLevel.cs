using UnityEngine;

public class ChainClimbZone : MonoBehaviour
{
    public Transform anchorTop;            // this chain's Anchor object
    public bool grabFromLeft = true;       // which side the player climbs from
    public float sideOffset = 0.5f;        // how far from the chain the player hangs

    [Header("Jump Off At The Top")]
    public float topJumpX = 5f;            // sideways speed (toward the platform)
    public float topJumpY = 10f;           // upward speed
}