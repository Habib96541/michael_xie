using UnityEngine;

namespace EggDrop
{
    public sealed class EggDropStage : MonoBehaviour
    {
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Transform launchMarker;
        [SerializeField] private Transform[] floorMarkers;
        [SerializeField] private Material upcomingMaterial;
        [SerializeField] private Material clearedMaterial;
        [SerializeField] private Material currentMaterial;

        private float targetSize;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private float shakeRemaining;
        private int currentFloor = 1;
        private bool wasPortrait;

        public static float HeightFor(int floor) => 2.8f + (floor - 1) * 1.05f;

        public void ShowFloor(int floor, int clearedFloors)
        {
            currentFloor = floor;
            float height = HeightFor(floor);
            launchMarker.position = new Vector3(0f, height - 0.7f, 0f);
            launchMarker.gameObject.SetActive(clearedFloors < EggDropRound.MaximumFloor);
            FrameCamera();
            for (int index = 0; index < floorMarkers.Length; index++)
            {
                if (!floorMarkers[index].TryGetComponent(out Renderer markerRenderer)) continue;
                markerRenderer.sharedMaterial = index < clearedFloors ? clearedMaterial
                    : index == floor - 1 ? currentMaterial : upcomingMaterial;
            }
        }

        private void Start()
        {
            ShowFloor(1, 0);
            stageCamera.orthographicSize = targetSize;
            stageCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
        }

        public void Impact(bool cracked) => shakeRemaining = cracked ? 0.3f : 0.12f;

        private void LateUpdate()
        {
            if (wasPortrait != (stageCamera.aspect < 1f)) FrameCamera();
            float blend = 1f - Mathf.Exp(-Time.deltaTime * 5f);
            stageCamera.orthographicSize = Mathf.Lerp(stageCamera.orthographicSize, targetSize, blend);
            stageCamera.transform.position = Vector3.Lerp(stageCamera.transform.position, targetPosition, blend);
            stageCamera.transform.rotation = Quaternion.Slerp(stageCamera.transform.rotation, targetRotation, blend);
            if (shakeRemaining <= 0f) return;
            shakeRemaining -= Time.deltaTime;
            stageCamera.transform.position += new Vector3(Mathf.Sin(Time.time * 75f), Mathf.Cos(Time.time * 63f), 0f)
                * shakeRemaining * 0.12f;
        }

        private void FrameCamera()
        {
            wasPortrait = stageCamera.aspect < 1f;
            float height = HeightFor(currentFloor);
            targetSize = wasPortrait ? Mathf.Max(5.3f, (height + 0.7f) * 1.55f) : Mathf.Max(3.6f, height * 0.65f + 1.6f);
            Vector3 focus = wasPortrait ? new Vector3(0f, targetSize * 0.12f, 0f)
                : new Vector3(targetSize * 0.58f, height * 0.48f + 0.5f, 0f);
            targetPosition = focus + new Vector3(5f, 3.2f, -18f);
            targetRotation = Quaternion.LookRotation(focus - targetPosition);
        }
    }
}
