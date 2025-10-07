using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;

public class PermissionService : MonoBehaviour
{
    private static PermissionService _instance;
    private readonly Dictionary<string, TaskCompletionSource<bool>> _pendingRequests = new();

    public static PermissionService Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("PermissionsService");
                _instance = go.AddComponent<PermissionService>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public IEnumerator RequestAsync(string permission)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(permission))
            yield break;

        if (_pendingRequests.ContainsKey(permission))
            yield return _pendingRequests[permission].Task;

        var tcs = new TaskCompletionSource<bool>();
        _pendingRequests.Add(permission, tcs);

        Permission.RequestUserPermission(permission);
        yield return WaitForPermission(permission);
        yield return tcs.Task;
#else
        Debug.Log($"[PermissionsService] {permission} разрешение эмулировано в Editor → true");
        yield break;
#endif
    }

    public static bool HasPermission(string permission)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(permission);
#else
        return true;
#endif
    }

    private IEnumerator WaitForPermission(string permission)
    {
        while (!Permission.HasUserAuthorizedPermission(permission))
        {
            Permission.RequestUserPermission(permission);
            yield return new WaitForEndOfFrame();
        }

        bool granted = Permission.HasUserAuthorizedPermission(permission);
        if (_pendingRequests.TryGetValue(permission, out var tcs))
        {
            tcs.TrySetResult(granted);
            _pendingRequests.Remove(permission);
        }
    }
}
