using UnityEngine;
using System.Collections;

public class partical_scaler : MonoBehaviour {
    public float scale = 10;
	// Use this for initialization
	void Start () {
        var particles = gameObject.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem p in particles)
        {
            p.startSize *= scale;
        }
    }

    public void eltersGo()
    {
        var particles = gameObject.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var p in particles)
        {
            var main = p.main;
            main.startSize = ScaleCurve(main.startSize, scale);

            var shape = p.shape;
            if (shape.enabled)
            {
                shape.radius *= scale;
                shape.scale *= scale;
            }
        }
    }

    // Update is called once per frame
	void Update () {
	
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
