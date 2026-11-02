using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

// Nghe 1 nhánh Firebase Realtime Database qua REST streaming (Server-Sent Events) — thay cho DatabaseReference.ChildAdded
// của SDK gốc (project không có Firebase Database SDK). GET <db>/<path>.json?orderBy="t"&limitToLast=N&auth=<idToken>,
// header Accept: text/event-stream. Server trả:
//   put  {path:"/", data:{key:msg,...}|null}  → ảnh chụp ban đầu (N tin cuối)
//   put  {path:"/<key>", data:msg|null}       → thêm tin / tin rơi khỏi N tin cuối
//   patch, keep-alive, cancel (bị từ chối quyền), auth_revoked (token hết hạn → nối lại với token mới).
// Đứt kết nối thì tự nối lại sau RECONNECT_DELAY giây cho tới khi Stop().
public class RealtimeDbStream
{
    private const float RECONNECT_DELAY = 3f;

    public event Action<JObject> Snapshot;              // data ban đầu (null = nhánh trống)
    public event Action<string, JObject> ChildChanged;  // key + tin (null = bị xoá)
    public event Action Failed;                         // không đọc được ảnh chụp ban đầu

    public bool IsRunning { get; private set; }
    public bool HasSnapshot { get; private set; }

    private readonly MonoBehaviour host;
    private readonly string path;
    private readonly int limitToLast;
    private Coroutine routine;
    private UnityWebRequest request;
    private int generation;

    public RealtimeDbStream(MonoBehaviour host, string path, int limitToLast)
    {
        this.host = host;
        this.path = path;
        this.limitToLast = limitToLast;
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }
        IsRunning = true;
        generation++;
        routine = host.StartCoroutine(RoutineListen(generation));
    }

    public void Stop()
    {
        IsRunning = false;
        HasSnapshot = false;
        generation++;
        if (routine != null)
        {
            host.StopCoroutine(routine);
            routine = null;
        }
        if (request != null)
        {
            request.Abort();
            request.Dispose();
            request = null;
        }
    }

    private IEnumerator RoutineListen(int gen)
    {
        while (IsRunning && gen == generation)
        {
            string token = null;
            yield return FirebaseManager.Instance.GetIdToken(t => token = t);
            if (gen != generation)
            {
                yield break;
            }

            if (string.IsNullOrEmpty(token) == false)
            {
                SseHandler handler = new SseHandler(OnEvent);
                request = new UnityWebRequest(BuildUrl(token), UnityWebRequest.kHttpVerbGET, handler, null);
                request.SetRequestHeader("Accept", "text/event-stream");
                yield return request.SendWebRequest();

                if (gen != generation)
                {
                    yield break;
                }
                if (request.result != UnityWebRequest.Result.Success)
                {
                    DebugCustom.LogWarning("[chat] stream " + path + " ended: " + request.error);
                }
                if (HasSnapshot == false)
                {
                    Failed?.Invoke();
                }
                request.Dispose();
                request = null;
            }
            else if (HasSnapshot == false)
            {
                Failed?.Invoke();
            }

            yield return new WaitForSecondsRealtime(RECONNECT_DELAY);
        }
    }

    private string BuildUrl(string token)
    {
        string query = "orderBy=" + UnityWebRequest.EscapeURL("\"t\"") + "&limitToLast=" + limitToLast + "&auth=" + token;
        string ns = FirebaseSettings.DatabaseNamespaceQuery;
        if (string.IsNullOrEmpty(ns) == false)
        {
            query += "&" + ns;
        }
        return FirebaseSettings.DatabaseUrl + "/" + path + ".json?" + query;
    }

    private void OnEvent(string eventName, string data)
    {
        switch (eventName)
        {
            case "put":
            case "patch":
                HandlePut(eventName == "patch", data);
                break;
            case "auth_revoked":
            case "cancel":
                DebugCustom.LogWarning("[chat] stream " + path + " " + eventName + ": " + data);
                request?.Abort();
                break;
        }
    }

    private void HandlePut(bool isPatch, string data)
    {
        JObject json;
        try
        {
            json = JObject.Parse(data);
        }
        catch (Exception e)
        {
            DebugCustom.LogWarning("[chat] stream parse: " + e.Message);
            return;
        }

        string eventPath = (string)json["path"] ?? "/";
        JObject value = json["data"] as JObject;
        if (eventPath == "/")
        {
            if (isPatch)
            {
                if (value == null) return;
                foreach (JProperty p in value.Properties())
                {
                    ChildChanged?.Invoke(p.Name, p.Value as JObject);
                }
                return;
            }
            HasSnapshot = true;
            Snapshot?.Invoke(value);
            return;
        }

        // "/<key>" hoặc "/<key>/<field>" (chỉ cần key — tin không bị sửa từng field).
        string key = eventPath.Substring(1);
        int slash = key.IndexOf('/');
        if (slash >= 0)
        {
            return;
        }
        ChildChanged?.Invoke(key, value);
    }

    // Tách dòng SSE: "event: X" + "data: Y" kết thúc bằng dòng trống. ReceiveData chạy trên main thread.
    private class SseHandler : DownloadHandlerScript
    {
        private readonly Action<string, string> onEvent;
        private readonly StringBuilder buffer = new StringBuilder();
        private string eventName;
        private string eventData;

        public SseHandler(Action<string, string> onEvent) : base(new byte[4096])
        {
            this.onEvent = onEvent;
        }

        protected override bool ReceiveData(byte[] bytes, int length)
        {
            buffer.Append(Encoding.UTF8.GetString(bytes, 0, length));
            string text = buffer.ToString();
            int start = 0;
            int newline;
            while ((newline = text.IndexOf('\n', start)) >= 0)
            {
                string line = text.Substring(start, newline - start).TrimEnd('\r');
                start = newline + 1;
                HandleLine(line);
            }
            buffer.Clear();
            buffer.Append(text.Substring(start));
            return true;
        }

        private void HandleLine(string line)
        {
            if (line.Length == 0)
            {
                if (eventName != null)
                {
                    onEvent(eventName, eventData ?? "null");
                }
                eventName = null;
                eventData = null;
                return;
            }
            if (line.StartsWith("event:"))
            {
                eventName = line.Substring(6).Trim();
            }
            else if (line.StartsWith("data:"))
            {
                eventData = line.Substring(5).Trim();
            }
        }
    }
}
