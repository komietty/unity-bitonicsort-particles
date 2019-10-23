using UnityEngine;

namespace kmty.Util {
    public class ComputeShaderUtil {

        static public void Destroy(ComputeBuffer buff) {
            if (buff != null) {
                buff.Release();
                buff = null;
            }
        }
        static public void Swap(ref ComputeBuffer src, ref ComputeBuffer dst) {
            ComputeBuffer tmp = src;
            src = dst;
            dst = tmp;
        }
    }
}
