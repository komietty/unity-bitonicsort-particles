using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using kmty.Util;

public interface ISimulator {
    ComputeBuffer GetParticleBuffer();
    ComputeBuffer GetDensityBuffer();
    ComputeBuffer GetForceBuffer();
    int GetParticleNum();
}

public class SortingDebugger : MonoBehaviour {
    public enum BufferType { particle, density, force }
    public bool showDebugTex;
    public float rangeMapper;
    public BufferType type;
    public GameObject simulatorObj;
    public Material debugMat;
    public ComputeShader baker;
    const int BLOCK_SIZE = 256; //be sure buff size is multiple of block size
    RenderTexture outRT;
    ISimulator simulator;
    int clmCount;
    int rowCount;

    void Start() {
        simulator = simulatorObj.GetComponent<ISimulator>();
    }

    void Update () {
        var pBuff = simulator.GetParticleBuffer();
        var dBuff = simulator.GetDensityBuffer();
        var fBuff = simulator.GetForceBuffer();
        if (outRT == null) {
            clmCount = (int)SimpleSim.PARTICLE_NUM.NUM16K;
            rowCount = simulator.GetParticleNum() / (int)SimpleSim.PARTICLE_NUM.NUM16K;
            outRT = new RenderTexture(clmCount, rowCount, 0, RenderTextureFormat.ARGBFloat);
            outRT.enableRandomWrite = true;
            outRT.filterMode = FilterMode.Point;
            outRT.wrapMode = TextureWrapMode.Clamp;
            outRT.Create();
        }

        baker.SetFloat("_RangeMapper", rangeMapper);
        baker.SetInt("_ClmCount", clmCount);
        baker.SetInt("_RowCount", rowCount);

        if (pBuff != null && type == BufferType.particle) {
            var knl = baker.FindKernel("PBuffToRT");
            baker.SetBuffer(knl, "_ParticleBuff", pBuff);
            baker.SetTexture(knl, "_OutRT", outRT);
            baker.Dispatch(knl, clmCount / BLOCK_SIZE, rowCount, 1);
        }
        if (dBuff != null && type == BufferType.density) {
            var knl = baker.FindKernel("DBuffToRT");
            baker.SetBuffer(knl, "_DensityBuff", dBuff);
            baker.SetTexture(knl, "_OutRT", outRT);
            baker.Dispatch(knl, clmCount / BLOCK_SIZE, rowCount, 1);
        }
        if (fBuff != null && type == BufferType.force) {
            var knl = baker.FindKernel("FBuffToRT");
            baker.SetBuffer(knl, "_ForceBuff", fBuff);
            baker.SetTexture(knl, "_OutRT", outRT);
            baker.Dispatch(knl, clmCount / BLOCK_SIZE, rowCount, 1);
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst) {
        if (showDebugTex == false) {
            Graphics.Blit(src, dst);
            return;
        }
        debugMat.SetPass(0);
        debugMat.SetTexture("_OutRT", outRT);
        debugMat.SetInt("_ClmCount", clmCount);
        debugMat.SetInt("_RowCount", rowCount);
        Graphics.Blit(src, dst, debugMat);
    }

    void OnDestroy() {
        RenderTextureUtil.Destroy(outRT);
    }
}
