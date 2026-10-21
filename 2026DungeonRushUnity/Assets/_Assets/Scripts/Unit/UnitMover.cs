using System.Collections.Generic;
using UnityEngine;

// Lái 1 unit theo đường A* bằng LỰC VẬT LÝ — port PhysicsMovementController gốc (qj = dữ liệu từng unit):
//   - RequestMoveTo (htj): tìm đường từ ô hiện tại tới ô đích; đang ở đúng ô đích / không có đường → false.
//   - Tick (htl): nhảy tới waypoint gần nhất phía trước; tới gần waypoint ≤ waypointReachedDistance thì sang ô kế
//     (hết path → dừng); còn không: AddForce = hệ số × (10 × (hướng × tốc độ − vận tốc) + hướng × Random(1, 10)).
//     Đi chưa được stuckThreshold × dt trong 1 tick ⇒ đang kẹt: hệ số lực tăng forceIncreaseRate × dt tới maxForce.
//   - Stop (htr): bỏ path, vận tốc = 0, hệ số lực về 1.
// Hero/quái dùng bộ tham số GameplayScene (10 / 0.5 / 6 / 3 / 0.05); pet dùng hằng số Companion (0.6 / 3 / 0.5).
public class UnitMover
{
    public struct Settings
    {
        public float moveForceMultiplier;
        public float waypointReachedDistance;
        public float maxForceMultiplier;
        public float forceIncreaseRate;
        public float stuckDetectionThreshold;
    }

    // PhysicsMovementController trong GameplayScene gốc.
    public static readonly Settings CharacterSettings = new Settings
    {
        moveForceMultiplier = 10f,
        waypointReachedDistance = 0.5f,
        maxForceMultiplier = 6f,
        forceIncreaseRate = 3f,
        stuckDetectionThreshold = 0.05f,
    };

    // Hằng số Companion gốc (wae / wab / waf / wag / wah).
    public static readonly Settings CompanionSettings = new Settings
    {
        moveForceMultiplier = 10f,
        waypointReachedDistance = 0.6f,
        maxForceMultiplier = 3f,
        forceIncreaseRate = 0.5f,
        stuckDetectionThreshold = 0.05f,
    };

    private const float RANDOM_PUSH_MIN = 1f;
    private const float RANDOM_PUSH_MAX = 10f;

    private readonly Rigidbody2D body;
    private readonly Settings settings;
    private List<Vector2Int> path;
    private int pathIndex;
    // Đi thẳng tới 1 điểm world (không A*) — RequestMoveToPoint; path = null khi đang ở chế độ này.
    private bool isPointMove;
    private Vector2 pointTarget;
    private float pointReachedDistance;
    private float forceMultiplier = 1f;
    private float stuckTime;
    private Vector2 lastPosition;

    public bool HasPath { get; private set; }
    public Vector2Int Goal { get; private set; }

    public UnitMover(Rigidbody2D body, Settings settings)
    {
        this.body = body;
        this.settings = settings;
    }

    // htj: đường từ ô đang đứng (kẹp vào lưới) tới goal. Waypoint đầu bỏ qua nếu chính là ô đang đứng.
    public bool RequestMoveTo(Vector2Int goal)
    {
        MapController map = MapController.Instance;
        if (map == null || !map.IsReady || body == null)
        {
            return false;
        }

        Vector2Int start = map.ClampCell(map.WorldToCell(body.position));
        if (start == goal)
        {
            return false;
        }

        List<Vector2Int> found = map.FindPath(start, goal);
        if (found.Count < 1)
        {
            return false;
        }

        path = found;
        pathIndex = found[0] == start ? 1 : 0;
        isPointMove = false;
        Goal = goal;
        HasPath = true;
        return true;
    }

    // KHÔNG có ở gốc: đã đứng trong ô đích nhưng còn lệch tâm (tới waypoint cuối chỉ cần ≤ waypointReachedDistance)
    // → lái thẳng vào điểm cho tới khi cách ≤ reachedDistance. Dùng cho ô vây boss (BaseMode.TryGetEngageCell).
    public bool RequestMoveToPoint(Vector2 point, float reachedDistance)
    {
        MapController map = MapController.Instance;
        if (map == null || !map.IsReady || body == null || Vector2.Distance(body.position, point) <= reachedDistance)
        {
            return false;
        }

        path = null;
        isPointMove = true;
        pointTarget = point;
        pointReachedDistance = reachedDistance;
        Goal = map.WorldToCell(point);
        HasPath = true;
        return true;
    }

    // htr
    public void Stop()
    {
        ClearPath();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
        forceMultiplier = 1f;
        stuckTime = 0f;
    }

    // Bỏ path nhưng KHÔNG hãm vận tốc (Companion.giu gốc khi đã đủ gần chủ: để lực cản tự dừng).
    public void ClearPath()
    {
        HasPath = false;
        path = null;
        isPointMove = false;
    }

    // htl — dt: thời gian tick (đã nhân tốc game); speed: tốc độ mong muốn.
    public void Tick(float dt, float speed)
    {
        if (!HasPath || body == null)
        {
            return;
        }

        if (isPointMove)
        {
            TickPoint(dt, speed);
            return;
        }

        if (pathIndex >= path.Count)
        {
            Stop();
            return;
        }

        MapController map = MapController.Instance;
        Vector2 position = body.position;

        // Nhảy tới waypoint gần nhất từ chỉ số hiện tại trở đi (bị đẩy lệch thì không quay lại ô cũ).
        int nearest = pathIndex;
        float nearestDistance = float.MaxValue;
        for (int i = pathIndex; i < path.Count; i++)
        {
            float d = Vector2.Distance(position, map.CellToWorld(path[i]));
            if (d < nearestDistance)
            {
                nearestDistance = d;
                nearest = i;
            }
        }
        pathIndex = Mathf.Max(pathIndex, nearest);

        Vector2 waypoint = map.CellToWorld(path[pathIndex]);
        Vector2 toWaypoint = waypoint - position;
        float distance = toWaypoint.magnitude;
        Vector2 direction = distance > 1e-5f ? toWaypoint / distance : Vector2.zero;

        if (distance <= settings.waypointReachedDistance)
        {
            pathIndex++;
            forceMultiplier = 1f;
            stuckTime = 0f;
            lastPosition = position;
            if (pathIndex >= path.Count)
            {
                ClearPath();
                body.linearVelocity = Vector2.zero;
            }
            return;
        }

        Steer(position, direction, dt, speed, true);
    }

    private void TickPoint(float dt, float speed)
    {
        Vector2 position = body.position;
        Vector2 toPoint = pointTarget - position;
        float distance = toPoint.magnitude;
        if (distance <= pointReachedDistance)
        {
            ClearPath();
            body.linearVelocity = Vector2.zero;
            return;
        }

        // Gần điểm thì giảm tốc, không cộng lực đẩy ngẫu nhiên → khỏi vọt qua điểm.
        Steer(position, toPoint / distance, dt, Mathf.Min(speed, distance * 4f), false);
    }

    private void Steer(Vector2 position, Vector2 direction, float dt, float speed, bool randomPush)
    {
        if (Vector2.Distance(position, lastPosition) < settings.stuckDetectionThreshold * dt)
        {
            stuckTime += dt;
            forceMultiplier = Mathf.Min(forceMultiplier + settings.forceIncreaseRate * dt, settings.maxForceMultiplier);
        }
        else
        {
            stuckTime = 0f;
        }
        lastPosition = position;

        float push = randomPush ? Random.Range(RANDOM_PUSH_MIN, RANDOM_PUSH_MAX) : 0f;
        Vector2 steering = settings.moveForceMultiplier * (direction * speed - body.linearVelocity) + direction * push;
        body.AddForce(forceMultiplier * steering);
    }
}
