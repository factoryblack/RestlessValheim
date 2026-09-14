using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

// Off-screen RT + panel-back Mask. Never UI-Mask the vanilla map RawImage.
// Large map keeps the source enabled (HideHit) so clicks work.
// Minimap disables the source and uses vanilla MapClick.
internal sealed class MapStudio
{
    public const int Layer = 28;

    public GameObject? Wrap { get; private set; }
    public GameObject? Backing { get; private set; }
    public RawImage? View { get; private set; }

    private readonly string _name;
    private readonly Vector3 _origin;
    private readonly float _depth;
    private GameObject? _studio;
    private Camera? _cam;
    private RawImage? _source;
    private RenderTexture? _rt;
    private Material? _mat;
    private Color _color = Color.white;
    private Texture? _seenTex;
    private Rect _seenUv;
    private int _seenW;
    private int _seenH;
    private int _seenMotion;
    private float _seenAt;
    private const float Resample = 0.2f;

    public MapStudio(string name, Vector3 origin, float depth)
    {
        _name = name;
        _origin = origin;
        _depth = depth;
    }

    public void EnsureStudio()
    {
        if (_studio != null)
            return;

        _studio = new GameObject(_name);
        Object.DontDestroyOnLoad(_studio);
        SetLayer(_studio, Layer);
        _studio.transform.position = _origin;

        _cam = _studio.AddComponent<Camera>();
        _cam.orthographic = true;
        _cam.orthographicSize = 1f;
        _cam.nearClipPlane = 0.1f;
        _cam.farClipPlane = 20f;
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = Color.clear;
        _cam.cullingMask = 1 << Layer;
        _cam.depth = _depth;
        _cam.allowHDR = false;
        _cam.allowMSAA = false;
        _cam.transform.position = _origin + new Vector3(0f, 0f, -4f);

        var canvasGo = new GameObject("canvas", typeof(RectTransform), typeof(Canvas));
        canvasGo.transform.SetParent(_studio.transform, false);
        SetLayer(canvasGo, Layer);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = _cam;
        var canvasRt = canvasGo.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(256f, 256f);
        canvasRt.localScale = new Vector3(2f / 256f, 2f / 256f, 1f);
        canvasRt.position = _origin;

        var sourceGo = new GameObject("source", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        sourceGo.transform.SetParent(canvasGo.transform, false);
        SetLayer(sourceGo, Layer);
        _source = sourceGo.GetComponent<RawImage>();
        _source.raycastTarget = false;
        RestlessUi.Stretch(sourceGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    public void EnsureView(RawImage src, string wrapName, bool ink, bool sliced, Vector4 tear)
    {
        if (Wrap != null)
            return;
        var parent = src.rectTransform.parent;
        if (parent == null)
            return;

        if (ink)
        {
            Backing = RestlessUi.Strip(parent, wrapName + "Ink", RestlessUi.Ink);
            Backing.GetComponent<Image>().raycastTarget = false;
            RestlessUi.CopyRect(Backing.GetComponent<RectTransform>(), src.rectTransform);
            Backing.transform.SetSiblingIndex(src.rectTransform.GetSiblingIndex() + 1);
        }

        Wrap = RestlessUi.Node(parent, wrapName);
        var wrapRt = Wrap.GetComponent<RectTransform>();
        RestlessUi.CopyRect(wrapRt, src.rectTransform);
        Wrap.transform.SetSiblingIndex(src.rectTransform.GetSiblingIndex() + (ink ? 2 : 1));

        var maskImg = Wrap.AddComponent<Image>();
        maskImg.sprite = sliced ? Kit.Sprite("panel-back", tear) : Kit.Sprite("panel-back");
        maskImg.color = Color.white;
        maskImg.raycastTarget = false;
        maskImg.preserveAspect = false;
        maskImg.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        var mask = Wrap.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var viewGo = new GameObject("view", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        viewGo.transform.SetParent(Wrap.transform, false);
        View = viewGo.GetComponent<RawImage>();
        View.raycastTarget = false;
        View.color = Color.white;
        RestlessUi.Stretch(viewGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    public bool Capture(RawImage src, int w, int h, bool disableSource, int motion = 0)
    {
        if (_source == null || _cam == null || View == null || Wrap == null)
            return false;

        EnsureStudio();
        if (_rt == null || _rt.width != w || _rt.height != h)
        {
            if (_rt != null)
                _rt.Release();
            _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            _rt.wrapMode = TextureWrapMode.Clamp;
            _rt.filterMode = FilterMode.Bilinear;
            _cam.targetTexture = _rt;
            if (_source.rectTransform.parent is RectTransform canvasRt)
            {
                canvasRt.sizeDelta = new Vector2(w, h);
                canvasRt.localScale = new Vector3(2f / h, 2f / h, 1f);
            }
        }

        KeepMat(src);
        var dirty = _seenTex != src.texture || _seenUv != src.uvRect || _seenW != w || _seenH != h
                    || _seenMotion != motion || Time.unscaledTime - _seenAt >= Resample;
        if (dirty)
        {
            _source.texture = src.texture;
            _source.uvRect = src.uvRect;
            _source.material = _mat != null ? _mat : src.material;
            _source.color = Color.white;
            _cam.enabled = true;
            _cam.Render();
            View.texture = _rt;
            View.uvRect = new Rect(0f, 0f, 1f, 1f);
            View.material = null;
            _seenTex = src.texture;
            _seenUv = src.uvRect;
            _seenW = w;
            _seenH = h;
            _seenMotion = motion;
            _seenAt = Time.unscaledTime;
        }

        if (disableSource)
            src.enabled = false;
        if (Backing != null)
        {
            Backing.SetActive(true);
            RestlessUi.CopyRect(Backing.GetComponent<RectTransform>(), src.rectTransform);
        }

        Wrap.SetActive(true);
        RestlessUi.CopyRect(Wrap.GetComponent<RectTransform>(), src.rectTransform);
        return true;
    }

    public void HideHit(RawImage src)
    {
        KeepMat(src);
        src.enabled = true;
        src.raycastTarget = true;
        var mat = src.material;
        var already = src.color.a < 0.02f &&
                      (mat == null || mat.shader == null || mat.shader.name == "UI/Default");
        if (!already)
        {
            src.material = null;
            src.color = Color.clear;
        }

        src.canvasRenderer.cullTransparentMesh = false;
        src.canvasRenderer.cull = false;
        foreach (var trigger in src.GetComponents<EventTrigger>())
            trigger.enabled = true;
        foreach (var graphic in src.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic == src || graphic.name == "MapClick")
            {
                graphic.enabled = true;
                graphic.raycastTarget = true;
                graphic.canvasRenderer.cull = false;
                graphic.canvasRenderer.cullTransparentMesh = false;
            }
        }
    }

    public void ShowSource(RawImage src)
    {
        src.enabled = true;
        src.raycastTarget = true;
        src.canvasRenderer.cull = false;
        src.canvasRenderer.cullTransparentMesh = false;
        if (_mat != null)
            src.material = _mat;
        src.color = _color.a > 0.2f ? _color : Color.white;
    }

    public void StackOver(RawImage src)
    {
        var i = src.rectTransform.GetSiblingIndex();
        if (Backing != null)
        {
            var want = i + 1;
            if (Backing.transform.GetSiblingIndex() != want)
                Backing.transform.SetSiblingIndex(want);
        }

        if (Wrap != null)
        {
            var want = i + (Backing != null ? 2 : 1);
            if (Wrap.transform.GetSiblingIndex() != want)
                Wrap.transform.SetSiblingIndex(want);
        }
    }

    public void Sleep()
    {
        if (Wrap != null)
            Wrap.SetActive(false);
        if (Backing != null)
            Backing.SetActive(false);
        if (_cam != null)
            _cam.enabled = false;
    }

    public void Dispose()
    {
        Sleep();
        if (Wrap != null)
            Object.Destroy(Wrap);
        if (Backing != null)
            Object.Destroy(Backing);
        Wrap = null;
        Backing = null;
        View = null;
        if (_rt != null)
        {
            _rt.Release();
            Object.Destroy(_rt);
            _rt = null;
        }

        if (_studio != null)
            Object.Destroy(_studio);
        _studio = null;
        _cam = null;
        _source = null;
        _mat = null;
        _seenTex = null;
        _seenW = _seenH = _seenMotion = 0;
        _seenAt = 0f;
    }

    public static void Adopt(Transform? child, Transform parent)
    {
        if (child == null || child.parent == parent)
            return;
        child.SetParent(parent, true);
    }

    private void KeepMat(RawImage src)
    {
        var mat = src.material;
        if (mat == null || mat.shader == null || mat.shader.name == "UI/Default")
            return;
        _mat = mat;
        if (src.color.a > 0.2f)
            _color = src.color;
    }

    private static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayer(child.gameObject, layer);
    }
}
