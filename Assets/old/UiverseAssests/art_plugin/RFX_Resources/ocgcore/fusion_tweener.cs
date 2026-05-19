using UnityEngine;
using System.Collections;
using System;

public class fusion_tweener : MonoBehaviour {
    ParticleSystem[] systems;
	// Use this for initialization
	void Start () {
        systems = GetComponentsInChildren<ParticleSystem>();
        start_time = Program.TimePassed();
        ScaleShurikenSystems(scaleFactor);
	}
    int step = 1;
    float scaleFactor = 0.1f;
    int start_time = 0;
	// Update is called once per frame
    void Update()
    {
        if (Program.TimePassed() - start_time > 0)
        {
            step = 1;
        }
        if (Program.TimePassed() - start_time > 1500)
        {
            step = 2;
        }
        if (Program.TimePassed() - start_time > 3000)
        {
            step = 3;
        }
        if (Program.TimePassed() - start_time > 3500)
        {
            step = 4;
        }

        if (step == 1)
        {
            scaleFactor = 1 + Time.deltaTime*1.5f;
        }
        if (step == 2)
        {
            scaleFactor = 1f;
        }
        if (step == 3)
        {
            scaleFactor = 0.2f;
        }
        if (step == 4)
        {
            Destroy(gameObject);
            return;
        }
        ScaleShurikenSystems(scaleFactor);
    }

    void ScaleShurikenSystems(float factor)
    {
        foreach (ParticleSystem system in systems)
        {
            var main = system.main;
            main.startSpeed = ScaleCurve(main.startSpeed, factor);
            main.startSize = ScaleCurve(main.startSize, factor);
            main.gravityModifier = ScaleCurve(main.gravityModifier, factor);
        }
    }

    static ParticleSystem.MinMaxCurve ScaleCurve(ParticleSystem.MinMaxCurve curve, float factor)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                curve.constant *= factor;
                break;
            case ParticleSystemCurveMode.TwoConstants:
                curve.constantMin *= factor;
                curve.constantMax *= factor;
                break;
            case ParticleSystemCurveMode.Curve:
                curve.curveMultiplier *= factor;
                break;
            case ParticleSystemCurveMode.TwoCurves:
                curve.curveMultiplier *= factor;
                break;
        }
        return curve;
    }
}
