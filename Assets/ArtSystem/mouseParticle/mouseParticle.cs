using UnityEngine;
using System.Collections;

public class mouseParticle : MonoBehaviour {
    public Camera camera;
    public ParticleSystem e1;
    public ParticleSystem e2;
    public Transform trans;
    // Use this for initialization
    void Start () {
        if (camera != null)
        {
            camera.depth = 99999;
        }
        EnsureEmitters();
    }
    float time = 0;
	// Update is called once per frame
	void Update () {
        EnsureEmitters();
        if (camera == null || trans == null || e1 == null || e2 == null)
        {
            return;
        }

        Vector3 screenPoint = Input.mousePosition;
        screenPoint.z = 10;
        trans.position = camera.ScreenToWorldPoint(screenPoint);

        if (Input.GetMouseButton(0))    
        {
            if (Input.GetMouseButtonDown(0))
            {
                time = 0;
            }
            time += Time.deltaTime;
            if (time > 0.49)
            {
                time = 0.49f;
            }
            float emission = (0.5f - time) * 60f;
            SetEmissionRate(e1, emission);
            SetEmissionRate(e2, emission / 3f);
            if (!e1.isPlaying) e1.Play();
            if (!e2.isPlaying) e2.Play();
        }
        else
        {
            SetEmissionRate(e1, 0f);
            SetEmissionRate(e2, 0f);
            if (e1.isPlaying) e1.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (e2.isPlaying) e2.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void EnsureEmitters()
    {
        if (trans == null)
        {
            Transform root = transform.Find("p");
            if (root != null)
            {
                trans = root;
            }
        }

        if (trans == null)
        {
            return;
        }

        e1 = EnsureEmitter(trans.Find("square"), e1, 0.2f, 0.3f);
        e2 = EnsureEmitter(trans.Find("tri"), e2, 0.2f, 0.3f);
    }

    ParticleSystem EnsureEmitter(Transform target, ParticleSystem current, float minSize, float maxSize)
    {
        if (current != null)
        {
            return current;
        }
        if (target == null)
        {
            return null;
        }

        Renderer sourceRenderer = target.GetComponent<Renderer>();
        ParticleSystem system = target.GetComponent<ParticleSystem>();
        if (system == null)
        {
            system = target.gameObject.AddComponent<ParticleSystem>();
        }
        ParticleSystemRenderer systemRenderer = target.GetComponent<ParticleSystemRenderer>();
        if (systemRenderer == null)
        {
            systemRenderer = target.gameObject.AddComponent<ParticleSystemRenderer>();
        }
        if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
        {
            systemRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        }

        var main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startSpeed = 0f;
        main.maxParticles = 256;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0f;

        var velocityOverLifetime = system.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-3f, 3f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-3f, 3f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

        systemRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        return system;
    }

    void SetEmissionRate(ParticleSystem system, float emissionRate)
    {
        var emission = system.emission;
        emission.rateOverTime = Mathf.Max(0f, emissionRate);
    }
}
