using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using NearestNeighbor;
using kmty.Util;

public class SimpleSim : MonoBehaviour, ISimulator
{
    public enum PARTICLE_NUM { NUM16K = 16384, NUM32K = 32768, NUM64K = 65536, NUM128K = 131072 }
    public PARTICLE_NUM mode;
    public KeyCode resetKey;
    public Vector3 gridDim = new Vector3(16, 16, 16);
    public ComputeShader sphSimulator;
    public Material rendererMat;
    public GameObject sphere;

    [Header("fluid variable")]
    public float smoothLen = 0.012f;
    public float pressureStiffness = 200f;
    public float restDensity = 1000f;
    public float particleMass = 0.0002f;
    public float restitution = 1;
    public float maxVelocity = 0.55f;
    public float viscosity = 0.1f;

    int numParticles;
    static readonly int SIM_BLOCK_SIZE = 256;
    static readonly Vector3 range = Vector3.one * 2; 
    static readonly float MAX_TIMESTEP = 0.001f;
    ComputeBuffer particleRO;
    ComputeBuffer particleRW;
    ComputeBuffer densityRO;
    ComputeBuffer densityRW;
    ComputeBuffer forceRO;
    ComputeBuffer forceRW;
    GridOptimizer3D<Particle> sorter;

    public ComputeBuffer GetParticleBuffer() => particleRO;
    public ComputeBuffer GetDensityBuffer() => densityRO;
    public ComputeBuffer GetForceBuffer() => forceRO;
    public int GetParticleNum() => numParticles;

    void Start() {
        numParticles = (int)mode;
        sorter = new GridOptimizer3D<Particle>(numParticles, range, gridDim);
        InitBuffer();
        InitParticles();
    }
    void Update() {
        sorter.GridSort(ref particleRO);
        Simulate();
        if (Input.GetKeyDown(resetKey)) Reset();
    }

    void Reset() {
        InitParticles();
    }
 
    void Simulate() {
        // set params 
        sphSimulator.SetInt("_NumParticles", numParticles);
        sphSimulator.SetFloat("_GridH", sorter.GetGridH());
        sphSimulator.SetVector("_GridDim", gridDim);
        sphSimulator.SetFloat("_TimeStep", Mathf.Min(MAX_TIMESTEP, Time.deltaTime));
        sphSimulator.SetFloat("_Smoothlen", smoothLen);
        sphSimulator.SetFloat("_PressureStiffness", pressureStiffness);
        sphSimulator.SetFloat("_RestDensity", restDensity);
        sphSimulator.SetFloat("_DensityCoef", particleMass * 315.0f / (64.0f * Mathf.PI * Mathf.Pow(smoothLen, 9.0f)));
        sphSimulator.SetFloat("_GradPressureCoef", particleMass * -45.0f / (Mathf.PI * Mathf.Pow(smoothLen, 6.0f)));
        sphSimulator.SetFloat("_LapViscosityCoef", particleMass * viscosity * 45.0f / (Mathf.PI * Mathf.Pow(smoothLen, 6.0f)));
        sphSimulator.SetFloat("_Restitution", restitution);
        sphSimulator.SetFloat("_MaxVelocity", maxVelocity);
        sphSimulator.SetFloat("_Time", Time.time);
        sphSimulator.SetFloat("_SphereRad", sphere.transform.localScale.x / 2);
        sphSimulator.SetVector("_SpherePos", sphere.transform.position);

        int knl = 0;
        //calc density
        knl = sphSimulator.FindKernel("SimDensity");
        sphSimulator.SetBuffer(knl, "_ParticlesBufferRO", particleRO);
        sphSimulator.SetBuffer(knl, "_DensityBufferRW", densityRW);
        sphSimulator.SetBuffer(knl, "_GridIndicesBufferRead", sorter.GetGridIndicesBuffer());
        sphSimulator.Dispatch(knl, numParticles / SIM_BLOCK_SIZE, 1, 1);
        ComputeShaderUtil.Swap(ref densityRO, ref densityRW);
       
        //calc force
        knl = sphSimulator.FindKernel("SimForce");
        sphSimulator.SetBuffer(knl, "_ParticlesBufferRO", particleRO);
        sphSimulator.SetBuffer(knl, "_DensityBufferRO", densityRO);
        sphSimulator.SetBuffer(knl, "_ForceBufferRW", forceRW);
        sphSimulator.SetBuffer(knl, "_GridIndicesBufferRead", sorter.GetGridIndicesBuffer());
        sphSimulator.Dispatch(knl, numParticles / SIM_BLOCK_SIZE, 1, 1);
        ComputeShaderUtil.Swap(ref forceRO, ref forceRW);

        //calc integration
        knl = sphSimulator.FindKernel("SimIntegrate");
        sphSimulator.SetBuffer(knl, "_ParticlesBufferRO", particleRO);
        sphSimulator.SetBuffer(knl, "_ParticlesBufferRW", particleRW);
        sphSimulator.SetBuffer(knl, "_ForceBufferRO", forceRO);
        sphSimulator.SetBuffer(knl, "_GridIndicesBufferRead", sorter.GetGridIndicesBuffer());
        sphSimulator.Dispatch(knl, numParticles / SIM_BLOCK_SIZE, 1, 1);
        ComputeShaderUtil.Swap(ref particleRO, ref particleRW);
    }
    void OnRenderObject() {
        rendererMat.SetPass(0);
        rendererMat.SetBuffer("_Particles", particleRO);
        Graphics.DrawProceduralNow(MeshTopology.Points, numParticles);
    }

    void OnDestroy() {
        ReleaseBuffer();
        sorter.Release();
    }

    void InitBuffer() {
        particleRO = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(Particle)));
        particleRW = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(Particle)));
        densityRO = new ComputeBuffer(numParticles, sizeof(float));
        densityRW = new ComputeBuffer(numParticles, sizeof(float));
        forceRO = new ComputeBuffer(numParticles, 4 * sizeof(float));
        forceRW = new ComputeBuffer(numParticles, 4 * sizeof(float));
    }

    void InitParticles() {
        Particle[] particles = new Particle[numParticles];
        for (int i = 0; i < numParticles; i++)
            particles[i] = new Particle(
                new Vector3(Random.Range(0.4f, 0.7f), Random.Range(1.7f, 1.9f), Random.Range(0.4f, 0.7f)),
                new Vector3(900000, 0, 900000)
                );
        particleRO.SetData(particles);
    }

    void ReleaseBuffer() {
        ComputeShaderUtil.Destroy(particleRO);
        ComputeShaderUtil.Destroy(particleRW);
        ComputeShaderUtil.Destroy(densityRO);
        ComputeShaderUtil.Destroy(densityRW);
        ComputeShaderUtil.Destroy(forceRO);
        ComputeShaderUtil.Destroy(forceRW);
    }
}
