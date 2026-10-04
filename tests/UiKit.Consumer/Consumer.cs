using RestlessQoL.Api;
using UnityEngine;

// Deliberately not an InternalsVisibleTo assembly. Compilation protects the
// supported API boundary; this consumer never imports Core's internal helpers.
public static class Consumer
{
    public static GameObject Build(Transform parent)
    {
        var button = UiKitApi.Button(parent,"Queue",new Rect(0,0,240,42),() => { });
        UiKitApi.ActionSurface(button.gameObject);
        UiKitApi.Tab(button,false);
        UiKitApi.Field(parent,"Search…",new Rect(0,50,240,40));
        var reader = UiKitApi.Reader(parent,"results",new Rect(0,100,240,300),64);
        UiKitApi.ResizeReader(reader,new Rect(0,100,280,300));
        UiKitApi.SizeReader(reader,600,true);
        UiKitApi.Quality(reader.content,3,100);
        var progress = UiKitApi.Progress(reader.content);
        progress.Set(new Rect(0,0,100,4),.2f,.6f,UiKitApi.Accent,UiKitApi.Muted);
        progress.Hide();
        var material = UiKitApi.Asset(UiKitAsset.Panel);
        return button.gameObject;
    }
}
