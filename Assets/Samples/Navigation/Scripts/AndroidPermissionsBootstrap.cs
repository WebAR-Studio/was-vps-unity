using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;

/// <summary>
/// Requests mandatory Android permissions (camera + optional location) before loading the main scene.
/// Place this on a bootstrap scene. It will keep requesting until all required permissions are granted.
/// </summary>
public class AndroidPermissionsBootstrap : MonoBehaviour
{
    [Header("Target Scene")]
    [Tooltip("Name of the scene to load after permissions are granted.")]
    [SerializeField] private string _nextSceneName = "Main";

    [Header("Permissions")]
    [Tooltip("Also request location (ACCESS_FINE_LOCATION). Enable if AR/VPS needs GPS.")]
    [SerializeField] private bool _requestLocation = false;

    [Tooltip("Delay between repeated permission prompts (seconds).")]
    [SerializeField] private float _retryDelay = 1.5f;

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(EnsurePermissionsAndLoad());
#else
        LoadNextScene();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator EnsurePermissionsAndLoad()
    {
        while (true)
        {
            bool cameraOk = Permission.HasUserAuthorizedPermission(Permission.Camera);
            bool locationOk = !_requestLocation || Permission.HasUserAuthorizedPermission(Permission.FineLocation);

            if (cameraOk && locationOk)
                break;

            RequestMissing(cameraOk, locationOk);

            // Wait a bit before re-checking; avoids spamming prompts every frame.
            yield return new WaitForSeconds(_retryDelay);
        }

        LoadNextScene();
    }

    private void RequestMissing(bool cameraOk, bool locationOk)
    {
        if (!cameraOk)
        {
            Permission.RequestUserPermission(Permission.Camera);
        }

        if (_requestLocation && !locationOk)
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
    }
#endif

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(_nextSceneName))
        {
            Debug.LogWarning("AndroidPermissionsBootstrap: Next scene name is not set.");
            return;
        }

        SceneManager.LoadScene(_nextSceneName);
    }
}
