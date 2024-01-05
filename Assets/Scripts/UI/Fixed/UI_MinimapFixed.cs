using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UI_MinimapFixed : UI_Base, IPointerMoveHandler
{
    enum RectTransforms
    {
        MinimapIconName,
    }

    enum Texts
    {
        NameText,
    }

    enum RawImages
    {
        MinimapImage,
    }

    enum Cameras
    {
        MinimapCamera,
    }

    [SerializeField]
    private float _height;

    [SerializeField, Tooltip("Distance from mouse")]
    private Vector2 _deltaPosition;

    protected override void Init()
    {
        BindRT(typeof(RectTransforms));
        BindText(typeof(Texts));
        Bind<RawImage>(typeof(RawImages));
        Bind<Camera>(typeof(Cameras));
    }

    private void Start()
    {
        Managers.UI.Register<UI_MinimapFixed>(this);
    }

    private void Update()
    {
        if (Managers.Input.CursorLocked && GetRT((int)RectTransforms.MinimapIconName).gameObject.activeSelf)
        {
            GetRT((int)RectTransforms.MinimapIconName).gameObject.SetActive(false);
        }

        SetPosition(Mouse.current.position.ReadValue());
    }

    private void LateUpdate()
    {
        var position = Player.GameObject.transform.position;
        position.y = _height;
        var euler = Camera.main.transform.rotation.eulerAngles;
        euler.x = 90f;
        euler.z = 0f;
        Get<Camera>((int)Cameras.MinimapCamera).transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(Get<RawImage>((int)RawImages.MinimapImage).rectTransform,
            eventData.position, eventData.enterEventCamera, out var cursor))
        {
            Texture texture = Get<RawImage>((int)RawImages.MinimapImage).texture;
            Rect rect = Get<RawImage>((int)RawImages.MinimapImage).rectTransform.rect;

            float coordX = Mathf.Clamp(0, ((cursor.x - rect.x) * texture.width) / rect.width, texture.width);
            float coordY = Mathf.Clamp(0, ((cursor.y - rect.y) * texture.height) / rect.height, texture.height);

            float calX = coordX / texture.width;
            float calY = coordY / texture.height;

            cursor = new Vector2(calX, calY);

            CastRayToWorld(cursor);
        }
    }

    private void CastRayToWorld(Vector2 vec)
    {
        int layerMask = 1 << LayerMask.NameToLayer("Minimap");
        var minimapCamera = Get<Camera>((int)Cameras.MinimapCamera);
        Ray mapRay = minimapCamera.ScreenPointToRay(new Vector2(vec.x * minimapCamera.pixelWidth, vec.y * minimapCamera.pixelHeight));
        if (Physics.Raycast(mapRay, out var miniMapHit, Mathf.Infinity, layerMask))
        {
            GetText((int)Texts.NameText).text = miniMapHit.collider.gameObject.GetComponent<MinimapIcon>().IconName;
            GetRT((int)RectTransforms.MinimapIconName).gameObject.SetActive(true);
        }
        else
        {
            GetRT((int)RectTransforms.MinimapIconName).gameObject.SetActive(false);
        }
    }

    private void SetPosition(Vector3 position)
    {
        var rt = GetRT((int)RectTransforms.MinimapIconName);
        var nextPosition = new Vector3
        {
            x = position.x + (rt.rect.width * 0.5f) + _deltaPosition.x,
            y = position.y + (rt.rect.height * 0.5f) + _deltaPosition.y
        };

        rt.position = nextPosition;
    }
}
