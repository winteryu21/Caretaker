using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class ElectricRespawn : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 2f;

    private readonly HashSet<Transform> _pendingPlayers = new();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") ||
            respawnPoint == null ||
            !_pendingPlayers.Add(other.transform))
        {
            return;
        }

        StartCoroutine(TeleportPlayer(other.transform));
    }

    private IEnumerator TeleportPlayer(Transform player)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (player != null)
        {
            player.position = respawnPoint.position;

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        _pendingPlayers.Remove(player);
    }
}
