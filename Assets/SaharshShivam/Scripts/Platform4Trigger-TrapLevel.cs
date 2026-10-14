using UnityEngine;

public class Platform4Trigger : MonoBehaviour
{
    public FallingBallSaharsh ball;   // drag FallingBall here

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.root.name == "Player")
        {
            Debug.Log("Player touched Platform 4");
            ball.Drop();
        }
    }
}