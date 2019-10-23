using UnityEngine;
using System.IO;

namespace kmty.Util {
    public class RenderTextureUtil {
        static public void Build(ref RenderTexture tgt, int w, int h, int d = 0, RenderTextureFormat f = 0) {
            if (tgt != null) {
                Object.Destroy(tgt);
                tgt = null;
            }
            tgt = new RenderTexture(w, h, d, f);
            tgt.Create();
        }
        static public void Rebuild(RenderTexture src, ref RenderTexture tgt) {
            if (src != null && (tgt == null || tgt.width != src.width || tgt.height != src.height))
                Build(ref tgt, src.width, src.height, src.depth, src.format);
        }
        static public void Clear(RenderTexture tgt) {
            if (tgt == null) return;
            var store = RenderTexture.active;
            RenderTexture.active = tgt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = store;
        }
        static public void Destroy(RenderTexture tgt) {
            if (tgt != null) {
                Object.Destroy(tgt);
                tgt = null;
            }
        }
        static public void savePng(RenderTexture rt, string path) {
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            Object.Destroy(tex);
            File.WriteAllBytes(Application.dataPath + $"{path}.png", bytes);
        }
    }
}