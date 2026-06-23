using UnityEngine;

public class JumpMultiplier : MonoBehaviour
{
    public CubemanController cubemanController;

    public float multiplier = 5f;
    public float smooth = 3f;

    public bool isGrounded = true;
    private float currentTargetY;
     public float baseHeight;

    void Update()
    {
        baseHeight = cubemanController.heightGround;

        isGrounded = baseHeight > 0.2f;

        // Define o alvo dependendo do estado
        if (isGrounded)
            currentTargetY = baseHeight;
        else
            currentTargetY = baseHeight * multiplier;

        // Aplica suavização SEMPRE (tanto subindo quanto descendo)
        Vector3 targetPos = new Vector3(
            transform.position.x,
            currentTargetY,
            transform.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smooth * Time.deltaTime
        );
    }
}