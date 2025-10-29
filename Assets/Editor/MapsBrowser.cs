#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using GLTFast;
using GLTFast.Loading;

namespace EditorTools.MapsBrowser
{
    // =========================================
    //            CONFIG & MODELS
    // =========================================
    internal static class ApiConfig
    {
        public const string AuthBaseUrl = "http://217.16.22.58:8085";
        public const string ApiBaseUrl = "http://217.16.22.58:5051";

        public static string AuthEndpoint => AuthBaseUrl + "/api/v1/auth/mobile/login";
        public static string RefreshEndpoint => AuthBaseUrl + "/api/v1/auth/mobile/refresh";
        public static string LogoutEndpoint  => AuthBaseUrl + "/api/v1/auth/logout";
        public static string OrganizationsEndpoint => ApiBaseUrl + "/api/v3/organizations/";
        public static string TeamsEndpoint(string companyId) =>
            ApiBaseUrl + $"/api/v3/organizations/{companyId}/teams";
        public static string MapsEndpoint(string teamId, int page, int pageSize) =>
            ApiBaseUrl + $"/api/v3/arclip-maps/?team_id={teamId}&page={page}&page_size={pageSize}";

        public const string PrefKeyAccess = "MapsBrowser.AccessToken";
        public const string PrefKeyRefresh = "MapsBrowser.RefreshToken";
    }

    [Serializable] internal class LoginRequest { public string email; public string password; }
    [Serializable] internal class LoginResponse { public string message; public string accessToken; public string refreshToken; }

    [Serializable] internal class Company { public string id; public string name; }
    [Serializable] internal class Team { public string id; public string name; }

    [Serializable] internal class Map 
    { 
        public string id; 
        public string title; 
        public string status;
        public string additionalInfo;
        public string glbDownloadUrl;
        public string plyDownloadUrl;
    }

    [Serializable] internal class CompaniesResponse { public Company[] companies; }
    [Serializable] internal class TeamsResponse { public Team[] teams; }

    [Serializable] internal class PagedMapsResponse
    {
        public int page;
        public int pageSize;
        public int total;
        public Map[] items;
    }

    [Serializable] internal class OrganizationResponseDto { public OrganizationDto[] data; public int total; }
    [Serializable] internal class OrganizationDto { public string organization_id; public string organization_name; public string organization_type; public string avatar; public string license; }

    [Serializable] internal class TeamResponseDto { public TeamDto[] data; }
    [Serializable] internal class TeamDto { public string team_id; public string team_name; }

    [Serializable] internal class MapListResponseDto { public MapItemDto[] maps; public int total; public int page; public int page_size; }
    [Serializable] internal class MapItemDto { public string id; public string name; public string status; public string reconstruction_status; public string created_at; public string updated_at; public int images_count; public string ply_file_url; public string location_id; public MapModelsDto models; }
    [Serializable] internal class MapModelsDto { public MapModelUrlsDto sparse; public MapModelUrlsDto dense; public MapModelUrlsDto glb; }
    [Serializable] internal class MapModelUrlsDto { public string download_url; public string view_url; }

    // =========================================
    //          CUSTOM GLTFAST LOGGER
    // =========================================
    internal class DetailedGltfLogger : GLTFast.Logging.ICodeLogger
    {
        public void Error(GLTFast.Logging.LogCode code, params string[] messages)
        {
            Debug.LogError($"[GLTFast ERROR] Code: {code}, Message: {string.Join(" | ", messages)}");
        }

        public void Warning(GLTFast.Logging.LogCode code, params string[] messages)
        {
            Debug.LogWarning($"[GLTFast WARNING] Code: {code}, Message: {string.Join(" | ", messages)}");
        }

        public void Info(GLTFast.Logging.LogCode code, params string[] messages)
        {
            Debug.Log($"[GLTFast INFO] Code: {code}, Message: {string.Join(" | ", messages)}");
        }

        public void Error(string message)
        {
            Debug.LogError($"[GLTFast ERROR] {message}");
        }

        public void Warning(string message)
        {
            Debug.LogWarning($"[GLTFast WARNING] {message}");
        }

        public void Info(string message)
        {
            Debug.Log($"[GLTFast INFO] {message}");
        }
    }

    // =========================================
    //      TOLERANT DOWNLOAD PROVIDER
    // =========================================
    internal class TolerantDownload : IDownload
    {
        private readonly IDownload innerDownload;
        private readonly bool forceSuccess;

        public TolerantDownload(IDownload inner, bool force)
        {
            innerDownload = inner;
            forceSuccess = force;
        }

        public bool Success => forceSuccess || innerDownload.Success;
        public string Error => innerDownload.Error;
        public byte[] Data => innerDownload.Data;
        public string Text => innerDownload.Text;
        public bool? IsBinary => innerDownload.IsBinary;

        public Task WaitAsync() => Task.CompletedTask;
        public void Dispose() => innerDownload.Dispose();
    }

    internal class TolerantTextureDownload : ITextureDownload
    {
        private readonly ITextureDownload innerDownload;
        private readonly bool forceSuccess;

        public TolerantTextureDownload(ITextureDownload inner, bool force)
        {
            innerDownload = inner;
            forceSuccess = force;
        }

        public bool Success => forceSuccess || innerDownload.Success;
        public string Error => innerDownload.Error;
        public byte[] Data => innerDownload.Data;
        public string Text => innerDownload.Text;
        public bool? IsBinary => innerDownload.IsBinary;
        public Texture2D Texture => innerDownload.Texture;

        public Task WaitAsync() => Task.CompletedTask;
        public void Dispose() => innerDownload.Dispose();
    }

    internal class TolerantDownloadProvider : IDownloadProvider
    {
        private static int requestCounter = 0;

        public async Task<IDownload> Request(Uri url)
        {
            int requestId = ++requestCounter;
            Debug.Log($"🌐 [HTTP #{requestId}] Request to: {url}");
            
            var download = new AwaitableDownload(url);
            await download.WaitAsync();
            
            bool success = download.Success;
            Debug.Log($"📥 [HTTP #{requestId}] Result: {(success ? "✅ SUCCESS" : "❌ ERROR")}");
            
            if (!success)
            {
                Debug.LogWarning($"⚠️ [HTTP #{requestId}] Failed to download: {url}\n" +
                                 $"   Continuing without this file...");
            }
            
            return new TolerantDownload(download, false);
        }

        public async Task<ITextureDownload> RequestTexture(Uri url, bool nonReadable)
        {
            int requestId = ++requestCounter;
            Debug.Log($"🖼️ [TEXTURE #{requestId}] Texture request: {url}");
            
            var download = new AwaitableTextureDownload(url, nonReadable);
            await download.WaitAsync();
            
            bool success = download.Success;
            
            if (!success)
            {
                Debug.LogWarning($"⚠️ [TEXTURE #{requestId}] Texture unavailable: {url}\n" +
                                 $"   Default material will be used.");
                return new TolerantTextureDownload(download, true);
            }
            else
            {
                Debug.Log($"✅ [TEXTURE #{requestId}] Texture loaded: {url}");
                return new TolerantTextureDownload(download, false);
            }
        }
    }

    // =========================================
    //              API CLIENT
    // =========================================
    internal class ApiClient
    {
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public bool HasTokens => !string.IsNullOrEmpty(AccessToken) && !string.IsNullOrEmpty(RefreshToken);

        internal static HttpClient httpClient;

        static ApiClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                AllowAutoRedirect = true
            };
            
            httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public ApiClient()
        {
            AccessToken = EditorPrefs.GetString(ApiConfig.PrefKeyAccess, string.Empty);
            RefreshToken = EditorPrefs.GetString(ApiConfig.PrefKeyRefresh, string.Empty);
        }

        public void ClearTokens()
        {
            AccessToken = null;
            RefreshToken = null;
            EditorPrefs.DeleteKey(ApiConfig.PrefKeyAccess);
            EditorPrefs.DeleteKey(ApiConfig.PrefKeyRefresh);
        }

        public void SaveTokens(string access, string refresh)
        {
            AccessToken = access;
            RefreshToken = refresh;
            EditorPrefs.SetString(ApiConfig.PrefKeyAccess, access ?? string.Empty);
            EditorPrefs.SetString(ApiConfig.PrefKeyRefresh, refresh ?? string.Empty);
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var payload = new LoginRequest { email = email, password = password };
            var json = JsonUtility.ToJson(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(ApiConfig.AuthEndpoint, content);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonUtility.FromJson<LoginResponse>(body);
                if (data != null && !string.IsNullOrEmpty(data.accessToken) && !string.IsNullOrEmpty(data.refreshToken))
                {
                    SaveTokens(data.accessToken, data.refreshToken);
                    return true;
                }
                throw new Exception("Login success but tokens not found in response (expected: accessToken, refreshToken).");
            }
            else
            {
                throw new Exception($"Login failed: {(int)response.StatusCode} {response.ReasonPhrase} \n{body}");
            }
        }

        private async Task<bool> RefreshAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(RefreshToken)) return false;
            var payload = new { refreshToken = RefreshToken };
            var json = JsonUtility.ToJson(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(ApiConfig.RefreshEndpoint, content);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var data = JsonUtility.FromJson<LoginResponse>(body);
                    if (data != null && !string.IsNullOrEmpty(data.accessToken))
                    {
                        SaveTokens(data.accessToken, string.IsNullOrEmpty(data.refreshToken) ? RefreshToken : data.refreshToken);
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public async Task LogoutAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, ApiConfig.LogoutEndpoint);
                if (!string.IsNullOrEmpty(AccessToken))
                    request.Headers.Add("Authorization", $"Bearer {AccessToken}");
                await httpClient.SendAsync(request);
            }
            catch { }
        }

        public async Task<Company[]> GetCompaniesAsync()
        {
            var response = await SendWithRefreshRetryAsync(ApiConfig.OrganizationsEndpoint, HttpMethod.Get);
            var body = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
                throw new Exception($"GetOrganizations failed: {(int)response.StatusCode} {response.ReasonPhrase} \n{body}");
            
            var dto = JsonUtility.FromJson<OrganizationResponseDto>(body);
            if (dto?.data == null) throw new Exception("OrganizationResponse parsing failed. Check field names.");

            var list = new List<Company>(dto.data.Length);
            foreach (var it in dto.data)
            {
                list.Add(new Company { id = it.organization_id, name = it.organization_name });
            }
            return list.ToArray();
        }

        public async Task<Team[]> GetTeamsAsync(string companyId)
        {
            var response = await SendWithRefreshRetryAsync(ApiConfig.TeamsEndpoint(companyId), HttpMethod.Get);
            var body = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
                throw new Exception($"GetTeams failed: {(int)response.StatusCode} {response.ReasonPhrase} \n{body}");
            
            var dto = JsonUtility.FromJson<TeamResponseDto>(body);
            if (dto?.data == null) throw new Exception("TeamResponse parsing failed. Check field names.");

            var list = new List<Team>(dto.data.Length);
            foreach (var it in dto.data)
            {
                list.Add(new Team { id = it.team_id, name = it.team_name });
            }
            return list.ToArray();
        }

        public async Task<PagedMapsResponse> GetMapsAsync(string teamId, int page, int pageSize)
        {
            var response = await SendWithRefreshRetryAsync(ApiConfig.MapsEndpoint(teamId, page, pageSize), HttpMethod.Get);
            var body = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
                throw new Exception($"GetMaps failed: {(int)response.StatusCode} {response.ReasonPhrase} \n{body}");
            
            var dto = JsonUtility.FromJson<MapListResponseDto>(body);
            if (dto == null || dto.maps == null) throw new Exception("MapListResponse parsing failed. Check field names.");

            var resp = new PagedMapsResponse
            {
                page = dto.page,
                pageSize = dto.page_size,
                total = dto.total,
                items = new Map[dto.maps.Length]
            };

            for (int i = 0; i < dto.maps.Length; i++)
            {
                var m = dto.maps[i];
                
                string statusText = string.IsNullOrEmpty(m.status) ? "-" : m.status;
                
                var additionalParts = new List<string>();
                if (!string.IsNullOrEmpty(m.reconstruction_status))
                    additionalParts.Add("recon: " + m.reconstruction_status);
                if (m.images_count != 0)
                    additionalParts.Add("images: " + m.images_count);
                string additionalInfo = additionalParts.Count > 0 ? string.Join(", ", additionalParts) : null;

                string glbUrl = null;
                if (m.models?.glb?.download_url != null)
                {
                    glbUrl = m.models.glb.download_url;
                }

                string plyUrl = null;
                if (m.models?.sparse?.download_url != null)
                {
                    plyUrl = m.models.sparse.download_url;
                }

                resp.items[i] = new Map
                {
                    id = m.id,
                    title = string.IsNullOrEmpty(m.name) ? m.id : m.name,
                    status = statusText,
                    additionalInfo = additionalInfo,
                    glbDownloadUrl = glbUrl,
                    plyDownloadUrl = plyUrl
                };
            }

            return resp;
        }

        async Task<HttpResponseMessage> SendWithRefreshRetryAsync(string url, HttpMethod method, HttpContent content = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (content != null)
                request.Content = content;
            if (!string.IsNullOrEmpty(AccessToken))
                request.Headers.Add("Authorization", $"Bearer {AccessToken}");

            var response = await httpClient.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            bool refreshed = await RefreshAccessTokenAsync();
            if (!refreshed)
                return response;

            var retryRequest = new HttpRequestMessage(method, url);
            if (content != null)
            {
                var contentBytes = await content.ReadAsByteArrayAsync();
                retryRequest.Content = new ByteArrayContent(contentBytes);
                foreach (var header in content.Headers)
                    retryRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            if (!string.IsNullOrEmpty(AccessToken))
                retryRequest.Headers.Add("Authorization", $"Bearer {AccessToken}");

            return await httpClient.SendAsync(retryRequest);
        }
    }

    // =========================================
    //             EDITOR WINDOW (IMGUI)
    // =========================================
    public class MapsBrowserWindow : EditorWindow
    {
        [MenuItem("Tools/Maps Browser")] private static void ShowWindow()
        {
            var wnd = GetWindow<MapsBrowserWindow>();
            wnd.titleContent = new GUIContent("Maps Browser");
            wnd.minSize = new Vector2(560, 420);
            wnd.Show();
        }

        private ApiClient api;
        private string login = string.Empty;
        private string password = string.Empty;
        private bool showPassword = false;

        private bool isBusy = false;
        private string status = string.Empty;
        private Vector2 scroll;

        private Company[] companies = Array.Empty<Company>();
        private int selectedCompanyIndex = -1;

        private Team[] teams = Array.Empty<Team>();
        private int selectedTeamIndex = -1;

        private PagedMapsResponse mapsPage;
        private int page = 1;
        private int pageSize = 20;

        private void OnEnable()
        {
            api = new ApiClient();
            if (api.HasTokens)
            {
                _ = SafeExec(async () =>
                {
                    await LoadCompaniesAsync();
                });
            }
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                DrawHeader();
                EditorGUILayout.Space();

                if (!api.HasTokens)
                {
                    DrawLoginUI();
                }
                else
                {
                    DrawDataUI();
                }

                if (!string.IsNullOrEmpty(status))
                {
                    EditorGUILayout.HelpBox(status, MessageType.Info);
                }
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Maps Browser", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(isBusy))
                {
                    if (api.HasTokens)
                    {
                        if (GUILayout.Button(new GUIContent("Refresh", EditorGUIUtility.IconContent("Refresh").image), EditorStyles.toolbarButton, GUILayout.Width(90)))
                        {
                            _ = SafeExec(async () =>
                            {
                                await ReloadCurrentAsync();
                            });
                        }

                        if (GUILayout.Button(new GUIContent("Logout", EditorGUIUtility.IconContent("winbtn_mac_close_h").image), EditorStyles.toolbarButton, GUILayout.Width(70)))
                        {
                            _ = SafeExec(async () =>
                            {
                                try { await api.LogoutAsync(); } catch (Exception ex) { Debug.LogWarning("Logout request failed: " + ex.Message); }
                                api.ClearTokens();
                                ClearAllData();
                            });
                        }
                    }
                }
            }
        }

        private void DrawLoginUI()
        {
            GUILayout.Label("Authorization", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            login = EditorGUILayout.TextField("Email", login);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (showPassword)
                    password = EditorGUILayout.TextField("Password", password);
                else
                    password = EditorGUILayout.PasswordField("Password", password);

                showPassword = GUILayout.Toggle(showPassword, showPassword ? "👁" : "🚫", GUILayout.Width(40));
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(isBusy || string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password)))
            {
                if (GUILayout.Button("Login", GUILayout.Height(28)))
                {
                    _ = SafeExec(async () =>
                    {
                        await DoLoginAsync();
                    });
                }
            }
        }

        private void DrawDataUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Step 1 — Company", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(isBusy))
                {
                    if (companies == null || companies.Length == 0)
                    {
                        EditorGUILayout.HelpBox("No companies or not loaded.", MessageType.None);
                        if (GUILayout.Button("Load Companies"))
                        {
                            _ = SafeExec(async () => await LoadCompaniesAsync());
                        }
                    }
                    else
                    {
                        var names = MapNames(companies);
                        int newIndex = EditorGUILayout.Popup("Company", Mathf.Max(0, selectedCompanyIndex), names);
                        if (newIndex != selectedCompanyIndex)
                        {
                            selectedCompanyIndex = newIndex;
                            _ = SafeExec(async () => await OnCompanyChangedAsync());
                        }
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Step 2 — Team", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(isBusy || selectedCompanyIndex < 0))
                {
                    if (teams == null || teams.Length == 0)
                    {
                        EditorGUILayout.HelpBox("No teams or not loaded.", MessageType.None);
                    }
                    else
                    {
                        var names = MapNames(teams);
                        int newIndex = EditorGUILayout.Popup("Team", Mathf.Max(0, selectedTeamIndex), names);
                        if (newIndex != selectedTeamIndex)
                        {
                            selectedTeamIndex = newIndex;
                            _ = SafeExec(async () => await OnTeamChangedAsync());
                        }
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Step 3 — Maps (pagination)", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(isBusy))
                    {
                        page = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Page"), page));
                        pageSize = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Per Page"), pageSize), 1, 200);

                        if (GUILayout.Button("Load", GUILayout.Width(110)))
                        {
                            _ = SafeExec(async () => await LoadMapsAsync());
                        }
                    }
                }

                EditorGUILayout.Space(4);

                // Navigation
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(isBusy || mapsPage == null))
                    {
                        if (GUILayout.Button("◀", GUILayout.Width(40)))
                        {
                            page = Mathf.Max(1, page - 1);
                            _ = SafeExec(async () => await LoadMapsAsync());
                        }

                        GUILayout.Label(mapsPage != null ? $"Page {mapsPage.page} / {ComputeTotalPages(mapsPage.total, mapsPage.pageSize)} (total {mapsPage.total})" : "Page - / -");

                        if (GUILayout.Button("▶", GUILayout.Width(40)))
                        {
                            int totalPages = mapsPage != null ? ComputeTotalPages(mapsPage.total, mapsPage.pageSize) : int.MaxValue;
                            page = Mathf.Min(totalPages, page + 1);
                            _ = SafeExec(async () => await LoadMapsAsync());
                        }
                    }
                }

                EditorGUILayout.Space(4);

                // Map list
                using (var sv = new EditorGUILayout.ScrollViewScope(scroll))
                {
                    scroll = sv.scrollPosition;

                    if (mapsPage?.items != null)
                    {
                        foreach (var m in mapsPage.items)
                        {
                            DrawMapItem(m);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No data. Select a team and load the list.", MessageType.None);
                    }
                }
            }
        }

        private void DrawMapItem(Map m)
        {
            var boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8)
            };
            
            using (new EditorGUILayout.VerticalScope(boxStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var titleStyle = new GUIStyle(EditorStyles.largeLabel)
                    {
                        fontStyle = FontStyle.Bold,
                        fontSize = 13
                    };
                    GUILayout.Label(string.IsNullOrEmpty(m.title) ? m.id : m.title, titleStyle);
                    
                    GUILayout.FlexibleSpace();
                    
                    if (!string.IsNullOrEmpty(m.id))
                    {
                        if (GUILayout.Button("Copy ID", GUILayout.Width(60), GUILayout.Height(20)))
                        {
                            EditorGUIUtility.systemCopyBuffer = m.id;
                            Debug.Log($"Map ID '{m.title}' copied: {m.id}");
                            status = $"ID copied: {m.id}";
                            Repaint();
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(m.plyDownloadUrl))
                    {
                        using (new EditorGUI.DisabledScope(isBusy))
                        {
                            if (GUILayout.Button("Load PLY", GUILayout.Width(75), GUILayout.Height(20)))
                            {
                                _ = SafeExec(async () => await LoadAndCreatePLYAsync(m.plyDownloadUrl, m.title));
                            }
                        }
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            GUILayout.Button("Load PLY", GUILayout.Width(75), GUILayout.Height(20));
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(m.glbDownloadUrl))
                    {
                        using (new EditorGUI.DisabledScope(isBusy))
                        {
                            if (GUILayout.Button("Load GLB", GUILayout.Width(75), GUILayout.Height(20)))
                            {
                                _ = SafeExec(async () => await LoadAndCreateGLBAsync(m.glbDownloadUrl, m.title));
                            }
                        }
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            GUILayout.Button("Load GLB", GUILayout.Width(75), GUILayout.Height(20));
                        }
                    }
                }
                
                EditorGUILayout.Space(2);
                var rect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
                EditorGUILayout.Space(4);
                
                if (!string.IsNullOrEmpty(m.status))
                {
                    var labelStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        wordWrap = true
                    };
                    
                    string fullDescription = "status: " + m.status;
                    if (!string.IsNullOrEmpty(m.additionalInfo))
                    {
                        fullDescription += ", " + m.additionalInfo;
                    }
                    
                    GUILayout.Label(fullDescription, labelStyle);
                    EditorGUILayout.Space(4);
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontStyle = FontStyle.Italic
                    };
                    GUILayout.Label("ID:", labelStyle, GUILayout.Width(25));
                    GUILayout.Label(m.id ?? "-", EditorStyles.miniLabel);
                }
            }
        }

        // =========================================
        //                 ACTIONS
        // =========================================
        private async Task DoLoginAsync()
        {
            status = "Authorizing...";
            isBusy = true; Repaint();
            try
            {
                bool ok = await api.LoginAsync(login?.Trim(), password);
                if (ok)
                {
                    status = "Login successful";
                    await LoadCompaniesAsync();
                }
                else
                {
                    status = "Authorization failed";
                }
            }
            catch (Exception ex)
            {
                status = "Authorization error: " + ex.Message;
                Debug.LogError(ex);
            }
            finally { isBusy = false; Repaint(); }
        }

        private async Task LoadCompaniesAsync()
        {
            status = "Loading companies...";
            isBusy = true; Repaint();
            try
            {
                companies = await api.GetCompaniesAsync();
                selectedCompanyIndex = companies != null && companies.Length > 0 ? 0 : -1;

                teams = Array.Empty<Team>();
                selectedTeamIndex = -1;
                mapsPage = null;

                if (selectedCompanyIndex >= 0)
                {
                    await LoadTeamsAsync();
                }
                else
                {
                    status = "No companies found";
                }
            }
            catch (Exception ex)
            {
                status = "Error loading companies: " + ex.Message;
                Debug.LogError(ex);
            }
            finally { isBusy = false; Repaint(); }
        }

        private async Task OnCompanyChangedAsync()
        {
            teams = Array.Empty<Team>();
            selectedTeamIndex = -1;
            mapsPage = null;
            page = 1;
            await LoadTeamsAsync();
        }

        private async Task LoadTeamsAsync()
        {
            if (selectedCompanyIndex < 0 || companies == null || companies.Length == 0)
                return;

            var company = companies[selectedCompanyIndex];
            status = $"Loading teams (company: {company.name})...";
            isBusy = true; Repaint();
            try
            {
                teams = await api.GetTeamsAsync(company.id);
                selectedTeamIndex = teams != null && teams.Length > 0 ? 0 : -1;
                mapsPage = null;

                if (selectedTeamIndex >= 0)
                {
                    await LoadMapsAsync();
                }
                else
                {
                    status = "No teams found";
                }
            }
            catch (Exception ex)
            {
                status = "Error loading teams: " + ex.Message;
                Debug.LogError(ex);
            }
            finally { isBusy = false; Repaint(); }
        }

        private async Task OnTeamChangedAsync()
        {
            mapsPage = null;
            page = 1;
            await LoadMapsAsync();
        }

        private async Task LoadMapsAsync()
        {
            if (selectedTeamIndex < 0 || teams == null || teams.Length == 0)
            {
                status = "Please select a team first";
                return;
            }

            var team = teams[selectedTeamIndex];
            status = $"Loading maps (team: {team.name})...";
            isBusy = true; Repaint();
            try
            {
                mapsPage = await api.GetMapsAsync(team.id, page, pageSize);
                if (mapsPage != null)
                {
                    page = Mathf.Max(1, mapsPage.page);
                    pageSize = Mathf.Max(1, mapsPage.pageSize);
                    status = $"Loaded {mapsPage.items?.Length ?? 0} of {mapsPage.total}";
                }
            }
            catch (Exception ex)
            {
                status = "Error loading maps: " + ex.Message;
                Debug.LogError(ex);
            }
            finally { isBusy = false; Repaint(); }
        }

        private async Task ReloadCurrentAsync()
        {
            if (!api.HasTokens)
            {
                status = "Not authorized";
                return;
            }

            if (selectedCompanyIndex < 0)
            {
                await LoadCompaniesAsync();
                return;
            }

            if (selectedTeamIndex < 0)
            {
                await LoadTeamsAsync();
                return;
            }

            await LoadMapsAsync();
        }

        private async Task LoadAndCreateGLBAsync(string url, string modelName)
        {
            status = $"Loading model '{modelName}'...";
            isBusy = true;
            Repaint();

            GameObject parentObject = null;

            try
            {
                parentObject = new GameObject(modelName);
                
                var logger = new DetailedGltfLogger();
                var downloadProvider = new TolerantDownloadProvider();
                var deferAgent = new UninterruptedDeferAgent();
                
                var importSettings = new ImportSettings
                {
                    GenerateMipMaps = true,
                    AnisotropicFilterLevel = 1,
                    NodeNameMethod = NameImportMethod.OriginalUnique
                };
                
                var gltfImport = new GltfImport(downloadProvider: downloadProvider, deferAgent: deferAgent, logger: logger);
                
                Debug.Log($"Loading GLB model: {modelName} from {url}");
                
                bool loadSuccess = await gltfImport.Load(url, importSettings);
                
                status = $"Creating object '{modelName}' on scene...";
                Repaint();
                
                var instantiator = new GameObjectInstantiator(gltfImport, parentObject.transform);
                bool instantiateSuccess = await gltfImport.InstantiateMainSceneAsync(instantiator);
                
                if (instantiateSuccess || parentObject.transform.childCount > 0)
                {
                    parentObject.transform.position = Vector3.zero;
                    parentObject.transform.rotation = Quaternion.identity;
                    
                    Selection.activeGameObject = parentObject;
                    
                    if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.FrameSelected();
                    }
                    
                    if (loadSuccess && instantiateSuccess)
                    {
                        status = $"Model '{modelName}' loaded successfully!";
                        Debug.Log($"✅ GLB model '{modelName}' fully created on scene from {url}");
                    }
                    else
                    {
                        status = $"Model '{modelName}' loaded (some textures may be missing)";
                        Debug.LogWarning($"⚠️ GLB model '{modelName}' created on scene, but some resources (textures) may be missing.");
                    }
                }
                else
                {
                    throw new Exception("GLTFast failed to create any objects. Check Unity console for detailed errors.");
                }
            }
            catch (Exception ex)
            {
                if (parentObject != null)
                {
                    DestroyImmediate(parentObject);
                }
                
                status = $"Model loading error: {ex.Message}";
                Debug.LogError($"Error loading GLB '{modelName}' from {url}: {ex}");
            }
            finally
            {
                isBusy = false;
                Repaint();
            }
        }

        private async Task LoadAndCreatePLYAsync(string url, string modelName)
        {
            status = $"Loading PLY model '{modelName}'...";
            isBusy = true;
            Repaint();

            GameObject parentObject = null;

            try
            {
                Debug.Log($"Downloading PLY from: {url}");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(api.AccessToken))
                    request.Headers.Add("Authorization", $"Bearer {api.AccessToken}");
                
                var response = await ApiClient.httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to download PLY: {response.StatusCode} {response.ReasonPhrase}");
                }

                byte[] plyData = await response.Content.ReadAsByteArrayAsync();
                Debug.Log($"Downloaded {plyData.Length} bytes");

                status = $"Parsing PLY file '{modelName}'...";
                Repaint();

                var (positions, colors) = ParseBinaryPLY(plyData);
                Debug.Log($"Parsed {positions.Count} points");

                if (positions.Count == 0)
                {
                    throw new Exception("No points found in PLY file");
                }

                status = $"Creating point cloud '{modelName}' on scene...";
                Repaint();

                parentObject = new GameObject($"{modelName} (PLY)");
                CreatePointCloudMesh(parentObject, positions, colors);

                parentObject.transform.position = Vector3.zero;
                parentObject.transform.rotation = Quaternion.identity;

                Selection.activeGameObject = parentObject;

                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }

                status = $"PLY model '{modelName}' loaded successfully! ({positions.Count} points)";
                Debug.Log($"✅ PLY model '{modelName}' created on scene with {positions.Count} points");
            }
            catch (Exception ex)
            {
                if (parentObject != null)
                {
                    DestroyImmediate(parentObject);
                }

                status = $"PLY loading error: {ex.Message}";
                Debug.LogError($"Error loading PLY '{modelName}' from {url}: {ex}");
            }
            finally
            {
                isBusy = false;
                Repaint();
            }
        }

        private (List<Vector3>, List<Color32>) ParseBinaryPLY(byte[] data)
        {
            var positions = new List<Vector3>();
            var colors = new List<Color32>();

            int headerEnd = -1;
            for (int i = 0; i < data.Length - 10; i++)
            {
                if (data[i] == 'e' && data[i + 1] == 'n' && data[i + 2] == 'd' && 
                    data[i + 3] == '_' && data[i + 4] == 'h' && data[i + 5] == 'e' && 
                    data[i + 6] == 'a' && data[i + 7] == 'd' && data[i + 8] == 'e' && 
                    data[i + 9] == 'r')
                {
                    for (int j = i + 10; j < data.Length; j++)
                    {
                        if (data[j] == '\n')
                        {
                            headerEnd = j + 1;
                            break;
                        }
                    }
                    break;
                }
            }

            if (headerEnd == -1)
            {
                throw new Exception("Could not find end_header in PLY file");
            }

            string header = System.Text.Encoding.ASCII.GetString(data, 0, headerEnd);
            int vertexCount = 0;
            
            string[] lines = header.Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith("element vertex"))
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length >= 3)
                    {
                        vertexCount = int.Parse(parts[2]);
                    }
                    break;
                }
            }

            if (vertexCount == 0)
            {
                throw new Exception("Could not find vertex count in PLY header");
            }

            // CRITICAL: each vertex is 15 bytes (3 floats + 3 bytes RGB)
            int dataStart = headerEnd;
            int bytesPerVertex = 15;

            for (int i = 0; i < vertexCount; i++)
            {
                int offset = dataStart + i * bytesPerVertex;
                
                if (offset + bytesPerVertex > data.Length)
                {
                    Debug.LogWarning($"Incomplete vertex data at vertex {i}, stopping");
                    break;
                }

                float x = BitConverter.ToSingle(data, offset);
                float y = BitConverter.ToSingle(data, offset + 4);
                float z = BitConverter.ToSingle(data, offset + 8);
                byte r = data[offset + 12];
                byte g = data[offset + 13];
                byte b = data[offset + 14];

                // CRITICAL: Flip X coordinate for ARKit to Unity conversion
                positions.Add(new Vector3(-x, y, z));
                colors.Add(new Color32(r, g, b, 255));
            }

            return (positions, colors);
        }

        private void CreatePointCloudMesh(GameObject parent, List<Vector3> positions, List<Color32> colors)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Editor/VertexColorShader.shader");
            if (shader == null)
            {
                Debug.LogError("VertexColorShader.shader not found! Make sure it exists in Assets/Editor/");
                shader = Shader.Find("Standard");
            }
            Material material = new Material(shader);

            // CRITICAL: Unity mesh vertex limit = 65000
            float pointSize = 0.01f;
            int maxVerticesPerMesh = 65000;
            int pointsPerMesh = maxVerticesPerMesh / 4;

            for (int meshIndex = 0; meshIndex * pointsPerMesh < positions.Count; meshIndex++)
            {
                int startPoint = meshIndex * pointsPerMesh;
                int endPoint = Mathf.Min(startPoint + pointsPerMesh, positions.Count);
                int pointCount = endPoint - startPoint;

                Mesh mesh = new Mesh();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                List<Color32> vertexColors = new List<Color32>();

                for (int i = 0; i < pointCount; i++)
                {
                    Vector3 center = positions[startPoint + i];
                    Color32 color = colors[startPoint + i];

                    int baseVertex = vertices.Count;
                    float halfSize = pointSize * 0.5f;

                    vertices.Add(center + new Vector3(-halfSize, -halfSize, 0));
                    vertices.Add(center + new Vector3(halfSize, -halfSize, 0));
                    vertices.Add(center + new Vector3(halfSize, halfSize, 0));
                    vertices.Add(center + new Vector3(-halfSize, halfSize, 0));

                    for (int v = 0; v < 4; v++)
                    {
                        vertexColors.Add(color);
                    }

                    triangles.Add(baseVertex + 0);
                    triangles.Add(baseVertex + 2);
                    triangles.Add(baseVertex + 1);
                    
                    triangles.Add(baseVertex + 0);
                    triangles.Add(baseVertex + 3);
                    triangles.Add(baseVertex + 2);
                }

                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();
                mesh.colors32 = vertexColors.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                GameObject meshObject = new GameObject($"PointCloud_Mesh_{meshIndex}");
                meshObject.transform.SetParent(parent.transform);
                meshObject.transform.localPosition = Vector3.zero;
                meshObject.transform.localRotation = Quaternion.identity;
                meshObject.transform.localScale = Vector3.one;

                MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
            }
        }

        private void ClearAllData()
        {
            login = password = string.Empty;
            companies = Array.Empty<Company>();
            teams = Array.Empty<Team>();
            mapsPage = null;
            selectedCompanyIndex = selectedTeamIndex = -1;
            page = 1; pageSize = 20;
            status = "Logged out.";
        }

        // =========================================
        //               SMALL HELPERS
        // =========================================
        private static string[] MapNames(Company[] arr)
        {
            var names = new string[arr.Length];
            for (int i = 0; i < arr.Length; i++) names[i] = string.IsNullOrEmpty(arr[i].name) ? arr[i].id : arr[i].name;
            return names;
        }
        private static string[] MapNames(Team[] arr)
        {
            var names = new string[arr.Length];
            for (int i = 0; i < arr.Length; i++) names[i] = string.IsNullOrEmpty(arr[i].name) ? arr[i].id : arr[i].name;
            return names;
        }

        private static int ComputeTotalPages(int total, int pageSize)
        {
            if (pageSize <= 0) return 1;
            return Mathf.Max(1, (int)Mathf.Ceil((float)total / pageSize));
        }

        private async Task SafeExec(Func<Task> action)
        {
            try { await action(); }
            catch (Exception ex)
            {
                status = ex.Message;
                Debug.LogError(ex);
            }
            finally { Repaint(); }
        }
    }
}
#endif
