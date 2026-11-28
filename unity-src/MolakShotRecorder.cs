using UnityEngine;

/// <summary>디버그: 주기적으로 + 삽화 변경 시 스크린샷을 남긴다. MolakShotRecorder.Start(dir, interval)</summary>
public class MolakShotRecorder : MonoBehaviour
{
    static MolakShotRecorder I;
    public string dir; public float interval = 2.5f; public int max = 40;
    float t; int n; string lastKey = "";

    public static void Begin (string dir, float interval, int max)
    {
        if (I == null) { var go = new GameObject("MolakShotRecorder"); DontDestroyOnLoad(go); I = go.AddComponent<MolakShotRecorder>(); }
        I.dir = dir; I.interval = interval; I.max = max; I.n = 0; I.t = 0f;
        System.IO.Directory.CreateDirectory(dir);
        foreach (var f in System.IO.Directory.GetFiles(dir, "s_*.png")) System.IO.File.Delete(f);
    }
    public static void End () { if (I != null) Destroy(I.gameObject); I = null; }

    void Update ()
    {
        if (n >= max || string.IsNullOrEmpty(dir)) return;
        t += Time.deltaTime;
        var band = GameObject.Find("MolakArtBand");
        string key = "";
        if (band != null)
        {
            var s = band.transform.Find("Scene").GetComponent<SpriteRenderer>();
            var c = band.transform.Find("Char").GetComponent<SpriteRenderer>();
            key = (s.sprite ? s.sprite.texture.name : "-") + "|" + (c.sprite ? c.sprite.texture.name : "-");
        }
        bool changed = key != lastKey && key.Replace("-|", "").Replace("|-", "") != "";
        if (t >= interval || changed)
        {
            if (changed) { lastKey = key; t = -1.2f; /* 페이드 끝난 뒤 찍히도록 지연 */ return; }
            t = 0f;
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, "s_" + (n++).ToString("00") + "_" + key.Replace("|", "_") + ".png"));
        }
        if (key != lastKey) lastKey = key;
    }
}
