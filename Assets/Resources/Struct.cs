using UnityEngine;

public struct Particle {
    public Vector3 pos;
    public Vector3 vel;
    public Vector3 col;
    public Particle(Vector3 pos, Vector3 vel) {
        this.pos = pos;
        this.vel = vel;
        this.col = new Vector3(1, 1, 1);
    }
}


