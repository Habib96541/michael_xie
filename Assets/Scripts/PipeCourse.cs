using System.Collections.Generic;
using UnityEngine;

namespace FlapDash
{
    public sealed class PipeCourse : MonoBehaviour
    {
        [SerializeField] private RoundController round;
        [SerializeField] private PipeGate pipePrefab;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.65f;
        [SerializeField, Min(1)] private int pipesPerSpeedIncrease = 10;
        [SerializeField, Min(0f)] private float speedIncrease = 0.2f;
        [SerializeField, Min(0.1f)] private float maximumSpeed = 3.65f;
        [SerializeField, Min(0.01f)] private float acceleration = 0.2f;
        [SerializeField, Min(0.1f)] private float spawnInterval = 1.48f;
        [SerializeField, Min(0f)] private float firstSpawnDelay = 1.1f;
        [SerializeField] private float spawnX = 5.8f;
        [SerializeField] private float despawnX = -6f;
        [SerializeField] private float[] gapCenters = { 0f, 1.8f, -1.8f, 2.4f, -2.2f, 1.6f, -2.6f, 2f, -1.4f, 2.6f, -2f, 1.4f };

        private readonly List<Rigidbody2D> pipes = new();
        private float currentSpeed;
        private float spawnCountdown;
        private int nextGapIndex;
        private bool isRunning;

        public int ActivePipeCount => pipes.Count;

        private void FixedUpdate()
        {
            if (!isRunning || !round.IsPlaying)
                return;

            UpdateSpeed();
            spawnCountdown -= Time.fixedDeltaTime;
            if (spawnCountdown <= 0f)
            {
                SpawnPipe();
                spawnCountdown += spawnInterval;
            }

            MovePipes();
        }

        public void BeginCourse()
        {
            currentSpeed = moveSpeed;
            spawnCountdown = firstSpawnDelay;
            isRunning = true;
        }

        public void StopCourse()
        {
            isRunning = false;
            foreach (Rigidbody2D pipe in pipes)
            {
                pipe.linearVelocity = Vector2.zero;
                pipe.simulated = false;
            }
        }

        public void ResetCourse()
        {
            StopCourse();
            foreach (Rigidbody2D pipe in pipes)
            {
                pipe.gameObject.SetActive(false);
                Destroy(pipe.gameObject);
            }

            pipes.Clear();
            currentSpeed = moveSpeed;
            nextGapIndex = 0;
            spawnCountdown = firstSpawnDelay;
        }

        private void SpawnPipe()
        {
            float gapCenter = gapCenters[nextGapIndex];
            nextGapIndex = (nextGapIndex + 1) % gapCenters.Length;
            PipeGate gate = Instantiate(pipePrefab, new Vector3(spawnX, gapCenter, 0f), Quaternion.identity, transform);
            gate.InitializeRound(round);
            if (gate.TryGetComponent(out Rigidbody2D pipe))
                pipes.Add(pipe);
        }

        private void UpdateSpeed()
        {
            int speedLevel = round.Score / pipesPerSpeedIncrease;
            float targetSpeed = Mathf.Min(moveSpeed + speedLevel * speedIncrease, maximumSpeed);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        }

        private void MovePipes()
        {
            Vector2 step = Vector2.left * (currentSpeed * Time.fixedDeltaTime);
            for (int index = pipes.Count - 1; index >= 0; index--)
            {
                Rigidbody2D pipe = pipes[index];
                pipe.MovePosition(pipe.position + step);
                if (pipe.position.x >= despawnX)
                    continue;

                pipe.gameObject.SetActive(false);
                Destroy(pipe.gameObject);
                pipes.RemoveAt(index);
            }
        }
    }
}
