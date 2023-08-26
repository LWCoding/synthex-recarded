using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampaignCameraController : MonoBehaviour
{

    public static CampaignCameraController Instance;

    [Header("Object Assignments")]
    [SerializeField] private Transform _cameraTransformToMove;

    private float _permanentZIndex;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
        }
        Instance = this;
        _permanentZIndex = _cameraTransformToMove.position.z;
    }

    public void MoveCameraToPosition(Vector2 position)
    {
        _cameraTransformToMove.position = new Vector3(position.x, position.y, _permanentZIndex);
    }

    ///<summary>
    /// Make the camera controller lerp to a specific position.
    /// Applies smoothing.
    ///</summary>
    public void LerpCameraToPosition(Vector2 position, float timeToWait)
    {
        StartCoroutine(LerpCameraToPositionCoroutine(position, timeToWait));
    }

    ///<summary>
    /// Make the camera controller lerp to a specific position.
    /// Applies smoothing. Coroutine version of above function.
    ///</summary>
    public IEnumerator LerpCameraToPositionCoroutine(Vector2 position, float timeToWait)
    {
        float currTime = 0;
        Vector3 initialPosition = _cameraTransformToMove.position;
        Vector3 targetPosition = new Vector3(position.x, position.y, _permanentZIndex);
        while (currTime < timeToWait)
        {
            currTime += Time.deltaTime;
            _cameraTransformToMove.position = Vector3.Lerp(initialPosition, targetPosition, Mathf.SmoothStep(0, 1, currTime / timeToWait));
            yield return null;
        }
    }

}
