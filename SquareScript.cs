using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private List<bool> actions;
    private int currentActionIndex = 0;
    private bool isDead = false;
    private bool canJump = true;
    private float startPositionX;

    public float moveSpeed = 5f;
    public float jumpForce = 15f;
    public int actionsPerSecond = 4;
    public float fallMultiplier = 2.5f;

    private GeneticAlgorithmManager manager;

    public void Initialize(List<bool> geneSequence, GeneticAlgorithmManager managerRef)
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody2D introuvable !");
            return;
        }

        actions = geneSequence;
        manager = managerRef;
        startPositionX = transform.position.x;
        currentActionIndex = 0;
        isDead = false;
        canJump = true;

        Debug.Log($"Initialisation du joueur {gameObject.name} - Taille des gènes : {actions.Count}");

        StartCoroutine(PerformActions());
    }

    private IEnumerator PerformActions()
    {
        float actionDelay = 1f / actionsPerSecond;

        while (!isDead && currentActionIndex < actions.Count)
        {
            UpdateManagerUI();

            if (actions[currentActionIndex] && canJump)
            {
                Jump();
            }

            currentActionIndex++;
            yield return new WaitForSeconds(actionDelay);
        }

        Debug.Log($"Joueur {gameObject.name} - Fin des actions ({currentActionIndex}/{actions.Count}) !");
    }

    private void Jump()
    {
        if (rb == null || isDead || !canJump) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        canJump = false;
        Debug.Log($"Le joueur {gameObject.name} a sauté !");
    }

    void Update()
    {
        if (rb == null || isDead) return;

        Move();

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }

        if (Mathf.Abs(rb.linearVelocity.y) < 0.01f && !canJump)
        {
            canJump = true;
        }

        UpdateManagerUI();
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);
    }

    private void UpdateManagerUI()
    {
        if (manager != null)
        {
            manager.UpdatePlayerInfo(this, currentActionIndex, actions.Count, canJump);
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float GetDistanceTravelled()
    {
        return transform.position.x - startPositionX;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("DeathZone"))
        {
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DeathZone"))
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        GetComponent<SpriteRenderer>().enabled = false;

        Debug.Log($"Le joueur {gameObject.name} est mort !");
    }

    public void ResetPlayer()
    {
        isDead = false;
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = false;
        transform.position = new Vector3(-10.854f, 0.403f, 0f);
        GetComponent<SpriteRenderer>().enabled = true;
        canJump = true;
    }
}
