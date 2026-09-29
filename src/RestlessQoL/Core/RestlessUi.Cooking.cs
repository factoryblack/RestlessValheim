using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

internal static partial class RestlessUi
{
    private static Sprite[]? _cookFrameParts;
    // Eleven regions: ordinary nine-slicing would stretch the bottom-centre knot.
    // The three bottom-middle regions keep that ornament at the same scale as the corners.
    public static void CookHeroSurface(GameObject target)
    {
        var source = Kit.Sprite("cook-portrait");
        var image = target.GetComponent<Image>();
        if (source == null || image == null) return;
        if (_cookFrameParts == null)
        {
            var regions = new[] {
                new Rect(0,674,330,350), new Rect(330,674,856,350), new Rect(1186,674,350,350),
                new Rect(0,210,330,464), new Rect(330,210,856,464), new Rect(1186,210,350,464),
                new Rect(0,0,330,210), new Rect(330,0,350,210), new Rect(680,0,180,210),
                new Rect(860,0,326,210), new Rect(1186,0,350,210)
            };
            _cookFrameParts = new Sprite[regions.Length];
            for (var i=0;i<regions.Length;i++)
                _cookFrameParts[i] = Sprite.Create(source.texture,regions[i],new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        }
        image.sprite = null; image.color = Color.clear; image.raycastTarget = true;
        Rim(target,on:false);
        var old = target.transform.Find("paperAccent");
        if (old != null) old.gameObject.SetActive(false);
        var frame = target.transform.Find("cookHeroFrame")?.gameObject ?? Node(target.transform,"cookHeroFrame");
        frame.SetActive(true); frame.transform.SetAsFirstSibling();
        Stretch(frame,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var size = ((RectTransform)target.transform).rect.size;
        const float scale = 1f/4.5f;
        var left=330*scale; var right=350*scale; var top=350*scale; var bottom=210*scale;
        var middle=size.x-left-right; var centreHeight=Mathf.Max(0,size.y-top-bottom);
        var knot=180*scale; var knotX=(size.x-knot)/2;
        var bounds = new[] {
            new Rect(0,0,left,top),new Rect(left,0,middle,top),new Rect(size.x-right,0,right,top),
            new Rect(0,top,left,centreHeight),new Rect(left,top,middle,centreHeight),new Rect(size.x-right,top,right,centreHeight),
            new Rect(0,size.y-bottom,left,bottom),new Rect(left,size.y-bottom,knotX-left,bottom),
            new Rect(knotX,size.y-bottom,knot,bottom),new Rect(knotX+knot,size.y-bottom,size.x-right-knotX-knot,bottom),
            new Rect(size.x-right,size.y-bottom,right,bottom)
        };
        for (var i=0;i<bounds.Length;i++)
        {
            var part=frame.transform.Find("part"+i)?.gameObject ?? Graphic(frame.transform,"part"+i,Color.white,false);
            var picture=part.GetComponent<Image>(); picture.sprite=_cookFrameParts[i]; picture.type=Image.Type.Simple;
            var b=bounds[i]; Pin(part,new Vector2(0,1),new Vector2(0,1),new Vector2(b.x,-b.y),new Vector2(b.width,b.height));
        }
    }
}

