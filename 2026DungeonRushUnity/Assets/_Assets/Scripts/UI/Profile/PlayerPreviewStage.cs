using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Hình hero mặc đồ + pet của người đang được xem (PlayerPreview gốc). Hero là SpriteRenderer/Spine ngoài world nên
// không đặt thẳng vào Canvas được: dựng 1 "sân khấu" nằm xa vùng map, camera riêng chụp vào RenderTexture rồi hiện
// qua RawImage. Hero = heroPrefab của mode hiện tại, gỡ hết logic chiến đấu, chỉ giữ HeroVisual để mặc đồ.
public class PlayerPreviewStage : MonoBehaviour
{
    // Xa hẳn map để camera trận không thấy sân khấu và camera sân khấu không thấy map.
    private static readonly Vector3 STAGE_POSITION = new Vector3(500f, 500f, 0f);
    // Pet đứng 2 bên và phía dưới hero (dưới tà áo choàng dài nhất).
    private static readonly Vector3[] PET_POSITIONS =
    {
        new Vector3(-1.5f, 0.1f, 0f),
        new Vector3(1.5f, 0.1f, 0f),
        new Vector3(0f, -1.35f, 0f),
    };
    private const float PET_SCALE = 0.5f;   // như node Icon trong prefab pet
    private const int TEXTURE_WIDTH = 900;
    private const int TEXTURE_HEIGHT = 500;

    public RawImage rawImage;
    public Color backgroundColor = new Color(0.15f, 0.15f, 0.18f, 1f);
    public float cameraSize = 1.45f;
    public Vector3 cameraOffset = new Vector3(0f, -0.35f, -10f);

    private GameObject stage;
    private RenderTexture texture;

    public void Show(UserEquipmentData equipment, bool showCloak, List<CompanionData> pets)
    {
        Clear();

        BaseMode mode = GameController.Instance.mode;
        if (mode == null || mode.heroPrefab == null)
        {
            return;
        }

        stage = new GameObject("PlayerPreviewStage");
        stage.transform.position = STAGE_POSITION;
        // Tắt sân khấu lúc dựng để Awake của HeroUnit không chạy trước khi bị gỡ.
        stage.SetActive(false);

        GameObject hero = Instantiate(mode.heroPrefab, stage.transform);
        hero.transform.localPosition = Vector3.zero;
        StripToVisual(hero);
        stage.SetActive(true);

        HeroVisual visual = hero.GetComponent<HeroVisual>();
        if (visual != null)
        {
            visual.SetOverrideEquipment(equipment, showCloak);
        }

        for (int i = 0; i < pets.Count && i < PET_POSITIONS.Length; i++)
        {
            GameObject pet = new GameObject(pets[i].assetName, typeof(SpriteRenderer));
            pet.transform.SetParent(stage.transform, false);
            pet.transform.localPosition = PET_POSITIONS[i];
            pet.transform.localScale = Vector3.one * PET_SCALE;
            pet.GetComponent<SpriteRenderer>().sprite = pets[i].icon;
        }

        if (texture == null)
        {
            texture = new RenderTexture(TEXTURE_WIDTH, TEXTURE_HEIGHT, 16);
        }

        GameObject cameraObject = new GameObject("Camera", typeof(Camera));
        cameraObject.transform.SetParent(stage.transform, false);
        cameraObject.transform.localPosition = cameraOffset;
        Camera cam = cameraObject.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = cameraSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
        cam.targetTexture = texture;

        rawImage.texture = texture;
        rawImage.enabled = true;
    }

    public void Clear()
    {
        if (stage != null)
        {
            Destroy(stage);
            stage = null;
        }
        rawImage.enabled = false;
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnDestroy()
    {
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }

    // Gỡ unit/va chạm/thanh máu/hiệu ứng: bản xem trước chỉ đứng yên, không tham gia trận.
    private static void StripToVisual(GameObject hero)
    {
        foreach (HealthBar bar in hero.GetComponentsInChildren<HealthBar>(true))
        {
            DestroyImmediate(bar.gameObject);
        }
        foreach (BaseUnit unit in hero.GetComponentsInChildren<BaseUnit>(true))
        {
            DestroyImmediate(unit);
        }
        foreach (Collider2D col in hero.GetComponentsInChildren<Collider2D>(true))
        {
            DestroyImmediate(col);
        }
        foreach (Rigidbody2D body in hero.GetComponentsInChildren<Rigidbody2D>(true))
        {
            DestroyImmediate(body);
        }
        foreach (ParticleSystem particle in hero.GetComponentsInChildren<ParticleSystem>(true))
        {
            particle.gameObject.SetActive(false);
        }
    }
}
