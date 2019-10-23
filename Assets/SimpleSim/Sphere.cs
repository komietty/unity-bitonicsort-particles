using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere : MonoBehaviour {

    Vector3 initPos;

    void Start() {
        initPos = transform.position;
    }

    void Update() {
        transform.position = initPos + new Vector3(Mathf.Cos(Time.time), 0, Mathf.Sin(Time.time * 2));
    }
}
